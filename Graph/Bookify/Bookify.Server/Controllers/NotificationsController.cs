using Bookify.Server.Application.Bookings.Contracts;
using Bookify.Server.Services;
using GraphNotifications;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph.Models;
using System.Security.Cryptography.X509Certificates;

namespace Bookify.Server.Controllers;

/// <summary>
/// Receives Microsoft Graph calendar change notifications (webhooks) and applies them to the local booking domain.
/// Implements validation handshake (GET with validationToken) and processes POST notifications including optional encrypted resource data.
/// </summary>
/// <remarks>
/// Microsoft Graph documentation references (provide multiple fallback URLs to mitigate restructuring / 404 issues):
/// Overview (change notifications): https://learn.microsoft.com/graph/change-notifications-overview
/// Subscription resource: https://learn.microsoft.com/graph/api/resources/subscription
/// Encrypted resource data: https://learn.microsoft.com/graph/change-notifications-with-resource-data
/// </remarks>
[ApiController]
[Route("api/[controller]")]
public class NotificationsController(AppConfig config, ILogger<NotificationsController> logger, IBookingService bookingService, IBookingCalendarSyncService calendarSync) : ControllerBase
{
    private X509Certificate2? _decryptingCert;

    /// <summary>
    /// Handles Microsoft Graph validation handshake GET requests by echoing back the provided validationToken.
    /// Returns a simple message if no token is supplied (health/info endpoint behavior).
    /// </summary>
    /// <param name="validationToken">The token provided by Graph during subscription validation.</param>
    [HttpGet]
    public IActionResult Get([FromQuery(Name = "validationToken")] string? validationToken)
    {
        if (!string.IsNullOrEmpty(validationToken))
        {
            // MUST return raw token as plain text within 10s per Graph webhooks contract.
            logger.LogInformation("Graph validation handshake received. Returning validation token (length {Length}).", validationToken.Length);
            return new ContentResult
            {
                Content = validationToken,
                ContentType = "text/plain",
                StatusCode = StatusCodes.Status200OK
            };
        }
        return Ok("Notifications endpoint");
    }

    /// <summary>
    /// Webhook receiver for Microsoft Graph calendar change notifications.
    /// Performs defensive validation, deserializes payload, decrypts resource data when present, and updates local booking state.
    /// Always returns 200 quickly per webhook contract to prevent retries.
    /// </summary>
    /// <param name="ct">Cancellation token propagated from ASP.NET pipeline.</param>
    [HttpPost]
    public async Task<IActionResult> Post(CancellationToken ct)
    {
        // Defensive: handle a (misrouted) validationToken on POST too.
        if (Request.Query.TryGetValue("validationToken", out var vt) && !string.IsNullOrEmpty(vt))
        {
            logger.LogInformation("Graph validation token received on POST (unusual). Returning token.");
            return new ContentResult
            {
                Content = vt!,
                ContentType = "text/plain",
                StatusCode = StatusCodes.Status200OK
            };
        }

        try
        {
            var raw = await ReadRequestBodyAsync();
            if (string.IsNullOrWhiteSpace(raw))
            {
                // Empty body: still respond 200 (avoid retries) but log for diagnostics.
                logger.LogWarning("Received empty notification payload");
                return Ok();
            }

            var payload = DeserializeNotifications(raw);
            if (payload?.Value == null)
            {
                // Graph sometimes may send malformed payload; treat gracefully.
                logger.LogWarning("Graph notification payload had no value array. Raw: {Raw}", raw);
                return Ok();
            }

            await ProcessNotificationsAsync(payload.Value, ct);
        }
        catch (Exception ex)
        {
            // Swallow exceptions (after logging) to ensure 200 return.
            logger.LogError(ex, "Error handling Graph notifications POST");
        }

        // Always respond 200 quickly per webhook contract
        return Ok();
    }

    /// <summary>
    /// Reads the raw request body as a UTF-8 string.
    /// Separate method keeps controller action concise and aids testability.
    /// </summary>
    private async Task<string> ReadRequestBodyAsync()
    {
        using var reader = new StreamReader(Request.Body);
        return await reader.ReadToEndAsync();
    }

    /// <summary>
    /// Deserializes the Graph change notification payload into a <see cref="ChangeNotificationCollection"/>.
    /// Logs and returns null if deserialization fails.
    /// </summary>
    /// <param name="raw">Raw JSON string received from Graph.</param>
    private ChangeNotificationCollection? DeserializeNotifications(string raw)
    {
        try
        {
            // Utils uses Kiota parse node factory for strongly-typed Graph models.
            return Utils.DeserializeGraphJson(raw, ChangeNotificationCollection.CreateFromDiscriminatorValue).GetAwaiter().GetResult();
        }
        catch (Exception dex)
        {
            logger.LogError(dex, "Failed to deserialize Graph notification payload. Raw: {Raw}", raw);
            return null;
        }
    }

