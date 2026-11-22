using System.IO;
using System.Text.Json;
using Kvieta.Core.Models;
using Kvieta.Core.Services;

namespace Kvieta.App.Services;

public sealed record ReleaseNotesEntry(string Version, string[] En, string[] Tr, string? Build = null)
{
    public string Heading => Build is null ? Version : $"{Version} · {Build}";
    public IReadOnlyList<string> Changes => LocalizationService.CurrentLanguage == LanguagePreference.English ? En : Tr;
}

public static class ReleaseNotesCatalog
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public static IReadOnlyList<ReleaseNotesEntry> ForRelease(ReleaseNotice notice)
    {
        using Stream stream = typeof(ReleaseNotesCatalog).Assembly.GetManifestResourceStream("Kvieta.ReleaseNotes.json")
            ?? throw new InvalidDataException("Release notes are missing.");
        return Select(JsonSerializer.Deserialize<ReleaseNotesEntry[]>(stream, Options) ?? [], notice);
    }

    public static IReadOnlyList<ReleaseNotesEntry> Select(IEnumerable<ReleaseNotesEntry> entries, ReleaseNotice notice)
    {
        return entries.Where(entry =>
        {
            if (!ReleaseVersion.IsNewer("0.0.0", entry.Version)) return false;
            if (ReleaseVersion.IsNewer(notice.Current.Label, entry.Version)) return false;
            if (entry.Build != null)
            {
                if (!Version.TryParse(entry.Build, out var build) ||
                    !Version.TryParse(notice.Current.Build, out var current) || build > current) return false;
                if (notice.Previous != null && Version.TryParse(notice.Previous.Build, out var previous))
                    return build > previous;
            }
            return notice.Previous != null
                ? ReleaseVersion.IsNewer(notice.Previous.Label, entry.Version)
                : !ReleaseVersion.IsNewer(entry.Version, notice.Current.Label);
        }).OrderByDescending(entry => entry, Comparer<ReleaseNotesEntry>.Create((left, right) =>
        {
            if (ReleaseVersion.IsNewer(left.Version, right.Version)) return -1;
            if (ReleaseVersion.IsNewer(right.Version, left.Version)) return 1;
            Version.TryParse(left.Build, out var leftBuild);
            Version.TryParse(right.Build, out var rightBuild);
            return Comparer<Version?>.Default.Compare(leftBuild, rightBuild);
        })).ToArray();
    }

    public static string FormatRange(ReleaseNotice notice)
    {
        string current = ReleaseVersion.ToDisplayName(notice.Current.Label);
        if (notice.Previous is not { } previous) return current;
        string earlier = ReleaseVersion.ToDisplayName(previous.Label);
        return string.Equals(earlier, current, StringComparison.OrdinalIgnoreCase)
            ? $"{earlier} · {previous.Build} → {current} · {notice.Current.Build}"
            : $"{earlier} → {current}";
    }
}
