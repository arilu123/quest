namespace quest.web.Services.KeyValue;

/// <summary>
/// Одна запись формата «[ KEY=value … ]», полученная от локальной модели.
/// Ключи case-insensitive. См. specs/F-format-strategy.md.
/// </summary>
public sealed class KvRecord
{
    private readonly Dictionary<string, string> _fields;

    public KvRecord(Dictionary<string, string> fields)
    {
        _fields = new Dictionary<string, string>(fields, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyDictionary<string, string> Fields => _fields;

    public string this[string key]
        => _fields.TryGetValue(key, out var v)
            ? v
            : throw new KeyNotFoundException($"KvRecord: поле '{key}' отсутствует. Есть: {string.Join(", ", _fields.Keys)}");

    public bool TryGet(string key, out string value) => _fields.TryGetValue(key, out value!);

    public string GetOrEmpty(string key) => _fields.TryGetValue(key, out var v) ? v : "";
}
