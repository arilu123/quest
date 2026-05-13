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

        ЗАДАЧА: придумать ровно 3 варианта «обложки» мира — короткое
        название и одно литературное предложение-аннотацию. Это всего лишь
        заголовок, без раскрытия деталей. Локации, население, сюжет — НЕ
        придумывай.

        ТРЕБОВАНИЯ:
        - Названия: 1–4 слова, русский язык, без англицизмов и штампов
          («Хроники X», «Сага о Y» и т.п. — избегай).
        - Аннотации: ровно одно предложение, литературное, ёмкое, не более
          240 символов.
        - Три варианта должны заметно отличаться по жанру или настроению.
        - Учти пожелания игрока, если они даны.

        ФОРМАТ ОТВЕТА: только валидный JSON-объект, ровно по этому образцу.
        Используй ровно эти имена полей: "options", "name", "tagline".
        Никаких других полей, никаких пояснений, никакого markdown, никаких
        тройных кавычек. Только JSON-объект, начинающийся с «{».

        Образец:
        {
          "options": [
            { "name": "Название первое",  "tagline": "Аннотация первого мира одним предложением." },
            { "name": "Название второе",  "tagline": "Аннотация второго мира одним предложением." },
            { "name": "Название третье",  "tagline": "Аннотация третьего мира одним предложением." }
          ]
        }
        """;

    public static string BuildUserMessage(string? userHint, string? presetKey)
    {
        var hint = string.IsNullOrWhiteSpace(userHint) ? "нет, сюрприз" : userHint.Trim();
        var preset = string.IsNullOrWhiteSpace(presetKey) ? "не выбран" : presetKey.Trim();
        return $"Пожелания игрока: «{hint}»\nПресет: {preset}";
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
                  "tagline": { "type": "string", "minLength": 1, "maxLength": 240 }
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
