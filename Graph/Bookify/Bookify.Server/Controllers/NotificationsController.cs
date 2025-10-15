using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Bookify.Server.Services;

namespace Bookify.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly ILogger<NotificationsController> _logger;
    private readonly IBookingService _bookingService;

    public NotificationsController(ILogger<NotificationsController> logger, IBookingService bookingService)
    {
        _logger = logger;
        _bookingService = bookingService;
    }

    // Microsoft Graph validation handshake (GET with validationToken) per docs:
    // https://learn.microsoft.com/graph/webhooks#notification-endpoint-validation
    [HttpGet]
    public IActionResult Get([FromQuery(Name = "validationToken")] string? validationToken)
    {
        if (!string.IsNullOrEmpty(validationToken))
        {
            _logger.LogInformation("Graph validation handshake received. Returning validation token (length {Length}).", validationToken.Length);
            // MUST return the raw token string as plain text within 10 seconds.
            return new ContentResult
            {
                Content = validationToken,
                ContentType = "text/plain",
                StatusCode = StatusCodes.Status200OK
            };
        }
        return Ok("Notifications endpoint");
    }

    // Record types for deserializing Graph change notifications (simplified)
    public record GraphNotificationCollection(List<GraphChangeNotification> Value);

    public record GraphChangeNotification(
        string? SubscriptionId,
        string? ClientState,
        string? ChangeType,
        string? Resource,
        DateTimeOffset? SubscriptionExpirationDateTime,
        GraphResourceData? ResourceData
    );

    public record GraphResourceData(
        string? ODataType,
        string? ODataId,
        string? Id,
        string? ETag
    );

    [HttpPost]
    public async Task<IActionResult> Post(CancellationToken ct)
    {
        // Defensive: handle a (misrouted) validationToken on POST too.
        if (Request.Query.TryGetValue("validationToken", out var vt) && !string.IsNullOrEmpty(vt))
        {
            _logger.LogInformation("Graph validation token received on POST (unusual). Returning token.");
            return new ContentResult
            {
                Content = vt!,
                ContentType = "text/plain",
                StatusCode = StatusCodes.Status200OK
            };
        }

        try
        {
            using var reader = new StreamReader(Request.Body);
            var raw = await reader.ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(raw))
            {
                _logger.LogWarning("Received empty notification payload");
                return Ok();
            }

            GraphNotificationCollection? payload = null;
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                payload = JsonSerializer.Deserialize<GraphNotificationCollection>(raw, options);
            }
            catch (Exception dex)
            {
                _logger.LogError(dex, "Failed to deserialize Graph notification payload. Raw: {Raw}", raw);
            }

            if (payload?.Value != null && payload.Value.Count > 0)
            {
                var updates = 0; var deletions = 0; var skipped = 0;
                foreach (var n in payload.Value)
                {
                    _logger.LogInformation("Graph change notification: Sub={Sub} Type={Type} Resource={Resource} Expires={Exp}", n.SubscriptionId, n.ChangeType, n.Resource, n.SubscriptionExpirationDateTime);

                    // Extract event id: prefer resourceData.id then last segment of resource path
                    var eventId = n.ResourceData?.Id;
                    if (string.IsNullOrWhiteSpace(eventId) && !string.IsNullOrWhiteSpace(n.Resource))
                    {
                        var parts = n.Resource.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        if (parts.Length > 0)
                        {
                            eventId = parts[^1];
                        }
                    }
                    if (string.IsNullOrWhiteSpace(eventId))
                    {
                        _logger.LogDebug("Skipping notification with no resolvable event id.");
                        skipped++;
                        continue;
                    }

                    // Process change notification via BookingService
                    switch (n.ChangeType?.ToLowerInvariant())
                    {
                        case "deleted":
                            if (await _bookingService.ApplyCalendarEventDeletedAsync(eventId, ct)) deletions++; else skipped++;
                            break;
                        case "updated":
                            if (await _bookingService.ApplyCalendarEventUpdatedAsync(eventId, ct)) updates++; else skipped++;
                            break;
                        default:
                            skipped++;
                            break;
                    }
                }
                _logger.LogInformation("Notification processing complete. Updates={Updates} Deletions={Deletions} Skipped={Skipped}", updates, deletions, skipped);
            }
            else
            {
                _logger.LogWarning("Graph notification payload had no value array. Raw: {Raw}", raw);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Graph notifications POST");
        }

        // Always respond 200 quickly per webhook contract
        return Ok();
    }
}
