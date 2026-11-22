using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.IO.Compression;
using Kvieta.Desktop;
using Velopack;
using Velopack.Locators;
using Velopack.Logging;
using Velopack.Sources;
using System.Reflection;

if (args.Length != 2) throw new ArgumentException("Expected the desktop package directory and the SDK updater path.");
var checkProcess = typeof(SignedDesktopSource).Assembly.GetType("Kvieta.Desktop.DesktopUpdates")!
    .GetMethod("IsDesktopProcessInInstall", BindingFlags.Static | BindingFlags.NonPublic)!;
bool InInstall(string candidate) => (bool)checkProcess.Invoke(null,
    [candidate, @"C:\Users\Tester\AppData\Local\Kvieta.Desktop\current\Kvieta.Desktop.exe",
        @"C:\Users\Tester\AppData\Local\Kvieta.Desktop"])!;
if (!InInstall(@"C:\Users\Tester\AppData\Local\Kvieta.Desktop\app-7.0.1\Kvieta.Desktop.exe") ||
    InInstall(@"C:\Users\Tester\AppData\Local\Kvieta.Desktop-other\Kvieta.Desktop.exe"))
    throw new InvalidOperationException("Running instances in versioned folders were not identified correctly.");
string directory = Path.GetFullPath(args[0]);
string updater = Path.GetFullPath(args[1]);
if (Environment.GetEnvironmentVariable("KVIETA_DESKTOP_PUBLIC_KEY") is { Length: > 0 } productionPublicKey &&
    File.Exists(Path.Combine(directory, "desktop.json")))
{
    var packaged = SignedDesktopSource.Verify(await File.ReadAllBytesAsync(Path.Combine(directory, "desktop.json")), productionPublicKey);
    if (!packaged.Feed.Assets.All(asset => SignedDesktopSource.MatchesFile(asset, Path.Combine(directory, asset.FileName))))
        throw new InvalidOperationException("The packaging script's signed feed does not match its packages.");
    Console.WriteLine("Packaging script's signed feed verified.");
}
string rawFeed = await File.ReadAllTextAsync(Path.Combine(directory, "releases.desktop-stable.json"));
var releaseFeed = VelopackAssetFeed.FromJson(rawFeed);
var contents = releaseFeed.Assets.Where(asset => asset.Type == VelopackAssetType.Full).Select(asset =>
{
    using var zip = ZipFile.OpenRead(Path.Combine(directory, asset.FileName));
    return new SignedDesktopSource.PackageContents(asset.FileName, zip.Entries.Select(entry =>
    {
        using var stream = entry.Open();
        return new SignedDesktopSource.ContentEntry(entry.FullName, entry.Length, Convert.ToHexString(SHA256.HashData(stream)));
    }).ToArray());
}).ToArray();
JsonObject feedObject = JsonNode.Parse(rawFeed)!.AsObject();
feedObject["Contents"] = JsonSerializer.SerializeToNode(contents);
byte[] feed = JsonSerializer.SerializeToUtf8Bytes(feedObject);
using var key = RSA.Create(2048);
string publicKey = Convert.ToBase64String(key.ExportSubjectPublicKeyInfo());
byte[] Sign(byte[] data) => JsonSerializer.SerializeToUtf8Bytes(new
{
    Payload = data,
    Signature = key.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
});
void Reject(Action operation)
{
    try { operation(); }
    catch (InvalidDataException) { return; }
    throw new InvalidOperationException("An invalid feed was accepted.");
}

var verified = SignedDesktopSource.Verify(Sign(feed), publicKey);
if (verified.Feed.Assets.Length < 2 || !verified.Feed.Assets.All(asset =>
        SignedDesktopSource.MatchesFile(asset, Path.Combine(directory, asset.FileName))))
    throw new InvalidOperationException("Desktop package files do not match the signed feed.");
string badFeed = Encoding.UTF8.GetString(feed).Replace("Kvieta.Desktop", "Other.Desktop", StringComparison.Ordinal);
Reject(() => SignedDesktopSource.Verify(Sign(Encoding.UTF8.GetBytes(badFeed)), publicKey));
using var unrelated = RSA.Create(2048);
Reject(() => SignedDesktopSource.Verify(Sign(feed), Convert.ToBase64String(unrelated.ExportSubjectPublicKeyInfo())));
Console.WriteLine($"Verified {verified.Feed.Assets.Length} desktop package assets; unrelated package IDs and signatures were rejected.");

