using Bookify.Server.Data;
using Bookify.Server.DTOs;
using Bookify.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Bookify.Server.Services;

public class BookingService : IBookingService
{
    private readonly BookifyDbContext _context;
    private readonly ILogger<BookingService> _logger;
    private readonly ICalendarService _calendarService;

    public BookingService(BookifyDbContext context, ILogger<BookingService> logger, ICalendarService calendarService)
    {
        _context = context;
        _logger = logger;
        _calendarService = calendarService;
    }

    public async Task<IEnumerable<BookingResponse>> GetBookingsAsync(DateTime? startDate, DateTime? endDate)
    {
        _logger.LogDebug("Fetching bookings with filters StartDate={StartDate} EndDate={EndDate}", startDate, endDate);
        var query = _context.Bookings.Include(b => b.Room).AsQueryable();

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
            .Select(b => new BookingResponse
            {
                Id = b.Id,
                RoomId = b.RoomId,
                RoomName = b.Room!.Name,
                BookedBy = b.BookedBy,
                BookedByEmail = b.BookedByEmail,
                StartTime = b.StartTime,
                EndTime = b.EndTime,
                Title = b.Title,
                Purpose = b.Purpose,
                CreatedAt = b.CreatedAt,
                CalendarEventId = b.CalendarEventId
            })
            .ToListAsync();
        _logger.LogDebug("Returning {Count} bookings", list.Count);
        return list;
    }

    public async Task<BookingResponse?> GetBookingAsync(int id)
    {
        _logger.LogDebug("Getting booking {Id}", id);
        var booking = await _context.Bookings
            .Include(b => b.Room)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null)
        {
            _logger.LogInformation("Booking {Id} not found", id);
            return null;
        }

