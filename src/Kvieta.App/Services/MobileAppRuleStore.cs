using System.IO;
using System.Text.Json;
using Kvieta.Core.Models;
using Kvieta.Core.Services;

namespace Kvieta.App.Services;

public sealed record MobileAppRuleCommand(
    string Name,
    string Mode,
    int DailyLimitMinutes);

public static class MobileAppRuleStore
{
    public static Task<bool> ApplyAsync(string? payload, CancellationToken cancellation = default) =>
        MobilePolicyCoordinator.ApplyAsync("update-app-rule", payload, cancellation);
}
