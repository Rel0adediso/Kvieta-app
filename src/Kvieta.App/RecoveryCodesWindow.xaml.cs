using System.Windows;
using System.IO;

namespace Kvieta.App;

public partial class RecoveryCodesWindow : Window
{
    private readonly bool _requiresAcknowledgement;

    public RecoveryCodesWindow(IEnumerable<string> codes, bool requiresAcknowledgement = false)
    {
        InitializeComponent();
        _requiresAcknowledgement = requiresAcknowledgement;
        CodesTextBox.Text = string.Join(Environment.NewLine, codes);
        AcknowledgementBox.Visibility = requiresAcknowledgement ? Visibility.Visible : Visibility.Collapsed;
        DoneButton.IsEnabled = !requiresAcknowledgement;
    }

    public bool WasAcknowledged { get; private set; }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        try { System.Windows.Clipboard.SetText(CodesTextBox.Text); }
        catch { }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        Microsoft.Win32.SaveFileDialog dialog = new()
        {
            Title = Services.LocalizationService.Get("SaveRecoveryCodes"),
            FileName = $"Kvieta-recovery-codes-{DateTime.Now:yyyy-MM-dd}.txt",
            DefaultExt = ".txt",
            Filter = "Text file (*.txt)|*.txt"
        };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            File.WriteAllText(dialog.FileName, CodesTextBox.Text);
            SaveButton.Content = Services.LocalizationService.Get("SavedShort");
        }
        catch (Exception exception)
        {
            System.Windows.MessageBox.Show(
                this,
                $"{Services.LocalizationService.Get("RecoveryCodesSaveFailed")}\n\n{exception.Message}",
                "Kvieta",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void Acknowledgement_Changed(object sender, RoutedEventArgs e) =>
        DoneButton.IsEnabled = !_requiresAcknowledgement || AcknowledgementBox.IsChecked == true;

    private void Done_Click(object sender, RoutedEventArgs e)
    {
        WasAcknowledged = !_requiresAcknowledgement || AcknowledgementBox.IsChecked == true;
        if (_requiresAcknowledgement)
        {
            DialogResult = WasAcknowledged;
        }
        else
        {
            Close();
        }
    }
}
