using System.Globalization;
using System.Text;

namespace quest.web.Services.Slug;

/// <summary>
/// Транслитерация русского текста в PascalCase-латиницу для использования в ArtifactId.
/// Источник идентификатора — русское «человеческое» NAME артефакта, которое генерирует модель.
/// См. specs/F-format-strategy.md, раздел «Идентификаторы артефактов делает приложение».
/// </summary>
public static class Slugger
{
    private static readonly Dictionary<char, string> Map = new()
    {
        ['а'] = "a", ['б'] = "b", ['в'] = "v", ['г'] = "g", ['д'] = "d",
        ['е'] = "e", ['ё'] = "yo", ['ж'] = "zh", ['з'] = "z", ['и'] = "i",
        ['й'] = "y", ['к'] = "k", ['л'] = "l", ['м'] = "m", ['н'] = "n",
        ['о'] = "o", ['п'] = "p", ['р'] = "r", ['с'] = "s", ['т'] = "t",
        ['у'] = "u", ['ф'] = "f", ['х'] = "kh", ['ц'] = "ts", ['ч'] = "ch",
        ['ш'] = "sh", ['щ'] = "sch", ['ъ'] = "", ['ы'] = "y", ['ь'] = "",
        ['э'] = "e", ['ю'] = "yu", ['я'] = "ya",
    };

    /// <summary>
    /// Переводит произвольную строку в PascalCase-идентификатор:
    /// «Хроники Зелёного Руина» → "KhronikiZelyonogoRuina".
    /// «  город-крепость» → "GorodKrepost".
    /// Цифры сохраняются, всё остальное удаляется.
    /// </summary>
    public static string ToPascal(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "Unnamed";

        var normalized = input.Normalize(NormalizationForm.FormKC);
        var words = new List<StringBuilder>();
        var current = new StringBuilder();

        foreach (var raw in normalized)
        {
            var ch = char.ToLowerInvariant(raw);

            if (Map.TryGetValue(ch, out var lat))
            {
                current.Append(lat);
                continue;
            }

            if (ch >= 'a' && ch <= 'z')
            {
                current.Append(ch);
                continue;
            }

            if (ch >= '0' && ch <= '9')
            {
                current.Append(ch);
                continue;
            }

            if (current.Length > 0)
            {
                words.Add(current);
                current = new StringBuilder();
            }
        }
        if (current.Length > 0) words.Add(current);

        if (words.Count == 0)
            return "Unnamed";

        var result = new StringBuilder();
        foreach (var w in words)
        {
            var s = w.ToString();
            if (s.Length == 0) continue;
            result.Append(char.ToUpperInvariant(s[0]));
            if (s.Length > 1) result.Append(s, 1, s.Length - 1);
        }
        return result.Length == 0 ? "Unnamed" : result.ToString();
    }
}
