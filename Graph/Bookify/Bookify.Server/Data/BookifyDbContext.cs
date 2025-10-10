using Microsoft.EntityFrameworkCore;
using Bookify.Server.Models;

namespace Bookify.Server.Data;

public class BookifyDbContext : DbContext
{
    public BookifyDbContext(DbContextOptions<BookifyDbContext> options) : base(options)
    {
    }

    public DbSet<Room> Rooms { get; set; }
    public DbSet<Booking> Bookings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Room entity
        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Location).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Equipment).HasMaxLength(500);
        });

        // Configure Booking entity
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BookedBy).IsRequired().HasMaxLength(100);
            entity.Property(e => e.BookedByEmail).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Purpose).HasMaxLength(500);
            
            // Configure relationship
            entity.HasOne(e => e.Room)
                  .WithMany(r => r.Bookings)
                  .HasForeignKey(e => e.RoomId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed initial data
        modelBuilder.Entity<Room>().HasData(
            new Room { Id = 1, Name = "Conference Room A", Location = "Floor 1, Building A", Capacity = 10 },
            new Room { Id = 2, Name = "Conference Room B", Location = "Floor 2, Building A", Capacity = 6 },
            new Room { Id = 3, Name = "Board Room", Location = "Floor 3, Building A", Capacity = 20, Equipment = "Projector, Video Conferencing" },
            new Room { Id = 4, Name = "Training Room", Location = "Floor 1, Building B", Capacity = 30, Equipment = "Whiteboard, Projector" }
        );
    }
}
