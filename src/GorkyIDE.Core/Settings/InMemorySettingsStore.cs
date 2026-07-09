using System.Collections.Concurrent;
using GorkyIDE.Abstractions.Settings;

namespace GorkyIDE.Core.Settings;

public sealed class InMemorySettingsStore : ISettingsStore
{
    private readonly ConcurrentDictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);

    public string? Get(string key) => values.TryGetValue(key, out var value) ? value : null;

    public void Set(string key, string value) => values[key] = value;

    public bool Remove(string key) => values.TryRemove(key, out _);
}
