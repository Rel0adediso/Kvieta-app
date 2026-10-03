using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Kvieta.App.Services;
using Kvieta.Core.Models;

namespace Kvieta.App;

public partial class MobileTimeRequestWindow : Window
{
    public MobileTimeRequestWindow()
    {
        InitializeComponent();
        TitleText.Text = T("Ek süre iste", "Request more time");
        DescriptionText.Text = T("İstediğin süreyi seç. Talep telefonuna gider; süre ancak oradan onaylandıktan sonra bu bilgisayarda eklenir.",
            "Choose a duration. The request goes to the paired phone; time is added only after it is approved and received by this computer.");
        NoteLabel.Text = T("KISA NOT · İSTEĞE BAĞLI", "SHORT NOTE · OPTIONAL");
        Minutes15.Content = "15 " + T("dk", "min"); Minutes30.Content = "30 " + T("dk", "min"); Minutes60.Content = "60 " + T("dk", "min");
        SendButton.Content = T("Talebi gönder", "Send request");
        Select(30);
    }
    public int SelectedMinutes { get; private set; } = 30;
    public string Note => NoteInput.Text;
    private static string T(string tr, string en) => LocalizationService.CurrentLanguage == LanguagePreference.English ? en : tr;
    private void Select_Click(object sender, RoutedEventArgs e) => Select(int.Parse((string)((System.Windows.Controls.Button)sender).Tag));
    private void Select(int minutes)
    {
        SelectedMinutes = minutes;
        SelectedText.Text = T($"{minutes} dakika istenecek.", $"Requesting {minutes} minutes.");
    }
    private void Send_Click(object sender, RoutedEventArgs e) => DialogResult = true;
    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); }
}
