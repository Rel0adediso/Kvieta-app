using System.IO;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Security.Cryptography.X509Certificates;
using Kvieta.Core.Services;

namespace Kvieta.App.Services;

public sealed record UpdateCheckResult(
    bool IsUpdateAvailable,
    string CurrentVersion,
    string? LatestVersion,
    string? ReleaseName,
    string? ReleaseUrl,
    string? DownloadUrl,
    string? ReleaseNotes,
    DateTimeOffset? PublishedAt,
    bool HasError = false,
    string? ErrorMessage = null
);

public static class UpdateCheckService
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private const string GitHubRepositoryApiUrl = "https://api.github.com/repos/Rel0adediso/Kvieta-app";
    private const string GitHubReleasesApiUrl = GitHubRepositoryApiUrl + "/releases/latest";

    static UpdateCheckService()
    {
        HttpClient.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("Kvieta-App", BuildInfo.Version.Length > 0 ? BuildInfo.Version : "1.0"));
        HttpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
    }

    public static async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        string currentVersion = BuildInfo.Version;
        try
        {
            using HttpResponseMessage response = await HttpClient.GetAsync(GitHubReleasesApiUrl, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return new UpdateCheckResult(
                    IsUpdateAvailable: false,
                    CurrentVersion: currentVersion,
                    LatestVersion: null,
                    ReleaseName: null,
                    ReleaseUrl: null,
                    DownloadUrl: null,
                    ReleaseNotes: null,
                    PublishedAt: null,
                    HasError: true,
                    ErrorMessage: $"HTTP {(int)response.StatusCode}");
            }

            string json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            GitHubReleaseDto? release = JsonSerializer.Deserialize<GitHubReleaseDto>(json);
            if (release is null || string.IsNullOrWhiteSpace(release.TagName))
            {
                return new UpdateCheckResult(
                    IsUpdateAvailable: false,
                    CurrentVersion: currentVersion,
                    LatestVersion: null,
                    ReleaseName: null,
                    ReleaseUrl: null,
                    DownloadUrl: null,
                    ReleaseNotes: null,
                    PublishedAt: null);
            }

            string remoteTag = release.TagName;
            bool isNewer = IsRemoteVersionNewer(currentVersion, remoteTag);
            if (isNewer && (!IsStableReleaseTag(remoteTag) ||
                !await IsReleaseOnStableAsync(remoteTag, cancellationToken).ConfigureAwait(false)))
            {
                return new UpdateCheckResult(false, currentVersion, null, null, null, null, null, null,
                    HasError: true, ErrorMessage: "The published release could not be verified against the stable branch.");
            }

            string? setupDownloadUrl = release.Assets?
                .FirstOrDefault(a => a.Name is not null && (a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || a.Name.EndsWith(".msi", StringComparison.OrdinalIgnoreCase)))
                ?.BrowserDownloadUrl ?? release.HtmlUrl;

            // Unsigned community releases use an explicit browser download, never an unverified auto-install.
            string? signer = ProtectionServiceManager.RegisteredSignerThumbprint;
            if (string.IsNullOrWhiteSpace(signer) || !Regex.IsMatch(signer, "^[a-fA-F0-9]{40}$"))
                setupDownloadUrl = release.HtmlUrl;
            if (!IsOfficialReleaseUrl(setupDownloadUrl)) setupDownloadUrl = "https://github.com/Rel0adediso/Kvieta-app/releases/latest";
            return new UpdateCheckResult(
                IsUpdateAvailable: isNewer,
                CurrentVersion: currentVersion,
                LatestVersion: CleanVersionString(remoteTag),
                ReleaseName: release.Name ?? remoteTag,
                ReleaseUrl: IsOfficialReleaseUrl(release.HtmlUrl) ? release.HtmlUrl : "https://github.com/Rel0adediso/Kvieta-app/releases/latest",
                DownloadUrl: setupDownloadUrl,
                ReleaseNotes: release.Body,
                PublishedAt: release.PublishedAt);
        }
        catch (Exception ex)
        {
            return new UpdateCheckResult(
                IsUpdateAvailable: false,
                CurrentVersion: currentVersion,
                LatestVersion: null,
                ReleaseName: null,
                ReleaseUrl: null,
                DownloadUrl: null,
                ReleaseNotes: null,
                PublishedAt: null,
                HasError: true,
                ErrorMessage: ex.Message);
        }
    }

    public static bool IsStableReleaseTag(string? tag) => tag is not null &&
        Regex.IsMatch(tag, @"^v?[0-9]+\.[0-9]+\.[0-9]+\z", RegexOptions.CultureInvariant);

    public static bool IsStableBranchComparison(string? status, int behindBy) =>
        behindBy == 0 && status is "ahead" or "identical";

    private static async Task<bool> IsReleaseOnStableAsync(string tag, CancellationToken cancellationToken)
    {
        // Resolve the actual tag and branch separately. The release's target_commitish
        // is not necessarily the commit to which an existing tag points.
        Task<string?> tagCommit = ReadCommitShaAsync("tags/" + Uri.EscapeDataString(tag), cancellationToken);
        Task<string?> stableCommit = ReadCommitShaAsync("heads/stable", cancellationToken);
        await Task.WhenAll(tagCommit, stableCommit).ConfigureAwait(false);
        string? releaseSha = await tagCommit.ConfigureAwait(false);
        string? stableSha = await stableCommit.ConfigureAwait(false);
        if (releaseSha is null || stableSha is null) return false;
        if (string.Equals(releaseSha, stableSha, StringComparison.OrdinalIgnoreCase)) return true;

        using HttpResponseMessage response = await HttpClient.GetAsync(
            $"{GitHubRepositoryApiUrl}/compare/{releaseSha}...{stableSha}?per_page=1", cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return false;
        using JsonDocument comparison = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
        JsonElement root = comparison.RootElement;
        return root.TryGetProperty("status", out JsonElement status) &&
            root.TryGetProperty("behind_by", out JsonElement behind) && behind.TryGetInt32(out int behindBy) &&
            IsStableBranchComparison(status.GetString(), behindBy);
    }

    private static async Task<string?> ReadCommitShaAsync(string reference, CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, $"{GitHubRepositoryApiUrl}/commits/{reference}");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.sha"));
        using HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        string sha = (await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false)).Trim();
        return Regex.IsMatch(sha, "^[0-9a-fA-F]{40}$") ? sha : null;
    }

    public static async Task<string?> DownloadUpdateAsync(
        string downloadUrl,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsOfficialDownloadUrl(downloadUrl) || !Uri.TryCreate(downloadUrl, UriKind.Absolute, out Uri? uri))
        {
            return null;
        }

        string updatesDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Kvieta",
            "Updates");
        Directory.CreateDirectory(updatesDirectory);

        string rawFileName = Path.GetFileName(uri.LocalPath);
        if (string.IsNullOrWhiteSpace(rawFileName) ||
            (!rawFileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) &&
             !rawFileName.EndsWith(".msi", StringComparison.OrdinalIgnoreCase)))
        {
            rawFileName = "Kvieta-Setup.exe";
        }

        string targetPath = Path.Combine(updatesDirectory, rawFileName);
        string tempPath = targetPath + ".downloading";

        try
        {
            using HttpResponseMessage response = await HttpClient.GetAsync(
                downloadUrl,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();
            long? totalBytes = response.Content.Headers.ContentLength;
            const long maximumBytes = 256L * 1024 * 1024;
            if (totalBytes > maximumBytes) throw new InvalidDataException("Update exceeds the download size limit.");

            using (Stream sourceStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
            using (FileStream targetStream = new(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                byte[] buffer = new byte[81920];
                long totalRead = 0;
                int bytesRead;

                while ((bytesRead = await sourceStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    await targetStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                    totalRead += bytesRead;
                    if (totalRead > maximumBytes) throw new InvalidDataException("Update exceeds the download size limit.");

                    if (totalBytes is > 0)
                    {
                        progress?.Report((double)totalRead / totalBytes.Value * 100.0);
                    }
                }
            }

            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }
            File.Move(tempPath, targetPath);

            // Authenticode verification
            if (!BuildInfo.IsDevelopmentBuild)
            {
                if (!IsExpectedPublisher(targetPath))
                {
                    try { File.Delete(targetPath); } catch { }
                    throw new InvalidOperationException("The update publisher could not be verified against the installed certificate. Check the official release notes for publisher or certificate changes before proceeding.");
                }
            }

            return targetPath;
        }
        catch
        {
            try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
            throw;
        }
    }

    public static bool ApplyUpdate(string installerPath)
    {
        if (string.IsNullOrWhiteSpace(installerPath) || !File.Exists(installerPath) || !IsExpectedPublisher(installerPath))
        {
            return false;
        }

        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = installerPath,
                UseShellExecute = true
            };

            if (installerPath.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
            {
                startInfo.FileName = "msiexec.exe";
                startInfo.Arguments = $"/i \"{installerPath}\" /passive";
            }
            else
            {
                startInfo.Arguments = string.Empty;
            }

            System.Diagnostics.Process.Start(startInfo);
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                System.Windows.Application.Current.Shutdown(0);
            });
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsRemoteVersionNewer(string currentVersion, string remoteTag) => ReleaseVersion.IsNewer(currentVersion, remoteTag);

    public static bool IsOfficialReleaseUrl(string? value) => Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) &&
        uri.Scheme == "https" && uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) && uri.IsDefaultPort &&
        string.IsNullOrEmpty(uri.UserInfo) && uri.AbsolutePath.StartsWith("/Rel0adediso/Kvieta-app/releases/", StringComparison.OrdinalIgnoreCase);

    public static bool IsOfficialDownloadUrl(string? value) => IsOfficialReleaseUrl(value) &&
        new Uri(value!).AbsolutePath.StartsWith("/Rel0adediso/Kvieta-app/releases/download/", StringComparison.OrdinalIgnoreCase) &&
        (value!.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || value.EndsWith(".msi", StringComparison.OrdinalIgnoreCase));

    private static bool IsExpectedPublisher(string path)
    {
        string? pinned = ProtectionServiceManager.RegisteredSignerThumbprint;
        if (string.IsNullOrWhiteSpace(pinned) || !Regex.IsMatch(pinned, "^[a-fA-F0-9]{40}$") || !AuthenticodeTrustVerifier.IsTrusted(path)) return false;
        try
        {
#pragma warning disable SYSLIB0057
            using X509Certificate2 signer = new(X509Certificate.CreateFromSignedFile(path));
#pragma warning restore SYSLIB0057
            return pinned.Equals(signer.Thumbprint, StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    private static string CleanVersionString(string input)
    {
        string s = input.Trim();
        s = Regex.Replace(s, @"^kvieta[-_.]?", "", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"^alpha[-_.]?", "", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"^v", "", RegexOptions.IgnoreCase);
        return s.Trim();
    }

    private static double ExtractNumericValue(string input)
    {
        Match match = Regex.Match(input, @"\d+(\.\d+)?");
        if (match.Success && double.TryParse(match.Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double val))
        {
            return val;
        }
        return 0;
    }

    private sealed class GitHubReleaseDto
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }

        [JsonPropertyName("published_at")]
        public DateTimeOffset? PublishedAt { get; set; }

        [JsonPropertyName("body")]
        public string? Body { get; set; }

        [JsonPropertyName("assets")]
        public List<GitHubReleaseAssetDto>? Assets { get; set; }
    }

    private sealed class GitHubReleaseAssetDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("browser_download_url")]
        public string? BrowserDownloadUrl { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }
    }
}
