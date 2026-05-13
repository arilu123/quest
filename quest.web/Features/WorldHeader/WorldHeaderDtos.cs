using System.Text.Json.Serialization;

namespace quest.web.Features.WorldHeader;

public sealed record WorldHeaderOption(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("tagline")] string Tagline);

public sealed record ModelOptionsResponse(
    [property: JsonPropertyName("options")] List<WorldHeaderOption> Options);

public sealed record GenerateRequest(
    string? UserHint,
    string? Preset,
    string? Model);

public sealed record GenerateResponse(
    Guid DraftId,
    IReadOnlyList<WorldHeaderOption> Options,
    string Model,
    int? DurationMs);

public sealed record ApproveRequest(int ChosenIndex);

public sealed record ApproveResponse(
    Guid ApprovedId,
    int Version,
    string Name,
    string Tagline);

public sealed record DraftPayload(
    string? UserHint,
    string? Preset,
    IReadOnlyList<WorldHeaderOption> Options);

public sealed record ApprovedPayload(
    string Name,
    string Tagline,
    string? UserHint,
    string? Preset,
    IReadOnlyList<WorldHeaderOption> RejectedOptions);
