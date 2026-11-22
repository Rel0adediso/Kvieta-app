using System.Reflection;
using Kvieta.Core.Services;

namespace Kvieta.App.Services;

public static class BuildInfo
{
    private static readonly Assembly EntryAssembly = typeof(BuildInfo).Assembly;

#if KVIETA_DEVELOPMENT_BUILD
    public static bool IsDevelopmentBuild => true;
    public const string Flavor = "development";
#else
    public static bool IsDevelopmentBuild => false;
    public const string Flavor = "public";
#endif

    public static string AssemblyVersion
    {
        get
        {
            System.Version? version = EntryAssembly.GetName().Version;
            return version is null ? "unknown" : $"{version.Major}.{version.Minor}.{Math.Max(0, version.Build)}";
        }
    }

    public static string InformationalVersion =>
        EntryAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? AssemblyVersion;

    public static string Version => InformationalVersion.Split('+')[0];

    public static string? StablePublicKey => MetadataValue("KvietaStablePublicKey");
    public static int GuardianRevision => int.TryParse(MetadataValue("KvietaGuardianRevision"), out int revision) ? revision : 0;

    public static string DisplayVersion => ToDisplayReleaseName(Version);

    public static string ToDisplayReleaseName(string? releaseLabel) => ReleaseVersion.ToDisplayName(releaseLabel);

    public static string RepositoryCommit => MetadataValue("RepositoryCommit") is { Length: > 0 } value
        ? value
        : InformationalVersion.Split('.', '+')
            .LastOrDefault(part => part.Length == 40 && part.All(Uri.IsHexDigit)) ?? "unknown";

    public static bool IsRepositoryDirty =>
        bool.TryParse(MetadataValue("RepositoryDirty"), out bool dirty) && dirty;

    public static string DisplayRevision
    {
        get
        {
            string revision = RepositoryCommit == "unknown"
                ? "unknown"
                : RepositoryCommit[..Math.Min(12, RepositoryCommit.Length)];
            return IsRepositoryDirty ? $"{revision}-dirty" : revision;
        }
    }

    private static string? MetadataValue(string key) => EntryAssembly
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .FirstOrDefault(attribute => string.Equals(attribute.Key, key, StringComparison.Ordinal))
        ?.Value;
}
