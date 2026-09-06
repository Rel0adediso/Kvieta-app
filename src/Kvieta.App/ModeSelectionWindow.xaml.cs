using System.Windows;
using System.Windows.Input;
using Kvieta.Core.Models;
using Kvieta.Core.Services;
using Kvieta.App.Services;

namespace Kvieta.App;

public partial class ModeSelectionWindow : Window
{
    private readonly ControlSettings _currentSettings;

    public ModeSelectionWindow(
        UsageMode? currentMode = null,
        PersonalProtectionLevel currentPersonalLevel = PersonalProtectionLevel.Balanced,
        ControlSettings? currentSettings = null)
    {
        InitializeComponent();
        SelectedMode = currentMode;
        SelectedPersonalProtectionLevel = currentPersonalLevel;
        _currentSettings = currentSettings ?? new ControlSettings
        {
            Mode = currentMode ?? UsageMode.Insights,
            PersonalProtectionLevel = currentPersonalLevel
        };
        UpdateSelectionStyles();
        UpdatePersonalSelectionStyles();
    }

    public UsageMode? SelectedMode { get; private set; }
    public PersonalProtectionLevel SelectedPersonalProtectionLevel { get; private set; }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Rect workArea = SystemParameters.WorkArea;
        MaxWidth = Math.Max(320, workArea.Width - 16);
        MaxHeight = Math.Max(240, workArea.Height - 16);
        MinWidth = Math.Min(MinWidth, MaxWidth);
        MinHeight = Math.Min(MinHeight, MaxHeight);
        Width = Math.Min(Width, MaxWidth);
        Height = Math.Min(Height, MaxHeight);
    }

    private void Insights_Click(object sender, RoutedEventArgs e) => SelectMode(UsageMode.Insights);
    private void Personal_Click(object sender, RoutedEventArgs e) => SelectMode(UsageMode.Personal);
    private void Family_Click(object sender, RoutedEventArgs e) => SelectMode(UsageMode.Family);

    private void SelectMode(UsageMode mode)
    {
        SelectedMode = mode;
        UpdateSelectionStyles();
    }

    private void UpdateSelectionStyles()
    {
        Style normal = (Style)InsightsButton.FindResource("ModeCardStyle");
        Style selected = (Style)InsightsButton.FindResource("SelectedModeCardStyle");
        InsightsButton.Style = SelectedMode == UsageMode.Insights ? selected : normal;
        PersonalButton.Style = SelectedMode == UsageMode.Personal ? selected : normal;
        FamilyButton.Style = SelectedMode == UsageMode.Family ? selected : normal;
        ConfirmModeButton.IsEnabled = SelectedMode is not null;
    }

    private void Flexible_Click(object sender, RoutedEventArgs e) => SelectPersonalLevel(PersonalProtectionLevel.Flexible);
    private void Balanced_Click(object sender, RoutedEventArgs e) => SelectPersonalLevel(PersonalProtectionLevel.Balanced);
    private void ProtectedLevel_Click(object sender, RoutedEventArgs e) => SelectPersonalLevel(PersonalProtectionLevel.Protected);

    private void SelectPersonalLevel(PersonalProtectionLevel level)
    {
        SelectedPersonalProtectionLevel = level;
        UpdatePersonalSelectionStyles();
    }

    private void UpdatePersonalSelectionStyles()
    {
        Style normal = (Style)FlexibleButton.FindResource("ModeCardStyle");
        Style selected = (Style)FlexibleButton.FindResource("SelectedModeCardStyle");
        FlexibleButton.Style = SelectedPersonalProtectionLevel == PersonalProtectionLevel.Flexible ? selected : normal;
        BalancedButton.Style = SelectedPersonalProtectionLevel == PersonalProtectionLevel.Balanced ? selected : normal;
        ProtectedLevelButton.Style = SelectedPersonalProtectionLevel == PersonalProtectionLevel.Protected ? selected : normal;
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedMode == UsageMode.Personal)
        {
            UsageStepPanel.Visibility = Visibility.Collapsed;
            PersonalStepPanel.Visibility = Visibility.Visible;
            return;
        }

        if (SelectedMode is not null)
        {
            if (SelectedMode == UsageMode.Family && RequiresProtectionReview())
            {
                ShowProtectionSummary();
            }
            else
            {
                DialogResult = true;
            }
        }
    }

    private void ConfirmPersonal_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedPersonalProtectionLevel == PersonalProtectionLevel.Protected && RequiresProtectionReview())
        {
            ShowProtectionSummary();
            return;
        }

        DialogResult = true;
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        PersonalStepPanel.Visibility = Visibility.Collapsed;
        UsageStepPanel.Visibility = Visibility.Visible;
    }

    private bool RequiresProtectionReview() =>
        SelectedMode is { } mode &&
        (mode != _currentSettings.Mode ||
         mode == UsageMode.Personal && SelectedPersonalProtectionLevel != _currentSettings.PersonalProtectionLevel);

    private void ShowProtectionSummary()
    {
        if (SelectedMode is not { } mode) return;
        ControlSettings target = new()
        {
            Mode = mode,
            PersonalProtectionLevel = mode == UsageMode.Personal
                ? SelectedPersonalProtectionLevel
                : PersonalProtectionLevel.Balanced,
            LimitAction = _currentSettings.LimitAction,
            PersonalChangeDelayMinutes = _currentSettings.PersonalChangeDelayMinutes,
            AdminPin = _currentSettings.AdminPin,
            RecoveryCodes = _currentSettings.RecoveryCodes,
            Schedule = _currentSettings.Schedule
        };
        ProtectionTransitionSummary summary = ProtectionTransitionAnalyzer.Analyze(
            _currentSettings,
            target,
            recoveryWillBePrepared: mode == UsageMode.Family);
        bool english = LocalizationService.CurrentLanguage == LanguagePreference.English;
        ProtectionSummaryTitle.Text = summary.TargetIsProtected
            ? english ? "Before protection is enabled" : "Koruma etkinleşmeden önce"
            : english ? "Before the mode changes" : "Kullanım biçimi değişmeden önce";
        ProtectionSummaryDescription.Text = english
            ? "This summary explains the selected policy. It does not grant authorization or replace a PIN."
            : "Bu özet seçilen policy sonucunu açıklar; yetki vermez ve PIN yerine geçmez.";
        string expiry = summary.LimitAction == LimitReachedAction.ShowBlockScreen
            ? english ? "the protection screen opens" : "koruma ekranı açılır"
            : english ? "Windows is locked" : "Windows kilitlenir";
        string application = summary.IsTightening
            ? english ? "This protection increase applies immediately after a successful save." : "Bu koruma artışı başarılı kayıttan sonra hemen uygulanır."
            : english ? "This mode change applies after a successful save." : "Bu kullanım biçimi değişikliği başarılı kayıttan sonra uygulanır.";
        ProtectionOutcomeText.Text = summary.TargetIsProtected
            ? english
                ? $"• When time expires: {expiry}.\n• The selected weekly plan has {summary.EnabledPlanDays} active days.\n• {application}"
                : $"• Süre dolunca {expiry}.\n• Seçilen haftalık planda {summary.EnabledPlanDays} etkin gün var.\n• {application}"
            : english
                ? $"• The selected mode does not enable a Guardian-enforced daily limit or weekly plan.\n• {application}"
                : $"• Seçilen kullanım biçimi Guardian tarafından uygulanan günlük limit veya haftalık planı etkinleştirmez.\n• {application}";
        ProtectionBoundaryText.Text = summary.RequiresGuardian
            ? english
                ? "Installing or repairing Guardian requires Windows administrator approval. For meaningful protection, use Kvieta in a standard Windows account managed by a separate administrator account."
                : "Guardian kurulumu veya onarımı Windows yönetici onayı ister. Anlamlı koruma için Kvieta'yı ayrı bir yönetici hesabınca yönetilen standart Windows hesabında kullan."
            : english
                ? "The selected mode does not require Guardian or Windows administrator approval."
                : "Seçilen kullanım biçimi Guardian veya Windows yönetici onayı gerektirmez.";
        ProtectionRecoveryText.Text = summary.RecoveryState switch
        {
            ProtectionRecoveryState.WillBePrepared when english => "Recovery: a PIN and one-time recovery codes will be prepared next. Save the codes outside this device.",
            ProtectionRecoveryState.WillBePrepared => "Kurtarma: sırada PIN ve tek kullanımlık kurtarma kodları hazırlanacak. Kodları bu cihazın dışında sakla.",
            ProtectionRecoveryState.WindowsAdministrator when english => "Recovery: protected Personal mode uses Windows administrator verification; it does not reveal or store a user PIN in this summary.",
            ProtectionRecoveryState.WindowsAdministrator => "Kurtarma: Korumalı Kişisel kullanım Windows yönetici doğrulamasını kullanır; bu özet kullanıcı PIN'i göstermez veya saklamaz.",
            ProtectionRecoveryState.Ready when english => "Recovery preparation is already present. No PIN or recovery-code content is shown here.",
            ProtectionRecoveryState.Ready => "Kurtarma hazırlığı mevcut. PIN veya kurtarma kodu içeriği burada gösterilmez.",
            ProtectionRecoveryState.NotRequired when english => "Recovery preparation is not required for the selected mode.",
            ProtectionRecoveryState.NotRequired => "Seçilen kullanım biçimi için kurtarma hazırlığı gerekmez.",
            _ when english => "Recovery preparation is missing and must be completed before protection is saved.",
            _ => "Kurtarma hazırlığı eksik; koruma kaydedilmeden önce tamamlanmalı."
        };
        ProtectionAcknowledgement.Content = english
            ? "I understand this is an explanation, not authorization."
            : "Bunun yetkilendirme değil, açıklama olduğunu anlıyorum.";
        ProtectionBackButton.Content = english ? "Back" : "Geri";
        ProtectionContinueButton.Content = english ? "Continue securely" : "Güvenle devam et";
        ProtectionAcknowledgement.IsChecked = false;
        ProtectionContinueButton.IsEnabled = false;
        UsageStepPanel.Visibility = Visibility.Collapsed;
        PersonalStepPanel.Visibility = Visibility.Collapsed;
        ProtectionSummaryPanel.Visibility = Visibility.Visible;
    }

    private void ProtectionAcknowledgement_Changed(object sender, RoutedEventArgs e) =>
        ProtectionContinueButton.IsEnabled = ProtectionAcknowledgement.IsChecked == true;

    private void ProtectionBack_Click(object sender, RoutedEventArgs e)
    {
        ProtectionSummaryPanel.Visibility = Visibility.Collapsed;
        if (SelectedMode == UsageMode.Personal)
        {
            PersonalStepPanel.Visibility = Visibility.Visible;
        }
        else
        {
            UsageStepPanel.Visibility = Visibility.Visible;
        }
    }

    private void ProtectionContinue_Click(object sender, RoutedEventArgs e)
    {
        if (ProtectionAcknowledgement.IsChecked == true) DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
}
