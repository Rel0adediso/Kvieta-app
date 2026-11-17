using System;
using System.Runtime.InteropServices;
using System.Windows.Automation;
using Kvieta.App.Services;
using Kvieta.Core.Services;
using System.Windows;

namespace Kvieta.App;

public partial class DopamineBarrierWindow : Window
{
    private static DopamineBarrierWindow? _activeInstance;
    private static readonly object SyncLock = new();

    private readonly nint _browserHwnd;
    private readonly string _currentUrl;
    private readonly bool _isShorts;

    public DopamineBarrierWindow(nint browserHwnd, string url, bool isShorts)
    {
        InitializeComponent();
        _browserHwnd = browserHwnd;
        _currentUrl = url ?? string.Empty;
        _isShorts = isShorts;

        TitleText.Text = T("Kısa Video Akışı Durduruldu", "Short video feed paused");
        DescriptionText.Text = T("Odaklanmak için bir ara ver. Önceki sayfaya veya boş bir sayfaya geçebilirsin.", "Take a moment to refocus. Return to the previous page or open a blank page.");
        ProtectionBadge.Text = T("KISA VİDEO KORUMASI", "SHORT VIDEO PROTECTION");
        BackLabel.Text = T("Önceki Sayfaya Dön", "Go back");
        BlankLabel.Text = T("Boş Sayfaya Geç", "Open blank page");
        HintLabel.Text = T("Ana sayfaya gitmek için Enter'a basabilirsin.", "Press Enter to return home.");
        CloseAction.ToolTip = T("Ana sayfaya dön", "Return home");
        string lower = _currentUrl.ToLowerInvariant();
        if (lower.Contains("instagram.com"))
        {
            GoHomeButtonText.Text = T("Instagram Ana Sayfasına Git", "Go to Instagram home");
        }
        else if (lower.Contains("tiktok.com"))
        {
            GoHomeButtonText.Text = T("Ana Sayfaya Git", "Go home");
        }
        else if (lower.Contains("youtube.com") || lower.Contains("youtu.be"))
        {
            GoHomeButtonText.Text = T("YouTube Ana Sayfasına Git", "Go to YouTube home");
        }
        else if (!_isShorts)
        {
            TitleText.Text = T("Web Sitesi Engellendi", "Website blocked");
            DescriptionText.Text = T("Bu web sitesi kuralların tarafından engellendi. Başka bir sayfaya geçebilirsin.", "Your rules block this website. You can navigate to another page.");
            GoHomeButtonText.Text = T("Başlangıç Sayfasına Git", "Go to a blank page");
        }

        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateLayout();
        PositionOverBrowser();
        GoHomeButton.Focus();
    }

    private void PositionOverBrowser()
    {
        double currentW = ActualWidth > 0 ? ActualWidth : Width;
        double currentH = ActualHeight > 0 ? ActualHeight : 380;

        if (_browserHwnd != 0 && GetWindowRect(_browserHwnd, out RECT rect))
        {
            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;
            if (width > 200 && height > 200)
            {
                Left = rect.Left + (width - currentW) / 2;
                Top = rect.Top + (height - currentH) / 2;
                return;
            }
        }

        Rect workArea = SystemParameters.WorkArea;
        Left = workArea.Left + (workArea.Width - currentW) / 2;
        Top = workArea.Top + (workArea.Height - currentH) / 2;
    }

    private void GoHome_Click(object sender, RoutedEventArgs e) => PerformAction(ActionKind.GoHome);
    private void GoBack_Click(object sender, RoutedEventArgs e) => PerformAction(ActionKind.GoBack);
    private void NewTab_Click(object sender, RoutedEventArgs e) => PerformAction(ActionKind.NewTab);
    private void CloseButton_Click(object sender, RoutedEventArgs e) => PerformAction(ActionKind.GoHome);

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Escape)
        {
            PerformAction(ActionKind.GoHome);
        }
        else if (e.Key == System.Windows.Input.Key.Enter)
        {
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
                        string safeUrl = DetermineSafeHomeUrl(_currentUrl);
                        NavigateToUrl(_browserHwnd, safeUrl);
                        break;
                    case ActionKind.GoBack:
                        SendAltLeft(_browserHwnd);
                        break;
                    case ActionKind.NewTab:
                        NavigateToUrl(_browserHwnd, "about:blank");
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
        string lower = currentUrl.ToLowerInvariant();
        if (lower.Contains("youtube.com") || lower.Contains("youtu.be"))
        {
            return "https://www.youtube.com";
        }
        if (lower.Contains("instagram.com"))
        {
            return "https://www.instagram.com";
        }
        if (lower.Contains("tiktok.com"))
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
            if (_activeInstance is not null)
            {
                try
                {
                    if (_activeInstance._browserHwnd == browserHwnd && _activeInstance._currentUrl == url)
                    {
                        _activeInstance.Activate();
                        return;
                    }
                    _activeInstance.Close();
                }
                catch { }
            }

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
    private static extern nint GetForegroundWindow();
    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SendMessageTimeout(nint hwnd, uint msg, nuint wParam, nint lParam, uint flags, uint timeout, out nuint result);
}
