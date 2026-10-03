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
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static async Task<bool> ApplyAsync(string? payload, CancellationToken cancellation = default)
    {
        if (string.IsNullOrWhiteSpace(payload)) return false;
        MobileAppRuleCommand? command;
        try { command = JsonSerializer.Deserialize<MobileAppRuleCommand>(payload, Json); }
        catch { return false; }

        if (command is null || string.IsNullOrWhiteSpace(command.Name)) return false;
        if (!Enum.TryParse<AppRuleMode>(command.Mode, true, out var ruleMode)) return false;

        if (!await Gate.WaitAsync(500, cancellation)) return false;
        try
        {
            string path = ProtectionServiceManager.ProtectedSettingsPath;
            bool isProtected = File.Exists(path);
            var store = isProtected ? new JsonSettingsStore(path, readOnly: false) : new JsonSettingsStore();
            ControlSettings settings = await store.LoadAsync(cancellation);

            string appName = command.Name.Trim();
            AppRule? rule = settings.AppRules.FirstOrDefault(r =>
                string.Equals(r.Name, appName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Path.GetFileNameWithoutExtension(r.ExecutablePath), appName, StringComparison.OrdinalIgnoreCase));

            if (ruleMode == AppRuleMode.Unlimited)
            {
                if (rule is not null)
                {
                    settings.AppRules.Remove(rule);
                }
            }
            else
            {
                if (rule is null)
                {
                    rule = new AppRule
                    {
                        Id = Guid.NewGuid(),
                        Name = appName,
                        ExecutablePath = appName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? appName : appName + ".exe",
                        Mode = ruleMode,
                        DailyLimitMinutes = Math.Clamp(command.DailyLimitMinutes, 0, 1440)
                    };
                    settings.AppRules.Add(rule);
                }
                else
                {
                    rule.Mode = ruleMode;
                    rule.DailyLimitMinutes = Math.Clamp(command.DailyLimitMinutes, 0, 1440);
                }
            }

            await store.SaveAsync(settings, cancellation);
            return true;
        }
        catch { return false; }
        finally { Gate.Release(); }
    }
}
