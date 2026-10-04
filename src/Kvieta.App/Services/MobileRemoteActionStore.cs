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

        // 1. Save to encrypted file for fallback / audit
        if (await Gate.WaitAsync(500, cancellation))
        {
            try
            {
                var action = new MobileRemoteAction(Guid.NewGuid().ToString("N"), command, DateTimeOffset.UtcNow, Applied: false);
                byte[] plain = JsonSerializer.SerializeToUtf8Bytes(action, Json);
                byte[] protectedBytes = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                await File.WriteAllBytesAsync(FilePath, protectedBytes, cancellation);
            }
            catch { }
            finally { Gate.Release(); }
        }

        // 2. Direct ledger persistence
        try
        {
            var usageStore = new JsonUsageStore();
            var ledger = await usageStore.LoadAsync(cancellation);
            switch (command)
            {
                case RemoteSessionCommand.Lock:
                    ledger.RemoteLockActive = true;
                    ledger.State = SessionState.TimeExpired;
                    ledger.LastUpdatedUtc = DateTimeOffset.UtcNow;
                    break;
                case RemoteSessionCommand.Pause:
                    if (ledger.State == SessionState.Active)
                    {
                        ledger.State = SessionState.Paused;
                    }
                    ledger.LastUpdatedUtc = DateTimeOffset.UtcNow;
                    break;
                case RemoteSessionCommand.Resume:
                    ledger.RemoteLockActive = false;
                    if (ledger.State is SessionState.Paused or SessionState.TimeExpired or SessionState.Ready)
                    {
                        ledger.State = SessionState.Active;
                    }
                    ledger.LastUpdatedUtc = DateTimeOffset.UtcNow;
                    break;
            }
            await usageStore.SaveAsync(ledger, cancellation);
        }
        catch { }

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
