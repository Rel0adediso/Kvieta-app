using System.Windows;
using System.Windows.Controls;

namespace Kvieta.App.Controls;

public partial class TodayDashboard : System.Windows.Controls.UserControl
{
    public TodayDashboard() => InitializeComponent();

    public event RoutedEventHandler? PrimaryActionRequested;

    private void PrimaryAction_Click(object sender, RoutedEventArgs e) => PrimaryActionRequested?.Invoke(this, e);
}
