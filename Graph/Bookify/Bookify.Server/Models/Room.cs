namespace Bookify.Server.Models;

public class Room
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Location { get; set; }
    public int Capacity { get; set; }
    public string? Equipment { get; set; }
    
    // Navigation property
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
