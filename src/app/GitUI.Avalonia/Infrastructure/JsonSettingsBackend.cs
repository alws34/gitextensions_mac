using System.Text.Json;

namespace GitUI.Avalonia.Infrastructure;

public class JsonSettingsBackend : ISettingsBackend
{
    public static string DefaultConfigPath =>
        Path.Join(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config",
            "gitextensions",
            "settings.json");

    private readonly string _path;
    private Dictionary<string, JsonElement> _data;

    public JsonSettingsBackend(string? path = null)
    {
        _path = path ?? DefaultConfigPath;
        _data = Load();
    }

    public string GetString(string key, string defaultValue)
    {
        if (_data.TryGetValue(key, out JsonElement el) && el.ValueKind == JsonValueKind.String)
        {
            return el.GetString() ?? defaultValue;
        }

        return defaultValue;
    }

    public void SetString(string key, string value)
        => _data[key] = JsonSerializer.SerializeToElement(value);

    public bool GetBool(string key, bool defaultValue)
    {
        if (_data.TryGetValue(key, out JsonElement el))
        {
            if (el.ValueKind == JsonValueKind.True)
            {
                return true;
            }

            if (el.ValueKind == JsonValueKind.False)
            {
                return false;
            }
        }

        return defaultValue;
    }

    public void SetBool(string key, bool value)
        => _data[key] = JsonSerializer.SerializeToElement(value);

    public int GetInt(string key, int defaultValue)
    {
        if (_data.TryGetValue(key, out JsonElement el) && el.TryGetInt32(out int v))
        {
            return v;
        }

        return defaultValue;
    }

    public void SetInt(string key, int value)
        => _data[key] = JsonSerializer.SerializeToElement(value);

    public IReadOnlyList<string> GetStringList(string key)
    {
        if (_data.TryGetValue(key, out JsonElement el) && el.ValueKind == JsonValueKind.Array)
        {
            return el.EnumerateArray()
                     .Where(e => e.ValueKind == JsonValueKind.String)
                     .Select(e => e.GetString()!)
                     .ToList();
        }

        return [];
    }

    public void SetStringList(string key, IEnumerable<string> values)
        => _data[key] = JsonSerializer.SerializeToElement(values.ToArray());

    public void Save()
    {
        string? dir = Path.GetDirectoryName(_path);
        if (dir is not null && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(_path, JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true }));
    }

    private Dictionary<string, JsonElement> Load()
    {
        if (!File.Exists(_path))
        {
            return [];
        }

        try
        {
            JsonDocument doc = JsonDocument.Parse(File.ReadAllText(_path));
            return doc.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone());
        }
        catch
        {
            return [];
        }
    }
}
