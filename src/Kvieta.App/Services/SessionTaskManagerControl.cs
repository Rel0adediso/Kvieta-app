using Microsoft.Win32;

namespace Kvieta.App.Services;

public static class SessionTaskManagerControl
{
    private const string PoliciesSubKey = @"Software\Microsoft\Windows\CurrentVersion\Policies\System";
    private const string DisableTaskMgrValue = "DisableTaskMgr";

    public static void SetTaskManagerDisabled(bool disabled)
    {
        if (!OperatingSystem.IsWindows()) return;

        try
        {
            using RegistryKey key = Registry.CurrentUser.CreateSubKey(PoliciesSubKey);
            if (disabled)
            {
                key.SetValue(DisableTaskMgrValue, 1, RegistryValueKind.DWord);
            }
            else
            {
                key.DeleteValue(DisableTaskMgrValue, throwOnMissingValue: false);
            }
        }
        catch
        {
            // Best effort policy application
        }
    }
}
