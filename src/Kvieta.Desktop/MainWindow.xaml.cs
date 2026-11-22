using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using Kvieta.App.Services;
using Kvieta.App.ViewModels;
using Kvieta.Core.Models;
using Kvieta.Core.Services;

namespace Kvieta.Desktop;

public partial class MainWindow : Window
{
    private static readonly string DataPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Kvieta");
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
    private readonly DispatcherTimer _updateTimer = new() { Interval = TimeSpan.FromHours(6) };
    private readonly CancellationTokenSource _updateLifetime = new();
    private bool _updateReady;
    private bool _checkingUpdate;
    private bool _initializingUpdates = true;
    private readonly JsonDisplayPreferencesStore _displayPreferences = new(Path.Combine(DataPath, "desktop-display-preferences.json"));
    private ReleaseNotice? _releaseNotice;
    private static ReleaseIdentity DesktopRelease
    {
        get
        {
            var assembly = typeof(MainWindow).Assembly;
            string build = assembly.GetName().Version?.ToString(3) ?? "0.0.0";
            string label = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? build;
            return new(label, build);
        }
    }

    public MainWindow()
    {
        InitializeComponent();
        _updateTimer.Tick += async (_, _) => await PrepareUpdateAsync();
        Closed += (_, _) =>
        {
            _updateTimer.Stop();
            _updateLifetime.Cancel();
        };
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await RefreshAsync();
        try
        {
            _releaseNotice = await _displayPreferences.ObserveReleaseAsync(DesktopRelease);
            WhatsNewRange.Text = ReleaseNotesCatalog.FormatRange(_releaseNotice);
            WhatsNewBanner.Visibility = _releaseNotice.Unread ? Visibility.Visible : Visibility.Collapsed;
        }
        catch { WhatsNewBanner.Visibility = Visibility.Collapsed; }
        AutomaticUpdates.IsEnabled = DesktopUpdates.Available;
        AutomaticUpdates.IsChecked = DesktopUpdates.Enabled && AutomaticUpdates.IsEnabled;
        _initializingUpdates = false;
        RefreshUpdateStatus();
        if (AutomaticUpdates.IsEnabled)
        {
            _updateTimer.Start();
            await PrepareUpdateAsync();
        }
    }
    private async void Refresh_Click(object sender, RoutedEventArgs e) => await RefreshAsync();

