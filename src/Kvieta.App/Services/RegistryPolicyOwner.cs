using System.Text.Json;
using Microsoft.Win32;

namespace Kvieta.App.Services;

/// <summary>A write-ahead journal: restore only values still equal to our last write.</summary>
public sealed class RegistryPolicyOwner : IDisposable
{
    public const string JournalPath = @"Software\Kvieta\WebGuardOwnership";
    private readonly IPolicyRegistry _registry;
    private readonly string _journalPath;
    private readonly Dictionary<string, Entry> _entries;

    public RegistryPolicyOwner(RegistryKey root, string journalPath = JournalPath) : this(new WindowsPolicyRegistry(root), journalPath) { }

    public RegistryPolicyOwner(IPolicyRegistry registry, string journalPath = JournalPath)
    {
        _registry = registry;
        _journalPath = journalPath;
        string? json = registry.Read(journalPath, "State") as string;
        _entries = json is null ? new(StringComparer.OrdinalIgnoreCase) :
            JsonSerializer.Deserialize<Dictionary<string, Entry>>(json) ?? throw new InvalidOperationException("Invalid policy ownership journal.");
    }

    public void Set(string path, string name, object? desired, RegistryValueKind kind = RegistryValueKind.String)
    {
        string id = path + "\n" + name;
        object? current = _registry.Read(path, name);
        if (_entries.TryGetValue(id, out Entry? entry))
        {
            // Someone else changed this setting. Relinquish ownership and preserve their value.
            if (!EqualsValue(current, entry.Applied, entry.Kind) &&
                !(entry.Pending && (entry.PreviousApplied is null ? current is null : EqualsValue(current, entry.PreviousApplied.Value, entry.PreviousKind))))
            {
                if (desired is not null) throw new InvalidOperationException("Policy changed outside Kvieta; preserving the external setting.");
                _entries.Remove(id);
                Persist();
                return;
            }
            if (desired is null)
            {
                if (entry.Original is null) _registry.Delete(path, name);
                else _registry.Write(path, name, Decode(entry.Original.Value, entry.OriginalKind), entry.OriginalKind);
                _entries.Remove(id);
                Persist();
                return;
            }
        }
        else
        {
            if (desired is null) return;
            entry = new(path, name, current is null ? null : JsonSerializer.SerializeToElement(current),
                current is null ? RegistryValueKind.None : _registry.Kind(path, name),
                JsonSerializer.SerializeToElement(desired), kind);
        }
        _entries[id] = entry with
        {
            Applied = JsonSerializer.SerializeToElement(desired),
            Kind = kind,
            Pending = true,
            PreviousApplied = current is null ? null : JsonSerializer.SerializeToElement(current),
            PreviousKind = current is null ? RegistryValueKind.None : _registry.Kind(path, name)
        };
        Persist(); // Never change the policy without first persisting its previous value.
        _registry.Write(path, name, desired!, kind);
        _entries[id] = _entries[id] with { Pending = false, PreviousApplied = null };
        Persist();
    }

    public void SetList(string path, IReadOnlyList<string> values)
    {
        foreach (Entry entry in _entries.Values.Where(e => e.Path.Equals(path, StringComparison.OrdinalIgnoreCase)).ToArray())
            Set(path, entry.Name, null);
        if (values.Count == 0) return;
        HashSet<string> present = _registry.Names(path).Select(n => _registry.Read(path, n) as string)
            .OfType<string>().ToHashSet(StringComparer.OrdinalIgnoreCase);
        int slot = 1;
        foreach (string value in values.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (present.Contains(value)) continue;
            while (_registry.Read(path, slot.ToString(System.Globalization.CultureInfo.InvariantCulture)) is not null) slot++;
            Set(path, slot.ToString(System.Globalization.CultureInfo.InvariantCulture), value);
            slot++;
        }
    }

    private void Persist() => _registry.Write(_journalPath, "State", JsonSerializer.Serialize(_entries), RegistryValueKind.String);
    private static bool EqualsValue(object? value, JsonElement applied, RegistryValueKind kind) =>
        value is not null && JsonSerializer.Serialize(value) == JsonSerializer.Serialize(Decode(applied, kind));
    private static object Decode(JsonElement value, RegistryValueKind kind) => kind switch
    {
        RegistryValueKind.DWord => value.GetInt32(),
        RegistryValueKind.QWord => value.GetInt64(),
        RegistryValueKind.MultiString => value.Deserialize<string[]>()!,
        RegistryValueKind.Binary => value.Deserialize<byte[]>()!,
        _ => value.GetString()!
    };
    public void Dispose() { }
    public sealed record Entry(string Path, string Name, JsonElement? Original, RegistryValueKind OriginalKind,
        JsonElement Applied, RegistryValueKind Kind, bool Pending = false, JsonElement? PreviousApplied = null, RegistryValueKind PreviousKind = RegistryValueKind.None);
}

public interface IPolicyRegistry
{
    object? Read(string path, string name);
    RegistryValueKind Kind(string path, string name);
    string[] Names(string path);
    void Write(string path, string name, object value, RegistryValueKind kind);
    void Delete(string path, string name);
}

internal sealed class WindowsPolicyRegistry(RegistryKey root) : IPolicyRegistry
{
    public object? Read(string path, string name)
    {
        using RegistryKey? key = root.OpenSubKey(path);
        return key?.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
    }
    public RegistryValueKind Kind(string path, string name)
    {
        using RegistryKey key = root.OpenSubKey(path) ?? throw new InvalidOperationException("Policy missing.");
        return key.GetValueKind(name);
    }
    public string[] Names(string path)
    {
        using RegistryKey? key = root.OpenSubKey(path);
        return key?.GetValueNames() ?? [];
    }
    public void Write(string path, string name, object value, RegistryValueKind kind)
    {
        using RegistryKey key = root.CreateSubKey(path);
        key.SetValue(name, value, kind);
    }
    public void Delete(string path, string name)
    {
        using RegistryKey? key = root.OpenSubKey(path, true);
        key?.DeleteValue(name, false);
    }
}
