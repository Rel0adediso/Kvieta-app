using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Windows;
using Kvieta.Core.Services;

namespace Kvieta.SetupApp;

internal static class StartupUpdateInstaller
{
    public static async Task ConfigureAsync()
    {
        if (SetupWindow.ReadInstallerValue("PackageKind") as string != "community") return;
        try
        {
            var info = new ProcessStartInfo(StartupUpdate.Executable) { UseShellExecute = false, CreateNoWindow = true };
            info.ArgumentList.Add(StartupUpdate.RegisterArgument);
            using var process = Process.Start(info);
            if (process == null) return;
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(25));
            await process.WaitForExitAsync(timeout.Token);
            if (process.ExitCode != 0 || !StartupUpdate.Enabled || Environment.ProcessPath is not { } source) return;

            var version = SetupWindow.GetPackageVersion();
            string destination = StartupUpdate.SetupPath(version);
            if (!File.Exists(destination))
            {
                string temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    await using (var input = File.OpenRead(source))
                    await using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 128 * 1024, useAsync: true))
                        await input.CopyToAsync(output, timeout.Token);
                    File.Move(temporary, destination);
                }
                finally { File.Delete(temporary); }
            }

            foreach (string file in Directory.EnumerateFiles(StartupUpdate.CacheDirectory, StableSetupCache.FilePattern))
            {
                if (StableSetupCache.ReadVersion(file) is { } cachedVersion && cachedVersion < version)
                {
                    File.Delete(file);
                    File.Delete(file + ".json");
                }
            }
        }
        catch (Exception exception)
        {
            ElevatedPackageInstaller.WriteSetupDiagnostic("Startup update registration or cache preparation", exception);
        }
    }

    public static async Task<int> InstallAsSystemAsync()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var version = SetupWindow.GetPackageVersion();
        if (!identity.IsSystem || !StartupUpdate.Enabled ||
            !string.Equals(Environment.ProcessPath, StartupUpdate.SetupPath(version), StringComparison.OrdinalIgnoreCase) ||
            SetupWindow.ReadInstallerValue("PackageKind") as string != "community" ||
            SetupWindow.ReadInstalledVersion() is not { } installed || installed >= version ||
            StartupUpdate.ReadAttempt() is not { ExitCode: null } attempt || attempt.Version != version ||
            DateTimeOffset.UtcNow - attempt.StartedAt >= StartupUpdate.InstallationTimeout)
            return 5;

        int result = ElevatedPackageInstaller.SetupFailedExitCode;
        try
        {
            result = await ElevatedPackageInstaller.RunAsync(null, null, forceReinstall: false);
            if (result == 0 && SetupWindow.ReadInstalledVersion() != version)
                result = ElevatedPackageInstaller.SetupFailedExitCode;
            if (result == 0)
                await ConfigureAsync();
            return result;
        }
        catch (Exception exception)
        {
            ElevatedPackageInstaller.WriteSetupDiagnostic("Startup update installation", exception);
            return result = ElevatedPackageInstaller.SetupFailedExitCode;
        }
        finally
        {
            StartupUpdate.WriteAttempt(attempt with { ExitCode = result });
        }
    }

    public static async Task ResumeAfterUpdateAsync()
    {
        using var identity = WindowsIdentity.GetCurrent();
        if (!Environment.UserInteractive || identity.IsSystem) return;
        using var waiter = new Mutex(true, @"Local\KvietaStartupUpdateWaiter", out bool ownsMutex);
        if (!ownsMutex) return;

        try
        {
            var installed = SetupWindow.ReadInstalledVersion();
            var previousLabel = SetupWindow.ReadInstallerValue("InstalledReleaseLabel") as string;
            var target = SetupWindow.GetPackageVersion();
            if (StartupUpdate.Enabled && installed is not null && installed < target &&
                string.Equals(Environment.ProcessPath, StartupUpdate.SetupPath(target), StringComparison.OrdinalIgnoreCase))
            {
                int? status = await RequestAndWaitAsync(target);
                if (status is 1641 or 3010)
                {
                    MessageBox.Show("Güncellemeyi tamamlamak için Windows'u yeniden başlat.\nRestart Windows to finish the update.", "Kvieta");
                    return;
                }
                if (status == 0)
                    await new JsonDisplayPreferencesStore().RememberPreviousReleaseAsync(new ReleaseIdentity(
                        previousLabel ?? installed.ToString(3), installed.ToString(3)));
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            // Continue to the installed version if the update is unavailable.
        }
        finally
        {
            waiter.ReleaseMutex();
        }

        var launch = new ProcessStartInfo(StartupUpdate.Executable) { UseShellExecute = false };
        launch.ArgumentList.Add(StartupUpdate.ResumeArgument);
        using var application = Process.Start(launch);
    }

    private static async Task<int?> RequestAndWaitAsync(Version target)
    {
        // A retry can leave the previous attempt on disk until the scheduled task starts.
        var earliestStart = DateTimeOffset.UtcNow - StartupUpdate.InstallationTimeout;
        var clock = Stopwatch.StartNew();
        if (!await StartupUpdate.RequestTaskAsync(StartupUpdate.ApplyTask)) return null;

        while (clock.Elapsed < StartupUpdate.InstallationTimeout)
        {
            if (StartupUpdate.ReadAttempt() is { } attempt && attempt.Version == target &&
                attempt.StartedAt >= earliestStart)
            {
                if (attempt.ExitCode != null) return attempt.ExitCode;
            }
            else if (clock.Elapsed >= TimeSpan.FromSeconds(25))
            {
                return null;
            }
            await Task.Delay(250);
        }
        return null;
    }
}
