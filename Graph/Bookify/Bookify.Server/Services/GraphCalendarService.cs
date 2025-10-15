using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Bookify.Server.Services;

public class GraphCalendarService : IExternalCalendarService
{
    private readonly GraphServiceClient _graph;
    private readonly ILogger<GraphCalendarService> _logger;

    // Open extension name used to tag events created by Bookify
    private const string BookifyExtensionName = "com.bookify.metadata";
    private const string BookifyExtensionSourceKey = "source";
    private const string BookifyExtensionSourceValue = "bookify";

    public GraphCalendarService(GraphServiceClient graph, ILogger<GraphCalendarService> logger)
    {
        _graph = graph;
        _logger = logger;
    }

    public async Task<string?> CreateRoomEventAsync(Bookify.Server.Models.Room room, DateTime startUtc, DateTime endUtc, string subject, string organiserName, string organiserEmail, string? body = null, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating Graph event for room {Room} ({Upn}) Start={Start} End={End}", room.Name, room.MailboxUpn, startUtc, endUtc);

            var @event = new Event
            {
                Subject = subject,
                Body = new ItemBody { ContentType = BodyType.Text, Content = body ?? subject },
                Start = new DateTimeTimeZone { DateTime = startUtc.ToString("o"), TimeZone = "UTC" },
                End = new DateTimeTimeZone { DateTime = endUtc.ToString("o"), TimeZone = "UTC" },
                Location = new Location { DisplayName = room.Name },
                Attendees = new List<Attendee>
                {
                    new()
                    {
                        Type = AttendeeType.Required,
                        EmailAddress = new EmailAddress { Address = organiserEmail, Name = organiserName }
                    }
                },
                IsAllDay = false,
                TransactionId = Guid.NewGuid().ToString(),
                // Tag event so we can safely identify it later
                Extensions = new List<Extension>
                {
                    new OpenTypeExtension
                    {
                        ExtensionName = BookifyExtensionName,
                        AdditionalData = new Dictionary<string, object>
                        {
                            { BookifyExtensionSourceKey, BookifyExtensionSourceValue },
                            { "roomId", room.Id }
                        }
                    }
                }
            };

            var created = await _graph.Users[room.MailboxUpn].Events.PostAsync(@event, cancellationToken: ct);
            _logger.LogInformation("Created event {EventId} for booking in room {RoomId}", created?.Id, room.Id);
            return created?.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create calendar event for room {RoomId}", room.Id);
            return null;
        }
    }

    public async Task<bool> UpdateRoomEventAsync(Bookify.Server.Models.Room room, string eventId, DateTime startUtc, DateTime endUtc, string subject, string organiserName, string organiserEmail, string? body = null, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Updating Graph event {EventId} for room {RoomId} Start={Start} End={End}", eventId, room.Id, startUtc, endUtc);

            // Fetch event first and ensure it is a Bookify managed event
            var existing = await _graph.Users[room.MailboxUpn].Events[eventId].GetAsync(rc =>
            {
                rc.QueryParameters.Expand = new[] { $"extensions($filter=id eq '{BookifyExtensionName}')" };
            }, cancellationToken: ct);

            var isBookify = existing?.Extensions?.OfType<OpenTypeExtension>()
                .Any(e => e.ExtensionName == BookifyExtensionName && e.AdditionalData?.TryGetValue(BookifyExtensionSourceKey, out var v) == true && (v?.ToString() ?? "") == BookifyExtensionSourceValue) == true;

            if (!isBookify)
            {
                _logger.LogWarning("Refusing to update event {EventId} in room {RoomId} because it is not tagged as a Bookify event", eventId, room.Id);
                return false;
            }

            var update = new Event
            {
                Subject = subject,
                Body = new ItemBody { ContentType = BodyType.Text, Content = body ?? subject },
                Start = new DateTimeTimeZone { DateTime = startUtc.ToString("o"), TimeZone = "UTC" },
                End = new DateTimeTimeZone { DateTime = endUtc.ToString("o"), TimeZone = "UTC" },
                Location = new Location { DisplayName = room.Name }
            };

            await _graph.Users[room.MailboxUpn].Events[eventId].PatchAsync(update, cancellationToken: ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update calendar event {EventId} for room {RoomId}", eventId, room.Id);
            return false;
        }
    }

    public async Task<bool> DeleteRoomEventAsync(Bookify.Server.Models.Room room, string eventId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Deleting Graph event {EventId} for room {RoomId}", eventId, room.Id);

            // Fetch event first and ensure it is a Bookify managed event
            var existing = await _graph.Users[room.MailboxUpn].Events[eventId].GetAsync(rc =>
            {
                rc.QueryParameters.Expand = new[] { $"extensions($filter=id eq '{BookifyExtensionName}')" };
            }, cancellationToken: ct);

            var isBookify = existing?.Extensions?.OfType<OpenTypeExtension>()
                .Any(e => e.ExtensionName == BookifyExtensionName && e.AdditionalData?.TryGetValue(BookifyExtensionSourceKey, out var v) == true && (v?.ToString() ?? "") == BookifyExtensionSourceValue) == true;

            if (!isBookify)
            {
                _logger.LogWarning("Refusing to delete event {EventId} in room {RoomId} because it is not tagged as a Bookify event", eventId, room.Id);
                return false;
            }

            await _graph.Users[room.MailboxUpn].Events[eventId].DeleteAsync(cancellationToken: ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete calendar event {EventId} for room {RoomId}", eventId, room.Id);
            return false;
        }
    }

    public async Task<(bool success, DateTime? startUtc, DateTime? endUtc, string? subject)> GetRoomEventAsync(Bookify.Server.Models.Room room, string eventId, CancellationToken ct = default)
    {
        try
        {
            var evt = await _graph.Users[room.MailboxUpn].Events[eventId].GetAsync(cancellationToken: ct);
            if (evt == null) return (false, null, null, null);
            DateTime? Parse(DateTimeTimeZone? dtz)
            {
                if (dtz?.DateTime == null) return null;
                if (DateTime.TryParse(dtz.DateTime, out var dt))
                {
                    if (dt.Kind == DateTimeKind.Unspecified && string.Equals(dtz.TimeZone, "UTC", StringComparison.OrdinalIgnoreCase))
                        dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                    return dt.ToUniversalTime();
                }
                return null;
            }
            return (true, Parse(evt.Start), Parse(evt.End), evt.Subject);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch event {EventId} for room {RoomId}", eventId, room.Id);
            return (false, null, null, null);
        }
    }
}
