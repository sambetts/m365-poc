using System.Text;
using System.Text.Json;
using Bot.Admin.Models;

namespace Bot.Admin.Services;

/// <summary>
/// Forwards join-call and start-script requests to the bot service over HTTP.
/// </summary>
public class BotProxyService : IBotProxyService
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public BotProxyService(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _baseUrl = configuration["BotService:BaseUrl"]?.TrimEnd('/')
            ?? throw new InvalidOperationException("Missing 'BotService:BaseUrl' configuration.");
    }

    /// <inheritdoc/>
    public async Task<JoinCallResponse> JoinCallAsync(JoinCallRequest request, CancellationToken ct = default)
    {
        var payload = new { request.JoinUrl, request.DisplayName };
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.PostAsync($"{_baseUrl}/joinCall", content, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<JoinCallResponse>(body, JsonOptions)
            ?? new JoinCallResponse();
    }

    /// <inheritdoc/>
    public async Task StartScriptAsync(string callId, string displayName, ScriptDto script, CancellationToken ct = default)
    {
        var payload = new
        {
            CallId = callId,
            DisplayName = displayName,
            Script = new
            {
                script.DefaultLanguage,
                Paragraphs = script.Paragraphs.Select(p => new
                {
                    p.Text,
                    p.Language,
                    p.PauseBeforeSeconds,
                    p.PauseAfterSeconds
                })
            }
        };

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.PostAsync($"{_baseUrl}/startScript", content, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
