namespace GitUI.Avalonia.Infrastructure;

public interface ISettingsBackend
{
    string GetString(string key, string defaultValue);

    void SetString(string key, string value);

    bool GetBool(string key, bool defaultValue);

    void SetBool(string key, bool value);

    int GetInt(string key, int defaultValue);

    void SetInt(string key, int value);

    IReadOnlyList<string> GetStringList(string key);

    void SetStringList(string key, IEnumerable<string> values);

    void Save();
}
