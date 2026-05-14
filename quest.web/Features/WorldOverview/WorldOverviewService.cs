using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using quest.db;
using quest.web.Services.KeyValue;
using quest.web.Services.Ollama;
using quest.web.Services.Slug;

namespace quest.web.Features.WorldOverview;

public sealed class WorldOverviewService
{
    private const int MaxRetries = 3;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly QuestDbContext _db;
    private readonly OllamaClient _ollama;
    private readonly OllamaOptions _ollamaOptions;
    private readonly ILogger<WorldOverviewService> _log;

    public WorldOverviewService(
        QuestDbContext db,
        OllamaClient ollama,
        IOptions<OllamaOptions> ollamaOptions,
        ILogger<WorldOverviewService> log)
    {
        _db = db;
        _ollama = ollama;
        _ollamaOptions = ollamaOptions.Value;
        _log = log;
    }

    public async Task<Artifact> GenerateAsync(Guid worldId, CancellationToken ct)
    {
        var world = await _db.Worlds.FirstOrDefaultAsync(w => w.Id == worldId, ct)
            ?? throw new InvalidOperationException($"World {worldId} not found");

        var headerArtifact = await _db.Artifacts
            .AsNoTracking()
            .Where(a => a.WorldId == worldId
                        && a.Kind == ArtifactKind.WorldHeader
                        && a.Status == ArtifactStatus.Approved)
            .OrderByDescending(a => a.Version)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"No approved WorldHeader for world {worldId}");

        var headerPayload = JsonSerializer.Deserialize<WorldHeader.ApprovedPayload>(headerArtifact.PayloadJson, JsonOpts)
            ?? throw new InvalidOperationException("Cannot parse WorldHeader payload");

        var model = world.GeneratorModel ?? _ollamaOptions.DefaultModel;

        var fates = DeserializeFates(world.Fates);
        var userMessage = WorldOverviewPrompt.BuildUserMessage(
            headerPayload.Name,
            headerPayload.Tagline,
            headerPayload.UserHint,
            headerPayload.Preset,
            fates,
            world.Pacing,
            world.Scale);

        var systemPrompt = WorldOverviewPrompt.System;

        OllamaGenerateResult? result = null;
        string description = "";

        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            result = await _ollama.GenerateAsync(model, systemPrompt, userMessage, ct: ct);

            if (string.IsNullOrWhiteSpace(result.Text))
            {
                _log.LogWarning("WorldOverview attempt {Attempt}/{Max}: model {Model} returned empty response for world {WorldId}",
                    attempt, MaxRetries, model, worldId);
                if (attempt < MaxRetries)
                {
                    await Task.Delay(1000, ct);
                    continue;
                }
                throw new InvalidOperationException($"Model {model} returned empty response after {MaxRetries} attempts for world {worldId}");
            }

            try
            {
                description = ParseDescription(result.Text);
                break;
            }
            catch (InvalidOperationException ex) when (attempt < MaxRetries)
            {
                _log.LogWarning(ex, "WorldOverview attempt {Attempt}/{Max}: parse failed for world {WorldId}, retrying",
                    attempt, MaxRetries, worldId);
                await Task.Delay(1000, ct);
            }
        }

        var slug = Slugger.ToPascal(headerPayload.Name);

        var artifact = new Artifact
        {
            Id = Guid.NewGuid(),
            WorldId = worldId,
            Kind = ArtifactKind.World,
            ArtifactId = $"World.{slug}",
            Name = headerPayload.Name,
            Stage = ArtifactStage.Initialization,
            Version = 1,
            Status = ArtifactStatus.Approved,
            PayloadJson = JsonSerializer.Serialize(new { description }, JsonOpts),
            Model = result!.Model,
            Prompt = "[SYSTEM]\n" + systemPrompt + "\n\n[USER]\n" + userMessage,
            RawResponse = result.Text,
            PromptTokens = result.PromptTokens,
            CompletionTokens = result.CompletionTokens,
            DurationMs = result.DurationMs,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _db.Artifacts.Add(artifact);
        await _db.SaveChangesAsync(ct);

        return artifact;
    }

    private static string[]? DeserializeFates(string? fatesJson)
    {
        if (string.IsNullOrWhiteSpace(fatesJson)) return null;
        try { return JsonSerializer.Deserialize<string[]>(fatesJson); }
        catch { return null; }
    }

    private static string ParseDescription(string raw)
    {
        KvRecord record;
        try { record = KvParser.ParseSingle(raw); }
        catch (KvParseException ex)
        {
            throw new InvalidOperationException(
                $"Не удалось распарсить ответ модели как kv-запись: {ex.Message}. Сырой ответ: {raw}", ex);
        }

        if (!record.TryGet("DESCR", out var description) || string.IsNullOrWhiteSpace(description))
            throw new InvalidOperationException(
                $"В записи нет поля DESCR. Поля: {string.Join(", ", record.Fields.Keys)}. Сырой ответ: {raw}");

        return description.Trim();
    }
}
