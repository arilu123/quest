using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using quest.web.Services.Ollama;

namespace quest.web.Controllers;

[ApiController]
[Route("api/ollama")]
public sealed class OllamaController : ControllerBase
{
    private readonly OllamaClient _client;
    private readonly OllamaOptions _options;

    public OllamaController(OllamaClient client, IOptions<OllamaOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    [HttpGet("configured")]
    public IActionResult Configured() => Ok(new
    {
        _options.BaseUrl,
        _options.DefaultModel,
        _options.EmbeddingModel,
        _options.Models
    });

    [HttpGet("installed")]
    public async Task<IActionResult> Installed(CancellationToken ct)
    {
        var models = await _client.ListInstalledAsync(ct);
        return Ok(models);
    }
}
