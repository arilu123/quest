using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using quest.db;
using quest.web.Features.WorldHeader;

namespace quest.web.Controllers;

[ApiController]
[Route("api/worlds")]
public sealed class WorldsApiController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly WorldHeaderService _headerService;
    private readonly QuestDbContext _db;
    private readonly ILogger<WorldsApiController> _log;

    public WorldsApiController(
        WorldHeaderService headerService,
        QuestDbContext db,
        ILogger<WorldsApiController> log)
    {
        _headerService = headerService;
        _db = db;
        _log = log;
    }

    [HttpPost("")]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var world = await _headerService.CreateWorldAsync(ct);
        return Ok(new { id = world.Id, status = world.Status.ToString(), createdAt = world.CreatedAt });
    }

    [HttpPost("{worldId:guid}/header/generate")]
    public async Task<IActionResult> Generate(
        Guid worldId,
        [FromBody] GenerateRequest request,
        CancellationToken ct)
    {
        try
        {
            var response = await _headerService.GenerateAsync(worldId, request, ct);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _log.LogWarning(ex, "Generate failed for world {WorldId}", worldId);
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status502BadGateway,
                title: "Generation failed");
        }
    }

    [HttpPost("{worldId:guid}/header/{draftId:guid}/approve")]
    public async Task<IActionResult> Approve(
        Guid worldId,
        Guid draftId,
        [FromBody] ApproveRequest request,
        CancellationToken ct)
    {
        try
        {
            var response = await _headerService.ApproveAsync(worldId, draftId, request, ct);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _log.LogWarning(ex, "Approve failed for draft {DraftId}", draftId);
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest,
                title: "Approve failed");
        }
    }

    [HttpGet("{worldId:guid}")]
    public async Task<IActionResult> Get(Guid worldId, CancellationToken ct)
    {
        var world = await _db.Worlds
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == worldId, ct);
        if (world is null) return NotFound();

        var header = await _db.Artifacts
            .AsNoTracking()
            .Where(a => a.WorldId == worldId
                        && a.Kind == ArtifactKind.WorldHeader
                        && a.Status == ArtifactStatus.Approved)
            .OrderByDescending(a => a.Version)
            .Select(a => new { a.Id, a.Version, a.PayloadJson, a.Model, a.CreatedAt })
            .FirstOrDefaultAsync(ct);

        object? headerOut = null;
        if (header is not null)
        {
            var payload = JsonSerializer.Deserialize<ApprovedPayload>(header.PayloadJson, JsonOpts);
            headerOut = new
            {
                id = header.Id,
                version = header.Version,
                model = header.Model,
                createdAt = header.CreatedAt,
                name = payload?.Name,
                tagline = payload?.Tagline,
                userHint = payload?.UserHint,
                preset = payload?.Preset,
            };
        }

        return Ok(new
        {
            id = world.Id,
            title = world.Title,
            status = world.Status.ToString(),
            createdAt = world.CreatedAt,
            header = headerOut,
        });
    }
}
