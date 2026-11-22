using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Kvieta.Core.Models;
using Kvieta.Core.Services;

namespace Kvieta.App.Services;

/// <summary>Family policy changes need knowledge of the PIN, not just a user-readable relay key.</summary>
public static class MobileParentProof
{
    public static string Content(string room, MobileTimeDecision command) => string.Join("\n",
        "kvieta-parent-policy-v2", room,
        command.Sequence.ToString(CultureInfo.InvariantCulture), command.DecisionId, command.RequestId,
        command.Action, command.GrantedMinutes?.ToString(CultureInfo.InvariantCulture) ?? "",
        command.DecidedAtUtc.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),
        Convert.ToBase64String(Encoding.UTF8.GetBytes(command.PayloadJson ?? "")));

    public static string? PublicKey(string? privateKey)
    {
        if (privateKey is null) return null;
        using RSA rsa = RSA.Create();
        byte[] bytes = [];
        try
        {
            bytes = Convert.FromBase64String(privateKey);
            rsa.ImportPkcs8PrivateKey(bytes, out _);
            return Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo());
        }
        catch (Exception exception) when (exception is CryptographicException or FormatException) { return null; }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }

    public static bool Verify(AdminCredential credential, string? privateKey, string room, MobileTimeDecision command)
    {
        if (!credential.IsConfigured || credential.IsPublicMarker || privateKey is null ||
            command.ParentProof is not { Length: 344 }) return false;
        byte[] key = [];
        byte[] plaintext = [];
        try
        {
            using RSA rsa = RSA.Create();
            key = Convert.FromBase64String(privateKey);
            rsa.ImportPkcs8PrivateKey(key, out _);
            plaintext = rsa.Decrypt(Convert.FromBase64String(command.ParentProof), RSAEncryptionPadding.OaepSHA256);
            byte[] expected = SHA256.HashData(Encoding.UTF8.GetBytes(Content(room, command)));
            if (plaintext.Length is < 36 or > 40 ||
                !CryptographicOperations.FixedTimeEquals(expected, plaintext.AsSpan(0, 32))) return false;
            string pin = Encoding.ASCII.GetString(plaintext.AsSpan(32));
            return AdminPinService.IsValidFormat(pin) && AdminPinService.Verify(pin, credential);
        }
        catch { return false; }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }
}
