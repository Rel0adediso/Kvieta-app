using Kvieta.App.Services;

namespace Kvieta.App.ViewModels;

public sealed record ApplicationUsageOverview(
    IReadOnlyList<AppCategoryUsageRow> Categories,
    AppCategoryUsageRow? TopCategory,
    AppUsageHistoryRow? MostUsed,
    AppUsageHistoryRow? FastestRising,
    double FastestRisingIncrease);

public static class ApplicationUsageOverviewBuilder
{
    public static ApplicationUsageOverview Build(IReadOnlyList<AppUsageHistoryRow> applications)
    {
        List<AppCategoryUsageRow> categories = applications
            .GroupBy(application => AppUsageHistoryRow.ApplicationCategoryKey(application.Name))
            .Select(group => new AppCategoryUsageRow
            {
                Key = group.Key,
                Name = LocalizationService.Get(group.Key),
                UsedSeconds = group.Sum(application => application.UsedSeconds),
                ApplicationCount = group.Count(),
                AccentBrush = ApplicationIconProvider.GetFallbackBrush(group.Key)
            })
            .OrderByDescending(category => category.UsedSeconds)
            .ToList();
        long maximumCategory = Math.Max(1, categories
            .Select(category => category.UsedSeconds)
            .DefaultIfEmpty(0)
            .Max());
        categories = categories
            .Select(category => new AppCategoryUsageRow
            {
                Key = category.Key,
                Name = category.Name,
                UsedSeconds = category.UsedSeconds,
                ApplicationCount = category.ApplicationCount,
                RelativePercent = Math.Clamp(category.UsedSeconds * 100d / maximumCategory, 0, 100),
                AccentBrush = category.AccentBrush
            })
            .ToList();

        var rising = applications
            .Where(application => application.TrendValues.Count >= 2)
            .Select(application => new
            {
                Application = application,
                Increase = application.TrendValues[^1] - application.TrendValues[^2]
            })
            .Where(item => item.Increase > 0)
            .OrderByDescending(item => item.Increase)
            .FirstOrDefault();

        return new ApplicationUsageOverview(
            categories,
            categories.FirstOrDefault(),
            applications.FirstOrDefault(),
            rising?.Application,
            rising?.Increase ?? 0);
    }
}
