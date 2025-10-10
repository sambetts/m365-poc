using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Bookify.Server.Services;

public class GraphCalendarService : ICalendarService
{
    private readonly GraphServiceClient _graph;
    private readonly ILogger<GraphCalendarService> _logger;

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
                TransactionId = Guid.NewGuid().ToString()
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

    public async Task<bool> DeleteRoomEventAsync(Bookify.Server.Models.Room room, string eventId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Deleting Graph event {EventId} for room {RoomId}", eventId, room.Id);
            await _graph.Users[room.MailboxUpn].Events[eventId].DeleteAsync(cancellationToken: ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete calendar event {EventId} for room {RoomId}", eventId, room.Id);
            return false;
        }
    }
}
