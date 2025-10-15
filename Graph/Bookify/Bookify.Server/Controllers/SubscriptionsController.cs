using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;

namespace Bookify.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubscriptionsController : ControllerBase
{
    private readonly GraphServiceClient _graph;
    private readonly ILogger<SubscriptionsController> _logger;
    private readonly IWebHostEnvironment _env;

    public SubscriptionsController(GraphServiceClient graph, ILogger<SubscriptionsController> logger, IWebHostEnvironment env)
    {
        _graph = graph;
        _logger = logger;
        _env = env;
    }

    public record CreateSubscriptionRequest(string Upn, string ChangeType, string? NotificationUrl);

    public record SubscriptionResponse(string? Id, string? Resource, string? ChangeType, DateTimeOffset? ExpirationDateTime, string? NotificationUrl, string? ClientState);

    public record ErrorResponse(string Error, string? Detail = null);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SubscriptionResponse>>> GetSubscriptions(CancellationToken ct)
    {
        try
        {
            var list = await _graph.Subscriptions.GetAsync();

            var responses = list?.Value?
                .Where(s => s.Resource != null && s.Resource.Contains("/events", StringComparison.OrdinalIgnoreCase))
                .Select(s => new SubscriptionResponse(s.Id, s.Resource, s.ChangeType, s.ExpirationDateTime, s.NotificationUrl, s.ClientState))
                .ToList() ?? new List<SubscriptionResponse>();

            return Ok(responses);
        }
        catch (ServiceException sex)
        {
            _logger.LogError(sex, "Graph ServiceException listing subscriptions.");
            var detail = _env.IsDevelopment() ? sex.Message : null;
            return StatusCode(StatusCodes.Status502BadGateway, new ErrorResponse("Graph error listing subscriptions", detail));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list subscriptions");
            var detail = _env.IsDevelopment() ? ex.Message : null;
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
        if (string.IsNullOrWhiteSpace(request.ChangeType))
        {
            return BadRequest(new ErrorResponse("ChangeType required"));
        }

        // Determine notification URL (client provided or default to current host)
        var notificationUrl = string.IsNullOrWhiteSpace(request.NotificationUrl)
            ? $"{Request.Scheme}://{Request.Host}/api/notifications"
            : request.NotificationUrl.Trim();

        // Graph requires HTTPS for webhook notifications
        if (!notificationUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new ErrorResponse("NotificationUrl must be HTTPS"));
        }

        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "created", "updated", "deleted" };
        var changeTypes = request.ChangeType.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(ctyp => allowed.Contains(ctyp))
            .Select(ctyp => ctyp.ToLowerInvariant())
            .Distinct()
            .ToList();
        if (changeTypes.Count == 0)
        {
            return BadRequest(new ErrorResponse("No valid change types supplied. Allowed: created, updated, deleted"));
        }

        var changeTypeJoined = string.Join(',', changeTypes);
        var resource = $"users/{request.Upn}/events"; // primary calendar events

        var expiration = DateTimeOffset.UtcNow.AddHours(1);
        var subscription = new Subscription
        {
            ChangeType = changeTypeJoined,
            Resource = resource,
            NotificationUrl = notificationUrl,
            ClientState = Guid.NewGuid().ToString("N"),
            ExpirationDateTime = expiration
        };

        try
        {
            var created = await _graph.Subscriptions.PostAsync(subscription, cancellationToken: ct);
            if (created == null)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new ErrorResponse("Graph did not return a subscription"));
            }
            var response = new SubscriptionResponse(created.Id, created.Resource, created.ChangeType, created.ExpirationDateTime, created.NotificationUrl, created.ClientState);
            return CreatedAtAction(nameof(GetSubscriptions), new { id = created.Id }, response);
        }
        catch (ODataError ex)
        {
            _logger.LogError(ex, "Graph ODataError creating subscription for {Upn}", request.Upn);
            var detail = _env.IsDevelopment() ? ex.Error?.Message : null;
            return StatusCode(StatusCodes.Status502BadGateway, new ErrorResponse("Graph error", detail));
        }
        catch (ServiceException ex)
        {
            _logger.LogError(ex, "Service error creating subscription for {Upn}", request.Upn);
            var detail = _env.IsDevelopment() ? ex.Message : null;
            return StatusCode(StatusCodes.Status502BadGateway, new ErrorResponse("Failed to create subscription", detail));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating subscription for {Upn}", request.Upn);
            var detail = _env.IsDevelopment() ? ex.Message : null;
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse("Failed to create subscription", detail));
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSubscription(string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id)) return BadRequest(new ErrorResponse("Id required"));
        try
        {
            await _graph.Subscriptions[id].DeleteAsync(cancellationToken: ct);
            return NoContent();
        }
        catch (ServiceException sex)
        {
            _logger.LogError(sex, "Graph ServiceException deleting subscription {Id}", id);
            var detail = _env.IsDevelopment() ? sex.Message : null;
            if (sex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new ErrorResponse("Subscription not found", detail));
            }
            return StatusCode(StatusCodes.Status502BadGateway, new ErrorResponse("Graph error deleting subscription", detail));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting subscription {Id}", id);
            var detail = _env.IsDevelopment() ? ex.Message : null;
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse("Failed to delete subscription", detail));
        }
    }
}
