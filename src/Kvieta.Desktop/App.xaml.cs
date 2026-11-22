using System.Windows;
using Kvieta.App.Services;
using Velopack;

namespace Kvieta.Desktop;

public partial class App : Application
{
    private readonly SystemThemeService _theme = new();

    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build().SetAutoApplyOnStartup(false).Run();
        if (args.Length == 1 && args[0] == "--verify-control-center-ui")
        {
            // Construct the real control center without starting its session or
            // changing protection settings. This is not a production entry point.
            var controlApplication = new Kvieta.App.App();
            controlApplication.InitializeComponent();
            var controlWindow = new Kvieta.App.MainWindow();
            if (controlWindow.Content is not FrameworkElement || controlWindow.DataContext is null)
                throw new InvalidOperationException("The full control center did not initialize.");
            return;
        }
        if (!args.Contains("--verify-ui")) DesktopUpdates.ApplyPreparedOnStartup();
        App application = new();
        application.InitializeComponent();
        if (args.Length == 1 && args[0] == "--verify-ui")
        {
            var window = new MainWindow();
            application.Shutdown(window.Content is FrameworkElement ? 0 : 1);
            return;
        }
        application.Run();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _theme.Start(this);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _theme.Dispose();
        base.OnExit(e);
    }
}
