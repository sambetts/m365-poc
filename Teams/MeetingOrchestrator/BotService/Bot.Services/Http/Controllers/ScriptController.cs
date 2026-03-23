using Bot.Model.Constants;
using Bot.Model.Models;
using Bot.Services.Contract;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph.Communications.Common.Telemetry;
using System;
using System.Text.Json;

namespace Bot.Services.Http.Controllers;

/// <summary>
/// Starts a speech script on an active call identified by call ID and bot display name.
/// </summary>
[ApiController]
[Route("")]
public class ScriptController : ControllerBase
{
    private readonly IGraphLogger _logger;
    private readonly IBotService _botService;

    public ScriptController(IGraphLogger logger, IBotService botService)
    {
        _logger = logger;
        _botService = botService;
    }

    /// <summary>
    /// Starts a speech script on the specified call handler.
    /// </summary>
    [HttpPost(HttpRouteConstants.StartScript)]
    public IActionResult StartScript([FromBody] StartScriptBody body)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(body?.CallId))
                return BadRequest("CallId is required.");
            if (string.IsNullOrWhiteSpace(body.DisplayName))
                return BadRequest("DisplayName is required.");
            if (body.Script?.Paragraphs is not { Count: > 0 })
                return BadRequest("Script with at least one paragraph is required.");

            if (!_botService.CallHandlers.TryGetValue(body.CallId, out var handler))
                return NotFound($"No active call with ID '{body.CallId}'.");

            if (!string.Equals(handler.DisplayName, body.DisplayName, StringComparison.OrdinalIgnoreCase))
                return NotFound($"Call '{body.CallId}' exists but display name '{body.DisplayName}' does not match (expected '{handler.DisplayName}').");

            var scriptJson = JsonSerializer.Serialize(body.Script);
            _logger.Info($"[ScriptController] Starting script on call {body.CallId} (bot={body.DisplayName})");
            handler.StartScript(scriptJson);

            return Accepted(new { body.CallId, body.DisplayName, status = "script_started" });
        }
        catch (Exception e)
        {
            _logger.Error(e, $"[ScriptController] Failed to start script");
            return StatusCode(500, e.Message);
        }
    }
}
