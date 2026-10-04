using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Kvieta.App.Services;

public sealed record RelayClientSettings(string Origin, string Room, string ReadToken, string Key, string DecisionToken = "");
public sealed record RelayCredentials(string WriteToken, RelayClientSettings Client)
{
    public const string Origin = "https://kvieta-companion-relay.ygz-gur39.workers.dev";
    public static RelayCredentials Create()
    {
        string writer = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        string room = HashToken(writer);
        return new(writer, new(Origin, room,
            Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant(),
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant()));
    }
    public RelayCredentials Upgrade() => Client.DecisionToken?.Length == 64 ? this : this with
    {
        Client = Client with { DecisionToken = HashToken(WriteToken + ":decision-v1") }
    };
    public static string HashToken(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}

public static class DashboardRelay
{
    public static string StorePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Kvieta", "dashboard-pairing.bin");
    private static readonly HttpClient Http = new(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(10) };
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static DashboardStore? Load(string path)
    {
        if (!File.Exists(path)) return null;
        byte[] plain = ProtectedData.Unprotect(File.ReadAllBytes(path), null, DataProtectionScope.CurrentUser);
        try
        {
            DashboardStore? store = JsonSerializer.Deserialize<DashboardStore>(plain);
            if (store?.Relay is not null)
            {
                RelayCredentials upgraded = store.Relay.Upgrade();
                if (!ReferenceEquals(upgraded, store.Relay))
                {
                    store = store with { Relay = upgraded };
                }
            }
            return store;
        }
        finally { CryptographicOperations.ZeroMemory(plain); }
    }

    public static string Encrypt(RelayClientSettings client, object snapshot, long sequence)
    {
        byte[] key = Convert.FromBase64String(client.Key);
        byte[] plain = JsonSerializer.SerializeToUtf8Bytes(new { sequence, publishedAtUtc = DateTimeOffset.UtcNow, snapshot }, Json);
        byte[] packed = new byte[12 + plain.Length + 16];
        RandomNumberGenerator.Fill(packed.AsSpan(0, 12));
        try
        {
            using AesGcm aes = new(key, 16);
            aes.Encrypt(packed.AsSpan(0, 12), plain, packed.AsSpan(12, plain.Length), packed.AsSpan(12 + plain.Length, 16),
                Encoding.UTF8.GetBytes("kvieta-relay-v1\n" + client.Room));
            return Convert.ToBase64String(packed);
        }
        finally { CryptographicOperations.ZeroMemory(key); CryptographicOperations.ZeroMemory(plain); }
    }

    public static async Task PublishAsync(RelayCredentials relay, object snapshot, CancellationToken cancellation)
    {
        long sequence = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        using HttpRequestMessage request = Request(relay, HttpMethod.Put);
        request.Content = JsonContent.Create(new
        {
            readHash = RelayCredentials.HashToken(relay.Client.ReadToken),
            decisionHash = RelayCredentials.HashToken(relay.Client.DecisionToken),
            sequence,
            box = Encrypt(relay.Client, snapshot, sequence)
        });
        using HttpResponseMessage response = await Http.SendAsync(request, cancellation);
        if (response.StatusCode != HttpStatusCode.TooManyRequests) response.EnsureSuccessStatusCode();
    }

    public static async Task<MobileTimeDecision?> ReadDecisionAsync(RelayCredentials relay, CancellationToken cancellation)
    {
        using HttpRequestMessage request = Request(relay, HttpMethod.Get, "/decision");
        using HttpResponseMessage response = await Http.SendAsync(request, cancellation);
        if (response.StatusCode == HttpStatusCode.NoContent) return null;
        response.EnsureSuccessStatusCode();
        RelayEnvelope? envelope = await response.Content.ReadFromJsonAsync<RelayEnvelope>(Json, cancellation);
        if (envelope is null || string.IsNullOrWhiteSpace(envelope.Box)) return null;
        return DecryptDecision(relay.Client, envelope);
    }

