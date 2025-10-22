using Bookify.Server.Application.Bookings.Contracts;
using Bookify.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Bookify.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly ILogger<BookingsController> _logger;

    public BookingsController(IBookingService bookingService, ILogger<BookingsController> logger)
    {
        _bookingService = bookingService;
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
        var bookings = await _bookingService.GetBookingsAsync(startDate, endDate);
        return Ok(bookings);
    }

    /// <summary>
    /// Get a specific booking by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<BookingResponse>> GetBooking(int id)
    {
        var booking = await _bookingService.GetBookingAsync(id);
        if (booking == null) return NotFound();
        return Ok(booking);
    }

    /// <summary>
    /// Create a new booking
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<BookingResponse>> CreateBooking([FromBody] CreateBookingRequest request)
    {
        var result = await _bookingService.CreateBookingAsync(request, true);
        if (!result.Success) return BadRequest(result.Error);
        var response = result.Value!;
        return CreatedAtAction(nameof(GetBooking), new { id = response.Id }, response);
    }

    /// <summary>
    /// Update a booking
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBooking(int id, [FromBody] CreateBookingRequest request)
    {
        var result = await _bookingService.UpdateBookingAsync(id, request);
        if (!result.Success)
        {
            return result.Error switch
            {
                "Booking not found" => NotFound(result.Error),
                "Room is already booked for the requested time" => Conflict(result.Error),
                _ => BadRequest(result.Error)
            };
        }
        return NoContent();
    }

    /// <summary>
    /// Delete a booking
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBooking(int id)
    {
        var deleted = await _bookingService.DeleteBookingAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }

    /// <summary>
    /// Get bookings for a specific user
    /// </summary>
    [HttpGet("user/{email}")]
    public async Task<ActionResult<IEnumerable<BookingResponse>>> GetUserBookings(string email)
    {
        var bookings = await _bookingService.GetUserBookingsAsync(email);
        return Ok(bookings);
    }
}
