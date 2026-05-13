namespace quest.web.Features.WorldHeader;

public sealed record WorldHeaderPacing(string Emoji, string Label, string Hint);

public static class WorldHeaderPacings
{
    /// <summary>
    /// Темп завязки — точка входа в [Историю] и ритм её начала.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, WorldHeaderPacing> All =
        new Dictionary<string, WorldHeaderPacing>
        {
            ["action"]     = new("🔥",   "Сразу в экшн",          "погоня/бой/кризис уже идут с первой страницы"),
            ["inception"]  = new("⚡",   "Прямо в момент",        "событие начинается на глазах, мы свидетели первой искры"),
            ["pre_storm"]  = new("⏳",   "Незадолго до событий",  "что-то надвигается, есть время оглядеться"),
            ["slow_build"] = new("🌱",   "Плавное развитие",      "спокойный заход, история раскрывается постепенно"),
            ["from_afar"]  = new("🕰️",  "Издалека",              "старт задолго до основного действия, неспешная эпопея"),
        };
}
