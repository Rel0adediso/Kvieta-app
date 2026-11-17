using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Kvieta.Core.Models;

namespace Kvieta.App.Services;

/// <summary>Family policy changes need knowledge of the PIN, not just a user-readable relay key.</summary>
public static class MobileParentProof
{
    public static string Content(string room, MobileTimeDecision command) => string.Join("\n",
        "kvieta-parent-policy-v1", room,
        command.Sequence.ToString(CultureInfo.InvariantCulture), command.DecisionId, command.RequestId,
        command.Action, command.GrantedMinutes?.ToString(CultureInfo.InvariantCulture) ?? "",
        command.DecidedAtUtc.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),
        Convert.ToBase64String(Encoding.UTF8.GetBytes(command.PayloadJson ?? "")));

    public static bool Verify(AdminCredential credential, string room, MobileTimeDecision command)
    {
        if (!credential.IsConfigured || credential.IsPublicMarker || string.IsNullOrWhiteSpace(command.ParentProof)) return false;
        byte[] key = [];
        try
        {
            key = Convert.FromBase64String(credential.HashBase64);
            byte[] expected = HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(Content(room, command)));
            return CryptographicOperations.FixedTimeEquals(expected, Convert.FromBase64String(command.ParentProof));
        }
        catch { return false; }
        finally { CryptographicOperations.ZeroMemory(key); }
    }
}

public sealed record MobilePinParameters(string SaltBase64, int Iterations);
