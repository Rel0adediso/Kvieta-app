using System.Globalization;
using System.Windows;
using System.Windows.Input;
using Kvieta.App.Services;
using Kvieta.Core.Models;

namespace Kvieta.App;

public partial class TemporaryAllowanceWindow : Window
{
    private DateTime _selectedDate = DateTime.Today.AddDays(1);

    public TemporaryAllowanceWindow()
    {
        InitializeComponent();
        UpdateSelectedDate();
    }

    public TemporaryAllowance? Result { get; private set; }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedDate.Date < DateTime.Today ||
            !TimeOnly.TryParseExact(StartInput.TimeText, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly from) ||
            !TimeOnly.TryParseExact(EndInput.TimeText, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly until) ||
            !int.TryParse(MinutesInput.Text.Trim(), out int minutes) || minutes is < 1 or > 1440)
        {
            ErrorText.Text = LocalizationService.CurrentLanguage == LanguagePreference.English
                ? "Choose a valid date, use HH:mm for hours, and enter 1–1440 minutes."
                : "Geçerli bir tarih seç; saatleri HH:mm, ek süreyi 1–1440 dakika olarak gir.";
            return;
        }

        Result = new TemporaryAllowance
        {
            Date = DateOnly.FromDateTime(_selectedDate),
            AllowedFrom = from,
            AllowedUntil = until,
            BonusMinutes = minutes,
            Note = NoteInput.Text.Trim()
        };
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void PreviousDate_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedDate.Date > DateTime.Today)
        {
            _selectedDate = _selectedDate.AddDays(-1);
            UpdateSelectedDate();
        }
    }

    private void NextDate_Click(object sender, RoutedEventArgs e)
    {
        _selectedDate = _selectedDate.AddDays(1);
        UpdateSelectedDate();
    }

    private void TodayDate_Click(object sender, RoutedEventArgs e)
    {
        _selectedDate = DateTime.Today;
        UpdateSelectedDate();
    }

    private void TomorrowDate_Click(object sender, RoutedEventArgs e)
    {
        _selectedDate = DateTime.Today.AddDays(1);
        UpdateSelectedDate();
    }

    private void UpdateSelectedDate()
    {
        CultureInfo culture = LocalizationService.CurrentLanguage == LanguagePreference.English
            ? CultureInfo.GetCultureInfo("en-US")
            : CultureInfo.GetCultureInfo("tr-TR");
        SelectedDateText.Text = _selectedDate.ToString("d MMMM yyyy", culture);
        SelectedDayText.Text = _selectedDate.ToString("dddd", culture);
        PreviousDateButton.IsEnabled = _selectedDate.Date > DateTime.Today;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); }
}
