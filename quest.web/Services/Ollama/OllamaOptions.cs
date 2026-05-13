namespace quest.web.Services.Ollama;

public sealed class OllamaOptions
{
    public const string SectionName = "Ollama";

    public string BaseUrl { get; set; } = "http://localhost:11434";
    public int RequestTimeoutSeconds { get; set; } = 600;
    public string DefaultModel { get; set; } = "";
    public string EmbeddingModel { get; set; } = "";

    /// <summary>
    /// Упорядоченный список моделей. Порядок в конфиге = порядок в UI (более подходящие сверху).
    /// </summary>
    public List<OllamaModelEntry> Models { get; set; } = new();
}

public sealed class OllamaModelEntry
{
    public string Name { get; set; } = "";
    public string Family { get; set; } = "";
    public string? Note { get; set; }
}
