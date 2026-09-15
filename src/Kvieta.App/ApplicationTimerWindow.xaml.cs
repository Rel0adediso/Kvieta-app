using System.Windows;
using System.Windows.Input;
using Kvieta.Core.Models;

namespace Kvieta.App;

public partial class ApplicationTimerWindow : Window
{
    public ApplicationTimerWindow() => InitializeComponent();

    public AppRuleMode? SelectedMode { get; private set; }

    private void SelectMode_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button { Tag: string modeName } ||
            !Enum.TryParse(modeName, out AppRuleMode mode))
        {
            return;
        }

        SelectedMode = mode;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }
}
