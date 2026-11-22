using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using Kvieta.Core.Services;

namespace Kvieta.App.Services;

public sealed record StableUpdatePackage(string Product, string Architecture, string Version, string Label,
    long Size, string Sha256, string[] Blocks, int[] Sizes, string PackageKind, int GuardianRevision)
{
    private const long MaximumSize = 256L * 1024 * 1024;
    private const string Releases = "https://github.com/Rel0adediso/Kvieta-app/releases";
    public const string FeedUrl = "https://raw.githubusercontent.com/Rel0adediso/Kvieta-app/stable-feed/stable.json";
    public string ReleaseUrl => $"{Releases}/tag/stable-{Version}";
    public string DownloadUrl => $"{Releases}/download/stable-{Version}/Kvieta-Setup-{Label}.exe";
    private string CacheFileName => StableSetupCache.FileName(Version);
    private sealed record SignedFeed(string Payload, string Signature);
    private byte[]? VerifiedEnvelope { get; init; }

    public static StableUpdatePackage Verify(byte[] envelope, string publicKey)
    {
        var feed = JsonSerializer.Deserialize<SignedFeed>(envelope) ?? throw new InvalidDataException("Missing update feed.");
        byte[] payload = Convert.FromBase64String(feed.Payload);
        using RSA rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(publicKey), out _);
        if (rsa.KeySize < 2048 || !rsa.VerifyData(payload, Convert.FromBase64String(feed.Signature),
                HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
            throw new InvalidDataException("Invalid stable feed signature.");
        var package = JsonSerializer.Deserialize<StableUpdatePackage>(payload) ?? throw new InvalidDataException("Missing package.");
        static bool IsHash(string? s) => s is { Length: 64 } && s.All(Uri.IsHexDigit);
        if (package.Product != "Kvieta" || package.Architecture != "win-x64" || package.PackageKind != "setup" ||
            package.GuardianRevision < 1 ||
            !System.Version.TryParse(package.Version, out var version) || version.ToString(3) != package.Version ||
            !UpdateCheckService.IsStableReleaseTag(package.Label) || !package.Label.StartsWith('v') ||
            package.Size is <= 0 or > MaximumSize ||
            !IsHash(package.Sha256) || package.Sizes is not { Length: > 0 } ||
            package.Blocks?.Length != package.Sizes.Length ||
            package.Sizes.Any(n => n is <= 0 or > StableChunker.Maximum) ||
            package.Sizes.SkipLast(1).Any(n => n < StableChunker.Minimum) ||
            package.Sizes.Sum(n => (long)n) != package.Size ||
            !package.Blocks.All(IsHash)) throw new InvalidDataException("Invalid stable package metadata.");
        return package with { VerifiedEnvelope = envelope.ToArray() };
    }

    public bool Matches(Stream stream) => stream.Length == Size &&
        Convert.ToHexString(SHA256.HashData(stream)).Equals(Sha256, StringComparison.OrdinalIgnoreCase);

    public bool CanApplySetup(FileStream stream, string publicKey, Version installedVersion, int installedGuardianRevision)
    {
        if (VerifiedEnvelope == null) return false;
        var verified = Verify(VerifiedEnvelope, publicKey);
        return verified.GuardianRevision == installedGuardianRevision &&
            string.Equals(Path.GetFileName(stream.Name), verified.CacheFileName, StringComparison.OrdinalIgnoreCase) &&
            System.Version.Parse(verified.Version) > installedVersion && verified.Matches(stream);
    }

    public async Task<string> StageAsync(HttpClient client, string directory, IProgress<double>? progress, CancellationToken token)
    {
        Directory.CreateDirectory(directory);
        string target = Path.Combine(directory, CacheFileName);
        if (File.Exists(target))
        {
            using var cached = File.OpenRead(target);
            if (Matches(cached))
            {
                await RememberVerifiedPackageAsync(target, token);
                progress?.Report(100);
                return target;
            }
        }
        string temp = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".partial");
        try
        {
            await using (var output = new FileStream(temp, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
            {
                var reused = new bool[Blocks.Length];
                var offsets = new long[Sizes.Length + 1];
                for (int i = 0; i < Sizes.Length; i++) offsets[i + 1] = offsets[i] + Sizes[i];
                var targetVersion = System.Version.Parse(Version);
                string? previous = Directory.EnumerateFiles(directory, StableSetupCache.FilePattern)
                    .Select(path => (Path: path, Version: StableSetupCache.ReadVersion(path)))
                    .Where(candidate => candidate.Version is not null && candidate.Version < targetVersion)
                    .OrderByDescending(candidate => candidate.Version).Select(candidate => candidate.Path).FirstOrDefault();
                if (previous != null)
                {
                    try { await CopyMatchingBlocksAsync(previous, output, reused, offsets, token); }
                    catch (IOException) { Array.Clear(reused); }
                    catch (UnauthorizedAccessException) { Array.Clear(reused); }
                }
                bool ready = false;
                if (reused.Any(block => block))
                {
                    try
                    {
                        await DownloadMissingBlocksAsync(client, output, reused, offsets, progress, token);
                        output.Position = 0;
                        ready = Matches(output);
                    }
                    catch (Exception exception) when (exception is InvalidDataException or EndOfStreamException or
                        HttpRequestException { StatusCode: HttpStatusCode.RequestedRangeNotSatisfiable })
                    {
                        // Restart once with the complete package if the range response cannot be used.
                    }
                }
                if (!ready)
                {
                    output.SetLength(0);
                    output.Position = 0;
                    using var full = await client.GetAsync(DownloadUrl, HttpCompletionOption.ResponseHeadersRead, token);
                    full.EnsureSuccessStatusCode();
                    if (full.StatusCode != HttpStatusCode.OK) throw new InvalidDataException("Expected a complete update.");
                    await CopyResponseAsync(full, output, Size, progress, token);
                    output.Position = 0;
                    if (!Matches(output)) throw new InvalidDataException("Stable package checksum mismatch.");
                }
            }
            // Preserve Windows' Internet Zone checks when the user opens the installer.
            await File.WriteAllTextAsync(temp + ":Zone.Identifier", "[ZoneTransfer]\r\nZoneId=3\r\n", token);
            File.Move(temp, target, overwrite: true);
            await RememberVerifiedPackageAsync(target, token);
            progress?.Report(100);
            return target;
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    private async Task RememberVerifiedPackageAsync(string target, CancellationToken token)
    {
        if (VerifiedEnvelope == null) return;
        string metadataPath = target + ".json";
        string temporary = metadataPath + "." + Guid.NewGuid().ToString("N") + ".partial";
        try
        {
            await File.WriteAllBytesAsync(temporary, VerifiedEnvelope, token);
            File.Move(temporary, metadataPath, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    public static async Task<(StableUpdatePackage Package, string InstallerPath)?> FindPreparedAsync(
        string directory, string publicKey, Version installedVersion, CancellationToken token = default)
    {
        if (!Directory.Exists(directory)) return null;
        (StableUpdatePackage Package, string InstallerPath)? latest = null;
        foreach (string metadata in Directory.EnumerateFiles(directory, StableSetupCache.FilePattern + ".json"))
        {
            token.ThrowIfCancellationRequested();
            try
            {
                if (new FileInfo(metadata).Length > 1024 * 1024) continue;
                var package = Verify(await File.ReadAllBytesAsync(metadata, token), publicKey);
                Version version = System.Version.Parse(package.Version);
                if (version <= installedVersion || (latest != null && version <= System.Version.Parse(latest.Value.Package.Version))) continue;
                string installer = Path.Combine(directory, package.CacheFileName);
                using var stream = File.OpenRead(installer);
                if (package.Matches(stream)) latest = (package, installer);
            }
            catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or FormatException or CryptographicException or ArgumentException)
            {
                // Corrupt and incomplete cached downloads are never presented as ready.
            }
        }
        return latest;
    }

    private async Task DownloadMissingBlocksAsync(HttpClient client, Stream output, bool[] reused, long[] offsets,
        IProgress<double>? progress, CancellationToken token)
    {
        for (int i = 0; i < reused.Length;)
        {
            if (reused[i]) { i++; continue; }
            int first = i++;
            while (i < reused.Length && !reused[i]) i++;
            long start = offsets[first];
            long end = offsets[i] - 1;
            using var request = new HttpRequestMessage(HttpMethod.Get, DownloadUrl);
            request.Headers.Range = new RangeHeaderValue(start, end);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();
            if (response.StatusCode == HttpStatusCode.OK)
            {
                output.Position = 0;
                await CopyResponseAsync(response, output, Size, progress, token);
                return;
            }
            if (response.StatusCode != HttpStatusCode.PartialContent ||
                response.Content.Headers.ContentRange is not { Unit: "bytes" } range ||
                range.From != start || range.To != end || range.Length != Size)
                throw new InvalidDataException("Unexpected update range.");
            output.Position = start;
            await CopyResponseAsync(response, output, end + 1, progress, token);
        }
    }

    private async Task CopyResponseAsync(HttpResponseMessage response, Stream target, long endOffset,
        IProgress<double>? progress, CancellationToken token)
    {
        using var source = await response.Content.ReadAsStreamAsync(token);
        byte[] buffer = new byte[81920];
        while (target.Position < endOffset)
        {
            int read = await source.ReadAsync(buffer.AsMemory(0, (int)Math.Min(buffer.Length, endOffset - target.Position)), token);
            if (read == 0) throw new EndOfStreamException("Incomplete update.");
            await target.WriteAsync(buffer.AsMemory(0, read), token);
            progress?.Report(100.0 * target.Position / Size);
        }
    }

    private async Task CopyMatchingBlocksAsync(string path, Stream output, bool[] reused, long[] offsets, CancellationToken token)
    {
        using var source = File.OpenRead(path);
        if (source.Length > MaximumSize) return;
        var wanted = Blocks.Select((hash, index) => (hash, index)).ToLookup(b => b.hash, b => b.index, StringComparer.OrdinalIgnoreCase);
        byte[] buffer = new byte[StableChunker.Maximum];
        int count;
        while ((count = StableChunker.ReadChunk(source, buffer)) > 0)
        {
            token.ThrowIfCancellationRequested();
            string hash = Convert.ToHexString(SHA256.HashData(buffer.AsSpan(0, count)));
            foreach (int i in wanted[hash])
            {
                if (reused[i] || Sizes[i] != count) continue;
                output.Position = offsets[i];
                await output.WriteAsync(buffer.AsMemory(0, count), token);
                reused[i] = true;
            }
        }
    }
}
