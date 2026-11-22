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
    private WebAddressPolicy.BrowserObservation? _previousObservation;

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

        if (settings is null) { ClearObservation(); return; }
        bool checkShorts = settings.WebGuardEnabled && settings.BlockShortFormVideo;
        List<string>? blockedDomains = settings.WebGuardEnabled ? settings.BlockedWebDomains : null;

        if (!checkShorts && (blockedDomains is null || blockedDomains.Count == 0))
        {
            ClearObservation();
            return;
        }

        nint hwnd = GetForegroundWindow();
        if (hwnd == 0) { ClearObservation(); return; }

        _ = GetWindowThreadProcessId(hwnd, out uint pid);
        if (pid == 0 || pid == Environment.ProcessId) { ClearObservation(); return; }

        string processName;
        try
        {
            using Process proc = Process.GetProcessById((int)pid);
            processName = proc.ProcessName;
        }
        catch
        {
            ClearObservation();
            return;
        }

        string cleanName = Path.GetFileNameWithoutExtension(processName).ToLowerInvariant();
        if (!BrowserProcessNames.Contains(cleanName))
        {
            ClearObservation();
            return;
        }

        string? url = TryExtractUrl(hwnd, pid, cleanName);
        if (string.IsNullOrWhiteSpace(url) || !IsCurrentBrowserWindow(hwnd, pid))
        {
            ClearObservation();
            return;
        }

        if (WebAddressPolicy.Parse(url) is not { } parsed)
        {
            ClearObservation();
            return;
        }
        string lowerUrl = parsed.AbsoluteUri;
        DateTimeOffset observedAt = DateTimeOffset.UtcNow;
        var observation = new WebAddressPolicy.BrowserObservation(hwnd, pid, lowerUrl, observedAt);
        bool stable = _previousObservation is { } previous && observation.Confirms(previous);
        _previousObservation = observation;
        if (!stable) return;

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
                    lock (_gate)
                    {
                        if (_isDisposed || !ReferenceEquals(_settings, settings)) return;
                    }
                    if (DateTimeOffset.UtcNow - observedAt <= TimeSpan.FromMilliseconds(600) && IsCurrentBrowserWindow(hwnd, pid))
                        DopamineBarrierWindow.ShowBarrier(hwnd, lowerUrl, isShorts);
                });
            }
            catch
            {
            }
        }
    }

    private void ClearObservation()
    {
        _previousObservation = null;
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

    private static bool IsCurrentBrowserWindow(nint hwnd, uint expectedPid)
    {
        return GetForegroundWindow() == hwnd &&
            GetWindowThreadProcessId(hwnd, out uint actualPid) != 0 &&
            actualPid == expectedPid;
    }

    private static bool IsBrowserAddressElement(AutomationElement element, uint expectedPid)
    {
        return element.Current.ProcessId == (int)expectedPid &&
            !element.Current.IsOffscreen &&
            !element.Current.HasKeyboardFocus &&
            element.Current.ControlType == ControlType.Edit;
    }

    private static string? TryExtractUrl(nint hwnd, uint expectedPid, string browserProcess)
    {
        try
        {
            AutomationElement window = AutomationElement.FromHandle(hwnd);
            if (window is null || window.Current.ProcessId != (int)expectedPid) return null;

            if (browserProcess.Equals("firefox", StringComparison.OrdinalIgnoreCase))
            {
                System.Windows.Automation.Condition condition = new PropertyCondition(AutomationElement.AutomationIdProperty, "urlbar-input");
                AutomationElement? urlBar = window.FindFirst(TreeScope.Descendants, condition);
                if (urlBar is not null && IsBrowserAddressElement(urlBar, expectedPid) && urlBar.TryGetCurrentPattern(ValuePattern.Pattern, out object? valObj) &&
                    valObj is ValuePattern valPat)
                {
                    string address = valPat.Current.Value;
                    return IsBrowserAddressElement(urlBar, expectedPid) ? address : null;
                }
                return null;
            }

            // Chromium (Chrome, Edge, Brave, Opera, Vivaldi)
            System.Windows.Automation.Condition omniboxCondition = new PropertyCondition(AutomationElement.ClassNameProperty, "OmniboxViewViews");
            AutomationElement? omnibox = window.FindFirst(TreeScope.Descendants, omniboxCondition);

            if (omnibox is not null && IsBrowserAddressElement(omnibox, expectedPid))
            {
                if (omnibox.TryGetCurrentPattern(ValuePattern.Pattern, out object? patObj) &&
                    patObj is ValuePattern pattern)
                {
                    string address = pattern.Current.Value;
                    return IsBrowserAddressElement(omnibox, expectedPid) ? address : null;
                }

                if (omnibox.TryGetCurrentPattern(TextPattern.Pattern, out object? txtObj) &&
                    txtObj is TextPattern textPattern)
                {
                    string? address = textPattern.DocumentRange.GetText(-1)?.Trim();
                    return IsBrowserAddressElement(omnibox, expectedPid) ? address : null;
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
