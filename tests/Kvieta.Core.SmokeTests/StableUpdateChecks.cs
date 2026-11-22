using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using Kvieta.App.Services;
using Kvieta.Core.Services;

internal static class StableUpdateChecks
{
    public static async Task RunAsync(string directory, Action<bool, string> check)
    {
        check(!await StartupUpdate.RequestTaskAsync("Kvieta Missing Task " + Guid.NewGuid().ToString("N")),
            "A missing startup update task should leave the installed version usable.");
        Directory.CreateDirectory(directory);
        byte[] target = RandomNumberGenerator.GetBytes(StableChunker.Maximum * 2 + 97);
        byte[] prior = (byte[])target.Clone();
        prior[^1] ^= 1;
        string priorPath = Path.Combine(directory, "Kvieta-Setup-7.0.41-win-x64.exe");
        await File.WriteAllBytesAsync(priorPath, prior);
        static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
        List<int> sizes = [];
        List<string> hashes = [];
        using var source = new MemoryStream(target);
        byte[] buffer = new byte[StableChunker.Maximum];
        int count;
        while ((count = StableChunker.ReadChunk(source, buffer)) > 0)
        {
            sizes.Add(count);
            hashes.Add(Hash(buffer[..count]));
        }
        var metadata = new StableUpdatePackage("Kvieta", "win-x64", "7.0.42", "v1.0.0",
            target.Length, Hash(target), hashes.ToArray(), sizes.ToArray(), "setup", 2);
        using var key = RSA.Create(2048);
        byte[] data = JsonSerializer.SerializeToUtf8Bytes(metadata);
        byte[] signature = key.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        byte[] Envelope() => JsonSerializer.SerializeToUtf8Bytes(new { Payload = data, Signature = signature });
        string publicKey = Convert.ToBase64String(key.ExportSubjectPublicKeyInfo());
        StableUpdatePackage verified = StableUpdatePackage.Verify(Envelope(), publicKey);
        signature[0] ^= 1;
        try { StableUpdatePackage.Verify(Envelope(), publicKey); check(false, "Invalid stable signature accepted."); }
        catch (InvalidDataException) { check(true, "Invalid stable signature rejected."); }
        var handler = new PackageServer(target);
        using var client = new HttpClient(handler);
        string path = await verified.StageAsync(client, directory, null, default);
        check(handler.Bytes == sizes[^1] && File.ReadAllBytes(path).SequenceEqual(target), "Stable delta did not reconstruct the target.");
        using (var installer = File.OpenRead(path))
        {
            check(verified.CanApplySetup(installer, publicKey, new Version("7.0.41"), 2),
                "A verified newer Setup was refused.");
            check(!verified.CanApplySetup(installer, publicKey, new Version("7.0.42"), 2),
                "Setup for the installed version was accepted.");
            check(!metadata.CanApplySetup(installer, publicKey, new Version("7.0.41"), 2),
                "Setup metadata without a verified envelope was accepted.");
            check(!verified.CanApplySetup(installer, publicKey, new Version("7.0.41"), 1),
                "A Setup containing a new Guardian revision was allowed to install automatically.");
        }
        var prepared = await StableUpdatePackage.FindPreparedAsync(directory, publicKey, new Version("7.0.41"));
        check(prepared?.InstallerPath == path && prepared.Value.Package.Version == "7.0.42", "Verified prepared update could not be restored.");
        check(await StableUpdatePackage.FindPreparedAsync(directory, publicKey, new Version("7.0.42")) == null,
            "Already installed update was offered from the cache.");
        await File.WriteAllBytesAsync(path + ".json", Envelope());
        check(await StableUpdatePackage.FindPreparedAsync(directory, publicKey, new Version("7.0.41")) == null,
            "Corrupt cached signature was accepted.");
        await verified.StageAsync(client, directory, null, default);
        check(handler.Bytes == sizes[^1], "Cached installer was downloaded again.");
        check((await StableUpdatePackage.FindPreparedAsync(directory, publicKey, new Version("7.0.41")))?.InstallerPath == path,
            "Verified cached installer did not restore its signed metadata.");
        File.Delete(path);
        handler.RejectRange = true;
        await verified.StageAsync(client, directory, null, default);
        check(handler.Bytes == target.Length + sizes[^1] && File.ReadAllBytes(path).SequenceEqual(target), "Rejected range did not fall back to full package.");
        File.Delete(path);
        handler.RejectRange = false;
        handler.IgnoreRange = true;
        await verified.StageAsync(client, directory, null, default);
        check(handler.Bytes == target.Length * 2 + sizes[^1] && File.ReadAllBytes(path).SequenceEqual(target), "Full-download fallback failed.");
        File.Delete(path);
        handler.IgnoreRange = false;
        handler.CorruptRange = true;
        await verified.StageAsync(client, directory, null, default);
        check(handler.Bytes == target.Length * 3 + sizes[^1] * 2 && File.ReadAllBytes(path).SequenceEqual(target),
            "Corrupt range did not fall back to the verified complete Setup.");
        byte[] changed = (byte[])target.Clone();
        changed[0] ^= 1;
        await File.WriteAllBytesAsync(path, changed);
        check(await StableUpdatePackage.FindPreparedAsync(directory, publicKey, new Version("7.0.41")) == null,
            "Changed cached installer was accepted.");
        File.Delete(path);
        handler.Corrupt = true;
        try { await verified.StageAsync(client, directory, null, default); check(false, "Corrupt stable package accepted."); }
        catch (InvalidDataException) { check(!File.Exists(path), "Corrupt installer committed."); }
        handler.Offline = true;
        try { await verified.StageAsync(client, directory, null, default); check(false, "Offline download succeeded."); }
        catch (HttpRequestException)
        {
            check(File.ReadAllBytes(priorPath).SequenceEqual(prior) && !Directory.EnumerateFiles(directory, "*.partial").Any(),
                "Failed download damaged previous package or left partial data.");
        }
    }

    private sealed class PackageServer(byte[] data) : HttpMessageHandler
    {
        public int Bytes { get; private set; }
        public bool IgnoreRange { get; set; }
        public bool RejectRange { get; set; }
        public bool Corrupt { get; set; }
        public bool CorruptRange { get; set; }
        public bool Offline { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (Offline) throw new HttpRequestException("Offline test.");
            var range = IgnoreRange ? null : request.Headers.Range?.Ranges.Single();
            if (RejectRange && range != null)
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.RequestedRangeNotSatisfiable));
            int first = (int)(range?.From ?? 0);
            int last = (int)(range?.To ?? data.Length - 1);
            byte[] bytes = data[first..(last + 1)];
            if (Corrupt || (CorruptRange && range != null)) bytes[^1] ^= 1;
            Bytes += bytes.Length;
            var response = new HttpResponseMessage(range == null ? HttpStatusCode.OK : HttpStatusCode.PartialContent)
            {
                Content = new ByteArrayContent(bytes)
            };
            if (range != null) response.Content.Headers.ContentRange = new ContentRangeHeaderValue(first, last, data.Length);
            return Task.FromResult(response);
        }
    }
}
