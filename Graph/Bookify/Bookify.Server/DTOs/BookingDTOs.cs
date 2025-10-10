namespace Bookify.Server.DTOs;

public class RoomAvailabilityRequest
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}

public class RoomAvailabilityResponse
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public int Capacity { get; set; }
    public List<string> Amenities { get; set; } = new List<string>();
    public bool IsAvailable { get; set; }
    public int Floor { get; set; }
    public List<BookingInfo> ExistingBookings { get; set; } = new();
}

public class BookingInfo
{
    public int Id { get; set; }
    public required string BookedBy { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Purpose { get; set; }
}

public class CreateBookingRequest
{
    public required string RoomId { get; set; }
    public required string BookedBy { get; set; }
    public required string BookedByEmail { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Purpose { get; set; }
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
    public string? Purpose { get; set; }
    public DateTime CreatedAt { get; set; }
}
