using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Velopack;
using Velopack.Logging;
using Velopack.Sources;

namespace Kvieta.Desktop;

public sealed class SignedDesktopSource(string publicKey) : IUpdateSource
{
    public const string PackageId = "Kvieta.Desktop";
    public const string Channel = "desktop-stable";
    private const string ReleaseRoot = "https://github.com/Rel0adediso/Kvieta-app/releases/download/";
    private const string FeedUrl = "https://raw.githubusercontent.com/Rel0adediso/Kvieta-app/desktop-stable-feed/desktop.json";
    private const long MaxPackageBytes = 256L * 1024 * 1024;
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(15) };
    private VerifiedDesktopFeed? _verifiedFeed;
    public byte[]? VerifiedEnvelope { get; private set; }

    private sealed record SignedFeed(string Payload, string Signature);
    public sealed record ContentEntry(string Name, long Length, string Sha256);
    public sealed record PackageContents(string FileName, ContentEntry[] Entries);
    public sealed record VerifiedDesktopFeed(VelopackAssetFeed Feed, PackageContents[] Contents);
    private sealed record ContentCatalog(PackageContents[] Contents);

    public static VerifiedDesktopFeed Verify(byte[] envelope, string publicKey)
    {
        var signed = JsonSerializer.Deserialize<SignedFeed>(envelope) ?? throw new InvalidDataException("Missing desktop feed.");
        byte[] payload = Convert.FromBase64String(signed.Payload);
        using RSA rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(publicKey), out _);
        if (rsa.KeySize < 2048 || !rsa.VerifyData(payload, Convert.FromBase64String(signed.Signature),
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
            throw new InvalidDataException("Invalid desktop feed signature.");

        VelopackAssetFeed feed = VelopackAssetFeed.FromJson(Encoding.UTF8.GetString(payload));
        if (feed.Assets.Any(asset => asset.PackageId != PackageId ||
            asset.Type is not (VelopackAssetType.Full or VelopackAssetType.Delta) ||
            !Regex.IsMatch(asset.Version.ToString(), @"^[0-9]+\.[0-9]+\.[0-9]+$") ||
            !Regex.IsMatch(asset.FileName, @"^[A-Za-z0-9][A-Za-z0-9._-]{0,200}\.nupkg$") ||
            asset.Size is <= 0 or > MaxPackageBytes ||
            asset.SHA256 is not { Length: 64 } || !asset.SHA256.All(Uri.IsHexDigit)))
            throw new InvalidDataException("Desktop feed contains an invalid package.");
        var catalog = JsonSerializer.Deserialize<ContentCatalog>(payload)
            ?? throw new InvalidDataException("Missing signed desktop package contents.");
        if (catalog.Contents == null || catalog.Contents.Length != feed.Assets.Count(asset => asset.Type == VelopackAssetType.Full) ||
            feed.Assets.Where(asset => asset.Type == VelopackAssetType.Full).Any(asset =>
                catalog.Contents.Count(item => item.FileName == asset.FileName) != 1) ||
            catalog.Contents.Any(item => item.Entries is not { Length: > 0 } || item.Entries.Length > 10000 ||
                item.Entries.Select(entry => entry.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != item.Entries.Length ||
                item.Entries.Any(entry => !IsSafeEntry(entry.Name) || entry.Length < 0 || entry.Length > MaxPackageBytes ||
                    entry.Sha256 is not { Length: 64 } || !entry.Sha256.All(Uri.IsHexDigit)) ||
                item.Entries.Sum(entry => entry.Length) > MaxPackageBytes * 2))
            throw new InvalidDataException("Invalid signed desktop package contents.");
        return new(feed, catalog.Contents);
    }

    public async Task<VelopackAssetFeed> GetReleaseFeed(IVelopackLogger logger, string? appId, string channel,
        Guid? stagingId = null, VelopackAsset? latestLocalRelease = null)
    {
        if (appId != PackageId || channel != Channel) throw new InvalidDataException("Unexpected desktop update channel.");
        using var response = await Client.GetAsync(FeedUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await response.Content.LoadIntoBufferAsync(1024 * 1024, timeout.Token);
        byte[] envelope = await response.Content.ReadAsByteArrayAsync(timeout.Token);
        _verifiedFeed = Verify(envelope, publicKey);
        VerifiedEnvelope = envelope;
        return _verifiedFeed.Feed;
    }

    public async Task DownloadReleaseEntry(IVelopackLogger logger, VelopackAsset releaseEntry, string localFile,
        Action<int> progress, CancellationToken cancelToken = default)
    {
        VelopackAsset asset = _verifiedFeed?.Feed.Assets.SingleOrDefault(item => SameAsset(item, releaseEntry))
            ?? throw new InvalidDataException("Package is absent from the signed desktop feed.");
        var source = new SimpleWebSource(ReleaseRoot + "desktop-" + asset.Version + "/");
        await source.DownloadReleaseEntry(logger, asset, localFile, progress, cancelToken);
        if (!MatchesFile(asset, localFile)) throw new InvalidDataException("Desktop package checksum did not match.");
    }

    public static bool SamePackageIdentity(VelopackAsset left, VelopackAsset right) =>
        left.PackageId == right.PackageId && left.Version == right.Version && left.Type == right.Type &&
        left.FileName == right.FileName;

    public static bool SameAsset(VelopackAsset left, VelopackAsset right) =>
        SamePackageIdentity(left, right) && left.Size == right.Size &&
        string.Equals(left.SHA256, right.SHA256, StringComparison.OrdinalIgnoreCase);

    public static bool MatchesFile(VelopackAsset asset, string path)
    {
        using var file = File.OpenRead(path);
        return file.Length == asset.Size &&
            Convert.ToHexString(SHA256.HashData(file)).Equals(asset.SHA256, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSafeEntry(string name) =>
        !string.IsNullOrWhiteSpace(name) && !name.Contains('\\') && !name.Contains(':') &&
        name.Split('/').All(part => part.Length > 0 && part is not "." and not "..");

    public static bool MatchesPrepared(VerifiedDesktopFeed feed, VelopackAsset asset, string path)
    {
        if (MatchesFile(asset, path)) return true;
        // Native delta reconstruction may change ZIP compression metadata. Authenticate every entry instead.
        var expected = feed.Contents.SingleOrDefault(package => package.FileName == asset.FileName);
        if (expected == null || new FileInfo(path).Length > MaxPackageBytes) return false;
        using var archive = ZipFile.OpenRead(path);
        if (archive.Entries.Count != expected.Entries.Length) return false;
        var entries = expected.Entries.ToDictionary(entry => entry.Name, StringComparer.Ordinal);
        foreach (var actual in archive.Entries)
        {
            if (!entries.Remove(actual.FullName, out var entry) || actual.Length != entry.Length ||
                (actual.FullName.EndsWith('/') || ((actual.ExternalAttributes >> 16) & 0xF000) == 0xA000)) return false;
            using var stream = actual.Open();
            if (!Convert.ToHexString(SHA256.HashData(stream)).Equals(entry.Sha256, StringComparison.OrdinalIgnoreCase)) return false;
        }
        return entries.Count == 0;
    }
}
