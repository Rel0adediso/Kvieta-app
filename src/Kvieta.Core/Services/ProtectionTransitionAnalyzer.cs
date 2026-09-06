using Kvieta.Core.Models;

namespace Kvieta.Core.Services;

public enum ProtectionRecoveryState
{
    NotRequired,
    Ready,
    WillBePrepared,
    Missing,
    WindowsAdministrator
}

public sealed record ProtectionTransitionSummary(
    bool TargetIsProtected,
    bool IsTightening,
    bool AppliesImmediately,
    bool RequiresGuardian,
    bool RequiresWindowsAdministrator,
    bool RequiresUserPin,
    ProtectionRecoveryState RecoveryState,
    LimitReachedAction LimitAction,
    int EnabledPlanDays,
    int PersonalChangeDelayMinutes);

public static class ProtectionTransitionAnalyzer
{
    public static ProtectionTransitionSummary Analyze(
        ControlSettings current,
        ControlSettings target,
        bool recoveryWillBePrepared = false)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(target);

        bool targetProtected = target.RequiresGuardian;
        bool tightening = targetProtected &&
            (!current.RequiresGuardian ||
             current.Mode != target.Mode ||
             current.PersonalProtectionLevel != target.PersonalProtectionLevel);
        ProtectionRecoveryState recovery = target.Mode switch
        {
            UsageMode.Family when target.AdminPin.IsConfigured &&
                                  target.RecoveryCodes.Any(code => code.UsedAtUtc is null) =>
                ProtectionRecoveryState.Ready,
            UsageMode.Family when recoveryWillBePrepared => ProtectionRecoveryState.WillBePrepared,
            UsageMode.Family => ProtectionRecoveryState.Missing,
            UsageMode.Personal when target.PersonalProtectionLevel == PersonalProtectionLevel.Protected =>
                ProtectionRecoveryState.WindowsAdministrator,
            _ => ProtectionRecoveryState.NotRequired
        };

        return new ProtectionTransitionSummary(
            targetProtected,
            tightening,
            tightening,
            target.RequiresGuardian,
            target.RequiresGuardian,
            target.Mode == UsageMode.Family,
            recovery,
            target.LimitAction,
            target.Schedule.Count(day => day.IsEnabled),
            Math.Max(0, target.PersonalChangeDelayMinutes));
    }
}
