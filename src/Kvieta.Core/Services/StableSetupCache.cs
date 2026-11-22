namespace Kvieta.Core.Services;

public static class StableSetupCache
{
    private const string Prefix = "Kvieta-Setup-";
    private const string Suffix = "-win-x64.exe";
    public const string FilePattern = Prefix + "*" + Suffix;

    public static string DirectoryPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Kvieta", "Updates", "Stable");

    public static string FileName(string version) => Prefix + version + Suffix;

    public static Version? ReadVersion(string path)
    {
        string name = Path.GetFileName(path);
        return name.Length > Prefix.Length + Suffix.Length &&
            name.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase) &&
            name.EndsWith(Suffix, StringComparison.OrdinalIgnoreCase) &&
            Version.TryParse(name[Prefix.Length..^Suffix.Length], out var version) ? version : null;
    }
}
