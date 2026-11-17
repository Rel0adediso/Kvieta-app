using Microsoft.Win32;

namespace Kvieta.App.Services;

public static class SessionTaskManagerControl
{
    private const string PoliciesSubKey = @"Software\Microsoft\Windows\CurrentVersion\Policies\System";
    private const string DisableTaskMgrValue = "DisableTaskMgr";

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
            using RegistryPolicyOwner owner = new(userRoot ?? Registry.CurrentUser, @"Software\Kvieta\TaskManagerOwnership");
            owner.Set(PoliciesSubKey, DisableTaskMgrValue, disabled ? 1 : null, RegistryValueKind.DWord);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
