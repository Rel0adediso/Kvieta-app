using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace Kvieta.App.Services;

public enum RemoteSessionCommand
{
    Lock,
    Pause,
    Resume
}

public sealed record MobileRemoteAction(
    string Id,
    RemoteSessionCommand Command,
    DateTimeOffset CreatedAtUtc,
    bool Applied = false);

public static class MobileRemoteActionStore
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Kvieta", "mobile-remote-action.bin");
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static async Task<bool> RecordActionAsync(string? payload, CancellationToken cancellation = default)
    {
        if (string.IsNullOrWhiteSpace(payload)) return false;
        string commandText = payload.Trim().Trim('"');
        try
        {
            using var doc = JsonDocument.Parse(payload);
            if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("command", out var prop))
            {
                commandText = prop.GetString() ?? commandText;
            }
        }
        catch { /* payload can be plain string or JSON object */ }

        if (!Enum.TryParse<RemoteSessionCommand>(commandText, true, out var command)) return false;
        if (!await Gate.WaitAsync(500, cancellation)) return false;
        try
        {
            var action = new MobileRemoteAction(Guid.NewGuid().ToString("N"), command, DateTimeOffset.UtcNow, Applied: false);
            byte[] plain = JsonSerializer.SerializeToUtf8Bytes(action, Json);
            byte[] protectedBytes = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            await File.WriteAllBytesAsync(FilePath, protectedBytes, cancellation);
            return true;
        }
        catch { return false; }
        finally { Gate.Release(); }
    }

    public static MobileRemoteAction? TryClaimPending()
    {
        if (!File.Exists(FilePath)) return null;
        if (!Gate.Wait(0)) return null;
        try
        {
            byte[] protectedBytes = File.ReadAllBytes(FilePath);
            byte[] plain = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
            var action = JsonSerializer.Deserialize<MobileRemoteAction>(plain, Json);
            if (action is null || action.Applied || DateTimeOffset.UtcNow - action.CreatedAtUtc > TimeSpan.FromMinutes(5))
            {
                return null;
            }

            var appliedAction = action with { Applied = true };
            byte[] updatedPlain = JsonSerializer.SerializeToUtf8Bytes(appliedAction, Json);
            byte[] updatedProtected = ProtectedData.Protect(updatedPlain, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(FilePath, updatedProtected);
            return action;
        }
        catch { return null; }
        finally { Gate.Release(); }
    }
}
