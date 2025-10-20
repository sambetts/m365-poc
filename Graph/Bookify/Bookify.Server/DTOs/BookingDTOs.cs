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
    public string? Title { get; set; } // added title to support edit prefill
    public string? Body { get; set; } // renamed from Purpose
}

public class CreateBookingRequest
{
    public required string RoomId { get; set; }
    public required string BookedBy { get; set; }
    public required string BookedByEmail { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; } // renamed from Purpose
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
    public string? Body { get; set; } // renamed from Purpose
    public DateTime CreatedAt { get; set; }
    public string? CalendarEventId { get; set; }
}
