using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Kvieta.App.Services;

internal static class DashboardEndpointChecks
{
    public static async Task RunAsync()
    {
        string directory = Path.Combine(Path.GetTempPath(), "Kvieta-DashboardTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "pairing.bin");
        using ECDsa phone = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        string deviceId = Guid.NewGuid().ToString();
        string key = Convert.ToBase64String(phone.ExportSubjectPublicKeyInfo());
        string pin;
        async Task<object> Snapshot() { await Task.Yield(); return new { deviceName = "Test desktop", usedSeconds = 300 }; }
        string Sign(string content) => Convert.ToBase64String(phone.SignData(Encoding.UTF8.GetBytes(content), HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence));
        DashboardProof Proof(long? time = null)
        {
            DashboardProof request = new(deviceId, time ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds(), Convert.ToHexString(RandomNumberGenerator.GetBytes(16)), "");
            return request with { Signature = Sign(DashboardEndpoint.ReadContent(request)) };
        }
        static void Check(bool success, string message) { if (!success) throw new InvalidOperationException(message); }
        static HttpClient Client(string origin, string expectedPin) => new(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, cert, _, _) => cert is not null && Convert.ToHexString(SHA256.HashData(cert.RawData)) == expectedPin,
            AllowAutoRedirect = false,
            UseProxy = false
        }) { BaseAddress = new Uri(origin), Timeout = TimeSpan.FromSeconds(10) };

        await using (DashboardEndpoint endpoint = await DashboardEndpoint.StartAsync(path, Snapshot, IPAddress.Loopback, 0))
        {
            pin = endpoint.CertificatePin;
            using HttpClient client = Client(endpoint.Origin, pin);
            using HttpClient untrusted = Client(endpoint.Origin, new string('0', 64));
            bool pinRejected = false;
            try { await untrusted.PostAsJsonAsync("/v1/snapshot", Proof()); }
            catch (HttpRequestException) { pinRejected = true; }
            Check(pinRejected, "Wrong desktop certificate was trusted.");
            Check((await client.PostAsJsonAsync("/v1/snapshot", Proof())).StatusCode == HttpStatusCode.Forbidden, "Unpaired device read usage.");
            string invitation = endpoint.CreateInvitation();
            string token = new Uri(invitation).Query.TrimStart('?').Split('&').Single(p => p.StartsWith("token=", StringComparison.Ordinal))[6..];
            DashboardPairRequest pair = new(token, deviceId, "Test Android", key, "");
            pair = pair with { Signature = Sign(DashboardEndpoint.PairContent(pair)) };
            Check((await client.PostAsJsonAsync("/v1/pair", pair with { Signature = "broken" })).StatusCode == HttpStatusCode.Forbidden, "Invalid enrollment proof accepted.");
            Check((await client.PostAsJsonAsync("/v1/pair", pair)).IsSuccessStatusCode, "Pair proposal failed.");
            Check(endpoint.VerificationCode == DashboardEndpoint.Code(key), "Confirmation code differs.");
            Check((await client.PostAsJsonAsync("/v1/pair", pair)).StatusCode == HttpStatusCode.Forbidden, "Invitation was reused.");
            using JsonDocument pending = JsonDocument.Parse(await (await client.PostAsJsonAsync("/v1/snapshot", Proof())).Content.ReadAsStringAsync());
            Check(pending.RootElement.GetProperty("state").GetString() == "pending" && !pending.RootElement.TryGetProperty("snapshot", out _), "Usage leaked before approval.");
            Check(!pending.RootElement.TryGetProperty("remote", out _), "Relay keys leaked before approval.");
            Check(endpoint.Approve(), "Computer approval failed.");
            DashboardProof read = Proof();
            using JsonDocument paired = JsonDocument.Parse(await (await client.PostAsJsonAsync("/v1/snapshot", read)).Content.ReadAsStringAsync());
            Check(paired.RootElement.GetProperty("snapshot").GetProperty("usedSeconds").GetInt64() == 300, "Approved client did not receive usage.");
            Check(paired.RootElement.GetProperty("remote").ValueKind == JsonValueKind.Null, "Remote access was enabled without opt-in.");
            endpoint.EnableRemote();
            using JsonDocument remote = JsonDocument.Parse(await (await client.PostAsJsonAsync("/v1/snapshot", Proof())).Content.ReadAsStringAsync());
            Check(remote.RootElement.GetProperty("remote").GetProperty("origin").GetString() == RelayCredentials.Origin,
                "Approved phone did not receive the allowed relay.");
            Check(!remote.RootElement.GetProperty("remote").TryGetProperty("writeToken", out _), "Phone received publisher authority.");
            Check((await client.PostAsJsonAsync("/v1/snapshot", read)).StatusCode == HttpStatusCode.Forbidden, "Read proof replay accepted.");
            Check((await client.PostAsJsonAsync("/v1/snapshot", Proof(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 1000))).StatusCode == HttpStatusCode.Forbidden, "Expired proof accepted.");
            Check((await client.PostAsJsonAsync("/v1/approve", new { })).StatusCode == HttpStatusCode.NotFound, "Network client can approve itself.");
            Check(!Encoding.UTF8.GetString(File.ReadAllBytes(path)).Contains(key, StringComparison.Ordinal), "Pair store contains unprotected key data.");
        }
        await using (DashboardEndpoint restarted = await DashboardEndpoint.StartAsync(path, Snapshot, IPAddress.Loopback, 0))
        {
            Check(restarted.CertificatePin == pin && restarted.PhoneName == "Test Android", "Pairing identity did not survive restart.");
            Check(restarted.RemoteEnabled, "Remote opt-in did not survive restart.");
            using HttpClient client = Client(restarted.Origin, pin);
            Check((await client.PostAsJsonAsync("/v1/snapshot", Proof())).IsSuccessStatusCode, "Saved pairing could not reconnect.");
            restarted.Revoke();
            var revokedState = DashboardRelay.Load(path)!;
            Check(revokedState.Relay is null && revokedState.RevokedRelays?.Length == 1, "Remote revocation was not queued durably.");
            Check((await client.PostAsJsonAsync("/v1/snapshot", Proof())).StatusCode == HttpStatusCode.Forbidden, "Revoked phone retained access.");
        }
        await using (DashboardEndpoint revoked = await DashboardEndpoint.StartAsync(path, Snapshot, IPAddress.Loopback, 0))
            Check(revoked.PhoneName is null, "Revocation was not persisted.");
        Console.WriteLine("Dashboard TLS, approval, replay, persistence and revocation checks passed.");
    }
}
