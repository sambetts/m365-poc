using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Bookify.Server.Data;
using Bookify.Server.Models;
using Bookify.Server.DTOs;

namespace Bookify.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly BookifyDbContext _context;
    private readonly ILogger<BookingsController> _logger;

    public BookingsController(BookifyDbContext context, ILogger<BookingsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all bookings
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookingResponse>>> GetBookings(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var query = _context.Bookings.Include(b => b.Room).AsQueryable();

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
            .Select(b => new BookingResponse
            {
                Id = b.Id,
                RoomId = b.RoomId,
                RoomName = b.Room!.Name,
                BookedBy = b.BookedBy,
                BookedByEmail = b.BookedByEmail,
                StartTime = b.StartTime,
                EndTime = b.EndTime,
                Purpose = b.Purpose,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync();

        return Ok(bookings);
    }

    /// <summary>
    /// Get a specific booking by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<BookingResponse>> GetBooking(int id)
    {
        var booking = await _context.Bookings
            .Include(b => b.Room)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null)
        {
            return NotFound();
        }

        var response = new BookingResponse
        {
            Id = booking.Id,
            RoomId = booking.RoomId,
            RoomName = booking.Room!.Name,
            BookedBy = booking.BookedBy,
            BookedByEmail = booking.BookedByEmail,
            StartTime = booking.StartTime,
            EndTime = booking.EndTime,
            Purpose = booking.Purpose,
            CreatedAt = booking.CreatedAt
        };

        return Ok(response);
    }

    /// <summary>
    /// Create a new booking
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<BookingResponse>> CreateBooking([FromBody] CreateBookingRequest request)
    {
        // Validate the request
        if (request.StartTime >= request.EndTime)
        {
            return BadRequest("End time must be after start time");
        }

        if (request.StartTime < DateTime.UtcNow)
        {
            return BadRequest("Cannot book a room in the past");
        }

        // Check if room exists
        var room = await _context.Rooms.FindAsync(request.RoomId);
        if (room == null)
        {
            return NotFound($"Room with ID {request.RoomId} not found");
        }

        // Check for conflicts
        var hasConflict = await _context.Bookings
            .AnyAsync(b => b.RoomId == request.RoomId &&
                          b.StartTime < request.EndTime &&
                          b.EndTime > request.StartTime);

        if (hasConflict)
        {
            return Conflict("Room is already booked for the requested time");
        }

        // Create the booking
        var booking = new Booking
        {
            RoomId = request.RoomId,
            BookedBy = request.BookedBy,
            BookedByEmail = request.BookedByEmail,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Purpose = request.Purpose,
            CreatedAt = DateTime.UtcNow
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        var response = new BookingResponse
        {
            Id = booking.Id,
            RoomId = booking.RoomId,
            RoomName = room.Name,
            BookedBy = booking.BookedBy,
            BookedByEmail = booking.BookedByEmail,
            StartTime = booking.StartTime,
            EndTime = booking.EndTime,
            Purpose = booking.Purpose,
            CreatedAt = booking.CreatedAt
        };

        return CreatedAtAction(nameof(GetBooking), new { id = booking.Id }, response);
    }

    /// <summary>
    /// Update a booking
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBooking(int id, [FromBody] CreateBookingRequest request)
    {
        var booking = await _context.Bookings.FindAsync(id);
        if (booking == null)
        {
            return NotFound();
        }

        // Validate the request
        if (request.StartTime >= request.EndTime)
        {
            return BadRequest("End time must be after start time");
        }

        // Check for conflicts (excluding current booking)
        var hasConflict = await _context.Bookings
            .AnyAsync(b => b.Id != id &&
                          b.RoomId == request.RoomId &&
                          b.StartTime < request.EndTime &&
                          b.EndTime > request.StartTime);

        if (hasConflict)
        {
            return Conflict("Room is already booked for the requested time");
        }

        // Update the booking
        booking.RoomId = request.RoomId;
        booking.BookedBy = request.BookedBy;
        booking.BookedByEmail = request.BookedByEmail;
        booking.StartTime = request.StartTime;
        booking.EndTime = request.EndTime;
        booking.Purpose = request.Purpose;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Delete a booking
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBooking(int id)
    {
        var booking = await _context.Bookings.FindAsync(id);
        if (booking == null)
        {
            return NotFound();
        }

        _context.Bookings.Remove(booking);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Get bookings for a specific user
    /// </summary>
    [HttpGet("user/{email}")]
    public async Task<ActionResult<IEnumerable<BookingResponse>>> GetUserBookings(string email)
    {
        var bookings = await _context.Bookings
            .Include(b => b.Room)
            .Where(b => b.BookedByEmail == email)
            .OrderBy(b => b.StartTime)
            .Select(b => new BookingResponse
            {
                Id = b.Id,
                RoomId = b.RoomId,
                RoomName = b.Room!.Name,
                BookedBy = b.BookedBy,
                BookedByEmail = b.BookedByEmail,
                StartTime = b.StartTime,
                EndTime = b.EndTime,
                Purpose = b.Purpose,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync();

        return Ok(bookings);
    }
}
