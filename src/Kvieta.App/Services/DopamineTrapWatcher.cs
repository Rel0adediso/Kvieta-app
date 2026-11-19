using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using Kvieta.Core.Models;
using Kvieta.Core.Services;

namespace Kvieta.App.Services;

public sealed class DopamineTrapWatcher : IDisposable
{
    private static readonly Lazy<DopamineTrapWatcher> LazyInstance = new(() => new DopamineTrapWatcher());
    public static DopamineTrapWatcher Instance => LazyInstance.Value;

    private static readonly HashSet<string> BrowserProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "chrome",
        "msedge",
        "brave",
        "opera",
        "opera gx",
        "vivaldi",
        "firefox"
    };

    private readonly object _gate = new();
    private System.Threading.Timer? _timer;
    private ControlSettings? _settings;
    private bool _isDisposed;
    private int _isChecking;
    private DateTimeOffset _lastActionTime = DateTimeOffset.MinValue;
    private nint _lastActionWindow;

    public void UpdateSettings(ControlSettings? settings)
    {
        lock (_gate)
        {
            _settings = settings;
            bool shouldRun = settings is not null && settings.WebGuardEnabled && (settings.BlockShortFormVideo || settings.BlockedWebDomains?.Count > 0);

            if (shouldRun && _timer is null && !_isDisposed)
            {
                // Poll foreground browser every 600ms
                _timer = new System.Threading.Timer(OnTimerTick, null, TimeSpan.FromMilliseconds(600), TimeSpan.FromMilliseconds(600));
            }
            else if (!shouldRun && _timer is not null)
            {
                _timer.Dispose();
                _timer = null;
                System.Windows.Application.Current?.Dispatcher?.InvokeAsync(DopamineBarrierWindow.CloseIfActive);
            }
        }
    }

    private void OnTimerTick(object? state)
    {
        if (_isDisposed || Interlocked.Exchange(ref _isChecking, 1) != 0) return;

        try
        {
            CheckForegroundBrowser();
        }
        catch
        {
        }
        finally
        {
            Interlocked.Exchange(ref _isChecking, 0);
        }
    }

    private void CheckForegroundBrowser()
    {
        ControlSettings? settings;
        lock (_gate)
        {
            settings = _settings;
        }

        if (settings is null) return;
        bool checkShorts = settings.WebGuardEnabled && settings.BlockShortFormVideo;
        List<string>? blockedDomains = settings.WebGuardEnabled ? settings.BlockedWebDomains : null;

        if (!checkShorts && (blockedDomains is null || blockedDomains.Count == 0))
        {
            return;
        }

        nint hwnd = GetForegroundWindow();
        if (hwnd == 0) return;

        _ = GetWindowThreadProcessId(hwnd, out uint pid);
        if (pid == 0 || pid == Environment.ProcessId) return;

        string processName;
        try
        {
            using Process proc = Process.GetProcessById((int)pid);
            processName = proc.ProcessName;
        }
        catch
        {
            return;
        }

        string cleanName = Path.GetFileNameWithoutExtension(processName).ToLowerInvariant();
        if (!BrowserProcessNames.Contains(cleanName))
        {
            return;
        }

        string? url = TryExtractUrl(hwnd, cleanName);
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        string lowerUrl = url.ToLowerInvariant();

        bool isShorts = checkShorts && MatchesShortVideo(lowerUrl);
        bool isBlockedDomain = blockedDomains is not null && MatchesBlockedDomain(lowerUrl, blockedDomains);

        if (isShorts || isBlockedDomain)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (hwnd == _lastActionWindow && (now - _lastActionTime).TotalMilliseconds < 2500)
            {
                return;
            }

            _lastActionTime = now;
            _lastActionWindow = hwnd;

            try
            {
                System.Windows.Application.Current?.Dispatcher?.InvokeAsync(() =>
                {
                    DopamineBarrierWindow.ShowBarrier(hwnd, lowerUrl, isShorts);
                });
            }
            catch
            {
            }
        }
    }

    private static bool MatchesShortVideo(string url)
    {
        return WebAddressPolicy.IsShortVideo(url);
    }

    private static bool MatchesBlockedDomain(string url, List<string> blockedDomains)
    {
        foreach (string raw in blockedDomains)
        {
            string domain = WebGuardService.CleanDomain(raw);
            if (!string.IsNullOrWhiteSpace(domain) && WebAddressPolicy.MatchesDomain(url, domain))
            {
                return true;
            }
        }
        return false;
    }

    private static string? TryExtractUrl(nint hwnd, string browserProcess)
    {
        try
        {
            AutomationElement window = AutomationElement.FromHandle(hwnd);
            if (window is null) return null;

            if (browserProcess.Equals("firefox", StringComparison.OrdinalIgnoreCase))
            {
                System.Windows.Automation.Condition condition = new OrCondition(
                    new PropertyCondition(AutomationElement.AutomationIdProperty, "urlbar-input"),
                    new PropertyCondition(AutomationElement.ClassNameProperty, "Edit")
                );
                AutomationElement? urlBar = window.FindFirst(TreeScope.Descendants, condition);
                if (urlBar is not null && urlBar.TryGetCurrentPattern(ValuePattern.Pattern, out object? valObj) &&
                    valObj is ValuePattern valPat)
                {
                    return valPat.Current.Value;
                }
                return null;
            }

            // Chromium (Chrome, Edge, Brave, Opera, Vivaldi)
            System.Windows.Automation.Condition omniboxCondition = new PropertyCondition(AutomationElement.ClassNameProperty, "OmniboxViewViews");
            AutomationElement? omnibox = window.FindFirst(TreeScope.Descendants, omniboxCondition);

            if (omnibox is not null)
            {
                if (omnibox.TryGetCurrentPattern(ValuePattern.Pattern, out object? patObj) &&
                    patObj is ValuePattern pattern)
                {
                    return pattern.Current.Value;
                }

                if (omnibox.TryGetCurrentPattern(TextPattern.Pattern, out object? txtObj) &&
                    txtObj is TextPattern textPattern)
                {
                    return textPattern.DocumentRange.GetText(-1)?.Trim();
                }

                string name = omnibox.Current.Name;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    return name;
                }
            }
        }
        catch
        {
        }

        return null;
    }

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

    public void Dispose()
    {
        _isDisposed = true;
        lock (_gate)
        {
            _timer?.Dispose();
            _timer = null;
        }
    }
}
