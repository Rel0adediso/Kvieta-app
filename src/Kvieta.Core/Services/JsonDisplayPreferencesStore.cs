using System.Text.Json;

namespace Kvieta.Core.Services;

public sealed class DisplayPreferences
{
    public int ZoomPercent { get; set; } = 100;
    public bool GuideShown { get; set; }
    public bool PairingPromptShown { get; set; }
}

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
}
