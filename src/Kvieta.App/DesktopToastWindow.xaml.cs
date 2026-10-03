using System.Collections.Concurrent;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Kvieta.App;

public partial class DesktopToastWindow : Window
{
    private static readonly ConcurrentDictionary<string, DateTime> LastToastTimes = new();
    private static DesktopToastWindow? _currentToast;
    private readonly DispatcherTimer _autoCloseTimer;

    public DesktopToastWindow(string title, string message, string icon = "🛡️")
    {
        InitializeComponent();

        TitleText.Text = title;
        MessageText.Text = message;
        IconText.Text = icon;

        Loaded += OnLoaded;

        _autoCloseTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(4.5)
        };
        _autoCloseTimer.Tick += (_, _) => CloseWithFade();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Rect workArea = SystemParameters.WorkArea;
        Left = workArea.Right - ActualWidth - 20;
        Top = workArea.Bottom - ActualHeight - 20;

        _autoCloseTimer.Start();

        var fadeIn = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(250));
        BeginAnimation(OpacityProperty, fadeIn);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        CloseWithFade();
    }

    private void CloseWithFade()
    {
        _autoCloseTimer.Stop();
        var fadeOut = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(200));
        fadeOut.Completed += (_, _) =>
        {
            try { Close(); }
            catch { }
        };
        BeginAnimation(OpacityProperty, fadeOut);
    }

    public static void ShowToast(string title, string message, string icon = "🛡️")
    {
        string throttleKey = $"{title}:{message}";
        DateTime now = DateTime.UtcNow;

        if (LastToastTimes.TryGetValue(throttleKey, out DateTime lastTime))
        {
            if ((now - lastTime).TotalSeconds < 15) return;
        }
        LastToastTimes[throttleKey] = now;

        System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            try
            {
                if (_currentToast is { IsLoaded: true })
                {
                    _currentToast.Close();
                }

                _currentToast = new DesktopToastWindow(title, message, icon);
                _currentToast.Show();
            }
            catch
            {
            }
        });
    }
}
