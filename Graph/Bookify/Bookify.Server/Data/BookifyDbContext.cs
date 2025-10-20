using Bookify.Server.Models;
using Microsoft.EntityFrameworkCore;

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
            entity.Property(e => e.Id).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.MailboxUpn).IsRequired().HasMaxLength(320); // RFC max email length
            entity.Property(e => e.Amenities)
                .HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
                .Metadata.SetValueComparer(
                    new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>>(
                        (c1, c2) => c1!.SequenceEqual(c2!),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToList()));
            // Unique index on MailboxUpn intentionally removed (see migration RemoveRoomMailboxUniqueIndex)
        });

        // Configure Booking entity
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RoomId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.BookedBy).IsRequired().HasMaxLength(100);
            entity.Property(e => e.BookedByEmail).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Body).HasMaxLength(500); // Body replaces Purpose
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.CalendarEventId).HasMaxLength(200);

            // Configure relationship
            entity.HasOne(e => e.Room)
                  .WithMany(r => r.Bookings)
                  .HasForeignKey(e => e.RoomId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed initial data with placeholder UPN to be overwritten at runtime from configuration
        modelBuilder.Entity<Room>().HasData(
            new Room { Id = "1", Name = "PIXEL PALACE", Capacity = 8, Amenities = new List<string> { "TV Screen", "WiFi", "Coffee" }, Available = true, Floor = 2, MailboxUpn = "shared@placeholder" },
            new Room { Id = "2", Name = "8-BIT BOARDROOM", Capacity = 12, Amenities = new List<string> { "TV Screen", "WiFi" }, Available = true, Floor = 3, MailboxUpn = "shared@placeholder" },
            new Room { Id = "3", Name = "RETRO RETREAT", Capacity = 6, Amenities = new List<string> { "WiFi", "Coffee" }, Available = false, Floor = 2, MailboxUpn = "shared@placeholder" },
            new Room { Id = "4", Name = "ARCADE ARENA", Capacity = 4, Amenities = new List<string> { "TV Screen", "WiFi" }, Available = true, Floor = 1, MailboxUpn = "shared@placeholder" },
            new Room { Id = "5", Name = "SPRITE SUMMIT", Capacity = 10, Amenities = new List<string> { "TV Screen", "WiFi", "Coffee" }, Available = true, Floor = 3, MailboxUpn = "shared@placeholder" },
            new Room { Id = "6", Name = "CONSOLE CHAMBER", Capacity = 6, Amenities = new List<string> { "WiFi" }, Available = false, Floor = 1, MailboxUpn = "shared@placeholder" }
        );
    }
}
