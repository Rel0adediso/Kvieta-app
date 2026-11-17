using System.IO;
using System.Text.Json;
using Kvieta.Core.Models;
using Kvieta.Core.Services;

namespace Kvieta.App.Services;

public sealed record MobilePolicyCommand(string Room, MobileTimeDecision Decision);
public sealed record MobilePolicyAuthorization(string Room, string Key);

public static class MobilePolicyCoordinator
{
    private static readonly JsonSerializerOptions Json = new() { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } };
    public static bool IsPolicyAction(string action) => action is "update-plan" or "update-app-rule" or "update-web-guard";

    public static async Task<bool> ApplyAsync(string action, string? payload, CancellationToken cancellation = default)
    {
        if (string.IsNullOrWhiteSpace(payload) || !IsPolicyAction(action)) return false;
        // Protected changes must arrive with the authenticated relay command, never as raw user-writable JSON.
        if (File.Exists(ProtectionServiceManager.ProtectedSettingsPath)) return false;
        try
        {
            ControlSettings saved = await new JsonSettingsStore().UpdateAsync(current => Mutate(current, action, payload, DateTimeOffset.UtcNow), cancellation);
            bool applied = WebGuardService.Apply(saved);
            DopamineTrapWatcher.Instance.UpdateSettings(saved);
            return applied;
        }
        catch { return false; }
    }

    public static ControlSettings Mutate(ControlSettings current, string action, string payload, DateTimeOffset now)
    {
        ControlSettings desired = JsonSerializer.Deserialize<ControlSettings>(JsonSerializer.Serialize(current, Json), Json)!;
        desired.PendingChange = null;
        MobilePolicyMutation.Apply(desired, action, payload);
        if (current.Mode == UsageMode.Personal && current.PersonalProtectionLevel != PersonalProtectionLevel.Flexible && SettingsPolicyComparer.HasRelaxation(current, desired))
        {
            if (current.PendingChange is { } pending && JsonSerializer.Serialize(pending.TargetSettings, Json) == JsonSerializer.Serialize(desired, Json)) return current;
            current.PendingChange = new PendingPolicyChange { RequestedAtUtc = now, ApplyAfterUtc = now.AddMinutes(Math.Max(0, current.PersonalChangeDelayMinutes)), TargetSettings = desired };
            return current;
        }
        if (current.PendingChange is { } existing)
        {
            ControlSettings target = JsonSerializer.Deserialize<ControlSettings>(JsonSerializer.Serialize(existing.TargetSettings, Json), Json)!;
            target.PendingChange = null;
            MobilePolicyMutation.Apply(target, action, payload);
            desired.PendingChange = new PendingPolicyChange
            {
                Id = existing.Id,
                RequestedAtUtc = existing.RequestedAtUtc,
                ApplyAfterUtc = existing.ApplyAfterUtc,
                TargetSettings = target
            };
        }
        return desired;
    }
}
