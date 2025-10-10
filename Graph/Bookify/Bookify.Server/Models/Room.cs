namespace Bookify.Server.Models;

public class Room
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public int Capacity { get; set; }
    public List<string> Amenities { get; set; } = new List<string>();
    public bool Available { get; set; }
    public int Floor { get; set; }
    
    // Navigation property
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
