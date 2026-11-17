using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
using Kvieta.Core.Models;
using Kvieta.Core.Services;

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
    private static readonly string DefaultFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Kvieta", "mobile-remote-action.bin");
    public static string? OverrideFilePath { get; set; }
    private static string EffectiveFilePath => OverrideFilePath ?? DefaultFilePath;
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static async Task<bool> RecordActionAsync(string? payload, CancellationToken cancellation = default, string? decisionId = null)
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

        if (!Enum.TryParse<RemoteSessionCommand>(commandText, true, out var command) || !Enum.IsDefined(command)) return false;

        if (!await Gate.WaitAsync(500, cancellation)) return false;
        try
        {
            string commandId = decisionId ?? Guid.NewGuid().ToString("N");
            var usageStore = OverrideFilePath is null ? new JsonUsageStore() :
                new JsonUsageStore(Path.Combine(Path.GetDirectoryName(EffectiveFilePath)!, "test-usage.json"));
            bool duplicate = false;
            await usageStore.UpdateAsync(ledger =>
            {
                if (ledger.LastRemoteCommandId == commandId) { duplicate = true; return ledger; }
                switch (command)
                {
                    case RemoteSessionCommand.Lock: ledger.RemoteLockActive = true; ledger.State = SessionState.TimeExpired; break;
                    case RemoteSessionCommand.Pause: if (ledger.State == SessionState.Active) ledger.State = SessionState.Paused; break;
                    case RemoteSessionCommand.Resume:
                        ledger.RemoteLockActive = false;
                        if (ledger.State == SessionState.TimeExpired) ledger.State = SessionState.Ready;
                        break;
                }
                ledger.LastRemoteCommandId = commandId;
                ledger.LastUpdatedUtc = DateTimeOffset.UtcNow;
                return ledger;
            }, cancellation);
            if (duplicate) return true;
            var action = new MobileRemoteAction(commandId, command, DateTimeOffset.UtcNow, Applied: false);
            byte[] plain = JsonSerializer.SerializeToUtf8Bytes(action, Json);
            byte[] encrypted = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
            Directory.CreateDirectory(Path.GetDirectoryName(EffectiveFilePath)!);
            string temporary = EffectiveFilePath + ".tmp";
            await File.WriteAllBytesAsync(temporary, encrypted, cancellation);
            File.Move(temporary, EffectiveFilePath, true);
        }
        catch { return false; }
        finally { Gate.Release(); }

        // Custom repositories are used for isolated storage checks and must not affect the host desktop.
        if (OverrideFilePath is not null) return true;

        // 3. Immediate system actions
        try
        {
            switch (command)
            {
                case RemoteSessionCommand.Lock:
                    SystemMediaController.StopPlayback();
                    DesktopToastWindow.ShowToast("Uzaktan Yönetim", "Bilgisayar telefondan uzaktan kilitlendi.", "🔒");
                    SystemPowerController.LockWindows();
                    break;

                case RemoteSessionCommand.Pause:
                    SystemMediaController.StopPlayback();
                    DesktopToastWindow.ShowToast("Uzaktan Yönetim", "Telefondan oturuma mola verildi.", "☕");
                    break;

                case RemoteSessionCommand.Resume:
                    DesktopToastWindow.ShowToast("Uzaktan Yönetim", "Oturum kilidi telefondan açıldı.", "▶️");
                    break;
            }
        }
        catch { }

        // 4. Dispatch to active UI surfaces
        try
        {
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    foreach (Window window in System.Windows.Application.Current.Windows)
                    {
                        if (window is SessionSurfaceWindow surface)
                        {
                            switch (command)
                            {
                                case RemoteSessionCommand.Lock:
                                    surface.ApplyRemoteLock();
                                    break;
                                case RemoteSessionCommand.Pause:
                                    surface.ApplyRemotePause();
                                    break;
                                case RemoteSessionCommand.Resume:
                                    surface.ApplyRemoteResume();
                                    break;
                            }
                        }
                    }
                }
                catch { }
            });
        }
        catch { }

        return true;
    }

    public static MobileRemoteAction? TryClaimPending()
    {
        if (!File.Exists(EffectiveFilePath)) return null;
        if (!Gate.Wait(500)) return null;
        try
        {
            byte[] protectedBytes = File.ReadAllBytes(EffectiveFilePath);
            byte[] plain = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
            var action = JsonSerializer.Deserialize<MobileRemoteAction>(plain, Json);
            if (action is null || action.Applied || DateTimeOffset.UtcNow - action.CreatedAtUtc > TimeSpan.FromMinutes(5))
            {
                return null;
            }

            var appliedAction = action with { Applied = true };
            byte[] updatedPlain = JsonSerializer.SerializeToUtf8Bytes(appliedAction, Json);
            byte[] updatedProtected = ProtectedData.Protect(updatedPlain, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(EffectiveFilePath, updatedProtected);
            return action;
        }
        catch { return null; }
        finally { Gate.Release(); }
    }
}
