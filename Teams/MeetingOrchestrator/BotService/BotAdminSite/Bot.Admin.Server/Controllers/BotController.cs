using Bot.Admin.Models;
using Bot.Admin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bot.Admin.Controllers;

/// <summary>
/// Proxies bot orchestration commands (join call, start script) to the bot service.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BotController(IBotProxyService botProxy, IScriptStorageService storage) : ControllerBase
{
    /// <summary>
    /// Joins a bot to a Teams meeting.
    /// </summary>
    [HttpPost("join")]
    public async Task<IActionResult> JoinCall([FromBody] JoinCallRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.JoinUrl))
            return BadRequest("JoinUrl is required.");

        var result = await botProxy.JoinCallAsync(request, ct).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Starts a saved script on an active bot instance.
    /// </summary>
    [HttpPost("start-script")]
    public async Task<IActionResult> StartScript([FromBody] StartScriptRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.CallId))
            return BadRequest("CallId is required.");
        if (string.IsNullOrWhiteSpace(request.DisplayName))
            return BadRequest("DisplayName is required.");
        if (string.IsNullOrWhiteSpace(request.ScriptId))
            return BadRequest("ScriptId is required.");

        var script = await storage.GetByIdAsync(request.ScriptId, ct).ConfigureAwait(false);
        if (script is null)
            return NotFound($"Script '{request.ScriptId}' not found.");

        await botProxy.StartScriptAsync(request.CallId, request.DisplayName, script, ct).ConfigureAwait(false);
        return Accepted(new { request.CallId, request.DisplayName, request.ScriptId, status = "script_started" });
    }
}
