using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using quest.db;
using quest.web.Features.WorldHeader;
using quest.web.Services.Ollama;

namespace quest.web.Controllers;

public sealed class WorldsController : Controller
{
    private readonly QuestDbContext _db;
    private readonly OllamaOptions _ollama;

    public WorldsController(QuestDbContext db, IOptions<OllamaOptions> ollama)
    {
        _db = db;
        _ollama = ollama.Value;
    }

    [HttpGet("/Worlds/Start")]
    public IActionResult Start()
    {
        ViewData["Title"] = "Новый Мир";
        ViewBag.Presets = WorldHeaderPresets.All
            .Select(kv => new { key = kv.Key, label = kv.Value.Label, hint = kv.Value.Hint })
            .ToArray();
        ViewBag.Fates = WorldHeaderFates.All
            .Select(kv => new { key = kv.Key, emoji = kv.Value.Emoji, label = kv.Value.Label, hint = kv.Value.Hint })
            .ToArray();
        ViewBag.Pacings = WorldHeaderPacings.All
            .Select(kv => new { key = kv.Key, emoji = kv.Value.Emoji, label = kv.Value.Label, hint = kv.Value.Hint })
            .ToArray();
        ViewBag.Scales = WorldHeaderScales.All
            .Select(kv => new { key = kv.Key, emoji = kv.Value.Emoji, label = kv.Value.Label, hint = kv.Value.Hint })
            .ToArray();
        ViewBag.Models = _ollama.Models
            .Select(m => new { name = m.Name, family = m.Family, note = m.Note })
            .ToArray();
        ViewBag.DefaultModel = _ollama.DefaultModel;
        return View();
    }

    [HttpGet("/Worlds/{id:guid}")]
    public async Task<IActionResult> Show(Guid id, CancellationToken ct)
    {
        var world = await _db.Worlds.AsNoTracking().FirstOrDefaultAsync(w => w.Id == id, ct);
        if (world is null) return NotFound();
        ViewData["Title"] = world.Title ?? "Мир без названия";
        return View(world);
    }
}
