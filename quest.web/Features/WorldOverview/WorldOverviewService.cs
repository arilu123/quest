using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using quest.db;
using quest.web.Services.Ollama;

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
        var schema = WorldOverviewPrompt.JsonSchema();

        OllamaGenerateResult? result = null;
        (string Slug, string Name, string Description) parsed = default;

        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            result = await _ollama.GenerateAsync(model, systemPrompt, userMessage, schema, ct);

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
                parsed = ParseResponse(result.Text);
                break;
            }
            catch (InvalidOperationException ex) when (attempt < MaxRetries)
            {
                _log.LogWarning(ex, "WorldOverview attempt {Attempt}/{Max}: parse failed for world {WorldId}, retrying",
                    attempt, MaxRetries, worldId);
                await Task.Delay(1000, ct);
            }
        }

        var artifact = new Artifact
        {
            Id = Guid.NewGuid(),
            WorldId = worldId,
            Kind = ArtifactKind.World,
            ArtifactId = $"World.{parsed.Slug}",
            Name = parsed.Name,
            Stage = ArtifactStage.Initialization,
            Version = 1,
            Status = ArtifactStatus.Approved,
            PayloadJson = JsonSerializer.Serialize(new { description = parsed.Description }, JsonOpts),
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

    private static (string Slug, string Name, string Description) ParseResponse(string text)
    {
        var cleaned = text.Trim();
        if (cleaned.StartsWith("```"))
        {
            var firstNl = cleaned.IndexOf('\n');
            if (firstNl >= 0) cleaned = cleaned[(firstNl + 1)..];
            var lastFence = cleaned.LastIndexOf("```", StringComparison.Ordinal);
            if (lastFence >= 0) cleaned = cleaned[..lastFence];
        }

        JsonNode? root;
        try { root = JsonNode.Parse(cleaned.Trim()); }
        catch (JsonException)
        {
            var descStart = cleaned.IndexOf("\"description\"");
            if (descStart >= 0)
            {
                var valStart = cleaned.IndexOf('"', descStart + 13);
                if (valStart >= 0)
                {
                    var valEnd = cleaned.LastIndexOf('"');
                    if (valEnd > valStart)
                    {
                        var desc = cleaned.Substring(valStart + 1, valEnd - valStart - 1)
                            .Replace("\\n", "\n").Replace("\\\"", "\"");
                        var slugMatch = System.Text.RegularExpressions.Regex.Match(cleaned, @"""slug""\s*:\s*""([^""]+)""");
                        var nameMatch = System.Text.RegularExpressions.Regex.Match(cleaned, @"""name""\s*:\s*""([^""]+)""");
                        var slugVal = slugMatch.Success ? slugMatch.Groups[1].Value : "Unknown";
                        var nameVal = nameMatch.Success ? nameMatch.Groups[1].Value : "Без названия";
                        return (slugVal, nameVal, desc);
                    }
                }
            }
            throw new InvalidOperationException($"Invalid JSON in WorldOverview response. Raw: {text}");
        }

        if (root is not JsonObject obj)
            throw new InvalidOperationException($"Expected JSON object, got: {root?.GetType().Name}. Raw: {text}");

        var slug = obj["slug"]?.GetValue<string>()?.Trim()
            ?? throw new InvalidOperationException($"Missing 'slug' in response. Raw: {text}");
        var name = obj["name"]?.GetValue<string>()?.Trim()
            ?? throw new InvalidOperationException($"Missing 'name' in response. Raw: {text}");
        var description = obj["description"]?.GetValue<string>()?.Trim()
            ?? throw new InvalidOperationException($"Missing 'description' in response. Raw: {text}");

        return (slug, name, description);
    }
}
