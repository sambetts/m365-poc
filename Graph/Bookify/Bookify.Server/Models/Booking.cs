namespace Bookify.Server.Models;

public class Booking
{
    public int Id { get; set; }
    public required string RoomId { get; set; }
    public required string BookedBy { get; set; }
    public required string BookedByEmail { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Title { get; set; } // Meeting title / subject
    public string? Body { get; set; } // Meeting body / description (renamed from Purpose)
    public DateTime CreatedAt { get; set; }
    public string? CalendarEventId { get; set; } // Graph event id

    // Navigation property
    public Room? Room { get; set; }
}
