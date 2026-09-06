using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Kvieta.App.Services;
using Kvieta.Core.Services;

namespace Kvieta.App;

public partial class SessionPreviewWindow : Window
{
    private readonly SessionPreviewScenario _scenario = SessionPreviewScenario.Create();

    public SessionPreviewWindow()
    {
        InitializeComponent();
        WarningText.Text = string.Format(LocalizationService.Get("SessionPreviewWarningRemaining"), _scenario.WarningMinutes);
    }

    private void ExampleAction_Click(object sender, RoutedEventArgs e)
    {
        string action = (sender as System.Windows.Controls.Button)?.Tag as string ?? string.Empty;
        ExampleResultText.Text = LocalizationService.Get(
            action == "request-time" ? "SessionPreviewRequestExample" : "SessionPreviewSavedExample");
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape) Close();
    }
}
