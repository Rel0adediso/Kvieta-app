using System.Windows;
using System.Windows.Controls;

namespace Kvieta.App.Controls;

public partial class TodayDashboard : System.Windows.Controls.UserControl
{
    public TodayDashboard() => InitializeComponent();

    public event RoutedEventHandler? PrimaryActionRequested;
    public event RoutedEventHandler? HistoryRequested;

    private void History_Click(object sender, RoutedEventArgs e) => HistoryRequested?.Invoke(this, e);

    private void PrimaryAction_Click(object sender, RoutedEventArgs e) => PrimaryActionRequested?.Invoke(this, e);
}
