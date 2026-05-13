namespace quest.db;

public sealed class World
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string? Fates { get; set; }
    public string? Pacing { get; set; }
    public WorldStatus Status { get; set; } = WorldStatus.Initializing;
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Artifact> Artifacts { get; set; } = new List<Artifact>();
}