    public static async Task RevokeAsync(RelayCredentials relay, CancellationToken cancellation)
    {
        using HttpRequestMessage request = Request(relay, HttpMethod.Delete);
        using HttpResponseMessage response = await Http.SendAsync(request, cancellation);
        response.EnsureSuccessStatusCode();
    }

    private static MobileTimeDecision DecryptDecision(RelayClientSettings client, RelayEnvelope envelope)
    {
        byte[] packed = Convert.FromBase64String(envelope.Box);
        if (packed.Length is < 29 or > 4096) throw new InvalidDataException("Invalid relay decision");
        byte[] key = Convert.FromBase64String(client.Key);
        byte[] plain = new byte[packed.Length - 28];
        try
        {
            using AesGcm aes = new(key, 16);
            aes.Decrypt(packed.AsSpan(0, 12), packed.AsSpan(12, plain.Length), packed.AsSpan(12 + plain.Length, 16), plain,
                Encoding.UTF8.GetBytes("kvieta-relay-decision-v1\n" + client.Room));
            DecisionPayload payload = JsonSerializer.Deserialize<DecisionPayload>(plain, Json) ?? throw new InvalidDataException("Invalid relay decision");
            if (payload.Sequence != envelope.Sequence || payload.Sequence < 1) throw new InvalidDataException("Invalid relay decision sequence");
            return new(payload.DecisionId, payload.RequestId, payload.Action, payload.GrantedMinutes, payload.DecidedAtUtc, payload.PayloadJson);
        }
        finally { CryptographicOperations.ZeroMemory(key); CryptographicOperations.ZeroMemory(plain); }
    }

    private static HttpRequestMessage Request(RelayCredentials relay, HttpMethod method, string suffix = "")
    {
        if (relay.Client.Origin != RelayCredentials.Origin || relay.Client.Room != RelayCredentials.HashToken(relay.WriteToken))
            throw new InvalidDataException("Invalid relay peer");
        var request = new HttpRequestMessage(method, relay.Client.Origin + "/v1/rooms/" + relay.Client.Room + suffix);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", relay.WriteToken);
        return request;
    }

