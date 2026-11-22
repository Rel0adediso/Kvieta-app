using System.Windows;

// Load the real control center from a separate executable without starting a
// session, contacting Guardian or changing the user's settings. This probe
// exercises WPF assembly resources before the desktop/Guardian split.
internal static class Program
{
    [STAThread]
    private static int Main()
    {
        var application = new Kvieta.App.App();
        application.InitializeComponent();
        var window = new Kvieta.App.MainWindow();
        if (window.Content is not FrameworkElement || window.DataContext is null)
            throw new InvalidOperationException("The control center did not initialize.");
        Console.WriteLine("The real control center and its resources loaded from a separate process.");
        return 0;
    }
}
