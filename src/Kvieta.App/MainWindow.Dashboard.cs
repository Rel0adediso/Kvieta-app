using System.IO;
using System.Windows;
using Kvieta.App.Services;
using Kvieta.Core.Models;
using Kvieta.Core.Services;

namespace Kvieta.App;

public partial class MainWindow
{
    private bool _phoneHubOpening;
    private async void AndroidDashboard_Click(object sender, RoutedEventArgs e)
    {
        if (_phoneHubOpening) return;
        _phoneHubOpening = true;
        try { await TryOpenManagerDeviceAsync(); }
        finally { _phoneHubOpening = false; }
    }

    private async Task OpenPhoneHubAsync()
    {
        if (_viewModel.HasAdminPin)
        {
            AdminPinWindow verification = AdminPinWindow.CreateVerification(_viewModel.VerifyAdminPinAsync, RecoverAdminPinAsync);
            verification.Owner = this;
            if (verification.ShowDialog() != true) return;
        }
        else if (_viewModel.IsFamilyMode)
        {
            _viewModel.StatusMessage = LocalizationService.Get("RecoveryCodesRequirePin");
            return;
        }

        DashboardEndpoint? endpoint = null;
        try
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Kvieta", "dashboard-pairing.bin");
            endpoint = await Task.Run(() => DashboardEndpoint.StartAsync(path, ReadDashboardSnapshotAsync));
        }
        catch (Exception)
        {
            // Keep recovery management available even when the local network is unavailable.
            try { endpoint = await Task.Run(() => DashboardEndpoint.OpenOffline(DashboardRelay.StorePath, ReadDashboardSnapshotAsync)); }
            catch (Exception) { /* Corrupt/unavailable local storage is displayed as unavailable. */ }
        }
        try
        {
            DashboardSharingWindow? window = null;
            window = new DashboardSharingWindow(endpoint, () => OpenManagerDeviceAsync(false, window)) { Owner = this };
            window.ShowDialog();
        }
        finally
        {
            if (endpoint is not null) await endpoint.DisposeAsync();
        }
    }

    internal static async Task<object> ReadDashboardSnapshotAsync()
    {
        ControlSettings settings = await (File.Exists(ProtectionServiceManager.ProtectedSettingsPath)
            ? new JsonSettingsStore(ProtectionServiceManager.ProtectedSettingsPath, readOnly: true)
            : new JsonSettingsStore()).LoadAsync();
        UsageLedger ledger = await new JsonUsageStore().LoadAsync();
        DateTimeOffset now = DateTimeOffset.Now;
        bool today = ledger.LocalDay == DateOnly.FromDateTime(now.DateTime);
        bool scheduled = settings.Mode != UsageMode.Insights &&
            !(settings.Mode == UsageMode.Personal && settings.PersonalProtectionLevel == PersonalProtectionLevel.Flexible);
        long? remaining = today && scheduled
            ? Math.Max(0, Math.Clamp(ScheduleEvaluator.Evaluate(settings, now).DailyLimitMinutes + ledger.BonusMinutes, 0, 1440) * 60L - ledger.UsedSeconds)
            : null;
        var applications = ledger.ForegroundAppUsedSeconds
            .OrderByDescending(x => x.Value)
            .Select(x =>
            {
                string name = Path.GetFileNameWithoutExtension(x.Key);
                AppRule? rule = settings.AppRules.FirstOrDefault(r =>
                    string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(Path.GetFileNameWithoutExtension(r.ExecutablePath), name, StringComparison.OrdinalIgnoreCase));
                return new
                {
                    name,
                    seconds = Math.Max(0, x.Value),
                    limitMinutes = rule?.DailyLimitMinutes,
                    mode = rule?.Mode.ToString() ?? "Unlimited",
                    iconBase64 = ApplicationIconProvider.GetIconPngBase64(x.Key)
                };
            }).ToArray();
        var appRules = settings.AppRules.Select(r => new
        {
            name = r.Name,
            mode = r.Mode.ToString(),
            dailyLimitMinutes = r.DailyLimitMinutes
        }).ToArray();
        var hourlyUsage = Enumerable.Range(0, 24).Select(hour => new
        {
            hour,
            seconds = ledger.AwarenessHourlyUsedSeconds.TryGetValue(hour, out long s) ? Math.Max(0, s) : 0L
        }).ToArray();
        var weeklyUsage = Enumerable.Range(0, 7).Select(offset => DateOnly.FromDateTime(now.DateTime).AddDays(offset - 6))
            .Select(day =>
            {
                long seconds = day == ledger.LocalDay
                    ? ledger.ForegroundAppUsedSeconds.Values.Sum(value => Math.Max(0, value))
                    : ledger.History.FirstOrDefault(item => item.LocalDay == day)?.ForegroundApplications.Sum(item => Math.Max(0, item.UsedSeconds)) ?? 0;
                return new { day = day.ToString("yyyy-MM-dd"), seconds };
            }).ToArray();
        long? focusRemainingSeconds = ledger.ActiveFocusSessionId is not null && ledger.ActiveFocusTargetSeconds > 0
            ? Math.Max(0, ledger.ActiveFocusTargetSeconds - ledger.ActiveFocusElapsedSeconds)
            : null;
        MobileTimeRequest? request = MobileTimeRequestStore.Current();
        string? decisionToken = null;
        try { decisionToken = DashboardRelay.Load(DashboardRelay.StorePath)?.Relay?.Client.DecisionToken; }
        catch { /* A damaged pairing store must not stop the local dashboard. */ }
        AdminCredential? adminCred = settings.AdminPin.IsConfigured && !settings.AdminPin.IsPublicMarker
            ? settings.AdminPin
            : null;
        if (adminCred is null)
        {
            try
            {
                var userSettings = await new JsonSettingsStore().LoadAsync();
                if (userSettings.AdminPin.IsConfigured && !userSettings.AdminPin.IsPublicMarker)
                {
                    adminCred = userSettings.AdminPin;
                }
            }
            catch { }
        }
        bool hasAdminPin = adminCred is { IsConfigured: true, IsPublicMarker: false };
        return new
        {
            deviceName = settings.DeviceName, mode = settings.Mode.ToString(), sessionState = ledger.State.ToString(),
            isRemotelyLocked = ledger.RemoteLockActive,
            canLockRemotely = true, canPauseRemotely = true,
            localDay = ledger.LocalDay.ToString("yyyy-MM-dd"),
            observedAtUtc = ledger.LastUpdatedUtc, servedAtUtc = now.ToUniversalTime(),
            stale = !today || now.ToUniversalTime() - ledger.LastUpdatedUtc > TimeSpan.FromMinutes(2),
            usedSeconds = ledger.ForegroundAppUsedSeconds.Values.Sum(x => Math.Max(0, x)), remainingSeconds = remaining,
            sessionUsedSeconds = Math.Max(0, ledger.UsedSeconds),
            applications, appRules, hourlyUsage,
            schedule = settings.Schedule.Select(day => new
            {
                day = day.Day.ToString(), isEnabled = day.IsEnabled,
                allowedFrom = day.AllowedFrom.ToString("HH:mm"), allowedUntil = day.AllowedUntil.ToString("HH:mm"),
                dailyLimitMinutes = Math.Clamp(day.DailyLimitMinutes, 0, 1440)
            }).ToArray(),
            weeklyUsage,
            focusRemainingSeconds,
            webGuardEnabled = settings.WebGuardEnabled,
            safeSearchEnforced = settings.SafeSearchEnforced,
            blockedWebDomains = settings.BlockedWebDomains.ToArray(),
            remoteDecisionToken = decisionToken,
            hasAdminPin,
            adminPinSalt = hasAdminPin ? adminCred!.SaltBase64 : null,
            adminPinHash = hasAdminPin ? adminCred!.HashBase64 : null,
            adminPinIterations = hasAdminPin ? adminCred!.Iterations : 0,
            timeRequest = request is null ? null : new
            {
                id = request.Id,
                requestedMinutes = request.RequestedMinutes,
                note = request.Note,
                createdAtUtc = request.CreatedAtUtc,
                expiresAtUtc = request.ExpiresAtUtc,
                status = request.Status.ToString(),
                grantedMinutes = request.GrantedMinutes,
                appliedAtUtc = request.AppliedAtUtc
            }
        };
    }
}
