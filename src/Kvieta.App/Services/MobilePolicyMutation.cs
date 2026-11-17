using System.Text.Json;
using System.IO;
using Kvieta.Core.Models;

namespace Kvieta.App.Services;

/// <summary>Only the three policy surfaces explicitly granted to a paired manager.</summary>
public static class MobilePolicyMutation
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public static void Apply(ControlSettings settings, string action, string payload)
    {
        if (payload.Length > 8192) throw new InvalidDataException("Command too large.");
        switch (action)
        {
            case "update-plan":
                var plan = JsonSerializer.Deserialize<MobilePlanChange>(payload, Json) ?? throw new InvalidDataException("Invalid plan.");
                if (plan.Schedule is null || plan.Schedule.Count != 7) throw new InvalidDataException("Seven days required.");
                var days = plan.Schedule.Select(day =>
                {
                    if (!Enum.TryParse(day.Day, true, out DayOfWeek name) || !Enum.IsDefined(name) || day.DailyLimitMinutes is < 0 or > 1440 ||
                        !TimeOnly.TryParseExact(day.AllowedFrom, "HH:mm", out TimeOnly from) || !TimeOnly.TryParseExact(day.AllowedUntil, "HH:mm", out TimeOnly until))
                        throw new InvalidDataException("Invalid day.");
                    return new DaySchedule { Day = name, IsEnabled = day.IsEnabled, AllowedFrom = from, AllowedUntil = until, DailyLimitMinutes = day.DailyLimitMinutes };
                }).ToList();
                if (days.Select(day => day.Day).Distinct().Count() != 7) throw new InvalidDataException("Duplicate day.");
                settings.Schedule = days;
                break;
            case "update-app-rule":
                var app = JsonSerializer.Deserialize<MobileAppRuleCommand>(payload, Json) ?? throw new InvalidDataException("Invalid rule.");
                if (string.IsNullOrWhiteSpace(app.Name) || app.Name.Length > 260 || app.Name.IndexOfAny(['\\', '/', ':']) >= 0 ||
                    !Enum.TryParse(app.Mode, true, out AppRuleMode mode) || !Enum.IsDefined(mode) || app.DailyLimitMinutes is < 0 or > 1440)
                    throw new InvalidDataException("Invalid rule.");
                string name = app.Name.Trim();
                AppRule? rule = settings.AppRules.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                    System.IO.Path.GetFileNameWithoutExtension(r.ExecutablePath).Equals(name, StringComparison.OrdinalIgnoreCase));
                if (mode == AppRuleMode.Unlimited) { if (rule is not null) settings.AppRules.Remove(rule); }
                else
                {
                    if (rule is null)
                    {
                        rule = new AppRule { Name = name, ExecutablePath = name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name : name + ".exe" };
                        settings.AppRules.Add(rule);
                    }
                    rule.Mode = mode;
                    rule.DailyLimitMinutes = app.DailyLimitMinutes;
                }
                break;
            case "update-web-guard":
                var web = JsonSerializer.Deserialize<MobileWebGuardCommand>(payload, Json) ?? throw new InvalidDataException("Invalid web policy.");
                if (web.SafeSearchEnforced.HasValue) settings.SafeSearchEnforced = web.SafeSearchEnforced.Value;
                if (web.BlockShortFormVideo.HasValue) settings.BlockShortFormVideo = web.BlockShortFormVideo.Value;
                if (web.WebGuardEnabled.HasValue) settings.WebGuardEnabled = web.WebGuardEnabled.Value;
                else if (web.SafeSearchEnforced == true || web.BlockShortFormVideo == true) settings.WebGuardEnabled = true;
                string Domain(string value)
                {
                    string cleaned = WebGuardService.CleanDomain(value);
                    return cleaned.Length is > 0 and <= 253 ? cleaned : throw new InvalidDataException("Invalid domain.");
                }
                if (web.BlockedWebDomains is not null) settings.BlockedWebDomains = web.BlockedWebDomains.Select(Domain).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if (!string.IsNullOrWhiteSpace(web.Domain))
                {
                    string domain = Domain(web.Domain);
                    if (web.Action == "remove-domain") settings.BlockedWebDomains.RemoveAll(d => d.Equals(domain, StringComparison.OrdinalIgnoreCase));
                    else if (web.Action == "add-domain")
                    {
                        if (!settings.BlockedWebDomains.Contains(domain, StringComparer.OrdinalIgnoreCase)) settings.BlockedWebDomains.Add(domain);
                    }
                    else throw new InvalidDataException("Invalid domain action.");
                }
                if (settings.BlockedWebDomains.Count > 200) throw new InvalidDataException("Too many domains.");
                break;
            default: throw new InvalidDataException("Unsupported mobile policy command.");
        }
    }
}