        return new BookingResponse
        {
            Id = booking.Id,
            RoomId = booking.RoomId,
            RoomName = booking.Room!.Name,
            BookedBy = booking.BookedBy,
            BookedByEmail = booking.BookedByEmail,
            StartTime = booking.StartTime,
            EndTime = booking.EndTime,
            Title = booking.Title,
            Purpose = booking.Purpose,
            CreatedAt = booking.CreatedAt,
            CalendarEventId = booking.CalendarEventId
        };
    }

    public async Task<(BookingOperationStatus status, BookingResponse? response, string? errorMessage)> CreateBookingAsync(CreateBookingRequest request)
    {
        _logger.LogInformation("Creating booking for Room={RoomId} By={User} Start={Start} End={End}", request.RoomId, request.BookedByEmail, request.StartTime, request.EndTime);
        if (request.StartTime >= request.EndTime)
        {
            _logger.LogWarning("Invalid time range for booking: Start={Start} End={End}", request.StartTime, request.EndTime);
            return (BookingOperationStatus.BadRequest, null, "End time must be after start time");
        }

        if (request.StartTime < DateTime.UtcNow)
        {
            _logger.LogWarning("Attempt to book in the past Start={Start}", request.StartTime);
            return (BookingOperationStatus.BadRequest, null, "Cannot book a room in the past");
        }

        var room = await _context.Rooms.FindAsync(request.RoomId);
        if (room == null)
        {
            _logger.LogWarning("Room {RoomId} not found when creating booking", request.RoomId);
            return (BookingOperationStatus.NotFound, null, $"Room with ID {request.RoomId} not found");
        }

        var hasConflict = await _context.Bookings.AnyAsync(b => b.RoomId == request.RoomId &&
                                                                b.StartTime < request.EndTime &&
                                                                b.EndTime > request.StartTime);
        if (hasConflict)
        {
            _logger.LogInformation("Booking conflict for Room={RoomId} Start={Start} End={End}", request.RoomId, request.StartTime, request.EndTime);
            return (BookingOperationStatus.Conflict, null, "Room is already booked for the requested time");
        }

        var booking = new Booking
        {
            RoomId = request.RoomId,
            BookedBy = request.BookedBy,
            BookedByEmail = request.BookedByEmail,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Title = request.Title,
            Purpose = request.Purpose,
            CreatedAt = DateTime.UtcNow
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Created booking {BookingId} for Room={RoomId}", booking.Id, booking.RoomId);

        // Create calendar event (fire-and-forget with logging)
        _ = Task.Run(async () =>
        {
            try
            {
                var subject = string.IsNullOrWhiteSpace(request.Title) ? (string.IsNullOrWhiteSpace(request.Purpose) ? $"Room booking - {room.Name}" : request.Purpose) : request.Title;
                var body = request.Purpose ?? request.Title;
                var eventId = await _calendarService.CreateRoomEventAsync(room, booking.StartTime, booking.EndTime, subject, booking.BookedBy, booking.BookedByEmail, body);
                if (!string.IsNullOrEmpty(eventId))
                {
                    // Persist event id
                    var tracked = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == booking.Id);
                    if (tracked != null)
                    {
                        tracked.CalendarEventId = eventId;
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Linked booking {BookingId} with calendar event {EventId}", booking.Id, eventId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to link booking {BookingId} with calendar event", booking.Id);
            }
        });

        var response = new BookingResponse
        {
            Id = booking.Id,
            RoomId = booking.RoomId,
            RoomName = room.Name,
            BookedBy = booking.BookedBy,
            BookedByEmail = booking.BookedByEmail,
            StartTime = booking.StartTime,
            EndTime = booking.EndTime,
            Title = booking.Title,
            Purpose = booking.Purpose,
            CreatedAt = booking.CreatedAt,
            CalendarEventId = booking.CalendarEventId
        };

        return (BookingOperationStatus.Success, response, null);
    }

    public async Task<(BookingOperationStatus status, string? errorMessage)> UpdateBookingAsync(int id, CreateBookingRequest request)
    {
        _logger.LogInformation("Updating booking {Id} Room={RoomId} Start={Start} End={End}", id, request.RoomId, request.StartTime, request.EndTime);
        var booking = await _context.Bookings.FindAsync(id);
        if (booking == null)
        {
            _logger.LogInformation("Booking {Id} not found for update", id);
            return (BookingOperationStatus.NotFound, null);
        }

        if (request.StartTime >= request.EndTime)
        {
            _logger.LogWarning("Invalid time range when updating booking {Id} Start={Start} End={End}", id, request.StartTime, request.EndTime);
            return (BookingOperationStatus.BadRequest, "End time must be after start time");
        }

        var room = await _context.Rooms.FindAsync(request.RoomId);
        if (room == null)
        {
            _logger.LogWarning("Room {RoomId} not found when updating booking {BookingId}", request.RoomId, id);
            return (BookingOperationStatus.NotFound, $"Room with ID {request.RoomId} not found");
        }

        var hasConflict = await _context.Bookings.AnyAsync(b => b.Id != id &&
                                                                b.RoomId == request.RoomId &&
                                                                b.StartTime < request.EndTime &&
                                                                b.EndTime > request.StartTime);
        if (hasConflict)
        {
            _logger.LogInformation("Conflict updating booking {Id} Room={RoomId} Start={Start} End={End}", id, request.RoomId, request.StartTime, request.EndTime);
            return (BookingOperationStatus.Conflict, "Room is already booked for the requested time");
        }

        booking.RoomId = request.RoomId;
        booking.BookedBy = request.BookedBy;
        booking.BookedByEmail = request.BookedByEmail;
        booking.StartTime = request.StartTime;
        booking.EndTime = request.EndTime;
        booking.Title = request.Title;
        booking.Purpose = request.Purpose;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated booking {Id}", id);

        // Fire-and-forget calendar update via helper
        _ = UpdateCalendarEventAsync(booking, room);

        return (BookingOperationStatus.Success, null);
    }

    private async Task UpdateCalendarEventAsync(Booking booking, Room room)
    {
        if (string.IsNullOrEmpty(booking.CalendarEventId)) return;
        try
        {
            var subject = string.IsNullOrWhiteSpace(booking.Title)
                ? (string.IsNullOrWhiteSpace(booking.Purpose) ? $"Room booking - {room.Name}" : booking.Purpose)
                : booking.Title;
            var body = booking.Purpose ?? booking.Title;
            var success = await _calendarService.UpdateRoomEventAsync(room, booking.CalendarEventId!, booking.StartTime, booking.EndTime, subject!, booking.BookedBy, booking.BookedByEmail, body);
            if (success)
            {
                _logger.LogInformation("Updated calendar event {EventId} for booking {BookingId}", booking.CalendarEventId, booking.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update calendar event for booking {BookingId}", booking.Id);
        }
    }

    public async Task<bool> DeleteBookingAsync(int id)
    {
        _logger.LogInformation("Deleting booking {Id}", id);
        var booking = await _context.Bookings.FindAsync(id);
        if (booking == null)
        {
            _logger.LogInformation("Booking {Id} not found for delete", id);
            return false;
        }

        _context.Bookings.Remove(booking);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Deleted booking {Id}", id);
        return true;
    }

    public async Task<IEnumerable<BookingResponse>> GetUserBookingsAsync(string email)
    {
        _logger.LogDebug("Fetching bookings for user {Email}", email);
        var list = await _context.Bookings
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
                Title = b.Title,
                Purpose = b.Purpose,
                CreatedAt = b.CreatedAt,
                CalendarEventId = b.CalendarEventId
            })
            .ToListAsync();
        _logger.LogDebug("Found {Count} bookings for user {Email}", list.Count, email);
        return list;
    }
}
