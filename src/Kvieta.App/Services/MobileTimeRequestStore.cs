using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace Kvieta.App.Services;

public enum MobileTimeRequestStatus
{
    Pending,
    ApprovedAwaitingDevice,
    Applying,
    Applied,
    Rejected,
    Expired
}

public sealed record MobileTimeRequest(
    string Id,
    string LocalDay,
    int RequestedMinutes,
    string Note,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    MobileTimeRequestStatus Status,
    int? GrantedMinutes = null,
    string? DecisionId = null,
    DateTimeOffset? DecidedAtUtc = null,
    DateTimeOffset? ClaimedAtUtc = null,
    DateTimeOffset? AppliedAtUtc = null,
    int? BonusMinutesBefore = null,
    int? TargetBonusMinutes = null,
    string? Room = null);

public sealed record MobileTimeDecision(string DecisionId, string RequestId, string Action, int? GrantedMinutes, DateTimeOffset DecidedAtUtc, string? PayloadJson = null);

/// <summary>
/// A single bounded request shared by Kvieta processes. The computer remains the
/// authority: phone decisions are validated here and a session claims/applies a
/// grant exactly once.
/// </summary>
public static class MobileTimeRequestStore
{
    private static readonly MobileTimeRequestRepository Repository = new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Kvieta", "mobile-time-request.bin"),
        () => DateTimeOffset.Now, () => CurrentRoom());
    private static string? CurrentRoom()
    {
        DashboardStore? dashboard = DashboardRelay.Load(DashboardRelay.StorePath);
        return dashboard?.Phone is null ? null : dashboard.Relay?.Client.Room;
    }
    public static MobileTimeRequest? Current() => Repository.Current();
    public static MobileTimeRequest Create(int minutes, string? note) => Repository.Create(minutes, note);
    public static bool AcceptDecision(MobileTimeDecision decision) => Repository.AcceptDecision(decision);
    public static MobileTimeRequest? TryClaimApproved(int currentBonusMinutes) => Repository.TryClaimApproved(currentBonusMinutes);
    public static void MarkApplied(string id) => Repository.MarkApplied(id);
    public static void ReleaseClaim(string id) => Repository.ReleaseClaim(id);

    public static bool IsAvailable()
    {
        try
        {
            DashboardStore? dashboard = DashboardRelay.Load(DashboardRelay.StorePath);
            return dashboard?.Phone is not null && dashboard.Relay is not null;
        }
        catch { return false; }
    }
}

