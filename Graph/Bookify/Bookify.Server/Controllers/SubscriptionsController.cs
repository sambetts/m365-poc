using GraphNotifications;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Microsoft.Graph.Models.ODataErrors;

namespace Bookify.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubscriptionsController(GraphServiceClient graph, AppConfig config, ILogger<SubscriptionsController> logger, IWebHostEnvironment env) : ControllerBase
{
    // Removed ChangeType and NotificationUrl from request; server uses fixed change types configured internally.
    public record CreateSubscriptionRequest(string Upn);

    // Removed ChangeType from response surface (kept internal in webhook manager)
    public record SubscriptionResponse(string? Id, string? Resource, DateTimeOffset? ExpirationDateTime, string? NotificationUrl, string? ClientState);

    public record ErrorResponse(string Error, string? Detail = null);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SubscriptionResponse>>> GetSubscriptions(CancellationToken ct)
    {
        try
        {
            var list = await graph.Subscriptions.GetAsync(cancellationToken: ct);

            var responses = list?.Value?
                .Where(s => s.Resource != null && s.Resource.Contains("/events", StringComparison.OrdinalIgnoreCase))
                .Select(s => new SubscriptionResponse(s.Id, s.Resource, s.ExpirationDateTime, s.NotificationUrl, s.ClientState))
                .ToList() ?? new List<SubscriptionResponse>();

            return Ok(responses);
        }
        catch (ServiceException sex)
        {
            logger.LogError(sex, "Graph ServiceException listing subscriptions.");
            var detail = env.IsDevelopment() ? sex.Message : null;
            return StatusCode(StatusCodes.Status502BadGateway, new ErrorResponse("Graph error listing subscriptions", detail));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to list subscriptions");
            var detail = env.IsDevelopment() ? ex.Message : null;
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse("Failed to retrieve subscriptions", detail));
        }
    }

    [HttpPost]
    public async Task<ActionResult<SubscriptionResponse>> CreateSubscription([FromBody] CreateSubscriptionRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Upn))
        {
            return BadRequest(new ErrorResponse("UPN required"));
        }

        var contentDecryptingCert = await AuthUtils.RetrieveKeyVaultCertificate("webhooks", config.AzureAdConfig.TenantId, config.AzureAdConfig.ClientId, config.AzureAdConfig.ClientSecret, config.KeyVaultUrl);
        var calendarWebHookManager = new CalendarWebhooksManager(graph, config.SharedRoomMailboxUpn, contentDecryptingCert, config, logger);

        try
        {
            var created = await calendarWebHookManager.CreateOrUpdateSubscription();

            var response = new SubscriptionResponse(created.Id, created.Resource, created.ExpirationDateTime, created.NotificationUrl, created.ClientState);
            return CreatedAtAction(nameof(GetSubscriptions), new { id = created.Id }, response);
        }
        catch (ODataError ex)
        {
            logger.LogError(ex, "Graph ODataError creating subscription for {Upn}", request.Upn);
            var detail = env.IsDevelopment() ? ex.Error?.Message : null;
            return StatusCode(StatusCodes.Status502BadGateway, new ErrorResponse("Graph error", detail));
        }
        catch (ServiceException ex)
        {
            logger.LogError(ex, "Service error creating subscription for {Upn}", request.Upn);
            var detail = env.IsDevelopment() ? ex.Message : null;
            return StatusCode(StatusCodes.Status502BadGateway, new ErrorResponse("Failed to create subscription", detail));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error creating subscription for {Upn}", request.Upn);
            var detail = env.IsDevelopment() ? ex.Message : null;
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse("Failed to create subscription", detail));
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSubscription(string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id)) return BadRequest(new ErrorResponse("Id required"));
        try
        {
            await graph.Subscriptions[id].DeleteAsync(cancellationToken: ct);
            return NoContent();
        }
        catch (ServiceException sex)
        {
            logger.LogError(sex, "Graph ServiceException deleting subscription {Id}", id);
            var detail = env.IsDevelopment() ? sex.Message : null;
            if (sex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new ErrorResponse("Subscription not found", detail));
            }
            return StatusCode(StatusCodes.Status502BadGateway, new ErrorResponse("Graph error deleting subscription", detail));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error deleting subscription {Id}", id);
            var detail = env.IsDevelopment() ? ex.Message : null;
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse("Failed to delete subscription", detail));
        }
    }
}