var baseline = verified.Feed.Assets.Single(asset => asset.Version.ToString() == "7.0.41" && asset.Type == VelopackAssetType.Full);
var latest = verified.Feed.Assets.Single(asset => asset.Version.ToString() == "7.0.42" && asset.Type == VelopackAssetType.Full);
string testRoot = Path.Combine(Path.GetTempPath(), "Kvieta-Desktop-Update-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(testRoot);
try
{
    foreach (bool failDelta in new[] { false, true })
    {
        string packages = Path.Combine(testRoot, failDelta ? "fallback" : "delta");
        Directory.CreateDirectory(packages);
        File.Copy(Path.Combine(directory, baseline.FileName), Path.Combine(packages, baseline.FileName));
        var source = new LocalSignedSource(verified.Feed, directory, failDelta);
        string appDir = Path.Combine(packages, "current");
        Directory.CreateDirectory(appDir);
        var locator = new TestVelopackLocator(SignedDesktopSource.PackageId, "7.0.41", packages,
            appDir, packages, updater, channel: SignedDesktopSource.Channel);
        var manager = new UpdateManager(source, new UpdateOptions { ExplicitChannel = SignedDesktopSource.Channel }, locator);
        var update = await manager.CheckForUpdatesAsync() ?? throw new InvalidOperationException("Desktop update was not detected.");
        Console.WriteLine($"Planned {update.DeltasToTarget.Length} delta(s); base: {update.BaseRelease?.FileName}");
        await manager.DownloadUpdatesAsync(update);
        string prepared = Path.Combine(packages, latest.FileName);
        if (!SignedDesktopSource.MatchesPrepared(verified, latest, prepared))
            throw new InvalidOperationException("The staged desktop package differs from the signed package contents.");
        if (manager.UpdatePendingRestart is not { } pending || !SignedDesktopSource.SamePackageIdentity(pending, latest))
            throw new InvalidOperationException($"The staged package could not be identified as ready for restart: {JsonSerializer.Serialize(manager.UpdatePendingRestart)}");
        if (failDelta != source.FetchedFull)
            throw new InvalidOperationException($"Expected full download={failDelta}; actual={source.FetchedFull}.");
        string changed = Path.Combine(packages, "modified.nupkg");
        File.Copy(prepared, changed);
        using (var archive = ZipFile.Open(changed, ZipArchiveMode.Update))
        {
            var entry = archive.GetEntry("lib/app/sq.version") ?? throw new InvalidOperationException("Package metadata absent.");
            entry.Delete();
            using var writer = new StreamWriter(archive.CreateEntry("lib/app/sq.version").Open());
            writer.Write("Modified test fixture");
        }
        if (SignedDesktopSource.MatchesPrepared(verified, latest, changed))
            throw new InvalidOperationException("The modified package was accepted.");
        Console.WriteLine(failDelta ? "Failed delta recovered using the signed complete package." : "SDK prepared the verified delta for the next launch.");
    }
}
finally { Directory.Delete(testRoot, recursive: true); }

sealed class LocalSignedSource(VelopackAssetFeed feed, string directory, bool failDelta) : IUpdateSource
{
    public bool FetchedFull { get; private set; }
    public Task<VelopackAssetFeed> GetReleaseFeed(IVelopackLogger logger, string? appId, string channel,
        Guid? stagingId = null, VelopackAsset? latestLocalRelease = null) => Task.FromResult(feed);

    public async Task DownloadReleaseEntry(IVelopackLogger logger, VelopackAsset asset, string localFile,
        Action<int> progress, CancellationToken cancelToken = default)
    {
        if (asset.Type == VelopackAssetType.Delta && failDelta) throw new IOException("Test delta download failure.");
        FetchedFull |= asset.Type == VelopackAssetType.Full;
        await using var source = File.OpenRead(Path.Combine(directory, asset.FileName));
        await using (var target = File.Create(localFile)) await source.CopyToAsync(target, cancelToken);
        if (!SignedDesktopSource.MatchesFile(asset, localFile)) throw new InvalidDataException("Fixture package was modified.");
        progress(100);
    }
}
