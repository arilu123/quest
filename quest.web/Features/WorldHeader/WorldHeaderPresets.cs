namespace quest.web.Features.WorldHeader;

public static class WorldHeaderPresets
{
    public static readonly IReadOnlyDictionary<string, WorldHeaderPreset> All =
        new Dictionary<string, WorldHeaderPreset>
        {
            ["fantasy_high"] = new("Высокое фэнтези", "Высокое фэнтези: магия, расы, эпический масштаб"),
            ["fantasy_dark"] = new("Мрачное фэнтези", "Мрачное фэнтези: тяжёлый мир, неоднозначная мораль"),
            ["scifi_far"]    = new("Sci-Fi (далёкое будущее)", "Далёкое будущее: космос, межзвёздные путешествия"),
            ["cyberpunk"]    = new("Киберпанк", "Киберпанк: корпорации, импланты, плотная урбанистика"),
            ["post_apoc"]    = new("Постапокалипсис", "Постапокалипсис: руины цивилизации, дефицит"),
            ["horror"]       = new("Хоррор", "Хоррор: давление, страх, неизвестное"),
            ["detective"]    = new("Детектив", "Детектив: расследование, тайна, реалистичный мир"),
            ["mythic"]       = new("Мифы и легенды", "Мифы и легенды: древние силы, культурные архетипы"),
            ["fairytale"]    = new("Сказка", "Сказка: волшебный, светлый, для всех возрастов"),
            ["mundane"]      = new("Бытовое", "Бытовое, реалистичное, без магии и фантастики"),
        };
}

public sealed record WorldHeaderPreset(string Label, string Hint);
