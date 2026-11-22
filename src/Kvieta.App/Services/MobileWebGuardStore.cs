using System.IO;
using System.Text.Json;
using Kvieta.Core.Models;
using Kvieta.Core.Services;

namespace Kvieta.App.Services;

public sealed record MobileWebGuardCommand(
    bool? WebGuardEnabled,
    bool? SafeSearchEnforced,
    bool? BlockShortFormVideo,
    List<string>? BlockedWebDomains,
    string? Action,
    string? Domain);

public static class MobileWebGuardStore
{
    public static Task<bool> ApplyAsync(string? payload, CancellationToken cancellation = default) =>
        MobilePolicyCoordinator.ApplyAsync("update-web-guard", payload, cancellation);
}
