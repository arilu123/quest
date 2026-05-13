namespace quest.db;

public sealed class Artifact
{
    public Guid Id { get; set; }
    public Guid WorldId { get; set; }

    public ArtifactKind Kind { get; set; }
    public ArtifactStage Stage { get; set; }
    public int Version { get; set; }
    public ArtifactStatus Status { get; set; }

    public string PayloadJson { get; set; } = "{}";

    public string? Model { get; set; }
    public string? Prompt { get; set; }
    public string? RawResponse { get; set; }
    public int? PromptTokens { get; set; }
    public int? CompletionTokens { get; set; }
    public int? DurationMs { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public World World { get; set; } = null!;
}
