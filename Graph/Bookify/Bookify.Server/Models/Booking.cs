namespace Bookify.Server.Models;

public class Booking
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public required string BookedBy { get; set; }
    public required string BookedByEmail { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Purpose { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation property
    public Room? Room { get; set; }
}
