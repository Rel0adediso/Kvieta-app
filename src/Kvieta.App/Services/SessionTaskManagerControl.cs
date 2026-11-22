using Microsoft.Win32;

namespace Kvieta.App.Services;

public static class SessionTaskManagerControl
{
    private const string PoliciesSubKey = @"Software\Microsoft\Windows\CurrentVersion\Policies\System";
    private const string DisableTaskMgrValue = "DisableTaskMgr";
    private const string OwnershipPath = @"Software\Kvieta\TaskManagerOwnership";

    public static void SetTaskManagerDisabled(bool disabled)
    {
        TrySetTaskManagerDisabled(disabled);
    }

    public static bool TrySetTaskManagerDisabled(bool disabled, string? userSid = null)
    {
        if (!OperatingSystem.IsWindows()) return true;

        try
        {
            using RegistryKey? userRoot = userSid is null ? null : Registry.Users.OpenSubKey(
                new System.Security.Principal.SecurityIdentifier(userSid).Value, writable: true);
            if (userSid is not null && userRoot is null) return false;
            using RegistryPolicyOwner owner = new(userRoot ?? Registry.CurrentUser, OwnershipPath);
            owner.Set(PoliciesSubKey, DisableTaskMgrValue, disabled ? 1 : null, RegistryValueKind.DWord);
            return true;
        }
        catch
        {
            return false;
        }
    }

    internal static bool RestoreLegacyGuardianSession()
    {
        if (!WebGuardService.IsAdministrator() || ProtectionServiceManager.LoadEnrollment() is not { } enrollment)
            return false;

        using RegistryKey? root = Registry.Users.OpenSubKey(
            new System.Security.Principal.SecurityIdentifier(enrollment.UserSid).Value, writable: true);
        if (root is null) return false;
        using RegistryPolicyOwner owner = new(root, OwnershipPath);
        owner.AdoptLegacy(PoliciesSubKey, DisableTaskMgrValue, 1, RegistryValueKind.DWord);
        owner.Set(PoliciesSubKey, DisableTaskMgrValue, null);
        return true;
    }
}
