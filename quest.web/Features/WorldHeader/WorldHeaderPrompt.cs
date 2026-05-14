using quest.web.Services.KeyValue;

namespace quest.web.Features.WorldHeader;

/// <summary>
/// Промпт для шага 1 инициализации [Мира] — генерация 3 вариантов «обложки».
/// Формат ответа — общий kv (см. specs/F-format-strategy.md). Это единственное
/// место, где у нас несколько записей за один вызов (исключение из правила
/// «один артефакт = один вызов»).
/// </summary>
public static class WorldHeaderPrompt
{
    private static readonly KvFormatter.FieldSpec[] Fields =
    {
        new("NAME",    "название мира на русском, 1–4 слова без штампов",
            Example: "Хроники Зелёного Руина"),
        new("TAGLINE", "литературная аннотация задней обложки, 1–2 предложения до 320 символов",
            Example: "Когда города поглотил лес, а древние технологии стали ядом — только тот, кто найдёт последний ключ от Земли, спасёт оставшихся."),
    };

    public static string System { get; } = """
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
          Если указано два тона судьбы — СМЕШАЙ их: ищи общее
          настроение на пересечении обоих тонов.
            • Тон action / inception → динамичный, рваный ритм, глаголы.
            • Тон pre_storm → напряжённый, выжидающий, готовый к прорыву.
            • Тон slow_build / from_afar → медитативный, плавный.
            • Тон survival → давление, нехватка ресурсов, на самом краю, выживая.
            • Тон mystery → намёки, недосказанность, загадочность, туманность, детективный.
            • Тон moral → дилемма как фон, без раскрытия.
            • Тон drama → акцент на чувствах, отношениях.
            • Тон adventure → размах, манящие горизонты, эпичность.
            • Тон intimate → тёплый, камерный, игривый.
        - Три варианта различаются РАКУРСОМ, главным крючком, образами —
          но все три остаются в выбранных жанре, тоне и темпе.
        - Учти свободные пожелания игрока, если они даны.

        """ + KvFormatter.FormatInstruction(Fields, recordCount: 3);

    public static string BuildUserMessage(string? userHint, string? presetKey, string[]? fateKeys, string? pacingKey, string? scaleKey)
    {
        var hint = string.IsNullOrWhiteSpace(userHint) ? "нет, сюрприз" : userHint.Trim();
        var preset = string.IsNullOrWhiteSpace(presetKey) ? "не выбран" : presetKey.Trim();

        string fateLine;
        if (fateKeys is { Length: > 0 })
        {
            var parts = new List<string>();
            foreach (var fk in fateKeys)
            {
                if (WorldHeaderFates.All.TryGetValue(fk.Trim(), out var f))
                    parts.Add($"{fk} — {f.Label} ({f.Hint})");
            }
            fateLine = parts.Count > 0
                ? "Тон судьбы: " + string.Join(" + ", parts)
                : "Тон судьбы: не выбран";
        }
        else
        {
            fateLine = "Тон судьбы: не выбран";
        }

        string pacingLine;
        if (!string.IsNullOrWhiteSpace(pacingKey) && WorldHeaderPacings.All.TryGetValue(pacingKey.Trim(), out var p))
            pacingLine = $"Темп завязки: {pacingKey} — {p.Label} ({p.Hint})";
        else
            pacingLine = "Темп завязки: не выбран";

        string scaleLine;
        if (!string.IsNullOrWhiteSpace(scaleKey) && WorldHeaderScales.All.TryGetValue(scaleKey.Trim(), out var sc))
            scaleLine = $"Масштаб: {scaleKey} — {sc.Label} ({sc.Hint}), уровней локаций: {sc.Levels}";
        else
            scaleLine = "Масштаб: не выбран";

        return $"Пожелания игрока: «{hint}»\nПресет стиля: {preset}\n{fateLine}\n{pacingLine}\n{scaleLine}";
    }
}
