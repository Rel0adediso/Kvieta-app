using System.Text.Json;

namespace Kvieta.Core.Services;

public sealed class DisplayPreferences
{
    public int ZoomPercent { get; set; } = 100;
    public bool GuideShown { get; set; }
    public bool PairingPromptShown { get; set; }
    public ReleaseIdentity? LastOpenedRelease { get; set; }
    public ReleaseIdentity? LastUpdateFrom { get; set; }
    public bool UpdateNoticeUnread { get; set; }
}

public sealed record ReleaseIdentity(string Label, string Build)
{
    public bool IsNewerThan(ReleaseIdentity previous)
    {
        if (Version.TryParse(Build, out var currentBuild) && Version.TryParse(previous.Build, out var previousBuild))
        {
            int comparison = currentBuild.CompareTo(previousBuild);
            if (comparison != 0) return comparison > 0;
        }
        return ReleaseVersion.IsNewer(previous.Label, Label);
    }
}

public sealed record ReleaseNotice(ReleaseIdentity Current, ReleaseIdentity? Previous, bool Unread);

public sealed class JsonDisplayPreferencesStore
{
    private readonly ResilientJsonFile<DisplayPreferences> _file;
    public JsonDisplayPreferencesStore(string? filePath = null)
    {
        string path = filePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Kvieta", "display-preferences.json");
        _file = new(path, new JsonSerializerOptions(), () => new DisplayPreferences(), value =>
        {
            int zoom = Math.Clamp(value.ZoomPercent, 100, 150);
            bool changed = zoom != value.ZoomPercent;
            value.ZoomPercent = zoom;
            return new MigrationResult<DisplayPreferences>(value, changed);
        });
    }
    public Task<DisplayPreferences> LoadAsync() => _file.LoadAsync();
    public async Task SaveAsync(int zoom) => await _file.UpdateAsync(value =>
    {
        value.ZoomPercent = Math.Clamp(zoom, 100, 150);
        return value;
    });

    public async Task<bool> TryClaimGuideAsync()
    {
        bool claimed = false;
        await _file.UpdateAsync(value =>
        {
            claimed = !value.GuideShown;
            value.GuideShown = true;
            return value;
        });
        return claimed;
    }

    public async Task<bool> TryClaimPairingPromptAsync()
    {
        bool claimed = false;
        await _file.UpdateAsync(value =>
        {
            claimed = !value.PairingPromptShown;
            value.PairingPromptShown = true;
            return value;
        });
        return claimed;
    }

    public async Task RememberPreviousReleaseAsync(ReleaseIdentity previous)
    {
        await _file.UpdateAsync(value =>
        {
            value.LastOpenedRelease ??= previous;
            return value;
        });
    }

    public async Task<ReleaseNotice> ObserveReleaseAsync(ReleaseIdentity current)
    {
        DisplayPreferences preferences = await _file.UpdateAsync(value =>
        {
            if (value.LastOpenedRelease is null)
                value.LastOpenedRelease = current;
            else if (current.IsNewerThan(value.LastOpenedRelease))
            {
                if (!value.UpdateNoticeUnread || value.LastUpdateFrom is null)
                    value.LastUpdateFrom = value.LastOpenedRelease;
                value.LastOpenedRelease = current;
                value.UpdateNoticeUnread = true;
            }
            return value;
        });
        bool matches = preferences.LastOpenedRelease == current;
        return new ReleaseNotice(current, matches ? preferences.LastUpdateFrom : null,
            matches && preferences.UpdateNoticeUnread);
    }

    public async Task AcknowledgeReleaseAsync(ReleaseIdentity current)
    {
        await _file.UpdateAsync(value =>
        {
            if (value.LastOpenedRelease == current) value.UpdateNoticeUnread = false;
            return value;
        });
    }
}
