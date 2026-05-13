using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using quest.db;

namespace quest.web.Controllers;

[ApiController]
[Route("api/db")]
public sealed class DbController : ControllerBase
{
    private readonly QuestDbContext _db;

    public DbController(QuestDbContext db) => _db = db;

    [HttpGet("ping")]
    public async Task<IActionResult> Ping(CancellationToken ct)
    {
        var canConnect = await _db.Database.CanConnectAsync(ct);
        var serverVersion = canConnect
            ? await _db.Database.SqlQueryRaw<string>("SELECT version() AS \"Value\"").FirstAsync(ct)
            : null;
        return Ok(new { canConnect, serverVersion });
    }
}
