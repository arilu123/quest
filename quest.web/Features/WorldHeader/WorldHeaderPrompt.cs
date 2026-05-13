using System.Text.Json.Nodes;

namespace quest.web.Features.WorldHeader;

/// <summary>
/// Промпт и JSON-схема для шага 1 инициализации [Мира].
/// См. specs/F-1.1-world-header.md.
/// </summary>
public static class WorldHeaderPrompt
{
    public const string System = """
        Ты — соавтор интерактивной книги-квеста. Сейчас идёт первый шаг
        инициализации нового [Мира] для игрока.

        ЗАДАЧА: придумать ровно 3 варианта «обложки» [Мира] — короткое
        название и литературную аннотацию в стиле задней обложки книги.

        АННОТАЦИЯ — это не описание сюжета, а ПРИГЛАШЕНИЕ ПРОЧИТАТЬ:
        ёмкий хук, атмосфера, обещание переживания. Без спойлеров, без
        раскрытия событий, без перечислений локаций и персонажей. Цель —
        чтобы читатель сразу захотел открыть книгу.

        ТРЕБОВАНИЯ:
        - Название: 1–4 слова, русский язык, без англицизмов и штампов
          («Хроники X», «Сага о Y» — избегай).
        - Аннотация: 1–2 предложения, до 320 символов, литературно,
          ёмко, интриговать без спойлеров.
        - Выбранные жанр (preset), тон судьбы (fate) и темп завязки
          (pacing) должны ЗВУЧАТЬ в тексте — атмосферой, ритмом,
          подбором образов и слов. НЕ упоминай их дословно
          («это история про моральный выбор» — НЕЛЬЗЯ).
            • Тон action / inception → динамичный, рваный ритм, глаголы.
            • Тон pre_storm → напряжённый, выжидающий.
            • Тон slow_build / from_afar → медитативный, плавный.
            • Тон survival → давление, нехватка, голос на краю.
            • Тон mystery → намёки, недосказанность.
            • Тон moral → дилемма как фон, без раскрытия.
            • Тон drama → акцент на чувстве, отношении.
            • Тон adventure → размах, манящие горизонты.
            • Тон intimate → тёплый, камерный, малый масштаб.
        - Три варианта различаются РАКУРСОМ, главным крючком, образами —
          но все три остаются в выбранных жанре, тоне и темпе.
        - Учти свободные пожелания игрока, если они даны.

        ФОРМАТ ОТВЕТА: только валидный JSON-объект, ровно по этому
        образцу. Используй ровно эти имена полей: "options", "name",
        "tagline". Никаких других полей, пояснений, markdown,
        тройных кавычек. Только JSON-объект, начинающийся с «{».

        Образец:
        {
          "options": [
            { "name": "Название первое",  "tagline": "Аннотация первого мира одним-двумя предложениями." },
            { "name": "Название второе",  "tagline": "Аннотация второго мира одним-двумя предложениями." },
            { "name": "Название третье",  "tagline": "Аннотация третьего мира одним-двумя предложениями." }
          ]
        }
        """;

    public static string BuildUserMessage(string? userHint, string? presetKey, string? fateKey, string? pacingKey)
    {
        var hint = string.IsNullOrWhiteSpace(userHint) ? "нет, сюрприз" : userHint.Trim();
        var preset = string.IsNullOrWhiteSpace(presetKey) ? "не выбран" : presetKey.Trim();

        string fateLine;
        if (!string.IsNullOrWhiteSpace(fateKey) && WorldHeaderFates.All.TryGetValue(fateKey.Trim(), out var f))
            fateLine = $"Тон судьбы: {fateKey} — {f.Label} ({f.Hint})";
        else
            fateLine = "Тон судьбы: не выбран";

        string pacingLine;
        if (!string.IsNullOrWhiteSpace(pacingKey) && WorldHeaderPacings.All.TryGetValue(pacingKey.Trim(), out var p))
            pacingLine = $"Темп завязки: {pacingKey} — {p.Label} ({p.Hint})";
        else
            pacingLine = "Темп завязки: не выбран";

        return $"Пожелания игрока: «{hint}»\nПресет стиля: {preset}\n{fateLine}\n{pacingLine}";
    }

    public static JsonNode JsonSchema() => JsonNode.Parse("""
        {
          "type": "object",
          "properties": {
            "options": {
              "type": "array",
              "minItems": 3,
              "maxItems": 3,
              "items": {
                "type": "object",
                "properties": {
                  "name":    { "type": "string", "minLength": 1, "maxLength": 60 },
                  "tagline": { "type": "string", "minLength": 1, "maxLength": 320 }
                },
                "required": ["name", "tagline"],
                "additionalProperties": false
              }
            }
          },
          "required": ["options"],
          "additionalProperties": false
        }
        """)!;
}
