namespace Bookify.Server.Application.Bookings.Contracts;

public class CreateBookingRequest
{
    public required string RoomId { get; set; }
    public required string BookedBy { get; set; }
    public required string BookedByEmail { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
}

public class BookingResponse
{
    public int Id { get; set; }
    public required string RoomId { get; set; }
    public required string RoomName { get; set; }
    public required string BookedBy { get; set; }
    public required string BookedByEmail { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CalendarEventId { get; set; }
}
