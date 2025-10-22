namespace Bookify.Server.Application.Rooms.Contracts;

public class RoomAvailabilityRequest
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}

public class BookingInfo
{
    public int Id { get; set; }
    public required string BookedBy { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
}

public class RoomAvailabilityResponse
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public int Capacity { get; set; }
    public List<string> Amenities { get; set; } = new();
    public bool IsAvailable { get; set; }
    public int Floor { get; set; }
    public List<BookingInfo> ExistingBookings { get; set; } = new();
}
