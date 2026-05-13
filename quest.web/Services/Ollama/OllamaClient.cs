using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace quest.web.Services.Ollama;

public sealed class OllamaClient
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _http;
    private readonly OllamaOptions _options;

    public OllamaClient(HttpClient http, IOptions<OllamaOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<OllamaTag>> ListInstalledAsync(CancellationToken ct = default)
    {
        var response = await _http.GetFromJsonAsync<TagsResponse>("/api/tags", ct);
        return response?.Models ?? new List<OllamaTag>();
    }

    /// <summary>
    /// Запрос к /api/generate с system+user-промптом и (опц.) JSON Schema в format.
    /// Возвращает текст ответа модели и метрики генерации.
    /// </summary>
    public async Task<OllamaGenerateResult> GenerateAsync(
        string model,
        string systemPrompt,
        string userPrompt,
        JsonNode? formatSchema = null,
        CancellationToken ct = default)
    {
        var request = new GenerateRequest(
            Model: model,
            System: string.IsNullOrEmpty(systemPrompt) ? null : systemPrompt,
            Prompt: userPrompt,
            Format: formatSchema,
            Stream: false);

        using var response = await _http.PostAsJsonAsync("/api/generate", request, JsonOpts, ct);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<GenerateResponse>(JsonOpts, ct)
                   ?? throw new InvalidOperationException("Empty response from Ollama");

        return new OllamaGenerateResult(
            Text: body.Response ?? "",
            Model: body.Model ?? model,
            PromptTokens: body.PromptEvalCount,
            CompletionTokens: body.EvalCount,
            DurationMs: body.TotalDuration is { } ns ? (int)(ns / 1_000_000) : null);
    }

    private sealed record TagsResponse(
        [property: JsonPropertyName("models")] List<OllamaTag> Models);

    private sealed record GenerateRequest(
        [property: JsonPropertyName("model")]  string Model,
        [property: JsonPropertyName("system")] string? System,
        [property: JsonPropertyName("prompt")] string Prompt,
        [property: JsonPropertyName("format")] JsonNode? Format,
        [property: JsonPropertyName("stream")] bool Stream);

    private sealed record GenerateResponse(
        [property: JsonPropertyName("model")]              string? Model,
        [property: JsonPropertyName("response")]           string? Response,
        [property: JsonPropertyName("done")]               bool Done,
        [property: JsonPropertyName("total_duration")]     long? TotalDuration,
        [property: JsonPropertyName("prompt_eval_count")]  int? PromptEvalCount,
        [property: JsonPropertyName("eval_count")]         int? EvalCount);
}

public sealed record OllamaGenerateResult(
    string Text,
    string Model,
    int? PromptTokens,
    int? CompletionTokens,
    int? DurationMs);

public sealed record OllamaTag(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("size")] long Size,
    [property: JsonPropertyName("details")] OllamaTagDetails? Details);

public sealed record OllamaTagDetails(
    [property: JsonPropertyName("family")] string? Family,
    [property: JsonPropertyName("parameter_size")] string? ParameterSize,
    [property: JsonPropertyName("quantization_level")] string? QuantizationLevel);
