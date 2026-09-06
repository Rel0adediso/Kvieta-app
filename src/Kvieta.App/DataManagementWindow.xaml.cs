using System.Windows;
using System.Windows.Input;
using Kvieta.App.Services;
using Kvieta.Core.Services;

namespace Kvieta.App;

public enum DataManagementAction { None, ExportJson, ExportCsv, Delete }

public partial class DataManagementWindow : Window
{
    public DataManagementWindow(DataInventorySummary inventory)
    {
        InitializeComponent();
        bool english = LocalizationService.CurrentLanguage == Kvieta.Core.Models.LanguagePreference.English;
        string range = inventory.FirstDay is { } first && inventory.LastDay is { } last
            ? $"{first:yyyy-MM-dd} – {last:yyyy-MM-dd}"
            : (english ? "No usage dates yet" : "Henüz kullanım tarihi yok");
        InventoryText.Text = english
            ? $"Usage history: {inventory.DetailedDayCount} days ({range}), retention: {inventory.RetentionDays} days. Rhythm summary: {(inventory.HasRhythmSummary ? "available" : "not created")} ."
            : $"Kullanım geçmişi: {inventory.DetailedDayCount} gün ({range}), saklama: {inventory.RetentionDays} gün. Ritim özeti: {(inventory.HasRhythmSummary ? "var" : "henüz oluşmadı")} .";
        ExportPreviewText.Text = english
            ? $"Exports usage totals and dates{(inventory.IncludesApplicationNames ? ", including application names" : "")}. It excludes PINs, recovery codes, keys, focus intentions, and diagnostics."
            : $"Kullanım toplamları ve tarihleri{(inventory.IncludesApplicationNames ? ", uygulama adlarıyla birlikte" : "")} dışa aktarılır. PIN, kurtarma kodu, anahtar, odak niyeti ve tanılama eklenmez.";
    }

    public DataManagementAction SelectedAction { get; private set; }
    public DataDeletionScope SelectedDeletionScope => RhythmOption.IsChecked == true
        ? DataDeletionScope.RhythmSummary
        : AllUsageOption.IsChecked == true ? DataDeletionScope.UsageAndRhythm : DataDeletionScope.DetailedUsage;

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        SelectedAction = (sender as System.Windows.Controls.Button)?.Tag as string == "csv"
            ? DataManagementAction.ExportCsv : DataManagementAction.ExportJson;
        DialogResult = true;
    }

    private void Delete_Click(object sender, RoutedEventArgs e) { SelectedAction = DataManagementAction.Delete; DialogResult = true; }
    private void Close_Click(object sender, RoutedEventArgs e) { SelectedAction = DataManagementAction.None; DialogResult = false; }
    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) { if (e.Key == Key.Escape) Close(); }
}
