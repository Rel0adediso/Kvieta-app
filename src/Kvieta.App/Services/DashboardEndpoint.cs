using System.Globalization;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging;

namespace Kvieta.App.Services;

public sealed record DashboardProof(string DeviceId, long Time, string Nonce, string Signature);
public sealed record DashboardPairRequest(string Token, string DeviceId, string Name, string PublicKey, string Signature);
public sealed record DashboardPhone(string DeviceId, string Name, string PublicKey);
public sealed record DashboardStore(string Certificate, DashboardPhone? Phone, RelayCredentials? Relay = null, RelayCredentials[]? RevokedRelays = null);

/// <summary>Opt-in, read-only LAN sharing. Recovery and policy mutation are deliberately absent.</summary>
public sealed class DashboardEndpoint : IAsyncDisposable
{
    private readonly object _gate = new();
    private readonly string _path;
    private readonly X509Certificate2 _certificate;
    private readonly Func<Task<object>> _snapshot;
    private readonly Dictionary<string, long> _nonces = [];
    private DashboardStore _store;
    private DashboardPhone? _pending;
    private WebApplication? _app;
    private string _token = string.Empty;
    private DateTimeOffset _expires;
    private int _attempts;

    private DashboardEndpoint(string path, Func<Task<object>> snapshot)
    {
        _path = path;
        _snapshot = snapshot;
        if (File.Exists(path))
        {
            byte[] plain = ProtectedData.Unprotect(File.ReadAllBytes(path), null, DataProtectionScope.CurrentUser);
            try { _store = JsonSerializer.Deserialize<DashboardStore>(plain) ?? throw new InvalidDataException(); }
            finally { CryptographicOperations.ZeroMemory(plain); }
            if (_store.Relay is not null)
            {
                RelayCredentials upgraded = _store.Relay.Upgrade();
                if (!ReferenceEquals(upgraded, _store.Relay))
                {
                    _store = _store with { Relay = upgraded };
                    Save(_store);
                }
            }
            _certificate = X509CertificateLoader.LoadPkcs12(Convert.FromBase64String(_store.Certificate), null,
                X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.Exportable);
        }
        else
        {
            using RSA key = RSA.Create(2048);
            CertificateRequest request = new("CN=Kvieta Companion", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new("1.3.6.1.5.5.7.3.1") }, false));
            using X509Certificate2 generated = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddYears(2));
            byte[] pfx = generated.Export(X509ContentType.Pfx);
            _certificate = X509CertificateLoader.LoadPkcs12(pfx, null, X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.Exportable);
            _store = new(Convert.ToBase64String(pfx), null);
            Save(_store);
        }
        GlobalStoreChanged += HandleGlobalStoreChanged;
    }

    public static event Action? GlobalStoreChanged;
    private void HandleGlobalStoreChanged()
    {
        lock (_gate)
        {
            try
            {
                DashboardStore? loaded = DashboardRelay.Load(_path);
                if (loaded is not null) _store = loaded;
            }
            catch { }
        }
        Changed?.Invoke();
    }

    public string CertificatePin => Convert.ToHexString(SHA256.HashData(_certificate.RawData));
    public string Origin { get; private set; } = "";
    // Relay pairing does not require a bindable LAN address; local HTTPS remains optional.
    public bool CanPair => true;
    public static DashboardEndpoint OpenOffline(string path, Func<Task<object>> snapshot) => new(path, snapshot);
    public string? PhoneName { get { lock (_gate) return _store.Phone?.Name; } }
    public bool RemoteEnabled { get { lock (_gate) return _store.Relay is not null; } }
    private RelayClientSettings? RemoteClient { get { lock (_gate) return _store.Phone is null ? null : _store.Relay?.Client; } }
    public string? PendingName { get { lock (_gate) return _pending?.Name; } }
    public string? VerificationCode { get { lock (_gate) return _pending is null ? null : Code(_pending.PublicKey); } }
    public event Action? Changed;

    public static IPAddress FindLanAddress() => NetworkInterface.GetAllNetworkInterfaces()
        .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
        .OrderByDescending(n => n.GetIPProperties().GatewayAddresses.Count)
        .SelectMany(n => n.GetIPProperties().UnicastAddresses).Select(n => n.Address)
        .FirstOrDefault(IsPrivateAddress) ?? throw new InvalidOperationException("Connect to a private local network first.");

    private static bool IsPrivateAddress(IPAddress address)
    {
        byte[] b = address.GetAddressBytes();
        return b.Length == 4 && (b[0] == 10 || b[0] == 192 && b[1] == 168 || b[0] == 172 && b[1] is >= 16 and <= 31);
    }

    public static async Task<DashboardEndpoint> StartAsync(string path, Func<Task<object>> snapshot, IPAddress? address = null, int port = 24882)
    {
        DashboardEndpoint endpoint = new(path, snapshot);
        try
        {
            IPAddress bind = address ?? FindLanAddress();
            WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { Args = [] });
            builder.Logging.ClearProviders(); // Invitation tokens, public keys and usage must not enter request logs.
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Limits.MaxRequestBodySize = 8192;
                options.Limits.MaxConcurrentConnections = 8;
                options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(10);
                options.Listen(bind, port, listen => { listen.Protocols = HttpProtocols.Http1; listen.UseHttps(endpoint._certificate); });
            });
            endpoint._app = builder.Build();
            endpoint._app.Use(async (context, next) =>
            {
                context.Response.Headers.CacheControl = "no-store";
                try { await next(context); }
                catch (Exception ex) when (ex is JsonException or Microsoft.AspNetCore.Http.BadHttpRequestException or ArgumentException or FormatException)
                { context.Response.StatusCode = 400; }
            });
            endpoint._app.MapPost("/v1/pair", async (HttpContext context) =>
            {
                DashboardPairRequest? request = await context.Request.ReadFromJsonAsync<DashboardPairRequest>(context.RequestAborted);
                return request is null ? Results.BadRequest() : endpoint.Propose(request);
            });
            endpoint._app.MapPost("/v1/snapshot", async (HttpContext context) =>
            {
                DashboardProof? proof = await context.Request.ReadFromJsonAsync<DashboardProof>(context.RequestAborted);
                string state = endpoint.Authenticate(proof);
                if (state == "denied") return Results.StatusCode(403);
                if (state == "pending") return Results.Json(new { state });
                object snapshot = await endpoint._snapshot();
                // Do not expose relay keys if access was revoked while loading the ledger.
                lock (endpoint._gate)
                {
                    if (endpoint._store.Phone?.DeviceId != proof!.DeviceId) return Results.StatusCode(403);
                    return Results.Json(new { state = "paired", snapshot, remote = endpoint.RemoteClient });
                }
            });
            await endpoint._app.StartAsync();
            endpoint.Origin = endpoint._app.Urls.Single().TrimEnd('/');
            return endpoint;
        }
        catch { await endpoint.DisposeAsync(); throw; }
    }

    public string CreateInvitation()
    {
        lock (_gate)
        {
            if (_store.Relay is not null)
            {
                if (_store.Phone is not null)
                {
                    DashboardStore next = _store with
                    {
                        Phone = null,
                        Relay = RelayCredentials.Create().Upgrade(),
                        RevokedRelays = [.. _store.RevokedRelays ?? [], _store.Relay]
                    };
                    Save(next);
                    _store = next;
                }
                RelayClientSettings relay = _store.Relay.Client;
                long expires = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds();
                return $"kvieta-companion://pair?v=2&origin={Uri.EscapeDataString(relay.Origin)}&room={relay.Room}&readToken={relay.ReadToken}&key={Uri.EscapeDataString(relay.Key)}&decisionToken={relay.DecisionToken}&expires={expires}";
            }
            _pending = null;
            _token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            _expires = DateTimeOffset.UtcNow.AddMinutes(2);
            _attempts = 0;
            return $"kvieta-companion://pair?v=1&origin={Uri.EscapeDataString(Origin)}&pin={CertificatePin}&token={_token}&expires={_expires.ToUnixTimeSeconds()}";
        }
    }

    private IResult Propose(DashboardPairRequest request)
    {
        lock (_gate)
        {
            if (_store.Phone is not null || _pending is not null || DateTimeOffset.UtcNow >= _expires || ++_attempts > 10 ||
                request.Token != _token || string.IsNullOrEmpty(_token) || !Guid.TryParseExact(request.DeviceId, "D", out _) ||
                string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 80 || request.Name.Any(char.IsControl) ||
                !Verify(request.PublicKey, PairContent(request), request.Signature)) return Results.StatusCode(403);
            _pending = new(request.DeviceId, request.Name, request.PublicKey);
            _token = ""; // Single-use, including before computer approval.
        }
        Changed?.Invoke();
        return Results.Json(new { state = "pending", code = Code(request.PublicKey) });
    }

    public bool Approve()
    {
        lock (_gate)
        {
            if (_pending is null || DateTimeOffset.UtcNow >= _expires) return false;
            DashboardStore next = _store with { Phone = _pending };
            Save(next);
            _store = next;
            _pending = null;
        }
        Changed?.Invoke();
        return true;
    }

    public void Revoke()
    {
        lock (_gate)
        {
            DashboardStore next = _store with
            {
                Phone = null,
                Relay = null,
                RevokedRelays = _store.Relay is null ? _store.RevokedRelays : [.. _store.RevokedRelays ?? [], _store.Relay]
            };
            Save(next);
            _store = next;
            _pending = null;
            _token = "";
            _nonces.Clear();
        }
        Changed?.Invoke();
    }

    public void EnableRemote()
    {
        lock (_gate)
        {
            DashboardStore next = _store with { Relay = (_store.Relay ?? RelayCredentials.Create()).Upgrade() };
            Save(next);
            _store = next;
        }
        Changed?.Invoke();
    }

    public async Task FlushRevocationsAsync()
    {
        RelayCredentials[] pending;
        lock (_gate) pending = _store.RevokedRelays ?? [];
        foreach (RelayCredentials relay in pending) await DashboardRelay.RevokeAsync(relay, CancellationToken.None);
    }

    private string Authenticate(DashboardProof? proof)
    {
        lock (_gate)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (proof is null || proof.Time < now - 90 || proof.Time > now + 90 ||
                proof.Nonce is null || proof.Nonce.Length != 32 || !proof.Nonce.All(Uri.IsHexDigit)) return "denied";
            DashboardPhone? phone = _store.Phone ?? (DateTimeOffset.UtcNow < _expires ? _pending : null);
            if (phone is null || phone.DeviceId != proof.DeviceId ||
                !Verify(phone.PublicKey, ReadContent(proof), proof.Signature)) return "denied";
            foreach (string nonce in _nonces.Where(n => n.Value < now).Select(n => n.Key).ToArray()) _nonces.Remove(nonce);
            if (_nonces.Count >= 512 || !_nonces.TryAdd(proof.Nonce, now + 180)) return "denied";
            return _store.Phone is null ? "pending" : "paired";
        }
    }

    public static string PairContent(DashboardPairRequest r) => $"kvieta-dashboard-pair-v1\n{r.Token}\n{r.DeviceId}\n{r.Name}\n{r.PublicKey}";
    public static string ReadContent(DashboardProof r) => $"kvieta-dashboard-read-v1\n{r.DeviceId}\n{r.Time.ToString(CultureInfo.InvariantCulture)}\n{r.Nonce}";
    public static string Code(string key) => Convert.ToHexString(SHA256.HashData(Convert.FromBase64String(key)))[..12];
    private static bool Verify(string? key, string content, string? signature)
    {
        if (key is null || signature is null || key.Length > 256 || signature.Length > 128) return false;
        try
        {
            using ECDsa ec = ECDsa.Create();
            byte[] bytes = Convert.FromBase64String(key);
            ec.ImportSubjectPublicKeyInfo(bytes, out int read);
            return read == bytes.Length && ec.ExportParameters(false).Curve.Oid.Value == "1.2.840.10045.3.1.7" &&
                ec.VerifyData(Encoding.UTF8.GetBytes(content), Convert.FromBase64String(signature), HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or CryptographicException) { return false; }
    }

    private void Save(DashboardStore value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        byte[] plain = JsonSerializer.SerializeToUtf8Bytes(value);
        string temp = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllBytes(temp, ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser));
            File.Move(temp, _path, true);
        }
        finally { CryptographicOperations.ZeroMemory(plain); if (File.Exists(temp)) File.Delete(temp); }
        GlobalStoreChanged?.Invoke();
    }

    public static void RecordPairedPhone(string deviceName, string deviceId = "android-relay")
    {
        try
        {
            string path = DashboardRelay.StorePath;
            if (!File.Exists(path)) return;
            byte[] plain = ProtectedData.Unprotect(File.ReadAllBytes(path), null, DataProtectionScope.CurrentUser);
            DashboardStore? store;
            try { store = JsonSerializer.Deserialize<DashboardStore>(plain); }
            finally { CryptographicOperations.ZeroMemory(plain); }
            if (store is not null && (store.Phone is null || store.Phone.Name != deviceName))
            {
                DashboardStore next = store with { Phone = new DashboardPhone(deviceId, deviceName, "") };
                byte[] updatedPlain = JsonSerializer.SerializeToUtf8Bytes(next);
                string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    File.WriteAllBytes(temp, ProtectedData.Protect(updatedPlain, null, DataProtectionScope.CurrentUser));
                    File.Move(temp, path, true);
                }
                finally { CryptographicOperations.ZeroMemory(updatedPlain); if (File.Exists(temp)) File.Delete(temp); }
                GlobalStoreChanged?.Invoke();
            }
        }
        catch { }
    }

    public async ValueTask DisposeAsync()
    {
        GlobalStoreChanged -= HandleGlobalStoreChanged;
        if (_app is not null) { await _app.StopAsync(); await _app.DisposeAsync(); }
        _certificate.Dispose();
    }
}
