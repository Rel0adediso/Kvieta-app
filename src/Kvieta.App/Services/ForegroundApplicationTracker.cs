using System.Diagnostics;
using System.Runtime.InteropServices;
using Kvieta.Core.Models;
using Kvieta.Core.Services;

namespace Kvieta.App.Services;

public sealed class ForegroundApplicationTracker
{
    private static readonly HashSet<string> IgnoredProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "LockApp",
        "LogonUI",
        "SearchHost",
        "ShellExperienceHost",
        "StartMenuExperienceHost",
        "ScreenClippingHost",
        "SnippingTool",
        "dwm",
        "SystemSettingsAdminFlows"
    };

    public static bool IsIgnoredProcessName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        string clean = System.IO.Path.GetFileNameWithoutExtension(name).Trim();
        return IgnoredProcessNames.Contains(clean);
    }

    private double _uncommittedSeconds;

    public bool Sample(UsageLedger ledger, TimeSpan elapsed)
    {
        double sampledSeconds = Math.Clamp(elapsed.TotalSeconds, 0, 5);
        _uncommittedSeconds += sampledSeconds;
        long wholeSeconds = (long)Math.Floor(_uncommittedSeconds);
        if (wholeSeconds <= 0)
        {
            return false;
        }

        _uncommittedSeconds -= wholeSeconds;
        string? applicationId = TryGetForegroundApplicationId();
        return applicationId is not null &&
            AwarenessUsageCounter.Accrue(ledger, applicationId, TimeSpan.FromSeconds(wholeSeconds), DateTimeOffset.Now);
    }

    private static string? TryGetForegroundApplicationId()
    {
        nint window = GetForegroundWindow();
        if (window == 0)
        {
            return null;
        }

        _ = GetWindowThreadProcessId(window, out uint processId);
        if (processId == 0 || processId == Environment.ProcessId)
        {
            return null;
        }

        try
        {
            using Process process = Process.GetProcessById((int)processId);
            if (IsIgnoredProcessName(process.ProcessName))
            {
                return null;
            }
            return $"{process.ProcessName}.exe";
        }
        catch
        {
            return null;
        }
    }

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint window, out uint processId);
}
