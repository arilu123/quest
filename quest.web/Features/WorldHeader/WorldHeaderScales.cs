namespace quest.web.Features.WorldHeader;

public sealed record WorldHeaderScale(string Emoji, string Label, string Hint, int Levels);

public static class WorldHeaderScales
{
    public static readonly IReadOnlyDictionary<string, WorldHeaderScale> All =
        new Dictionary<string, WorldHeaderScale>
        {
            ["room"]     = new("🏠", "Комнатный",  "комната, дом, замкнутое пространство",     1),
            ["compact"]  = new("🏘️", "Малый",      "деревня, корабль, малый город",            2),
            ["regional"] = new("🏰", "Средний",    "город с районами, остров, область",         3),
            ["grand"]    = new("🌍", "Крупный",    "страна, континент, планета",                4),
            ["cosmic"]   = new("🌌", "Эпический",  "вселенная, галактика, мультиверс",          5),
        };
}
