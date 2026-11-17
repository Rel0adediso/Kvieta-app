using System;
using System.Runtime.InteropServices;
using System.Threading;
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

        string lower = _currentUrl.ToLowerInvariant();
        if (lower.Contains("instagram.com"))
        {
            GoHomeButtonText.Text = "Instagram Ana Sayfasına Git";
        }
        else if (lower.Contains("tiktok.com"))
        {
            GoHomeButtonText.Text = "Ana Sayfaya Git";
        }
        else if (lower.Contains("youtube.com") || lower.Contains("youtu.be"))
        {
            GoHomeButtonText.Text = "YouTube Ana Sayfasına Git";
        }
        else if (!_isShorts)
        {
            TitleText.Text = "Web Sitesi Engellendi";
            DescriptionText.Text = "Bu web sitesi Kvieta Web Koruması kurallarına göre engellendi. Tarayıcın kapatılmadı; dilediğin yöne geçebilirsin.";
            GoHomeButtonText.Text = "Başlangıç Sayfasına Git";
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

    private void PerformAction(ActionKind action)
    {
        try
        {
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
        }
        catch
        {
        }
        finally
        {
            try { Close(); } catch { }
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
        SetForegroundWindow(hwnd);
        Thread.Sleep(80);

        string? previousClipboard = null;
        try
        {
            if (System.Windows.Clipboard.ContainsText())
            {
                previousClipboard = System.Windows.Clipboard.GetText();
            }
        }
        catch { }

        try
        {
            System.Windows.Clipboard.SetText(url);

            // Ctrl + L (Focus address bar)
            SendKeyCombo(0x11, 0x4C);
            Thread.Sleep(80);

            // Ctrl + V (Paste URL)
            SendKeyCombo(0x11, 0x56);
            Thread.Sleep(80);

            // Enter (Navigate)
            SendSingleKey(0x0D);
            Thread.Sleep(120);
        }
        catch { }
        finally
        {
            if (previousClipboard is not null)
            {
                try { System.Windows.Clipboard.SetText(previousClipboard); } catch { }
            }
        }
    }

    private static void SendAltLeft(nint hwnd)
    {
        SetForegroundWindow(hwnd);
        Thread.Sleep(80);
        SendKeyCombo(0x12, 0x25); // Alt + Left
    }

    private static void SendKeyCombo(byte modifierKey, byte key)
    {
        const uint KEYEVENTF_KEYUP = 0x0002;
        keybd_event(modifierKey, 0, 0, 0);
        keybd_event(key, 0, 0, 0);
        keybd_event(key, 0, KEYEVENTF_KEYUP, 0);
        keybd_event(modifierKey, 0, KEYEVENTF_KEYUP, 0);
    }

    private static void SendSingleKey(byte key)
    {
        const uint KEYEVENTF_KEYUP = 0x0002;
        keybd_event(key, 0, 0, 0);
        keybd_event(key, 0, KEYEVENTF_KEYUP, 0);
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
                    _activeInstance.Activate();
                    _activeInstance.Topmost = true;
                }
                catch { }
                return;
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
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, nuint dwExtraInfo);
}
