using System.Text;

namespace quest.web.Services.KeyValue;

/// <summary>
/// Помощники для генерации kv-инструкций в системном промпте.
/// Описание формата едино для всех моделей. См. specs/F-format-strategy.md.
/// </summary>
public static class KvFormatter
{
    public sealed record FieldSpec(string Key, string Description, bool Multiline = false, string? Example = null);

    /// <summary>
    /// Описание формата + образец. Кладётся в системный промпт перед задачей.
    /// </summary>
    public static string FormatInstruction(IReadOnlyList<FieldSpec> fields, int recordCount = 1)
    {
        if (fields.Count == 0)
            throw new ArgumentException("Нужно хотя бы одно поле", nameof(fields));

        var sb = new StringBuilder();
        sb.AppendLine("ФОРМАТ ОТВЕТА: только текст в указанном виде, без markdown,");
        sb.AppendLine("без блоков ```, без вступлений и заключений. Начинай ответ прямо с символа '['.");
        sb.AppendLine();

        if (recordCount == 1)
            sb.AppendLine("Выдай ровно ОДНУ запись. Структура записи:");
        else
            sb.AppendLine($"Выдай ровно {recordCount} запис(ей/ь) подряд, каждая по такой структуре:");

        sb.AppendLine("[");
        foreach (var f in fields)
        {
            if (f.Multiline)
            {
                sb.AppendLine($"{f.Key}<<<");
                sb.AppendLine($"{f.Description}");
                sb.AppendLine(">>>");
            }
            else
            {
                sb.AppendLine($"{f.Key}={f.Description}");
            }
        }
        sb.AppendLine("]");

        var hasExample = fields.Any(f => !string.IsNullOrEmpty(f.Example));
        if (hasExample)
        {
            sb.AppendLine();
            sb.AppendLine("Пример заполнения:");
            for (var n = 0; n < recordCount; n++)
            {
                sb.AppendLine("[");
                foreach (var f in fields)
                {
                    var example = f.Example ?? "…";
                    if (f.Multiline)
                    {
                        sb.AppendLine($"{f.Key}<<<");
                        sb.AppendLine(example);
                        sb.AppendLine(">>>");
                    }
                    else
                    {
                        sb.AppendLine($"{f.Key}={example}");
                    }
                }
                sb.AppendLine("]");
            }
        }

        sb.AppendLine();
        sb.AppendLine("ПРАВИЛА:");
        sb.AppendLine("- Имена полей — ровно как указано выше, заглавными латинскими буквами.");
        sb.AppendLine("- Одностроковое значение пишется в одной строке после '='.");
        sb.AppendLine("- Многострочное значение открывается 'KEY<<<' и закрывается '>>>' на отдельной строке.");
        sb.AppendLine("- Не добавляй никаких других полей или комментариев.");
        return sb.ToString();
    }
}