public sealed class MobileTimeRequestRepository(string path, Func<DateTimeOffset> clock, Func<string?> currentRoom)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly string PathValue = path;
    private DateTimeOffset Now => clock();
    private string Today => DateOnly.FromDateTime(Now.DateTime).ToString("yyyy-MM-dd");
    private bool IsCurrent(MobileTimeRequest request) => request.ExpiresAtUtc > Now && request.LocalDay == Today &&
        request.Room is not null && request.Room == currentRoom();

    public MobileTimeRequest? Current()
    {
        using IDisposable mutex = Lock();
        MobileTimeRequest? value = Read();
        if (value is not null && (value.Status is MobileTimeRequestStatus.Pending or MobileTimeRequestStatus.ApprovedAwaitingDevice or MobileTimeRequestStatus.Applying) && !IsCurrent(value))
        {
            value = value with { Status = MobileTimeRequestStatus.Expired };
            Write(value);
        }
        return value;
    }

    public MobileTimeRequest Create(int minutes, string? note)
    {
        if (minutes is < 1 or > 180) throw new ArgumentOutOfRangeException(nameof(minutes));
        string clean = (note ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ').Trim();
        if (clean.Length > 120) clean = clean[..120];
        using IDisposable mutex = Lock();
        MobileTimeRequest? current = Read();
        if (current is not null && (current.Status is MobileTimeRequestStatus.Pending or MobileTimeRequestStatus.ApprovedAwaitingDevice or MobileTimeRequestStatus.Applying) &&
            IsCurrent(current))
            throw new InvalidOperationException("A time request is already active.");
        string room = currentRoom() ?? throw new InvalidOperationException("No paired remote phone.");
        DateTimeOffset now = Now.ToUniversalTime();
        MobileTimeRequest created = new(Guid.NewGuid().ToString("D"), Today,
            minutes, clean, now, now.AddMinutes(30), MobileTimeRequestStatus.Pending, Room: room);
        Write(created);
        return created;
    }

    public bool AcceptDecision(MobileTimeDecision decision)
    {
        using IDisposable mutex = Lock();
        MobileTimeRequest? request = Read();
        if (request is null || request.Id != decision.RequestId || request.Status != MobileTimeRequestStatus.Pending ||
            !IsCurrent(request) ||
            string.IsNullOrWhiteSpace(decision.DecisionId) || decision.DecisionId.Length > 80 ||
            decision.DecidedAtUtc < request.CreatedAtUtc.AddMinutes(-1) || decision.DecidedAtUtc > Now.AddMinutes(2))
            return false;
        if (string.Equals(decision.Action, "reject", StringComparison.OrdinalIgnoreCase))
        {
            Write(request with { Status = MobileTimeRequestStatus.Rejected, DecisionId = decision.DecisionId, DecidedAtUtc = decision.DecidedAtUtc });
            return true;
        }
        if (!string.Equals(decision.Action, "approve", StringComparison.OrdinalIgnoreCase) || decision.GrantedMinutes is not (>= 1 and <= 180))
            return false;
        Write(request with
        {
            Status = MobileTimeRequestStatus.ApprovedAwaitingDevice,
            GrantedMinutes = decision.GrantedMinutes,
            DecisionId = decision.DecisionId,
            DecidedAtUtc = decision.DecidedAtUtc
        });
        return true;
    }

    public MobileTimeRequest? TryClaimApproved(int currentBonusMinutes)
    {
        using IDisposable mutex = Lock();
        MobileTimeRequest? request = Read();
        if (request is null || !IsCurrent(request)) return null;
        bool claimable = request.Status == MobileTimeRequestStatus.ApprovedAwaitingDevice ||
            request.Status == MobileTimeRequestStatus.Applying &&
            (request.ClaimedAtUtc is null || request.ClaimedAtUtc < Now.AddMinutes(-2));
        if (!claimable || request.GrantedMinutes is not (>= 1 and <= 180)) return null;
        int target = request.TargetBonusMinutes ?? Math.Min(1440, checked(Math.Max(0, currentBonusMinutes) + request.GrantedMinutes.Value));
        request = request with
        {
            Status = MobileTimeRequestStatus.Applying,
            ClaimedAtUtc = Now.ToUniversalTime(),
            BonusMinutesBefore = request.BonusMinutesBefore ?? Math.Max(0, currentBonusMinutes),
            TargetBonusMinutes = target
        };
        Write(request);
        return request;
    }

    public void MarkApplied(string id)
    {
        using IDisposable mutex = Lock();
        MobileTimeRequest? request = Read();
        if (request?.Id == id && request.Status == MobileTimeRequestStatus.Applying)
            Write(request with { Status = MobileTimeRequestStatus.Applied, AppliedAtUtc = Now.ToUniversalTime() });
    }

    public void ReleaseClaim(string id)
    {
        using IDisposable mutex = Lock();
        MobileTimeRequest? request = Read();
        if (request?.Id == id && request.Status == MobileTimeRequestStatus.Applying)
            Write(request with { Status = MobileTimeRequestStatus.ApprovedAwaitingDevice, ClaimedAtUtc = null });
    }

    private IDisposable Lock()
    {
        Mutex mutex = new(false, "Local\\Kvieta.MobileTimeRequest." + RelayCredentials.HashToken(Path.GetFullPath(PathValue)));
        bool acquired = false;
        try { acquired = mutex.WaitOne(TimeSpan.FromSeconds(5)); }
        catch (AbandonedMutexException) { acquired = true; }
        if (!acquired) { mutex.Dispose(); throw new TimeoutException("Kvieta time request store is busy."); }
        return new MutexLease(mutex);
    }

    private sealed class MutexLease(Mutex mutex) : IDisposable
    {
        public void Dispose()
        {
            try { mutex.ReleaseMutex(); }
            finally { mutex.Dispose(); }
        }
    }

    private MobileTimeRequest? Read()
    {
        if (!File.Exists(PathValue)) return null;
        byte[] plain = ProtectedData.Unprotect(File.ReadAllBytes(PathValue), null, DataProtectionScope.CurrentUser);
        try { return JsonSerializer.Deserialize<MobileTimeRequest>(plain, Json); }
        finally { CryptographicOperations.ZeroMemory(plain); }
    }

    private void Write(MobileTimeRequest value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(PathValue)!);
        byte[] plain = JsonSerializer.SerializeToUtf8Bytes(value, Json);
        string temporary = PathValue + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllBytes(temporary, ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser));
            File.Move(temporary, PathValue, true);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plain);
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
