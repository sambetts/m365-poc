using Bot.Admin.Models;

namespace Bot.Admin.Services;

/// <summary>
/// Proxies HTTP requests to the running bot service.
/// </summary>
public interface IBotProxyService
{
    /// <summary>
    /// Joins a bot to a Teams meeting.
    /// </summary>
    Task<JoinCallResponse> JoinCallAsync(JoinCallRequest request, CancellationToken ct = default);

    /// <summary>
    /// Starts a speech script on an active call.
    /// </summary>
    Task StartScriptAsync(string callId, string displayName, ScriptDto script, CancellationToken ct = default);
}
