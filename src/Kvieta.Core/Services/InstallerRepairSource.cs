namespace Kvieta.Core.Services;

public static class InstallerRepairSource
{
    // Windows Installer records this source name during the original installation.
    public static string Path => System.IO.Path.Combine(StartupUpdate.InstallDirectory, "Kvieta.msi");
}
