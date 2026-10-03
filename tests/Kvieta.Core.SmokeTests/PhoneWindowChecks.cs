using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Kvieta.App;

internal static class PhoneWindowChecks
{
    public static void Run()
    {
        Exception? failure = null;
        Thread thread = new(() =>
        {
            try
            {
                var app = new Kvieta.App.App();
                app.InitializeComponent();
                // No MainWindow exists: dialog resources must be self-contained.
                var window = new DashboardSharingWindow(null, () => Task.CompletedTask);
                window.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                if (window.FindName("QrBorder") is not Border { Visibility: Visibility.Collapsed } ||
                    window.FindName("RecoveryButton") is not Button { IsEnabled: true })
                    throw new InvalidOperationException("Offline phone dialog must hide QR and retain recovery access.");
                var section = (Expander)window.FindName("RecoverySection");
                if (section.IsExpanded) throw new InvalidOperationException("Recovery details should start closed.");
                section.IsExpanded = true;
                var content = (FrameworkElement)window.Content;
                content.Measure(new Size(540, 720));
                content.Arrange(new Rect(0, 0, 540, 720));
                content.UpdateLayout();
                var bitmap = new RenderTargetBitmap(540, 720, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(content);
                window.Close();
                app.Shutdown();
            }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        if (!thread.Join(TimeSpan.FromSeconds(30))) throw new TimeoutException("Phone dialog initialization hung.");
        if (failure is not null) throw new InvalidOperationException("Phone dialog failed to initialize/render.", failure);
        Console.WriteLine("Phone dialog standalone initialization, offline recovery and render checks passed.");
    }
}
