using System.Reflection;
using Kvieta.Core.Services;

namespace Kvieta.SetupApp;

public partial class App : System.Windows.Application
{
    protected override async void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args is [StartupUpdate.WaitArgument])
        {
            try { await StartupUpdateInstaller.ResumeAfterUpdateAsync(); Shutdown(); }
            catch { Shutdown(ElevatedPackageInstaller.SetupFailedExitCode); }
            return;
        }
        if (e.Args is [StartupUpdate.InstallArgument])
        {
            try { Shutdown(await StartupUpdateInstaller.InstallAsSystemAsync()); }
            catch (Exception exception)
            {
                ElevatedPackageInstaller.WriteSetupDiagnostic("Startup update", exception);
                Shutdown(ElevatedPackageInstaller.SetupFailedExitCode);
            }
            return;
        }
        if (e.Args.Any(argument => string.Equals(argument, "--verify-package", StringComparison.OrdinalIgnoreCase)))
        {
            bool payloadExists = Assembly.GetExecutingAssembly()
                .GetManifestResourceNames()
                .Contains("Kvieta.Payload.msi", StringComparer.Ordinal);
            Shutdown(payloadExists ? 0 : 2);
            return;
        }

        if (e.Args.Any(argument => string.Equals(argument, "--elevated-install", StringComparison.OrdinalIgnoreCase)))
        {
            bool preserveInstallation = e.Args.Contains("--preserve-installation", StringComparer.Ordinal);
            bool desktopShortcut = e.Args.Any(argument =>
                string.Equals(argument, "--desktop-shortcut", StringComparison.OrdinalIgnoreCase));
            bool forceReinstall = e.Args.Any(argument =>
                string.Equals(argument, "--force-reinstall", StringComparison.OrdinalIgnoreCase));
            int guardianPayloadIndex = e.Args.ToList().FindIndex(argument =>
                string.Equals(argument, "--guardian-payload", StringComparison.OrdinalIgnoreCase));
            string? guardianPayload = guardianPayloadIndex >= 0 && guardianPayloadIndex + 1 < e.Args.Length
                ? e.Args[guardianPayloadIndex + 1]
                : null;
            int legacyUserIndex = Array.IndexOf(e.Args, "--legacy-user-sid");
            string? legacyUserSid = legacyUserIndex >= 0 && legacyUserIndex + 1 < e.Args.Length ? e.Args[legacyUserIndex + 1] : null;
            try
            {
                if (preserveInstallation)
                {
                    var installed = SetupWindow.ReadInstalledVersion();
                    var package = SetupWindow.GetPackageVersion();
                    bool validVersion = forceReinstall ? installed == package : installed < package;
                    if (installed is null || !validVersion || guardianPayload != null ||
                        SetupWindow.ReadInstallerValue("PackageKind") as string != "community")
                    {
                        Shutdown(1638);
                        return;
                    }
                }

                int result = await ElevatedPackageInstaller.RunAsync(
                    preserveInstallation ? null : desktopShortcut, guardianPayload, forceReinstall, legacyUserSid);
                if (result == 0) await StartupUpdateInstaller.ConfigureAsync();
                Shutdown(result);
            }
            catch (Exception exception)
            {
                ElevatedPackageInstaller.WriteSetupDiagnostic("Elevated installation", exception);
                Shutdown(ElevatedPackageInstaller.SetupFailedExitCode);
            }
            return;
        }

        if (e.Args.Any(argument => string.Equals(argument, "--elevated-reset-broken-protection", StringComparison.OrdinalIgnoreCase)))
        {
            Shutdown(ElevatedPackageInstaller.ResetBrokenProtection());
            return;
        }

        MainWindow = new SetupWindow(e.Args is ["--apply-stable-update"]);
        MainWindow.Show();
    }
}
