namespace GorkyIDE.Abstractions.Settings;

public interface ISettingsStore
{
    string? Get(string key);

    void Set(string key, string value);

    bool Remove(string key);
}
