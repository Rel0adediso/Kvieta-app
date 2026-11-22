using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;
using Velopack;
using Velopack.Locators;

namespace Kvieta.Desktop;

internal static class DesktopUpdates
{
    private static readonly string DataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Kvieta", "DesktopUpdates");
    private static readonly string DisabledPreferencePath = Path.Combine(DataDirectory, "automatic-updates-disabled");
    private static readonly string FeedPath = Path.Combine(DataDirectory, "prepared-feed.json");
    private static readonly string? PublicKey = typeof(DesktopUpdates).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .FirstOrDefault(attribute => attribute.Key == "KvietaDesktopPublicKey")?.Value;

    public static bool Enabled => !File.Exists(DisabledPreferencePath);

    public static bool Available
    {
        get
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                if (identity.IsSystem || new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator) ||
                    !Environment.UserInteractive || string.IsNullOrWhiteSpace(PublicKey)) return false;
                string expectedRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    SignedDesktopSource.PackageId);
                var locator = VelopackLocator.Current;
                return locator.AppId == SignedDesktopSource.PackageId &&
                    locator.RootAppDir != null &&
                    string.Equals(Path.GetFullPath(locator.RootAppDir).TrimEnd(Path.DirectorySeparatorChar),
                        expectedRoot, StringComparison.OrdinalIgnoreCase) && CreateManager().IsInstalled;
            }
            catch { return false; }
        }
    }

    public static void SetEnabled(bool enabled)
    {
        if (enabled)
        {
            if (File.Exists(DisabledPreferencePath)) File.Delete(DisabledPreferencePath);
        }
        else
        {
            Directory.CreateDirectory(DataDirectory);
            File.WriteAllText(DisabledPreferencePath, "disabled");
        }
    }

    private static UpdateManager CreateManager(SignedDesktopSource? source = null) =>
        new(source ?? new SignedDesktopSource(PublicKey!), new UpdateOptions { ExplicitChannel = SignedDesktopSource.Channel });

    public static void ApplyPreparedOnStartup()
    {
        if (!Enabled || !Available) return;
        try
        {
            // A second window may hold the current version open. Leave it running and
            // retry on a later launch instead of having the updater terminate it.
            if (HasOtherDesktopInstance()) return;
            var manager = CreateManager();
            if (manager.UpdatePendingRestart is not { } pending || !File.Exists(FeedPath) ||
                new FileInfo(FeedPath).Length > 1024 * 1024) return;
            var feed = SignedDesktopSource.Verify(File.ReadAllBytes(FeedPath), PublicKey!);
            // The SDK's pending asset is read from the local package and has no feed checksum.
            var signed = feed.Feed.Assets.SingleOrDefault(asset => SignedDesktopSource.SamePackageIdentity(asset, pending));
            if (signed == null || manager.CurrentVersion == null || signed.Version <= manager.CurrentVersion) return;
            string? packages = VelopackLocator.Current.PackagesDir;
            if (string.IsNullOrEmpty(packages) || !SignedDesktopSource.MatchesPrepared(feed, signed, Path.Combine(packages, signed.FileName))) return;
            manager.ApplyUpdatesAndRestart(pending);
        }
        catch { /* An interrupted or invalid update never prevents this desktop from opening. */ }
    }

    private static bool HasOtherDesktopInstance()
    {
        string? currentPath = Environment.ProcessPath;
        string? installRoot = VelopackLocator.Current.RootAppDir;
        if (string.IsNullOrWhiteSpace(currentPath) || string.IsNullOrWhiteSpace(installRoot)) return true;
        using var current = Process.GetCurrentProcess();
        foreach (Process process in Process.GetProcessesByName(current.ProcessName))
        {
            using (process)
            {
                if (process.Id == current.Id) continue;
                try
                {
                    if (IsDesktopProcessInInstall(process.MainModule?.FileName, currentPath, installRoot))
                        return true;
                }
                catch
                {
                    // Inaccessible same-name processes are treated as potentially active instances.
                    return true;
                }
            }
        }
        return false;
    }

    internal static bool IsDesktopProcessInInstall(string? processPath, string currentPath, string installRoot)
    {
        if (string.IsNullOrWhiteSpace(processPath)) return true;
        string root = Path.GetFullPath(installRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        string candidate = Path.GetFullPath(processPath);
        return candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(Path.GetFileName(candidate), Path.GetFileName(currentPath), StringComparison.OrdinalIgnoreCase);
    }

    public static async Task<bool> PrepareAsync(CancellationToken token)
    {
        if (!Enabled || !Available) return false;
        var source = new SignedDesktopSource(PublicKey!);
        var manager = CreateManager(source);
        var update = await manager.CheckForUpdatesAsync().ConfigureAwait(false);
        if (update == null || update.IsDowngrade) return false;
        await manager.DownloadUpdatesAsync(update, cancelToken: token).ConfigureAwait(false);
        if (!Enabled || source.VerifiedEnvelope is not { } envelope) return false;
        string? packages = VelopackLocator.Current.PackagesDir;
        if (string.IsNullOrEmpty(packages) ||
            !SignedDesktopSource.MatchesPrepared(SignedDesktopSource.Verify(envelope, PublicKey!),
                update.TargetFullRelease, Path.Combine(packages, update.TargetFullRelease.FileName)))
            throw new InvalidDataException("The prepared desktop package did not match the signed release.");
        string temp = FeedPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            Directory.CreateDirectory(DataDirectory);
            await File.WriteAllBytesAsync(temp, envelope, token).ConfigureAwait(false);
            File.Move(temp, FeedPath, overwrite: true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
        return true;
    }
}