    private async void WhatsNew_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var notice = _releaseNotice ?? new ReleaseNotice(DesktopRelease, null, false);
            new Kvieta.App.WhatsNewWindow(notice) { Owner = this }.ShowDialog();
            await DismissNoticeAsync();
        }
        catch { UpdateStatus.Text = LocalizationService.Get("WhatsNewUnavailable"); }
    }

    private async void DismissWhatsNew_Click(object sender, RoutedEventArgs e) => await DismissNoticeAsync();

    private async Task DismissNoticeAsync()
    {
        try
        {
            await _displayPreferences.AcknowledgeReleaseAsync(DesktopRelease);
            WhatsNewBanner.Visibility = Visibility.Collapsed;
            if (_releaseNotice != null) _releaseNotice = _releaseNotice with { Unread = false };
        }
        catch { UpdateStatus.Text = LocalizationService.Get("WhatsNewUnavailable"); }
    }

    private bool English => LocalizationService.CurrentLanguage == LanguagePreference.English;
    private string L(string tr, string en) => English ? en : tr;

    private void RefreshUpdateStatus()
    {
        AutomaticUpdates.Content = L("Otomatik masaüstü güncellemeleri", "Automatic desktop updates");
        UpdateStatus.Text = !AutomaticUpdates.IsEnabled
            ? L("Bu kurulum için imzalı masaüstü güncelleme kanalı hazır değil.", "The signed desktop update channel is not available for this installation.")
            : _updateReady && DesktopUpdates.Enabled
                ? L("Güncelleme hazır. Sonraki açılışta uygulanacak.", "Update ready for the next launch.")
                : DesktopUpdates.Enabled ? L("Otomatik güncellemeler açık.", "Automatic updates enabled.")
                : L("Otomatik güncellemeler kapalı.", "Automatic updates disabled.");
    }

    private async void AutomaticUpdates_Changed(object sender, RoutedEventArgs e)
    {
        if (_initializingUpdates || !AutomaticUpdates.IsEnabled) return;
        try
        {
            DesktopUpdates.SetEnabled(AutomaticUpdates.IsChecked == true);
            RefreshUpdateStatus();
            await PrepareUpdateAsync();
        }
        catch (IOException) { UpdateStatus.Text = L("Güncelleme tercihi kaydedilemedi.", "Could not save the update preference."); }
        catch (UnauthorizedAccessException) { UpdateStatus.Text = L("Güncelleme tercihi kaydedilemedi.", "Could not save the update preference."); }
    }

    private async Task PrepareUpdateAsync()
    {
        if (_checkingUpdate || !DesktopUpdates.Enabled || _updateLifetime.IsCancellationRequested) return;
        _checkingUpdate = true;
        try
        {
            _updateReady = await DesktopUpdates.PrepareAsync(_updateLifetime.Token) || _updateReady;
            RefreshUpdateStatus();
        }
        catch (OperationCanceledException) when (_updateLifetime.IsCancellationRequested) { }
        catch
        {
            UpdateStatus.Text = L("Güncelleme denetlenemedi. Mevcut sürümü kullanabilirsin.", "Could not check updates. You can continue using the current version.");
        }
        finally { _checkingUpdate = false; }
    }

    private async Task RefreshAsync()
    {
        RefreshButton.IsEnabled = false;
        try
        {
            Applications.ItemsSource = null;
            string protectedSettings = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Kvieta", "protected-settings.json");
            if (File.Exists(protectedSettings) || !File.Exists(Path.Combine(DataPath, "settings.json")))
            {
                Summary.Text = "Kullanım özetini veya ayarları Kontrol Merkezi üzerinden açabilirsin.";
                return;
            }
            var settings = JsonSerializer.Deserialize<ControlSettings>(
                await File.ReadAllTextAsync(Path.Combine(DataPath, "settings.json")), ReadOptions);
            if (settings is null || settings.RequiresGuardian || !settings.SetupCompleted)
            {
                Summary.Text = "Bu profilin verilerine Kontrol Merkezi üzerinden erişebilirsin.";
                return;
            }
            LocalizationService.SetLanguage(Application.Current, settings.Language);
            bool english = settings.Language == LanguagePreference.English;
            Heading.Text = english ? "Today" : "Bugün";
            RefreshButton.Content = english ? "Refresh" : "Yenile";
            SettingsButton.Content = english ? "Control center" : "Kontrol Merkezi";
            string path = Path.Combine(DataPath, "usage.json");
            UsageLedger? ledger = File.Exists(path)
                ? JsonSerializer.Deserialize<UsageLedger>(await File.ReadAllTextAsync(path), ReadOptions)
                : null;
            if (ledger == null || ledger.LocalDay != DateOnly.FromDateTime(DateTime.Today))
            {
                Summary.Text = english ? "Your usage will appear here as it is recorded." : "Kullanım kaydedildikçe burada görünecek.";
                return;
            }
            Summary.Text = UsageHistoryFormatting.FormatDuration(ledger.AwarenessUsedSeconds);
            Applications.ItemsSource = ledger.ForegroundAppUsedSeconds
                .Where(item => item.Value > 0)
                .OrderByDescending(item => item.Value)
                .Select((item, index) => new AppUsageHistoryRow
                {
                    Rank = index + 1,
                    Name = item.Key,
                    UsedSeconds = item.Value,
                    FallbackBrush = ApplicationIconProvider.GetFallbackBrush(item.Key)
                }).ToArray();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            Summary.Text = "Kullanım kaydı okunamadı. Daha sonra yeniden dene.";
        }
        finally { RefreshButton.IsEnabled = true; }
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Kvieta", "Kvieta.exe");
        if (File.Exists(path)) Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }
}
