using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using quest.db;
using quest.web.Services.Ollama;

namespace quest.web.Features.WorldHeader;

public sealed class WorldHeaderService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly QuestDbContext _db;
    private readonly OllamaClient _ollama;
    private readonly OllamaOptions _ollamaOptions;
    private readonly ILogger<WorldHeaderService> _log;

    public WorldHeaderService(
        QuestDbContext db,
        OllamaClient ollama,
        IOptions<OllamaOptions> ollamaOptions,
        ILogger<WorldHeaderService> log)
    {
        _db = db;
        _ollama = ollama;
        _ollamaOptions = ollamaOptions.Value;
        _log = log;
    }

    public async Task<World> CreateWorldAsync(CancellationToken ct)
    {
        var world = new World
        {
            Id = Guid.NewGuid(),
            Status = WorldStatus.Initializing,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _db.Worlds.Add(world);
        await _db.SaveChangesAsync(ct);
        return world;
    }

    public async Task<GenerateResponse> GenerateAsync(
        Guid worldId,
        GenerateRequest request,
        CancellationToken ct)
    {
        var world = await _db.Worlds.FirstOrDefaultAsync(w => w.Id == worldId, ct)
            ?? throw new InvalidOperationException($"World {worldId} not found");

        var model = ResolveModel(request.Model);
        var preset = NormalizePreset(request.Preset);
        var userMessage = WorldHeaderPrompt.BuildUserMessage(request.UserHint, preset);

        var result = await _ollama.GenerateAsync(
            model: model,
            systemPrompt: WorldHeaderPrompt.System,
            userPrompt: userMessage,
            formatSchema: WorldHeaderPrompt.JsonSchema(),
            ct: ct);

        var parsed = ParseOptions(result.Text);

        var draft = new Artifact
        {
            Id = Guid.NewGuid(),
            WorldId = worldId,
            Kind = ArtifactKind.WorldHeader,
            Stage = ArtifactStage.Initialization,
            Version = 0,
            Status = ArtifactStatus.Draft,
            PayloadJson = JsonSerializer.Serialize(
                new DraftPayload(request.UserHint, preset, parsed),
                JsonOpts),
            Model = result.Model,
            Prompt = "[SYSTEM]\n" + WorldHeaderPrompt.System + "\n\n[USER]\n" + userMessage,
            RawResponse = result.Text,
            PromptTokens = result.PromptTokens,
            CompletionTokens = result.CompletionTokens,
            DurationMs = result.DurationMs,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _db.Artifacts.Add(draft);
        await _db.SaveChangesAsync(ct);

        return new GenerateResponse(draft.Id, parsed, result.Model, result.DurationMs);
    }

    public async Task<ApproveResponse> ApproveAsync(
        Guid worldId,
        Guid draftId,
        ApproveRequest request,
        CancellationToken ct)
    {
        var world = await _db.Worlds.FirstOrDefaultAsync(w => w.Id == worldId, ct)
            ?? throw new InvalidOperationException($"World {worldId} not found");

        var draft = await _db.Artifacts.FirstOrDefaultAsync(
            a => a.Id == draftId && a.WorldId == worldId && a.Kind == ArtifactKind.WorldHeader,
            ct) ?? throw new InvalidOperationException($"Draft {draftId} not found");

        if (draft.Status != ArtifactStatus.Draft)
            throw new InvalidOperationException($"Artifact {draftId} is not a Draft (status={draft.Status})");

        var draftPayload = JsonSerializer.Deserialize<DraftPayload>(draft.PayloadJson, JsonOpts)
            ?? throw new InvalidOperationException("Draft payload could not be parsed");

        if (request.ChosenIndex < 0 || request.ChosenIndex >= draftPayload.Options.Count)
            throw new InvalidOperationException(
                $"chosenIndex {request.ChosenIndex} out of range (count={draftPayload.Options.Count})");

        var chosen = draftPayload.Options[request.ChosenIndex];
        var rejected = draftPayload.Options
            .Where((_, i) => i != request.ChosenIndex)
            .ToList();

        // mark all existing WorldHeader artifacts for this world as Superseded
        var prior = await _db.Artifacts
            .Where(a => a.WorldId == worldId
                        && a.Kind == ArtifactKind.WorldHeader
                        && (a.Status == ArtifactStatus.Approved || a.Status == ArtifactStatus.Draft))
            .ToListAsync(ct);

        var nextVersion = 1;
        foreach (var a in prior)
        {
            if (a.Status == ArtifactStatus.Approved && a.Version >= nextVersion)
                nextVersion = a.Version + 1;
            a.Status = ArtifactStatus.Superseded;
        }

        var approved = new Artifact
        {
            Id = Guid.NewGuid(),
            WorldId = worldId,
            Kind = ArtifactKind.WorldHeader,
            Stage = ArtifactStage.Initialization,
            Version = nextVersion,
            Status = ArtifactStatus.Approved,
            PayloadJson = JsonSerializer.Serialize(
                new ApprovedPayload(chosen.Name, chosen.Tagline, draftPayload.UserHint, draftPayload.Preset, rejected),
                JsonOpts),
            Model = draft.Model,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _db.Artifacts.Add(approved);

        world.Title = chosen.Name;
        await _db.SaveChangesAsync(ct);

        return new ApproveResponse(approved.Id, approved.Version, chosen.Name, chosen.Tagline);
    }

    private string ResolveModel(string? requested)
    {
        if (string.IsNullOrWhiteSpace(requested))
            return _ollamaOptions.DefaultModel;

        var known = _ollamaOptions.Models.Select(m => m.Name).ToHashSet();
        if (!known.Contains(requested))
            throw new InvalidOperationException(
                $"Model '{requested}' is not in configured Ollama:Models. " +
                $"Known: {string.Join(", ", known)}");
        return requested;
    }

    private static string? NormalizePreset(string? presetKey)
    {
        if (string.IsNullOrWhiteSpace(presetKey)) return null;
        var key = presetKey.Trim();
        return WorldHeaderPresets.All.ContainsKey(key) ? key : null;
    }

    private static IReadOnlyList<WorldHeaderOption> ParseOptions(string responseText)
    {
        ModelOptionsResponse? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<ModelOptionsResponse>(responseText, JsonOpts);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Ollama returned invalid JSON: {ex.Message}. Response: {responseText}", ex);
        }

        if (parsed is null || parsed.Options is null || parsed.Options.Count != 3)
            throw new InvalidOperationException(
                $"Expected 3 options, got {parsed?.Options?.Count ?? 0}. Response: {responseText}");

        return parsed.Options;
    }
}
