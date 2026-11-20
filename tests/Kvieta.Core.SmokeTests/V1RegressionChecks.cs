using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using Kvieta.App.Services;
using Kvieta.Core.Models;
using Kvieta.Core.Services;
using Kvieta.SetupApp;
using Microsoft.Win32;

internal static class V1RegressionChecks
{
    public static async Task RunAsync()
    {
        int checks = 0;
        void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); checks++; }
        ControlSettings Copy(ControlSettings value) => JsonSerializer.Deserialize<ControlSettings>(JsonSerializer.Serialize(value))!;

        var credential = AdminPinService.Create("482619");
        var proofCommand = new MobileTimeDecision("proof-one", "request", "update-web-guard", null, DateTimeOffset.UtcNow, "{}", 123);
        string proof = Convert.ToBase64String(HMACSHA256.HashData(Convert.FromBase64String(credential.HashBase64), Encoding.UTF8.GetBytes(MobileParentProof.Content("room", proofCommand))));
        proofCommand = proofCommand with { ParentProof = proof };
        Check(MobileParentProof.Verify(credential, "room", proofCommand), "Valid parent proof rejected.");
        Check(!MobileParentProof.Verify(credential, "other-room", proofCommand), "Parent proof reused in another room.");
        Check(!MobileParentProof.Verify(credential, "room", proofCommand with { PayloadJson = "{changed}" }), "Tampered command accepted.");
        Check(!MobileParentProof.Verify(credential, "room", proofCommand with { ParentProof = null }), "Missing parent proof accepted.");
        var now = DateTimeOffset.Now;
        var family = new ControlSettings { Mode = UsageMode.Family };
        foreach (DaySchedule day in family.Schedule) { day.AllowedFrom = new TimeOnly(0, 0); day.AllowedUntil = new TimeOnly(0, 0); }
        var ledger = new UsageLedger { LocalDay = DateOnly.FromDateTime(now.DateTime), RemoteLockActive = true };
        var engine = new SessionEngine(family, ledger, now);
        Check(ledger.RemoteLockActive && ledger.State == SessionState.TimeExpired, "Restart must preserve a Family lock.");
        engine.ObserveClock(now, TimeSpan.FromHours(1), "old-boot");
        engine.ObserveClock(now.AddMinutes(1), TimeSpan.FromSeconds(15), "new-boot");
        Check(ledger.RemoteLockActive && engine.GetSnapshot(now.AddMinutes(1)).State == SessionState.TimeExpired, "Reboot removed Family lock.");
        engine.GetSnapshot(now.AddDays(1));
        Check(ledger.RemoteLockActive, "Midnight removed Family lock.");

        var current = new ControlSettings { Mode = UsageMode.Personal, WebGuardEnabled = true, SafeSearchEnforced = true, BlockShortFormVideo = true, BlockedWebDomains = ["example.com"] };
        foreach (Action<ControlSettings> change in new Action<ControlSettings>[] {
            s => s.WebGuardEnabled = false, s => s.SafeSearchEnforced = false,
            s => s.BlockShortFormVideo = false, s => s.BlockedWebDomains.Clear() })
        {
            var desired = Copy(current); change(desired);
            Check(SettingsPolicyComparer.HasRelaxation(current, desired), "Web relaxation escaped the delay.");
        }
        Check(!SettingsPolicyComparer.HasRelaxation(new ControlSettings(), current), "Tightening was treated as relaxation.");
        var pending = MobilePolicyCoordinator.Mutate(Copy(current), "update-web-guard", "{\"webGuardEnabled\":false}", now);
        Check(pending.WebGuardEnabled && pending.PendingChange?.TargetSettings.WebGuardEnabled == false && pending.PendingChange.ApplyAfterUtc > now, "Remote policy bypassed Personal delay.");
        bool malformed = false;
        try { MobilePolicyMutation.Apply(Copy(current), "update-app-rule", "{\"name\":\"app\",\"mode\":\"99\",\"dailyLimitMinutes\":5}"); }
        catch (InvalidDataException) { malformed = true; }
        Check(malformed, "Undefined app-rule enum accepted.");
        var disabled = Copy(current); disabled.WebGuardEnabled = false;
        var normalized = WebGuardService.Normalize(WebGuardService.FromSettings(disabled));
        Check(!normalized.BlockShortFormVideo && !normalized.SafeSearchEnforced && normalized.BlockedWebDomains.Count == 0, "Master switch did not disable child restrictions.");

        Check(WebAddressPolicy.IsShortVideo("youtube.com/shorts/123"), "Short video missed.");
        Check(WebAddressPolicy.IsShortVideo("https://m.instagram.com/reels/abc"), "Instagram short video missed.");
        Check(!WebAddressPolicy.IsShortVideo("https://example.org/search?q=tiktok.com"), "Query caused false short-video match.");
        Check(!WebAddressPolicy.IsShortVideo("https://youtube.com/shortstory"), "Path boundary ignored.");
        Check(!WebAddressPolicy.IsShortVideo("y"), "Typed address prefix caused a short-video match.");
        Check(!WebAddressPolicy.IsShortVideo("https://youtube.com/watch?v=123"), "Normal YouTube video treated as Shorts.");
        Check(!WebAddressPolicy.IsShortVideo("https://youtube.com.example.org/shorts/123"), "Unrelated host treated as YouTube.");
        Check(WebAddressPolicy.BrowserStartPage("chrome") == "chrome://newtab/", "Chrome start page is not native.");
        Check(WebAddressPolicy.BrowserStartPage("msedge") == "edge://newtab/", "Edge start page is not native.");
        var previousObservation = new WebAddressPolicy.BrowserObservation(1, 10, "https://youtube.com/shorts/123", now);
        var nextObservation = previousObservation with { ObservedAt = now.AddMilliseconds(600) };
        Check(nextObservation.Confirms(previousObservation), "Matching observations were not confirmed.");
        Check(!(nextObservation with { Window = 2 }).Confirms(previousObservation), "Different browser window accepted.");
        Check(!(nextObservation with { ProcessId = 11 }).Confirms(previousObservation), "Different browser process accepted.");
        Check(!(nextObservation with { Address = "https://youtube.com/" }).Confirms(previousObservation), "Different active address accepted.");
        Check(!(nextObservation with { ObservedAt = now.AddSeconds(3) }).Confirms(previousObservation), "Stale browser observation accepted.");
        Check(!WebAddressPolicy.MatchesDomain("https://notexample.com", "example.com"), "Host boundary ignored.");
        Check(WebAddressPolicy.MatchesDomain("https://sub.example.com/a", "example.com"), "Subdomain not matched.");
        Check(!WebAddressPolicy.MatchesDomain("https://example.com@safe.org", "example.com"), "Userinfo treated as host.");

        Check(ReleaseVersion.IsNewer("Alpha 6.5", "v1.0.0"), "Legacy Alpha did not upgrade to stable.");
        Check(ReleaseVersion.IsNewer("kvieta-alpha-6.5", "v1.0.0"), "Git tag Alpha did not upgrade to stable.");
        Check(!ReleaseVersion.IsNewer("v1.0.0", "kvieta-alpha-6.5"), "Stable downgraded to Alpha.");
        Check(ReleaseVersion.IsNewer("1.0.0-rc.2", "1.0.0-rc.10"), "Prerelease numeric ordering wrong.");
        Check(!ReleaseVersion.IsNewer("1.0.0", "v1.0"), "Equal versions ordered differently.");
        Check(!ReleaseVersion.IsNewer("1.0.0", "garbage"), "Invalid release accepted.");
        Check(UpdateCheckService.IsStableReleaseTag("v1.0.1"), "Stable patch tag rejected.");
        Check(!UpdateCheckService.IsStableReleaseTag("v1.1.0-rc.1"), "Prerelease entered stable channel.");
        Check(!UpdateCheckService.IsStableReleaseTag("main"), "A branch was accepted as a release tag.");
        Check(UpdateCheckService.IsStableBranchComparison("identical", 0), "Stable tip rejected.");
        Check(UpdateCheckService.IsStableBranchComparison("ahead", 0), "Earlier stable release rejected.");
        Check(!UpdateCheckService.IsStableBranchComparison("diverged", 1), "Unmerged release entered stable channel.");
        Check(!UpdateCheckService.IsStableBranchComparison("behind", 1), "Release ahead of stable entered stable channel.");
        Check(UpdateCheckService.IsOfficialDownloadUrl("https://github.com/Rel0adediso/Kvieta-app/releases/download/v1.0.1/Kvieta.exe"), "Official update rejected.");
        Check(!UpdateCheckService.IsOfficialDownloadUrl("https://github.com.evil.test/Rel0adediso/Kvieta-app/releases/download/v1/a.exe"), "Wrong update host accepted.");
        Check(!UpdateCheckService.IsOfficialDownloadUrl("http://github.com/Rel0adediso/Kvieta-app/releases/download/v1/a.exe"), "HTTP update accepted.");
        Check(SetupPlan.DeterminePackageAction(new Version(6, 5, 0), new Version(7, 0, 0)) == SetupPackageAction.Update, "Installer version cannot upgrade Alpha 6.5.");

        string temp = Path.Combine(Path.GetTempPath(), "Kvieta-V1-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            string path = Path.Combine(temp, "command.bin");
            var journal = new MobileCommandJournal(path);
            var decision = new MobileTimeDecision("command-one", "", "update-web-guard", null, DateTimeOffset.UtcNow, "{}", 1);
            int applied = 0;
            Check(!await journal.ProcessAsync("room", decision, () => Task.FromResult(false)), "Failed command marked applied.");
            Check(journal.Current()?.State == "Failed", "Failed receipt absent.");
            Check(await journal.ProcessAsync("room", decision, () => { applied++; return Task.FromResult(true); }), "Failed command not retryable.");
            Check(await new MobileCommandJournal(path).ProcessAsync("room", decision, () => { applied++; return Task.FromResult(true); }) && applied == 1, "Applied command repeated after process restart.");
            Check(!await journal.ProcessAsync("room", decision with { DecisionId = "stale", Sequence = 2, DecidedAtUtc = DateTimeOffset.UtcNow.AddHours(-1) }, () => { applied++; return Task.FromResult(true); }) && applied == 1, "Expired command applied.");
            Check(!await journal.ProcessAsync("room", decision with { DecisionId = "replay", Sequence = 1 }, () => { applied++; return Task.FromResult(true); }), "Older sequence replayed.");
        }
        finally { Directory.Delete(temp, true); }

        // Exercise real ownership/journal logic without modifying the host registry.
        var registry = new MemoryRegistry();
        registry.Write("Browser", "SafeSearch", 9, RegistryValueKind.DWord);
        using (var owner = new RegistryPolicyOwner(registry)) owner.Set("Browser", "SafeSearch", 1, RegistryValueKind.DWord);
        using (var owner = new RegistryPolicyOwner(registry)) owner.Set("Browser", "SafeSearch", null);
        Check((int)registry.Read("Browser", "SafeSearch")! == 9, "Original policy not restored.");
        using (var owner = new RegistryPolicyOwner(registry)) owner.Set("Browser", "SafeSearch", 1, RegistryValueKind.DWord);
        registry.Write("Browser", "SafeSearch", 7, RegistryValueKind.DWord);
        using (var owner = new RegistryPolicyOwner(registry)) owner.Set("Browser", "SafeSearch", null);
        Check((int)registry.Read("Browser", "SafeSearch")! == 7, "External policy change overwritten.");
        registry.Write("Blocklist", "1", "company.example", RegistryValueKind.String);
        using (var owner = new RegistryPolicyOwner(registry)) owner.SetList("Blocklist", ["blocked.example"]);
        using (var owner = new RegistryPolicyOwner(registry)) owner.SetList("Blocklist", []);
        Check(registry.Names("Blocklist").Length == 1 && (string)registry.Read("Blocklist", "1")! == "company.example", "Unrelated blocklist entries lost.");
        using (var owner = new RegistryPolicyOwner(registry)) owner.Set("Browser", "Fresh", 1, RegistryValueKind.DWord);
        using (var owner = new RegistryPolicyOwner(registry)) owner.Set("Browser", "Fresh", null);
        Check(registry.Read("Browser", "Fresh") is null, "New owned policy not removed.");
        registry.Write("Browser", "Retry", 9, RegistryValueKind.DWord);
        using (var owner = new RegistryPolicyOwner(registry)) owner.Set("Browser", "Retry", 1, RegistryValueKind.DWord);
        registry.FailNextPolicyWrite = true;
        try { using var owner = new RegistryPolicyOwner(registry); owner.Set("Browser", "Retry", 2, RegistryValueKind.DWord); }
        catch (IOException) { }
        using (var owner = new RegistryPolicyOwner(registry)) owner.Set("Browser", "Retry", null);
        Check((int)registry.Read("Browser", "Retry")! == 9, "Interrupted policy write lost the original setting.");
        var retriedPending = MobilePolicyCoordinator.Mutate(pending, "update-web-guard", "{\"webGuardEnabled\":false}", now.AddMinutes(1));
        Check(retriedPending.PendingChange?.ApplyAfterUtc == pending.PendingChange?.ApplyAfterUtc, "Retry extended policy delay.");
        Console.WriteLine($"V1 regressions passed: {checks} checks (core, commands, versioning, policy ownership).");
    }
    private sealed class MemoryRegistry : IPolicyRegistry
    {
        private readonly Dictionary<(string Path, string Name), (object Value, RegistryValueKind Kind)> _values = [];
        public object? Read(string path, string name) => _values.TryGetValue((path, name), out var value) ? value.Value : null;
        public RegistryValueKind Kind(string path, string name) => _values[(path, name)].Kind;
        public string[] Names(string path) => _values.Keys.Where(key => key.Path == path).Select(key => key.Name).ToArray();
        public bool FailNextPolicyWrite { get; set; }
        public void Write(string path, string name, object value, RegistryValueKind kind)
        {
            if (FailNextPolicyWrite && path == "Browser") { FailNextPolicyWrite = false; throw new IOException("Simulated interrupted write."); }
            _values[(path, name)] = (value, kind);
        }
        public void Delete(string path, string name) => _values.Remove((path, name));
    }
}
