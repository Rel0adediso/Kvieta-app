using System;
using System.Runtime.InteropServices;
using System.Windows.Automation;
using Kvieta.App.Services;
using Kvieta.Core.Services;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace Kvieta.App;

public partial class DopamineBarrierWindow : Window
{
    private static DopamineBarrierWindow? _activeInstance;
    private static readonly object SyncLock = new();
    private const uint NoActivate = 0x0010;
    private const uint NoZOrder = 0x0004;

    private readonly nint _browserHwnd;
    private readonly string _currentUrl;
    private readonly bool _isShorts;
    private readonly DispatcherTimer _positionTimer = new() { Interval = TimeSpan.FromMilliseconds(300) };
    private readonly CancellationTokenSource _pauseCancellation = new();

    public DopamineBarrierWindow(nint browserHwnd, string url, bool isShorts)
    {
        InitializeComponent();
        _browserHwnd = browserHwnd;
        _currentUrl = url ?? string.Empty;
        _isShorts = isShorts;

        TitleText.Text = T("Kısa Video Molası", "Short video break");
        DescriptionText.Text = T("Odaklanmak için bir ara ver. Önceki sayfaya dönebilir veya tarayıcının başlangıç sayfasına geçebilirsin.", "Take a moment to refocus. Return to the previous page or go to your browser's start page.");
        ProtectionBadge.Text = T("KISA VİDEO KORUMASI", "SHORT VIDEO PROTECTION");
        BackLabel.Text = T("Önceki Sayfaya Dön", "Go back");
        BlankLabel.Text = T("Başlangıç Sayfasına Geç", "Go to browser start page");
        HintLabel.Text = T("Ana sayfaya gitmek için Enter'a basabilirsin.", "Press Enter to return home.");
        CloseAction.ToolTip = T("Uyarıyı kapat", "Dismiss reminder");
        if (WebAddressPolicy.MatchesDomain(_currentUrl, "instagram.com"))
        {
            GoHomeButtonText.Text = T("Instagram Ana Sayfasına Git", "Go to Instagram home");
        }
        else if (WebAddressPolicy.MatchesDomain(_currentUrl, "tiktok.com"))
        {
            GoHomeButtonText.Text = T("Ana Sayfaya Git", "Go home");
        }
        else if (WebAddressPolicy.MatchesDomain(_currentUrl, "youtube.com") || WebAddressPolicy.MatchesDomain(_currentUrl, "youtu.be"))
        {
            GoHomeButtonText.Text = T("YouTube Ana Sayfasına Git", "Go to YouTube home");
        }
        if (!_isShorts)
        {
            ProtectionBadge.Text = T("WEB KORUMASI", "WEB GUARD");
            TitleText.Text = T("Web Sitesi Engellendi", "Website blocked");
            DescriptionText.Text = T("Bu web sitesi kuralların tarafından engellendi. Başka bir sayfaya geçebilirsin.", "Your rules block this website. You can navigate to another page.");
            GoHomeButtonText.Text = T("Başlangıç Sayfasına Git", "Go to a blank page");
        }

        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!PositionOverBrowser())
        {
            Close();
            return;
        }
        Opacity = 1;
        _positionTimer.Tick += PositionTimer_Tick;
        _positionTimer.Start();
        if (_isShorts) _ = RequestBrowserPauseAsync(_pauseCancellation.Token);
        GoHomeButton.Focus();
    }

    private bool PositionOverBrowser()
    {
        if (!IsWindow(_browserHwnd) || IsIconic(_browserHwnd) ||
            !GetWindowRect(_browserHwnd, out RECT rect)) return false;

        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;
        if (width <= 0 || height <= 0) return false;

        // Keep both Win32 calls in the same coordinate space. WPF Left/Top are in DIPs.
        nint overlay = new WindowInteropHelper(this).Handle;
        if (GetWindowRect(overlay, out RECT positioned) && positioned.Equals(rect)) return true;
        if (!SetWindowPos(overlay, 0, rect.Left, rect.Top, width, height, NoActivate | NoZOrder)) return false;
        return true;
    }

    private void PositionTimer_Tick(object? sender, EventArgs e)
    {
        nint foreground = GetForegroundWindow();
        nint overlay = new WindowInteropHelper(this).Handle;
        if ((foreground != _browserHwnd && foreground != overlay) || !PositionOverBrowser()) Close();
    }

    private Task RequestBrowserPauseAsync(CancellationToken cancellation)
    {
        // Use an explicit, visible Pause control exposed by the browser. Some sites
        // do not provide one through accessibility; do not toggle global media playback.
        return Task.Run(() =>
        {
            try
            {
                if (cancellation.IsCancellationRequested || !IsWindow(_browserHwnd)) return;
                AutomationElement browser = AutomationElement.FromHandle(_browserHwnd);
                AutomationElement? document = browser.FindFirst(TreeScope.Descendants,
                    new AndCondition(new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Document),
                        new PropertyCondition(AutomationElement.IsOffscreenProperty, false)));
                if (document is null) return;
                var pauseNames = new OrCondition(
                    new PropertyCondition(AutomationElement.NameProperty, "Pause"),
                    new PropertyCondition(AutomationElement.NameProperty, "Pause (k)"),
                    new PropertyCondition(AutomationElement.NameProperty, "Pause video"),
                    new PropertyCondition(AutomationElement.NameProperty, "Duraklat"),
                    new PropertyCondition(AutomationElement.NameProperty, "Duraklat (k)"),
                    new PropertyCondition(AutomationElement.NameProperty, "Videoyu duraklat"));
                var buttons = document.FindAll(TreeScope.Descendants, new AndCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
                    new PropertyCondition(AutomationElement.IsOffscreenProperty, false),
                    new PropertyCondition(AutomationElement.IsEnabledProperty, true), pauseNames));
                if (buttons.Count == 1 && !cancellation.IsCancellationRequested &&
                    buttons[0].TryGetCurrentPattern(InvokePattern.Pattern, out object? value) && value is InvokePattern pause)
                    pause.Invoke();
            }
            catch (Exception ex) when (ex is ElementNotAvailableException or InvalidOperationException or COMException or UnauthorizedAccessException)
            {
                // A missing or changing media control is not evidence that playback paused.
            }
        }, cancellation);
    }

    private void GoHome_Click(object sender, RoutedEventArgs e) => PerformAction(ActionKind.GoHome);
    private void GoBack_Click(object sender, RoutedEventArgs e) => PerformAction(ActionKind.GoBack);
    private void NewTab_Click(object sender, RoutedEventArgs e) => PerformAction(ActionKind.NewTab);
    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Escape)
        {
            e.Handled = true;
            Close();
        }
        else if (e.Key == System.Windows.Input.Key.Enter && GoHomeButton.IsKeyboardFocused)
        {
            e.Handled = true;
            PerformAction(ActionKind.GoHome);
        }
    }

    private enum ActionKind
    {
        GoHome,
        GoBack,
        NewTab
    }

    private static string T(string tr, string en) => LocalizationService.CurrentLanguage == Kvieta.Core.Models.LanguagePreference.English ? en : tr;

    private void PerformAction(ActionKind action)
    {
        try
        {
            Hide();
            if (_browserHwnd != 0)
            {
                switch (action)
                {
                    case ActionKind.GoHome:
                        string safeUrl = _isShorts ? DetermineSafeHomeUrl(_currentUrl) : "about:blank";
                        NavigateToUrl(_browserHwnd, safeUrl);
                        break;
                    case ActionKind.GoBack:
                        SendAltLeft(_browserHwnd);
                        break;
                    case ActionKind.NewTab:
                        AutomationElement browser = AutomationElement.FromHandle(_browserHwnd);
                        using (var process = System.Diagnostics.Process.GetProcessById(browser.Current.ProcessId))
                            NavigateToUrl(_browserHwnd, WebAddressPolicy.BrowserStartPage(process.ProcessName));
                        break;
                }
            }
            Close();
        }
        catch
        {
            Show();
            DescriptionText.Text = T("Tarayıcıya geçilemedi. Lütfen sayfayı tarayıcıdan değiştir.", "Could not navigate. Please change the page in your browser.");
        }
    }

    private static string DetermineSafeHomeUrl(string currentUrl)
    {
        if (WebAddressPolicy.MatchesDomain(currentUrl, "youtube.com") || WebAddressPolicy.MatchesDomain(currentUrl, "youtu.be"))
        {
            return "https://www.youtube.com";
        }
        if (WebAddressPolicy.MatchesDomain(currentUrl, "instagram.com"))
        {
            return "https://www.instagram.com";
        }
        if (WebAddressPolicy.MatchesDomain(currentUrl, "tiktok.com"))
        {
            return "https://www.google.com";
        }
        return "about:blank";
    }

    private static void NavigateToUrl(nint hwnd, string url)
    {
        // UIA targets the known browser's address control. Never use or overwrite the clipboard.
        if (!IsWindow(hwnd)) throw new InvalidOperationException("Browser window closed.");
        AutomationElement window = AutomationElement.FromHandle(hwnd);
        var condition = new OrCondition(
            new PropertyCondition(AutomationElement.ClassNameProperty, "OmniboxViewViews"),
            new PropertyCondition(AutomationElement.AutomationIdProperty, "urlbar-input"));
        AutomationElement? address = window.FindFirst(TreeScope.Descendants, condition);
        if (address is null || !address.TryGetCurrentPattern(ValuePattern.Pattern, out object? value) || value is not ValuePattern pattern)
            throw new InvalidOperationException("Browser address control unavailable.");
        if (!SetForegroundWindow(hwnd) || GetForegroundWindow() != hwnd)
            throw new InvalidOperationException("Browser focus could not be verified.");
        pattern.SetValue(url);
        // Send navigation only to this window, never to whichever app happens to be foreground.
        SendMessageTimeout(hwnd, 0x0100, 0x0D, 0, 2, 1000, out _);
        SendMessageTimeout(hwnd, 0x0101, 0x0D, 0, 2, 1000, out _);
    }

    private static void SendAltLeft(nint hwnd)
    {
        if (!IsWindow(hwnd)) throw new InvalidOperationException("Browser window closed.");
        SendMessageTimeout(hwnd, 0x0319, (nuint)hwnd, (nint)(1 << 16), 2, 1000, out _);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _pauseCancellation.Cancel();
        _pauseCancellation.Dispose();
        _positionTimer.Stop();
        _positionTimer.Tick -= PositionTimer_Tick;
        lock (SyncLock)
        {
            if (_activeInstance == this)
            {
                _activeInstance = null;
            }
        }
    }

    public static void ShowBarrier(nint browserHwnd, string url, bool isShorts)
    {
        lock (SyncLock)
        {
            if (GetForegroundWindow() != browserHwnd) return;
            if (_activeInstance is not null)
            {
                try
                {
                    if (_activeInstance._browserHwnd == browserHwnd && _activeInstance._currentUrl == url)
                    {
                        return;
                    }
                    _activeInstance.Close();
                }
                catch { }
            }

            if (GetForegroundWindow() != browserHwnd) return;
            var barrier = new DopamineBarrierWindow(browserHwnd, url, isShorts);
            _activeInstance = barrier;
            barrier.Show();
        }
    }

    public static void CloseIfActive()
    {
        lock (SyncLock)
        {
            if (_activeInstance is not null)
            {
                try { _activeInstance.Close(); } catch { }
                _activeInstance = null;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(nint hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(nint hwnd);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(nint hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(nint hwnd, nint insertAfter, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();
    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SendMessageTimeout(nint hwnd, uint msg, nuint wParam, nint lParam, uint flags, uint timeout, out nuint result);
}
