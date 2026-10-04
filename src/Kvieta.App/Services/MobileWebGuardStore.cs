using System.IO;
using System.Text.Json;
using Kvieta.Core.Models;
using Kvieta.Core.Services;

namespace Kvieta.App.Services;

public sealed record MobileWebGuardCommand(
    bool? WebGuardEnabled,
    bool? SafeSearchEnforced,
    List<string>? BlockedWebDomains,
    string? Action,
    string? Domain);

public static class MobileWebGuardStore
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static async Task<bool> ApplyAsync(string? payload, CancellationToken cancellation = default)
    {
        if (string.IsNullOrWhiteSpace(payload)) return false;
        MobileWebGuardCommand? command;
        try { command = JsonSerializer.Deserialize<MobileWebGuardCommand>(payload, Json); }
        catch { return false; }

        if (command is null) return false;

        if (!await Gate.WaitAsync(1000, cancellation)) return false;
        try
        {
            string path = ProtectionServiceManager.ProtectedSettingsPath;
            bool isProtected = File.Exists(path);
            var store = isProtected ? new JsonSettingsStore(path, readOnly: false) : new JsonSettingsStore();
            ControlSettings settings = await store.LoadAsync(cancellation);

            bool changed = false;

            if (command.WebGuardEnabled.HasValue && settings.WebGuardEnabled != command.WebGuardEnabled.Value)
            {
                settings.WebGuardEnabled = command.WebGuardEnabled.Value;
                changed = true;
            }

            if (command.SafeSearchEnforced.HasValue && settings.SafeSearchEnforced != command.SafeSearchEnforced.Value)
            {
                settings.SafeSearchEnforced = command.SafeSearchEnforced.Value;
                changed = true;
            }

            if (command.BlockedWebDomains is not null)
            {
                var newDomains = command.BlockedWebDomains
                    .Select(d => d.Trim().ToLowerInvariant())
                    .Where(d => !string.IsNullOrWhiteSpace(d))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (!settings.BlockedWebDomains.SequenceEqual(newDomains, StringComparer.OrdinalIgnoreCase))
                {
                    settings.BlockedWebDomains = newDomains;
                    changed = true;
                }
            }

            if (!string.IsNullOrWhiteSpace(command.Domain))
            {
                string domain = command.Domain.Trim().ToLowerInvariant();
                if (string.Equals(command.Action, "remove-domain", StringComparison.OrdinalIgnoreCase))
                {
                    if (settings.BlockedWebDomains.RemoveAll(d => string.Equals(d, domain, StringComparison.OrdinalIgnoreCase)) > 0)
                    {
                        changed = true;
                    }
                }
                else if (string.Equals(command.Action, "add-domain", StringComparison.OrdinalIgnoreCase))
                {
                    if (!settings.BlockedWebDomains.Contains(domain, StringComparer.OrdinalIgnoreCase))
                    {
                        settings.BlockedWebDomains.Add(domain);
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                await store.SaveAsync(settings, cancellation);
                WebGuardService.Apply(settings);

                string toastMessage = "Web koruma ayarları güncellendi.";
                if (!string.IsNullOrWhiteSpace(command.Domain))
                {
                    toastMessage = string.Equals(command.Action, "remove-domain", StringComparison.OrdinalIgnoreCase)
                        ? $"{command.Domain} web engel listesinden kaldırıldı."
                        : $"{command.Domain} web sitesi telefondan engellendi.";
                }
                else if (command.WebGuardEnabled.HasValue)
                {
                    toastMessage = settings.WebGuardEnabled
                        ? "Web koruması telefondan etkinleştirildi."
                        : "Web koruması telefondan devre dışı bırakıldı.";
                }
                else if (command.SafeSearchEnforced.HasValue)
                {
                    toastMessage = settings.SafeSearchEnforced
                        ? "Güvenli Arama telefondan etkinleştirildi."
                        : "Güvenli Arama telefondan devre dışı bırakıldı.";
                }

                DesktopToastWindow.ShowToast("Web Koruması", toastMessage, "🌐");
            }

            return true;
        }
        catch { return false; }
        finally { Gate.Release(); }
    }
}
