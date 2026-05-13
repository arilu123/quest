using System.Text.Json.Nodes;

namespace quest.web.Features.WorldOverview;

public static class WorldOverviewPrompt
{
    public const string System = """
        Ты — соавтор интерактивной книги-квеста. Сейчас идёт шаг
        инициализации [Сеттинга] — внутреннего описания мира для
        ИИ-ведущего. Игрок этот текст НЕ видит.

        ЗАДАЧА: составить общее описание [Мира] — где происходит
        история, что это за место, как оно устроено в целом.
        Это НЕ аннотация и НЕ литературный текст. Это содержательное,
        конкретное описание для ИИ, который будет вести игру.

        ТРЕБОВАНИЯ:
        - Описание должно быть содержательным: что это за место,
          его устройство, масштаб, ключевые характеристики.
        - Объём определяй сам: для маленького мира хватит одного
          предложения, для огромного — несколько абзацев.
          Главное — дать ИИ достаточный контекст для генерации
          локаций, населения и предыстории.
        - Учти выбранный масштаб: он определяет размер мира.
        - Учти жанр, тон судьбы, темп завязки — пусть они
          чувствуются в описании мира.
        - Придумай короткий английский PascalCase-идентификатор
          для мира (одно-два слова без пробелов, например
          "ShipNostromo", "UndergroundBunker", "GalacticFrontier").

        ФОРМАТ ОТВЕТА: только валидный JSON-объект с полями:
        - "slug" — английский PascalCase идентификатор
        - "name" — название мира на русском (уже известно, повтори его)
        - "description" — описание мира

        Образец:
        {
          "slug": "UndergroundBunker",
          "name": "Последний свет",
          "description": "Действие происходит в подземном бункере..."
        }
        """;

    public static string BuildUserMessage(
        string worldName,
        string? tagline,
        string? userHint,
        string? presetKey,
        string[]? fateKeys,
        string? pacingKey,
        string? scaleKey)
    {
        var hint = string.IsNullOrWhiteSpace(userHint) ? "нет" : userHint.Trim();
        var preset = string.IsNullOrWhiteSpace(presetKey) ? "не выбран" : presetKey.Trim();

        string fateLine;
        if (fateKeys is { Length: > 0 })
        {
            var parts = new List<string>();
            foreach (var fk in fateKeys)
            {
                if (WorldHeader.WorldHeaderFates.All.TryGetValue(fk.Trim(), out var f))
                    parts.Add($"{f.Label} ({f.Hint})");
            }
            fateLine = parts.Count > 0 ? string.Join(" + ", parts) : "не выбран";
        }
        else
        {
            fateLine = "не выбран";
        }

        string pacingLine;
        if (!string.IsNullOrWhiteSpace(pacingKey) && WorldHeader.WorldHeaderPacings.All.TryGetValue(pacingKey.Trim(), out var p))
            pacingLine = $"{p.Label} ({p.Hint})";
        else
            pacingLine = "не выбран";

        string scaleLine;
        if (!string.IsNullOrWhiteSpace(scaleKey) && WorldHeader.WorldHeaderScales.All.TryGetValue(scaleKey.Trim(), out var sc))
            scaleLine = $"{sc.Label} ({sc.Hint}), уровней: {sc.Levels}";
        else
            scaleLine = "не выбран";

        return $"Название мира: «{worldName}»\n" +
               $"Аннотация: «{tagline}»\n" +
               $"Пожелания игрока: «{hint}»\n" +
               $"Пресет стиля: {preset}\n" +
               $"Тон судьбы: {fateLine}\n" +
               $"Темп завязки: {pacingLine}\n" +
               $"Масштаб: {scaleLine}";
    }

    public static JsonNode JsonSchema() => JsonNode.Parse("""
        {
          "type": "object",
          "properties": {
            "slug":        { "type": "string", "minLength": 1, "maxLength": 64, "pattern": "^[A-Z][a-zA-Z0-9]*$" },
            "name":        { "type": "string", "minLength": 1, "maxLength": 120 },
            "description": { "type": "string", "minLength": 10 }
          },
          "required": ["slug", "name", "description"],
          "additionalProperties": false
        }
        """)!;
}
