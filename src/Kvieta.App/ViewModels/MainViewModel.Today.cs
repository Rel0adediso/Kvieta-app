using System.Collections.ObjectModel;
using System.Globalization;
using Kvieta.Core.Models;
using Kvieta.App.Services;

namespace Kvieta.App.ViewModels;

public sealed partial class MainViewModel
{
    public ObservableCollection<TodayHourRow> TodayHours { get; } = [];
    public string TodayDateText => DateTime.Today.ToString("dddd, d MMMM",
        CultureInfo.GetCultureInfo(LocalizationService.CurrentLanguage == LanguagePreference.English ? "en-US" : "tr-TR"));
    public bool HasTodayFocus => _lastUsageLedger is { ActiveFocusSessionId: not null, ActiveFocusTargetSeconds: > 0 };
    public string TodayHeadline => HasTodayFocus
        ? L("Şimdi odak zamanı.", "Time to focus.")
        : IsInsightsMode ? L("Gününü tanı.\nKendine alan aç.", "Know your day.\nMake room for you.")
        : L("Bugün kendine\nalan aç.", "Make room\nfor yourself today.");
    public string TodayLeadText => HasTodayFocus
        ? L("Bir sonraki adımına odaklan. Günün geri kalanı bekleyebilir.", "Focus on your next step. The rest of the day can wait.")
        : L("Zamanının nereye gittiğini gör. Sıradaki anını sen seç.", "See where your time goes. Choose what comes next.");
    public string TodayPrimaryActionText => IsInsightsMode
        ? L("Günlük özeti incele", "Explore today's summary")
        : IsPersonalMode && !HasTodayFocus
            ? L("25 dk odak başlat", "Start 25 min focus")
            : L("Oturuma dön", "Return to session");
    public string TodayPrimaryActionHint => IsInsightsMode
        ? L("Kullanımını ve günlük ritmini keşfet.", "Discover your usage and daily rhythm.")
        : IsPersonalMode && !HasTodayFocus
            ? L("Tek bir iş. Kendine ayırdığın 25 dakika.", "One task. Twenty-five minutes for yourself.")
            : L("Kalan süren ve oturum kontrollerin bir arada.", "Your remaining time and session controls, together.");
    public string TodayMeasuredText { get; private set; } = "—";
    public string TodayMeasuredChangeText { get; private set; } = "—";
    public string TodayObservationText { get; private set; } = "—";
    public string TodayOtherUsageText { get; private set; } = "—";
    public bool HasTodayOtherUsage { get; private set; }
    public bool HasTodayHourlyUsage => TodayHours.Any(hour => hour.UsedSeconds > 0);
    public bool HasNoTodayHourlyUsage => !HasTodayHourlyUsage;
    public bool ShowTodayScheduledLimit => HasScheduledPlan;

    private void BuildTodayPresentation(DailyUsageRecord current, DailyUsageRecord previous)
    {
        // Keep the headline total and ring on the same application-usage basis.
        long total = TodayApplications.Sum(application => application.UsedSeconds);
        long previousTotal = current.ForegroundApplications.Count > 0
            ? previous.ForegroundApplications.Sum(application => application.UsedSeconds)
            : previous.Applications.Sum(application => application.UsedSeconds);
        TodayMeasuredText = UsageHistoryFormatting.FormatDuration(total);
        TodayMeasuredChangeText = previousTotal > 0
            ? L($"Dünün toplamı: {UsageHistoryFormatting.FormatDuration(previousTotal)}", $"Yesterday's total: {UsageHistoryFormatting.FormatDuration(previousTotal)}")
            : L("Ölçülen uygulama kullanımı", "Measured application usage");
        long otherSeconds = TodayApplications.Skip(3).Sum(application => application.UsedSeconds);
        HasTodayOtherUsage = otherSeconds > 0;
        TodayOtherUsageText = UsageHistoryFormatting.FormatDuration(otherSeconds);

        TodayHours.Clear();
        for (int hour = 0; hour < 24; hour++)
        {
            TodayHours.Add(new TodayHourRow(hour, Math.Max(0, current.AwarenessHourlyUsedSeconds.GetValueOrDefault(hour))));
        }

        TodayHourRow? peak = TodayHours.Where(hour => hour.UsedSeconds > 0)
            .OrderByDescending(hour => hour.UsedSeconds).ThenBy(hour => hour.Hour).FirstOrDefault();
        TodayObservationText = peak is not null
            ? L($"Bugün en yoğun saatin {peak.Hour:00}.00–{peak.Hour + 1:00}.00. Bu aralıkta {peak.UsedText} uygulama kullandın.",
                $"Your busiest hour today was {peak.Hour:00}:00–{peak.Hour + 1:00}:00, with {peak.UsedText} of application use.")
            : L("Saatlik kullanım ölçüldükçe gününün akışı burada belirecek.", "Your day's pattern will appear here as hourly usage is measured.");
        foreach (string property in new[] { nameof(TodayMeasuredText), nameof(TodayMeasuredChangeText), nameof(TodayOtherUsageText),
            nameof(HasTodayOtherUsage), nameof(TodayObservationText), nameof(HasTodayHourlyUsage), nameof(HasNoTodayHourlyUsage) })
        {
            OnPropertyChanged(property);
        }
        NotifyTodayPresentation();
    }

    private void NotifyTodayPresentation()
    {
        foreach (string property in new[] { nameof(TodayDateText), nameof(HasTodayFocus), nameof(TodayHeadline), nameof(TodayLeadText),
            nameof(TodayPrimaryActionText), nameof(TodayPrimaryActionHint), nameof(ShowTodayScheduledLimit) })
        {
            OnPropertyChanged(property);
        }
    }
}

public sealed record TodayHourRow(int Hour, long UsedSeconds)
{
    public double BarHeight => Math.Clamp(UsedSeconds / 3600d, 0, 1) * 104;
    public string UsedText => UsageHistoryFormatting.FormatDuration(UsedSeconds);
    public string DetailText => $"{Hour:00}:00–{Hour + 1:00}:00 · {UsedText}";
}
