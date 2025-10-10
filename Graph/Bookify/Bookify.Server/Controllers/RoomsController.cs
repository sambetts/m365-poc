using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Bookify.Server.Data;
using Bookify.Server.Models;
using Bookify.Server.DTOs;

namespace Bookify.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomsController : ControllerBase
{
    private readonly BookifyDbContext _context;
    private readonly ILogger<RoomsController> _logger;

    public RoomsController(BookifyDbContext context, ILogger<RoomsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all rooms
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Room>>> GetRooms()
    {
        return await _context.Rooms.ToListAsync();
    }

    /// <summary>
    /// Get a specific room by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Room>> GetRoom(string id)
    {
        var room = await _context.Rooms.FindAsync(id);

        if (room == null)
        {
            return NotFound();
        }

        return room;
    }

    /// <summary>
    /// Check room availability for a specific time range
    /// </summary>
    [HttpPost("availability")]
    public async Task<ActionResult<IEnumerable<RoomAvailabilityResponse>>> CheckAvailability(
        [FromBody] RoomAvailabilityRequest request)
    {
        if (request.StartTime >= request.EndTime)
        {
            return BadRequest("End time must be after start time");
        }

        var rooms = await _context.Rooms
            .Include(r => r.Bookings)
            .ToListAsync();

        var response = rooms.Select(room =>
        {
            // Find bookings that overlap with the requested time
            var overlappingBookings = room.Bookings
                .Where(b => b.StartTime < request.EndTime && b.EndTime > request.StartTime)
                .Select(b => new BookingInfo
                {
                    Id = b.Id,
                    BookedBy = b.BookedBy,
                    StartTime = b.StartTime,
                    EndTime = b.EndTime,
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

        return Ok(response);
    }

    /// <summary>
    /// Get room availability for a specific room
    /// </summary>
    [HttpGet("{id}/bookings")]
    public async Task<ActionResult<IEnumerable<BookingInfo>>> GetRoomBookings(
        string id,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var room = await _context.Rooms.FindAsync(id);
        if (room == null)
        {
            return NotFound();
        }

        var query = _context.Bookings.Where(b => b.RoomId == id);

        if (startDate.HasValue)
        {
            query = query.Where(b => b.EndTime >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(b => b.StartTime <= endDate.Value);
        }

        var bookings = await query
            .OrderBy(b => b.StartTime)
            .Select(b => new BookingInfo
            {
                Id = b.Id,
                BookedBy = b.BookedBy,
                StartTime = b.StartTime,
                EndTime = b.EndTime,
                Purpose = b.Purpose
            })
            .ToListAsync();

        return Ok(bookings);
    }
}
