namespace Bookify.Server.Models;

public class UpdateLog
{
    public int Id { get; set; }
    public int? BookingId { get; set; }
    public string? CalendarEventId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public required string Source { get; set; } // "web-app" or "notification"
    public required string Action { get; set; } // e.g. BookingCreated, BookingUpdated, CalendarEventCreated, etc.
}
