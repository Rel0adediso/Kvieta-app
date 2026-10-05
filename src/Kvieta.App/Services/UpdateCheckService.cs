using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

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

    private const string GitHubReleasesApiUrl = "https://api.github.com/repos/Rel0adediso/Kvieta-app/releases/latest";

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

            string? setupDownloadUrl = release.Assets?
                .FirstOrDefault(a => a.Name is not null && (a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || a.Name.EndsWith(".msi", StringComparison.OrdinalIgnoreCase)))
                ?.BrowserDownloadUrl ?? release.HtmlUrl;

            return new UpdateCheckResult(
                IsUpdateAvailable: isNewer,
                CurrentVersion: currentVersion,
                LatestVersion: CleanVersionString(remoteTag),
                ReleaseName: release.Name ?? remoteTag,
                ReleaseUrl: release.HtmlUrl ?? "https://github.com/Rel0adediso/Kvieta-app/releases/latest",
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

    public static bool IsRemoteVersionNewer(string currentVersion, string remoteTag)
    {
        if (string.IsNullOrWhiteSpace(remoteTag))
        {
            return false;
        }

        string curClean = CleanVersionString(currentVersion);
        string remClean = CleanVersionString(remoteTag);

        if (string.Equals(curClean, remClean, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (Version.TryParse(curClean, out Version? curVer) && Version.TryParse(remClean, out Version? remVer))
        {
            return remVer > curVer;
        }

        double curNum = ExtractNumericValue(curClean);
        double remNum = ExtractNumericValue(remClean);
        return remNum > curNum;
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
