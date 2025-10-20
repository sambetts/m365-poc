using Microsoft.Graph;
using Microsoft.Graph.Models;
using System.Diagnostics;

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

    public async Task<string?> CreateEventAsync(Bookify.Server.Models.Room room, DateTime startUtc, DateTime endUtc, string subject, string organiserName, string organiserEmail, string? body = null, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation(ServiceLogEvents.ExternalCreate, "Creating Graph event for room {Room} ({Upn}) Start={Start:o} End={End:o}", room.Name, room.MailboxUpn, startUtc, endUtc);

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
            sw.Stop();
            _logger.LogInformation(ServiceLogEvents.ExternalCreate, "Created event {EventId} for room {RoomId} in {ElapsedMs}ms", created?.Id, room.Id, sw.ElapsedMilliseconds);
            return created?.Id;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Failed to create calendar event for room {RoomId} after {ElapsedMs}ms", room.Id, sw.ElapsedMilliseconds);
            return null;
        }
    }

    public async Task<bool> UpdateEventAsync(Bookify.Server.Models.Room room, string eventId, DateTime startUtc, DateTime endUtc, string subject, string organiserName, string organiserEmail, string? body = null, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation(ServiceLogEvents.ExternalUpdate, "Updating Graph event {EventId} for room {RoomId} Start={Start:o} End={End:o}", eventId, room.Id, startUtc, endUtc);

            // Fetch event first and ensure it is a Bookify managed event
            var existing = await _graph.Users[room.MailboxUpn].Events[eventId].GetAsync(rc =>
            {
                rc.QueryParameters.Expand = [$"extensions($filter=id eq '{BookifyExtensionName}')"];
            }, cancellationToken: ct);

            var isBookify = existing?.Extensions?.OfType<OpenTypeExtension>()
                .Any(e => e.ExtensionName == BookifyExtensionName && e.AdditionalData?.TryGetValue(BookifyExtensionSourceKey, out var v) == true && (v?.ToString() ?? "") == BookifyExtensionSourceValue) == true;

            if (!isBookify)
            {
                sw.Stop();
                _logger.LogWarning(ServiceLogEvents.ExternalUpdate, "Refusing to update event {EventId} in room {RoomId} because it is not tagged as a Bookify event (Elapsed {ElapsedMs}ms)", eventId, room.Id, sw.ElapsedMilliseconds);
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
            sw.Stop();
            _logger.LogInformation(ServiceLogEvents.ExternalUpdate, "Updated event {EventId} for room {RoomId} in {ElapsedMs}ms", eventId, room.Id, sw.ElapsedMilliseconds);
            return true;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Failed to update calendar event {EventId} for room {RoomId} after {ElapsedMs}ms", eventId, room.Id, sw.ElapsedMilliseconds);
            return false;
        }
    }

    public async Task<bool> DeleteRoomEventAsync(Bookify.Server.Models.Room room, string eventId, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation(ServiceLogEvents.ExternalDelete, "Deleting Graph event {EventId} for room {RoomId}", eventId, room.Id);

            // Fetch event first and ensure it is a Bookify managed event
            var existing = await _graph.Users[room.MailboxUpn].Events[eventId].GetAsync(rc =>
            {
                rc.QueryParameters.Expand = new[] { $"extensions($filter=id eq '{BookifyExtensionName}')" };
            }, cancellationToken: ct);

            var isBookify = existing?.Extensions?.OfType<OpenTypeExtension>()
                .Any(e => e.ExtensionName == BookifyExtensionName && e.AdditionalData?.TryGetValue(BookifyExtensionSourceKey, out var v) == true && (v?.ToString() ?? "") == BookifyExtensionSourceValue) == true;

            if (!isBookify)
            {
                sw.Stop();
                _logger.LogWarning(ServiceLogEvents.ExternalDelete, "Refusing to delete event {EventId} in room {RoomId} because it is not tagged as a Bookify event (Elapsed {ElapsedMs}ms)", eventId, room.Id, sw.ElapsedMilliseconds);
                return false;
            }

            await _graph.Users[room.MailboxUpn].Events[eventId].DeleteAsync(cancellationToken: ct);
            sw.Stop();
            _logger.LogInformation(ServiceLogEvents.ExternalDelete, "Deleted event {EventId} for room {RoomId} in {ElapsedMs}ms", eventId, room.Id, sw.ElapsedMilliseconds);
            return true;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Failed to delete calendar event {EventId} for room {RoomId} after {ElapsedMs}ms", eventId, room.Id, sw.ElapsedMilliseconds);
            return false;
        }
    }

    public async Task<(bool success, DateTime? startUtc, DateTime? endUtc, string? subject, List<string> attendees)> GetRoomEventAsync(Bookify.Server.Models.Room room, string eventId, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            _logger.LogDebug(ServiceLogEvents.ExternalFetch, "Fetching Graph event {EventId} for room {RoomId}", eventId, room.Id);
            var evt = await _graph.Users[room.MailboxUpn].Events[eventId].GetAsync(rc =>
            {
                rc.QueryParameters.Select = new[] { "subject", "start", "end", "attendees" };
            }, cancellationToken: ct);
            sw.Stop();
            if (evt == null)
            {
                _logger.LogWarning(ServiceLogEvents.ExternalFetch, "Event {EventId} for room {RoomId} not found (Elapsed {ElapsedMs}ms)", eventId, room.Id, sw.ElapsedMilliseconds);
                return (false, null, null, null, new List<string>());
            }
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
            var attendeeEmails = evt.Attendees?.Select(a => a.EmailAddress?.Address)
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Select(a => a!.Trim().ToLowerInvariant())
                .Distinct()
                .ToList() ?? new List<string>();
            _logger.LogInformation(ServiceLogEvents.ExternalFetch, "Fetched event {EventId} for room {RoomId} in {ElapsedMs}ms (Attendees={AttendeeCount})", eventId, room.Id, sw.ElapsedMilliseconds, attendeeEmails.Count);
            return (true, Parse(evt.Start), Parse(evt.End), evt.Subject, attendeeEmails);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Failed to fetch event {EventId} for room {RoomId} after {ElapsedMs}ms", eventId, room.Id, sw.ElapsedMilliseconds);
            return (false, null, null, null, new List<string>());
        }
    }
}
