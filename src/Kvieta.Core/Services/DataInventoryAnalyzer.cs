using Kvieta.Core.Models;

namespace Kvieta.Core.Services;

public sealed record DataInventorySummary(
    int DetailedDayCount,
    DateOnly? FirstDay,
    DateOnly? LastDay,
    bool IncludesApplicationNames,
    bool HasRhythmSummary,
    int RetentionDays);

public static class DataInventoryAnalyzer
{
    public static DataInventorySummary Analyze(ControlSettings settings, UsageLedger ledger)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(ledger);
        List<DateOnly> days = ledger.History.Select(item => item.LocalDay).ToList();
        bool hasCurrent = ledger.UsedSeconds > 0 || ledger.AwarenessUsedSeconds > 0 ||
            ledger.AppUsedSeconds.Count > 0 || ledger.ForegroundAppUsedSeconds.Count > 0;
        if (hasCurrent) days.Add(ledger.LocalDay);
        bool hasRhythm = ledger.RhythmCheckpoint.ProcessedThroughDay is not null ||
            ledger.RhythmCheckpoint.BestStreak > 0 || ledger.RhythmGoal is not null;
        bool names = ledger.History.Any(day => day.Applications.Count > 0 || day.ForegroundApplications.Count > 0) ||
            ledger.AppUsedSeconds.Count > 0 || ledger.ForegroundAppUsedSeconds.Count > 0;
        return new DataInventorySummary(
            days.Distinct().Count(),
            days.Count == 0 ? null : days.Min(),
            days.Count == 0 ? null : days.Max(),
            names,
            hasRhythm,
            settings.UsageRetentionDays);
    }
}

public enum DataDeletionScope
{
    DetailedUsage,
    RhythmSummary,
    UsageAndRhythm
}
