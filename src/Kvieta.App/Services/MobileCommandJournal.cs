using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace Kvieta.App.Services;

public sealed record MobileCommandReceipt(string Room, string DecisionId, long Sequence, string State, DateTimeOffset UpdatedAtUtc, string? Error = null);

/// <summary>Durable cross-process receipts. Failed commands remain retryable.</summary>
public sealed class MobileCommandJournal(string path)
{
    public static string DefaultPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Kvieta", "mobile-command-receipt.bin");
    public static bool IsFresh(MobileTimeDecision decision, DateTimeOffset now) =>
        !string.IsNullOrWhiteSpace(decision.DecisionId) && decision.DecisionId.Length <= 80 && decision.Sequence > 0 &&
        decision.DecidedAtUtc >= now.AddMinutes(-5) && decision.DecidedAtUtc <= now.AddMinutes(1);

    public MobileCommandReceipt? Current()
    {
        if (!File.Exists(path)) return null;
        byte[] plain = ProtectedData.Unprotect(File.ReadAllBytes(path), null, DataProtectionScope.CurrentUser);
        try { return JsonSerializer.Deserialize<MobileCommandReceipt>(plain) ?? throw new InvalidDataException("Invalid command receipt."); }
        finally { CryptographicOperations.ZeroMemory(plain); }
    }

    public async Task<bool> ProcessAsync(string room, MobileTimeDecision decision, Func<Task<bool>> apply, CancellationToken cancellation = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using FileStream gate = new(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        MobileCommandReceipt? previous = Current();
        if (previous?.Room == room)
        {
            if (decision.Sequence < previous.Sequence || decision.Sequence == previous.Sequence && decision.DecisionId != previous.DecisionId) return false;
            if (decision.DecisionId == previous.DecisionId && previous.State == "Applied") return true;
        }
        if (!IsFresh(decision, DateTimeOffset.UtcNow))
        {
            Write(new(room, decision.DecisionId, decision.Sequence, "Expired", DateTimeOffset.UtcNow, "Command expired; send a new command."));
            return false;
        }
        cancellation.ThrowIfCancellationRequested();
        Write(new(room, decision.DecisionId, decision.Sequence, "Applying", DateTimeOffset.UtcNow));
        bool applied;
        try { applied = await apply(); }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { throw; }
        catch { applied = false; }
        Write(new(room, decision.DecisionId, decision.Sequence, applied ? "Applied" : "Failed", DateTimeOffset.UtcNow,
            applied ? null : "PC could not apply the command. Retrying while the command is valid."));
        return applied;
    }

    private void Write(MobileCommandReceipt receipt)
    {
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        byte[] plain = JsonSerializer.SerializeToUtf8Bytes(receipt);
        try
        {
            byte[] encrypted = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
            using (FileStream file = new(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                file.Write(encrypted);
                file.Flush(true);
            }
            File.Move(temporary, path, true);
        }
        finally { CryptographicOperations.ZeroMemory(plain); if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
