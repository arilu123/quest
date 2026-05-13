namespace quest.web.Features.WorldHeader;

public sealed record WorldHeaderFate(string Emoji, string Label, string Hint);

public static class WorldHeaderFates
{
    /// <summary>
    /// Тон [Истории] — обещание типа переживания. Влияет на промпт всех шагов инициализации.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, WorldHeaderFate> All =
        new Dictionary<string, WorldHeaderFate>
        {
            ["adventure"]  = new("🎢", "Яркие приключения",     "путешествия, открытия, размах"),
            ["moral"]      = new("⚖️", "Непростой моральный выбор", "дилеммы, серая мораль"),
            ["mystery"]    = new("🧩", "Загадки и тайны",       "расследование, скрытое прошлое"),
            ["drama"]      = new("🌹", "Драма и отношения",     "чувства, конфликты персонажей"),
            ["survival"]   = new("😱", "Выживание под давлением", "высокие ставки, нехватка ресурсов, на краю гибели"),
            ["intimate"]   = new("🌅", "Камерная история",      "атмосфера, малые радости, тихий тон"),
        };
}