    private sealed record RelayEnvelope(string Box, long Sequence);
    private sealed record DecisionPayload(long Sequence, string DecisionId, string RequestId, string Action, int? GrantedMinutes, DateTimeOffset DecidedAtUtc, string? PayloadJson);
}

/// <summary>Only publishes explicitly enabled, encrypted summaries while Kvieta is running.</summary>
public sealed class DashboardRelayPublisher : IDisposable
{
    private readonly CancellationTokenSource _stop = new();
    public DashboardRelayPublisher(Func<Task<object>> snapshot)
    {
        _ = Task.Run(async () =>
        {
            string? lastProcessedDecisionId = null;
            DateTimeOffset lastPublishUtc = DateTimeOffset.MinValue;

            // The session and control center can be different processes. Only one publishes at a time.
            while (!_stop.IsCancellationRequested)
            {
                // File handles are released by Windows on process death; a named semaphore
                // could stay locked when the publishing process was terminated.
                using FileStream? gate = TryAcquirePublisher();
                if (gate is not null)
                {
                    try
                    {
                        DashboardStore? store = DashboardRelay.Load(DashboardRelay.StorePath);
                        foreach (RelayCredentials old in store?.RevokedRelays ?? [])
                            await DashboardRelay.RevokeAsync(old, _stop.Token);

                        if (store?.Relay is not null)
                        {
                            RelayCredentials relay = store.Relay.Upgrade();

                            // 1. Regular snapshot publishing (every 48 seconds to respect Cloudflare 45s write rate limit)
                            if (DateTimeOffset.UtcNow - lastPublishUtc >= TimeSpan.FromSeconds(48))
                            {
                                try
                                {
                                    object value = await snapshot();
                                    if (DashboardRelay.Load(DashboardRelay.StorePath)?.Relay?.Client.Room == relay.Client.Room)
                                    {
                                        await DashboardRelay.PublishAsync(relay, value, _stop.Token);
                                        lastPublishUtc = DateTimeOffset.UtcNow;
                                    }
                                }
                                catch (Exception) { /* transient network or rate limit, retry later */ }
                            }

                            // 2. High-frequency decision checking (every 3 seconds)
                            MobileTimeDecision? decision = null;
                            try { decision = await DashboardRelay.ReadDecisionAsync(relay, _stop.Token); }
                            catch (Exception) { /* transient network error */ }

                            if (decision is not null && decision.DecisionId != lastProcessedDecisionId &&
                                DashboardRelay.Load(DashboardRelay.StorePath)?.Relay?.Client.Room == relay.Client.Room)
                            {
                                lastProcessedDecisionId = decision.DecisionId;

                                if (string.Equals(decision.Action, "pair-device", StringComparison.OrdinalIgnoreCase))
                                {
                                    string phoneName = "Android Telefon";
                                    try
                                    {
                                        if (!string.IsNullOrWhiteSpace(decision.PayloadJson))
                                        {
                                            using var doc = JsonDocument.Parse(decision.PayloadJson);
                                            if (doc.RootElement.TryGetProperty("deviceName", out var prop))
                                                phoneName = prop.GetString() ?? phoneName;
                                        }
                                    }
                                    catch { }
                                    DashboardEndpoint.RecordPairedPhone(phoneName);
                                }
                                else if (string.Equals(decision.Action, "update-plan", StringComparison.OrdinalIgnoreCase))
                                {
                                    await MobilePlanChangeStore.ApplyAsync(decision.PayloadJson, _stop.Token);
                                    DashboardEndpoint.RecordPairedPhone("Android Telefon");
                                }
                                else if (string.Equals(decision.Action, "session-action", StringComparison.OrdinalIgnoreCase))
                                {
                                    await MobileRemoteActionStore.RecordActionAsync(decision.PayloadJson, _stop.Token);
                                    DashboardEndpoint.RecordPairedPhone("Android Telefon");
                                }
                                else if (string.Equals(decision.Action, "update-app-rule", StringComparison.OrdinalIgnoreCase))
                                {
                                    await MobileAppRuleStore.ApplyAsync(decision.PayloadJson, _stop.Token);
                                    DashboardEndpoint.RecordPairedPhone("Android Telefon");
                                }
                                else
                                {
                                    MobileTimeRequestStore.AcceptDecision(decision);
                                    DashboardEndpoint.RecordPairedPhone("Android Telefon");
                                }

                                // If allowed by rate-limit window, push fresh snapshot so phone immediately sees updated state
                                if (DateTimeOffset.UtcNow - lastPublishUtc >= TimeSpan.FromSeconds(45))
                                {
                                    try
                                    {
                                        object value = await snapshot();
                                        await DashboardRelay.PublishAsync(relay, value, _stop.Token);
                                        lastPublishUtc = DateTimeOffset.UtcNow;
                                    }
                                    catch { }
                                }
                            }
                        }

                        // Poll every 3 seconds for responsive remote actions
                        await Task.Delay(TimeSpan.FromSeconds(3), _stop.Token);
                    }
                    catch (OperationCanceledException) when (_stop.IsCancellationRequested) { return; }
                    catch (Exception)
                    {
                        // Slow retry on unexpected failures
                        try { await Task.Delay(TimeSpan.FromSeconds(10), _stop.Token); }
                        catch (OperationCanceledException) { return; }
                    }
                }
                else
                {
                    try { await Task.Delay(TimeSpan.FromSeconds(5), _stop.Token); }
                    catch (OperationCanceledException) { return; }
                }
            }
        });
    }
    private static FileStream? TryAcquirePublisher()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DashboardRelay.StorePath)!);
            return new FileStream(DashboardRelay.StorePath + ".publisher.lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException) { return null; }
    }
    public void Dispose() => _stop.Cancel();
}
