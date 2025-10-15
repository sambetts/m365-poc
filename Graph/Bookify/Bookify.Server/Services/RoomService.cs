using Bookify.Server.Data;
using Bookify.Server.DTOs;
using Bookify.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Bookify.Server.Services;

public class RoomService : IRoomService
{
    private readonly BookifyDbContext _context;
    private readonly ILogger<RoomService> _logger;

    public RoomService(BookifyDbContext context, ILogger<RoomService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Room>> GetRoomsAsync()
    {
        _logger.LogDebug("Fetching all rooms");
        var list = await _context.Rooms.ToListAsync();
        _logger.LogDebug("Returning {Count} rooms", list.Count);
        return list;
    }

    public async Task<Room?> GetRoomAsync(string id)
    {
        _logger.LogDebug("Fetching room {Id}", id);
        var room = await _context.Rooms.FindAsync(id);
        if (room == null)
        {
            _logger.LogInformation("Room {Id} not found", id);
        }
        return room;
    }

    public async Task<IEnumerable<RoomAvailabilityResponse>> CheckAvailabilityAsync(RoomAvailabilityRequest request)
    {
        _logger.LogDebug("Checking availability Start={Start} End={End}", request.StartTime, request.EndTime);
        var rooms = await _context.Rooms
            .Include(r => r.Bookings)
            .ToListAsync();

        var response = rooms.Select(room =>
        {
            var overlappingBookings = room.Bookings
                .Where(b => b.StartTime < request.EndTime && b.EndTime > request.StartTime)
                .Select(b => new BookingInfo
                {
                    Id = b.Id,
                    BookedBy = b.BookedBy,
                    StartTime = b.StartTime,
                    EndTime = b.EndTime,
                    Title = b.Title, // include title so client can display / edit
                    Purpose = b.Purpose
                })
                .ToList();

            return new RoomAvailabilityResponse
            {
                Id = room.Id,
                Name = room.Name,
                Capacity = room.Capacity,
                Amenities = room.Amenities,
                IsAvailable = overlappingBookings.Count == 0,
                Floor = room.Floor,
                ExistingBookings = overlappingBookings
            };
        }).ToList();

        _logger.LogDebug("Computed availability for {RoomCount} rooms", response.Count());
        return response;
    }

    public async Task<IEnumerable<BookingInfo>> GetRoomBookingsAsync(string roomId, DateTime? startDate, DateTime? endDate)
    {
        _logger.LogDebug("Fetching bookings for room {RoomId} StartDate={StartDate} EndDate={EndDate}", roomId, startDate, endDate);
        var query = _context.Bookings.Where(b => b.RoomId == roomId);

        if (startDate.HasValue)
        {
            query = query.Where(b => b.EndTime >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(b => b.StartTime <= endDate.Value);
        }

        var list = await query
            .OrderBy(b => b.StartTime)
            .Select(b => new BookingInfo
            {
                Id = b.Id,
                BookedBy = b.BookedBy,
                StartTime = b.StartTime,
                EndTime = b.EndTime,
                Title = b.Title, // include title so edit dialog pre-fills correctly
                Purpose = b.Purpose
            })
            .ToListAsync();
        _logger.LogDebug("Found {Count} bookings for room {RoomId}", list.Count, roomId);
        return list;
    }
}
