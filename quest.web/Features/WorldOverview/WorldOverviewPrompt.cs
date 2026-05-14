using quest.web.Services.KeyValue;

namespace quest.web.Features.WorldOverview;

/// <summary>
/// Промпт шага «общее описание [Мира]». Один артефакт за вызов.
/// Slug строит приложение (см. specs/F-format-strategy.md), модель этого не делает.
/// </summary>
public static class WorldOverviewPrompt
{
    private static readonly KvFormatter.FieldSpec[] Fields =
    {
        new("DESCR",
            "содержательное описание мира для ИИ-ведущего: что это за место, " +
            "его устройство, масштаб, ключевые характеристики (можно несколько абзацев)",
            Multiline: true,
            Example: "Действие происходит в подземном бункере, последнем убежище человечества после Великой Пыли...\nВ бункере живёт около двух тысяч человек, разделённых на касты по доступу к ресурсам..."),
    };

    public static string System { get; } = """
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
        - Не повторяй название мира и не пересказывай аннотацию —
          они уже даны во входных данных. Описывай само устройство.

        """ + KvFormatter.FormatInstruction(Fields, recordCount: 1);

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
            scaleLine = $"{sc.Label} (Например: {sc.Hint}), будет уровней вложенности: {sc.Levels}, но сейчас тебе нужно описать только верхиний уровень";
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
}
