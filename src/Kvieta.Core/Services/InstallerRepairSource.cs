namespace Kvieta.Core.Services;

public static class InstallerRepairSource
{
    public static string GetPath(Guid productCode) =>
        Path.Combine(StartupUpdate.InstallDirectory, $"Kvieta-Repair-{productCode:D}.msi");
}
