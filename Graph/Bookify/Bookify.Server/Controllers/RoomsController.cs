using Bookify.Server.DTOs;
using Bookify.Server.Models;
using Bookify.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Bookify.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomsController : ControllerBase
{
    private readonly IRoomService _roomService;
    private readonly ILogger<RoomsController> _logger;

    public RoomsController(IRoomService roomService, ILogger<RoomsController> logger)
    {
        _roomService = roomService;
        _logger = logger;
    }

    /// <summary>
    /// Get all rooms
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Room>>> GetRooms()
    {
        var rooms = await _roomService.GetRoomsAsync();
        return Ok(rooms);
    }

    /// <summary>
    /// Get a specific room by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Room>> GetRoom(string id)
    {
        var room = await _roomService.GetRoomAsync(id);
        if (room == null)
        {
            return NotFound();
        }
        return Ok(room);
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

        var response = await _roomService.CheckAvailabilityAsync(request);
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
        var room = await _roomService.GetRoomAsync(id);
        if (room == null)
        {
            return NotFound();
        }

        var bookings = await _roomService.GetRoomBookingsAsync(id, startDate, endDate);
        return Ok(bookings);
    }
}
