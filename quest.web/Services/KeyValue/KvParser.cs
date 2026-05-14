using System.Text;

namespace quest.web.Services.KeyValue;

/// <summary>
/// Парсер ответа локальной модели в формате «[ KEY=value … ]».
/// Несколько записей подряд тоже поддерживаются. См. specs/F-format-strategy.md.
/// </summary>
public static class KvParser
{
    /// <summary>
    /// Парсит ровно одну запись из ответа модели. Бросает, если записей 0 или больше одной.
    /// </summary>
    public static KvRecord ParseSingle(string raw)
    {
        var all = ParseAll(raw);
        if (all.Count == 0)
            throw new KvParseException($"Ожидалась 1 запись, не найдено ни одной. Ответ: {Trunc(raw)}");
        if (all.Count > 1)
            throw new KvParseException($"Ожидалась 1 запись, получено {all.Count}. Ответ: {Trunc(raw)}");
        return all[0];
    }

    /// <summary>
    /// Парсит все записи из ответа модели подряд. Игнорирует пробелы, markdown-обёртки,
    /// текст вне блоков «[ … ]».
    /// </summary>
    public static IReadOnlyList<KvRecord> ParseAll(string raw)
    {
        var text = Sanitize(raw);
        var records = new List<KvRecord>();
        var i = 0;
        while (i < text.Length)
        {
            var open = IndexOfLine(text, i, "[");
            if (open < 0) break;

            var close = IndexOfLine(text, open + 1, "]");
            if (close < 0)
                throw new KvParseException($"Открыли '[' на позиции {open}, но не нашли закрывающую ']'. Ответ: {Trunc(raw)}");

            records.Add(ParseRecord(text, open + 1, close));
            i = close + 1;
        }
        return records;
    }

    private static KvRecord ParseRecord(string text, int from, int toExclusive)
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var lines = text.Substring(from, toExclusive - from).Split('\n');
        var li = 0;
        while (li < lines.Length)
        {
            var line = lines[li].Trim('\r', ' ', '\t');
            li++;
            if (line.Length == 0) continue;

            var heredocOpen = TryMatchHeredocOpen(line);
            if (heredocOpen is not null)
            {
                var (key, _) = heredocOpen.Value;
                var sb = new StringBuilder();
                var closed = false;
                while (li < lines.Length)
                {
                    var inner = lines[li].TrimEnd('\r');
                    li++;
                    if (inner.Trim() == ">>>")
                    {
                        closed = true;
                        break;
                    }
                    if (sb.Length > 0) sb.Append('\n');
                    sb.Append(inner);
                }
                if (!closed)
                    throw new KvParseException($"Heredoc для поля '{key}' не закрыт строкой '>>>'.");
                fields[key] = sb.ToString().Trim('\n');
                continue;
            }

            var eq = line.IndexOf('=');
            if (eq <= 0)
                throw new KvParseException($"Не распознана строка записи: «{line}». Ожидается «KEY=value» или «KEY<<<».");

            var key2 = line[..eq].Trim();
            var val = line[(eq + 1)..].Trim();
            if (key2.Length == 0)
                throw new KvParseException($"Пустой ключ в строке: «{line}».");
            fields[key2] = val;
        }
        if (fields.Count == 0)
            throw new KvParseException("Пустая запись «[…]» без полей.");
        return new KvRecord(fields);
    }

    private static (string Key, bool _)? TryMatchHeredocOpen(string line)
    {
        var idx = line.IndexOf("<<<", StringComparison.Ordinal);
        if (idx <= 0) return null;
        if (idx + 3 != line.Length) return null;
        var key = line[..idx].Trim();
        if (key.Length == 0) return null;
        return (key, true);
    }

    /// <summary>
    /// Находит строку, состоящую ровно из заданного маркера (после трима пробелов и \r).
    /// Возвращает позицию начала найденной строки (символ маркера), или -1.
    /// </summary>
    private static int IndexOfLine(string text, int from, string marker)
    {
        var pos = from;
        while (pos < text.Length)
        {
            var lineEnd = text.IndexOf('\n', pos);
            if (lineEnd < 0) lineEnd = text.Length;

            var line = text[pos..lineEnd].Trim('\r', ' ', '\t');
            if (line == marker)
            {
                var markerStart = text.IndexOf(marker, pos, lineEnd - pos, StringComparison.Ordinal);
                if (markerStart >= 0) return markerStart;
            }
            pos = lineEnd + 1;
        }
        return -1;
    }

    /// <summary>
    /// Снимает BOM и markdown-обёртки «```» вокруг ответа.
    /// </summary>
    private static string Sanitize(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";
        var t = raw.TrimStart('﻿').Trim();

        if (t.StartsWith("```"))
        {
            var firstNl = t.IndexOf('\n');
            if (firstNl > 0) t = t[(firstNl + 1)..];
            var lastFence = t.LastIndexOf("```", StringComparison.Ordinal);
            if (lastFence >= 0) t = t[..lastFence];
            t = t.Trim();
        }
        return t;
    }

    private static string Trunc(string s)
        => s.Length <= 500 ? s : s[..500] + "…";
}

public sealed class KvParseException : Exception
{
    public KvParseException(string message) : base(message) { }
}
