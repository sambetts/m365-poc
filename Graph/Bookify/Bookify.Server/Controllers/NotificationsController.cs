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
public class NotificationsController(AppConfig config, ILogger<NotificationsController> logger, IBookingService bookingService) : ControllerBase
{
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

            // Retrieve certificate used for decrypting resource data (if encrypted notifications used).
            var contentDecryptingCert = await AuthUtils.RetrieveKeyVaultCertificate(
                "webhooks", 
                config.AzureAdConfig.TenantId, 
                config.AzureAdConfig.ClientId, 
                config.AzureAdConfig.ClientSecret, 
                config.KeyVaultUrl);

            await ProcessNotificationsAsync(payload.Value, contentDecryptingCert, ct);
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
    private async Task ProcessNotificationsAsync(IEnumerable<ChangeNotification> notifications, X509Certificate2? decryptingCert, CancellationToken ct)
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
                    if (await bookingService.ApplyCalendarEventDeletedAsync(eventId, ct)) deletions++; else skipped++;
                    break;

                case ChangeType.Updated:
                    // Prefer encrypted content when provided (more data, privacy compliance).
                    if (n.EncryptedContent != null && decryptingCert != null)
                    {
                        var success = await HandleEncryptedUpdateAsync(n, eventId, decryptingCert, ct);
                        if (success) updates++; else skipped++;
                    }
                    else
                    {
                        // Non-encrypted: may require separate Graph fetch inside booking service.
                        logger.LogInformation("Graph change notification: Sub={Sub} Type={Type} Resource={Resource} Expires={Exp}", n.SubscriptionId, n.ChangeType, n.Resource, n.SubscriptionExpirationDateTime);
                        if (await bookingService.ApplyCalendarEventUpdatedAsync(eventId, ct)) updates++; else skipped++;
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
    /// Handles an Updated change notification that includes encrypted content.
    /// Decrypts the resource data, deserializes the Event fragment, and applies it through booking service.
    /// </summary>
    /// <param name="n">The originating change notification.</param>
    /// <param name="eventId">Resolved calendar event id.</param>
    /// <param name="decryptingCert">Certificate for decrypting payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if update applied successfully; false if skipped or failed.</returns>
    private async Task<bool> HandleEncryptedUpdateAsync(ChangeNotification n, string eventId, X509Certificate2 decryptingCert, CancellationToken ct)
    {
        try
        {
            // Decrypt resource data to obtain minimal event fragment provided by webhook.
            var notificationContentJson = EncryptedContentUtils.DecryptResourceDataContent(n.EncryptedContent!, decryptingCert);
            var eventUpdate = await Utils.DeserializeGraphJson(notificationContentJson, Event.CreateFromDiscriminatorValue);
            if (eventUpdate == null)
            {
                logger.LogWarning("Failed to deserialize decrypted notification content for event id {EventId}. Content: {Content}", eventId, notificationContentJson);
                return false;
            }
            // Apply external fragment (partial event data) to local booking store.
            return await bookingService.ApplyCalendarEventUpdateFromExternalFragmentAsync(eventId, eventUpdate, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling encrypted update for event id {EventId}", eventId);
            return false;
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