    /// <summary>
    /// Iterates through each notification, resolves the calendar event id, branches on change type, decrypts content when present, and invokes booking service methods.
    /// Maintains counters for logging summary metrics.
    /// </summary>
    /// <param name="notifications">Enumerable of Graph change notifications.</param>
    /// <param name="decryptingCert">Certificate used to decrypt encrypted resource data payloads.</param>
    /// <param name="ct">Cancellation token.</param>
    private async Task ProcessNotificationsAsync(IEnumerable<ChangeNotification> notifications, CancellationToken ct)
    {
        var updates = 0; var deletions = 0; var skipped = 0;
        foreach (var n in notifications)
        {
            // Resolve event id from resourceData.id first, then fallback to last path segment of resource.
            var eventId = ResolveEventId(n);
            if (string.IsNullOrWhiteSpace(eventId))
            {
                logger.LogDebug("Skipping notification with no resolvable event id.");
                skipped++;
                continue;
            }

            switch (n.ChangeType)
            {
                case ChangeType.Deleted:
                    // Attempt to propagate deletion into local store.
                    if (await calendarSync.ApplyCalendarEventDeletedAsync(eventId, ct)) deletions++; else skipped++;
                    break;

                case ChangeType.Updated:
                case ChangeType.Created:
                    // Prefer encrypted content when provided (more data, privacy compliance).
                    if (n.EncryptedContent != null)
                    {
                        var success = await HandleEncryptedUpsertAsync(n, eventId, ct);
                        if (success) updates++; else skipped++;
                    }
                    else
                    {
                        // Non-encrypted: may require separate Graph fetch inside booking service.
                        logger.LogInformation("Graph change notification: Sub={Sub} Type={Type} Resource={Resource} Expires={Exp}", n.SubscriptionId, n.ChangeType, n.Resource, n.SubscriptionExpirationDateTime);
                        if (await calendarSync.UpdateBookingFromCalendarEventAsync(eventId, ct)) updates++; else skipped++;
                    }
                    break;
                default:
                    // Unhandled change types (e.g., Created) currently ignored; could be added later.
                    skipped++;
                    break;
            }
        }

        logger.LogInformation("Notification processing complete. Updates={Updates} Deletions={Deletions} Skipped={Skipped}", updates, deletions, skipped);
    }

    /// <summary>
    /// Ensures the decrypting certificate is loaded once (lazy initialization).
    /// </summary>
    private async Task<X509Certificate2> EnsureDecryptingCertificateAsync()
    {
        if (_decryptingCert != null) return _decryptingCert;
        _decryptingCert = await AuthUtils.RetrieveKeyVaultCertificate(
            "webhooks",
            config.AzureAdConfig.TenantId,
            config.AzureAdConfig.ClientId,
            config.AzureAdConfig.ClientSecret,
            config.KeyVaultUrl);
        return _decryptingCert;
    }

    /// <summary>
    /// Unified handler for encrypted Created or Updated notifications.
    /// Decrypts payload, materializes Event fragment, then dispatches to create or update logic.
    /// </summary>
    private async Task<bool> HandleEncryptedUpsertAsync(ChangeNotification n, string eventId, CancellationToken ct)
    {
        try
        {
            var cert = await EnsureDecryptingCertificateAsync();
            var notificationContentJson = EncryptedContentUtils.DecryptResourceDataContent(n.EncryptedContent!, cert);
            var evt = await Utils.DeserializeGraphJson(notificationContentJson, Event.CreateFromDiscriminatorValue);
            if (evt == null)
            {
                logger.LogWarning("Failed to deserialize decrypted notification content for event id {EventId}. Content: {Content}", eventId, notificationContentJson);
                return false;
            }



            if (n.ChangeType == ChangeType.Updated)
            {
                // Update existing booking from partial external fragment
                var changed = await calendarSync.ApplyBookingFromExternalFragmentAsync(eventId, evt, ct);
                return changed;
            }
            else if (n.ChangeType == ChangeType.Created)
            {
                // Create booking locally from new event fragment
                var start = ParseDateTime(evt.Start);
                var end = ParseDateTime(evt.End);
                var request = new CreateBookingRequest
                {
                    RoomId = evt.Location?.UniqueId
                             ?? evt.Location?.LocationEmailAddress
                             ?? evt.Location?.DisplayName
                             ?? "unknown",
                    BookedBy = evt.Organizer?.EmailAddress?.Name
                               ?? evt.Organizer?.EmailAddress?.Address
                               ?? "unknown",
                    BookedByEmail = evt.Organizer?.EmailAddress?.Address
                                    ?? config.SharedRoomMailboxUpn,
                    StartTime = start,
                    EndTime = end,
                    Title = evt.Subject,
                    Body = evt.Body?.Content ?? evt.BodyPreview
                };
                var result = await bookingService.CreateBookingAsync(request, false);
                if (!result.Success)
                {
                    logger.LogWarning("CreateBookingAsync failed for event {EventId}. Error={Error}", eventId, result.Error);
                    return false;
                }
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling encrypted upsert for event id {EventId}", eventId);
            return false;
        }

        static DateTime ParseDateTime(DateTimeTimeZone? dt)
        {
            if (dt?.DateTime == null) return DateTime.UtcNow;
            if (DateTime.TryParse(dt.DateTime, out var parsed))
            {
                // Treat unspecified as UTC if timezone provided; else leave as is.
                if (!string.IsNullOrWhiteSpace(dt.TimeZone) && parsed.Kind == DateTimeKind.Unspecified)
                    return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
                return parsed;
            }
            return DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Resolves the calendar event identifier from a change notification.
    /// Prefers resourceData.id; falls back to last path segment of resource string; returns empty string if unresolved.
    /// </summary>
    private static string ResolveEventId(ChangeNotification n)
    {
        if (n.ResourceData?.AdditionalData != null && n.ResourceData.AdditionalData.TryGetValue("id", out var idObj))
        {
            if (idObj is string s && !string.IsNullOrWhiteSpace(s)) return s;
        }

        if (!string.IsNullOrWhiteSpace(n.Resource))
        {
            var parts = n.Resource.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length > 0) return parts[^1];
        }
        return string.Empty;
    }
}
