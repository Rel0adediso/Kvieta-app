using System.Text.Json;
using Kvieta.Core.Models;
using Kvieta.Core.Services;

namespace Kvieta.App.Services;

public sealed record MobilePlanDay(string Day, bool IsEnabled, string AllowedFrom, string AllowedUntil, int DailyLimitMinutes);
public sealed record MobilePlanChange(string DeviceId, IReadOnlyList<MobilePlanDay> Schedule);

/// <summary>Applies the narrow, schedule-only command sent by a paired phone.</summary>
public static class MobilePlanChangeStore
{
    public static Task<bool> ApplyAsync(string? payload, CancellationToken cancellation = default) =>
        MobilePolicyCoordinator.ApplyAsync("update-plan", payload, cancellation);
}
