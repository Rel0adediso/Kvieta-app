using System.Text.Json;
using Kvieta.Core.Models;
using Kvieta.Core.Services;

namespace Kvieta.App.Services;

public sealed record MobilePlanDay(string Day, bool IsEnabled, string AllowedFrom, string AllowedUntil, int DailyLimitMinutes);
public sealed record MobilePlanChange(string DeviceId, IReadOnlyList<MobilePlanDay> Schedule);

/// <summary>Applies the narrow, schedule-only command sent by a paired phone.</summary>
public static class MobilePlanChangeStore
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static async Task<bool> ApplyAsync(string? payload, CancellationToken cancellation = default)
    {
        if (string.IsNullOrWhiteSpace(payload)) return false;
        MobilePlanChange? change;
        try { change = JsonSerializer.Deserialize<MobilePlanChange>(payload, Json); }
        catch (JsonException) { return false; }
        if (change is null || change.Schedule.Count != 7 || change.Schedule.Any(day =>
            !Enum.TryParse<DayOfWeek>(day.Day, true, out _) || day.DailyLimitMinutes is < 0 or > 1440 ||
            !TimeOnly.TryParse(day.AllowedFrom, out _) || !TimeOnly.TryParse(day.AllowedUntil, out _))) return false;
        if (!await Gate.WaitAsync(0, cancellation)) return false;
        try
        {
            string path = ProtectionServiceManager.ProtectedSettingsPath;
            ControlSettings current = await new JsonSettingsStore(path, readOnly: true).LoadAsync(cancellation);
            current.Schedule = change.Schedule.Select(day => new DaySchedule
            {
                Day = Enum.Parse<DayOfWeek>(day.Day, true), IsEnabled = day.IsEnabled,
                AllowedFrom = TimeOnly.Parse(day.AllowedFrom), AllowedUntil = TimeOnly.Parse(day.AllowedUntil),
                DailyLimitMinutes = day.DailyLimitMinutes
            }).OrderBy(day => day.Day == DayOfWeek.Sunday ? 7 : (int)day.Day).ToList();
            if (current.Schedule.Select(day => day.Day).Distinct().Count() != 7) return false;
            await new JsonSettingsStore(path).SaveAsync(current, cancellation);
            return true;
        }
        catch (Exception) { return false; }
        finally { Gate.Release(); }
    }
}
