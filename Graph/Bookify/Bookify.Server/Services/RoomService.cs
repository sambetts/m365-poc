using Bookify.Server.Data;
using Bookify.Server.Application.Rooms.Contracts;
using Bookify.Server.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

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
        var sw = Stopwatch.StartNew();
        _logger.LogDebug(ServiceLogEvents.Fetch, "Fetching all rooms");
        var list = await _context.Rooms.ToListAsync();
        sw.Stop();
        _logger.LogInformation(ServiceLogEvents.Fetch, "Returned {Count} rooms in {ElapsedMs}ms", list.Count, sw.ElapsedMilliseconds);
        return list;
    }

    public async Task<Room?> GetRoomAsync(string id)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogDebug(ServiceLogEvents.Fetch, "Fetching room {Id}", id);
        var room = await _context.Rooms.FindAsync(id);
        sw.Stop();
        if (room == null)
        {
            _logger.LogWarning(ServiceLogEvents.Fetch, "Room {Id} not found after {ElapsedMs}ms", id, sw.ElapsedMilliseconds);
        }
        else
        {
            _logger.LogInformation(ServiceLogEvents.Fetch, "Fetched room {Id} in {ElapsedMs}ms", id, sw.ElapsedMilliseconds);
        }
        return room;
    }

    public async Task<IEnumerable<RoomAvailabilityResponse>> CheckAvailabilityAsync(RoomAvailabilityRequest request)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogDebug(ServiceLogEvents.AvailabilityCheck, "Checking availability Start={Start} End={End}", request.StartTime, request.EndTime);
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
                    Title = b.Title,
                    Body = b.Body
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
        sw.Stop();

        _logger.LogInformation(ServiceLogEvents.AvailabilityCheck, "Computed availability for {RoomCount} rooms in {ElapsedMs}ms (Range {Start}->{End})", response.Count, sw.ElapsedMilliseconds, request.StartTime, request.EndTime);
        return response;
    }

    public async Task<IEnumerable<BookingInfo>> GetRoomBookingsAsync(string roomId, DateTime? startDate, DateTime? endDate)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogDebug(ServiceLogEvents.Fetch, "Fetching bookings for room {RoomId} StartDate={StartDate} EndDate={EndDate}", roomId, startDate, endDate);
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
                Title = b.Title,
                Body = b.Body
            })
            .ToListAsync();
        sw.Stop();
        _logger.LogInformation(ServiceLogEvents.Fetch, "Found {Count} bookings for room {RoomId} in {ElapsedMs}ms", list.Count, roomId, sw.ElapsedMilliseconds);
        return list;
    }
}
