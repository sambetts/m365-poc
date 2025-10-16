using Bookify.Server.Data;
using Bookify.Server.DTOs;
using Bookify.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Bookify.Server.Services;

public class BookingService : IBookingService
{
    private readonly BookifyDbContext _context;
    private readonly ILogger<BookingService> _logger;
    private readonly IExternalCalendarService _calendarService;

    public BookingService(BookifyDbContext context, ILogger<BookingService> logger, IExternalCalendarService calendarService)
    {
        _context = context;
        _logger = logger;
        _calendarService = calendarService;
    }

    private static DateTime AsUtc(DateTime dt)
    {
        // Data coming back from EF (datetime2) has Kind=Unspecified; treat as UTC because we always persist UTC
        return dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Utc);
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
                StartTime = AsUtc(b.StartTime),
                EndTime = AsUtc(b.EndTime),
                Title = b.Title,
                Purpose = b.Purpose,
                CreatedAt = AsUtc(b.CreatedAt),
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
            StartTime = AsUtc(booking.StartTime),
            EndTime = AsUtc(booking.EndTime),
            Title = booking.Title,
            Purpose = booking.Purpose,
            CreatedAt = AsUtc(booking.CreatedAt),
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

        // Ensure incoming times are treated as UTC (client sends ISO with Z)
        var startUtc = request.StartTime.Kind == DateTimeKind.Utc ? request.StartTime : DateTime.SpecifyKind(request.StartTime, DateTimeKind.Utc);
        var endUtc = request.EndTime.Kind == DateTimeKind.Utc ? request.EndTime : DateTime.SpecifyKind(request.EndTime, DateTimeKind.Utc);

        if (startUtc < DateTime.UtcNow)
        {
            _logger.LogWarning("Attempt to book in the past Start={Start}", startUtc);
            return (BookingOperationStatus.BadRequest, null, "Cannot book a room in the past");
        }

        var room = await _context.Rooms.FindAsync(request.RoomId);
        if (room == null)
        {
            _logger.LogWarning("Room {RoomId} not found when creating booking", request.RoomId);
            return (BookingOperationStatus.NotFound, null, $"Room with ID {request.RoomId} not found");
        }

        var hasConflict = await _context.Bookings.AnyAsync(b => b.RoomId == request.RoomId &&
                                                                b.StartTime < endUtc &&
                                                                b.EndTime > startUtc);
        if (hasConflict)
        {
            _logger.LogInformation("Booking conflict for Room={RoomId} Start={Start} End={End}", request.RoomId, startUtc, endUtc);
            return (BookingOperationStatus.Conflict, null, "Room is already booked for the requested time");
        }

        var booking = new Booking
        {
            RoomId = request.RoomId,
            BookedBy = request.BookedBy,
            BookedByEmail = request.BookedByEmail,
            StartTime = startUtc,
            EndTime = endUtc,
            Title = request.Title,
            Purpose = request.Purpose,
            CreatedAt = DateTime.UtcNow
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync(); // get booking Id
        _logger.LogInformation("Created booking {BookingId} for Room={RoomId}", booking.Id, booking.RoomId);

        // Create calendar event synchronously so we can persist its ID reliably
        try
        {
            var subject = string.IsNullOrWhiteSpace(request.Title)
                ? (string.IsNullOrWhiteSpace(request.Purpose) ? $"Room booking - {room.Name}" : request.Purpose)
                : request.Title;
            var body = request.Purpose ?? request.Title;
            var eventId = await _calendarService.CreateRoomEventAsync(room, booking.StartTime, booking.EndTime, subject!, booking.BookedBy, booking.BookedByEmail, body);
            if (!string.IsNullOrEmpty(eventId))
            {
                booking.CalendarEventId = eventId;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Linked booking {BookingId} with calendar event {EventId}", booking.Id, eventId);
            }
            else
            {
                _logger.LogWarning("Calendar event creation returned null for booking {BookingId}", booking.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create/link calendar event for booking {BookingId}", booking.Id);
        }

        var response = new BookingResponse
        {
            Id = booking.Id,
            RoomId = booking.RoomId,
            RoomName = room.Name,
            BookedBy = booking.BookedBy,
            BookedByEmail = booking.BookedByEmail,
            StartTime = AsUtc(booking.StartTime),
            EndTime = AsUtc(booking.EndTime),
            Title = booking.Title,
            Purpose = booking.Purpose,
            CreatedAt = AsUtc(booking.CreatedAt),
            CalendarEventId = booking.CalendarEventId
        };

        return (BookingOperationStatus.Success, response, null);
    }

    public async Task<(BookingOperationStatus status, string? errorMessage)> UpdateBookingAsync(int id, CreateBookingRequest request)
    {
        _logger.LogInformation("Updating booking {Id} Room={RoomId} Start={Start} End={End}", id, request.RoomId, request.StartTime, request.EndTime);

        // Load booking with original room to detect room change
        var booking = await _context.Bookings.Include(b => b.Room).FirstOrDefaultAsync(b => b.Id == id);
        if (booking == null)
        {
            _logger.LogInformation("Booking {Id} not found for update", id);
            return (BookingOperationStatus.NotFound, null);
        }
        var originalRoom = booking.Room; // may be null if not found, but should exist
        var originalRoomId = booking.RoomId;

        if (request.StartTime >= request.EndTime)
        {
            _logger.LogWarning("Invalid time range when updating booking {Id} Start={Start} End={End}", id, request.StartTime, request.EndTime);
            return (BookingOperationStatus.BadRequest, "End time must be after start time");
        }

        var startUtc = request.StartTime.Kind == DateTimeKind.Utc ? request.StartTime : DateTime.SpecifyKind(request.StartTime, DateTimeKind.Utc);
        var endUtc = request.EndTime.Kind == DateTimeKind.Utc ? request.EndTime : DateTime.SpecifyKind(request.EndTime, DateTimeKind.Utc);

        var newRoom = await _context.Rooms.FindAsync(request.RoomId);
        if (newRoom == null)
        {
            _logger.LogWarning("Room {RoomId} not found when updating booking {BookingId}", request.RoomId, id);
            return (BookingOperationStatus.NotFound, $"Room with ID {request.RoomId} not found");
        }

        var hasConflict = await _context.Bookings.AnyAsync(b => b.Id != id &&
                                                                b.RoomId == request.RoomId &&
                                                                b.StartTime < endUtc &&
                                                                b.EndTime > startUtc);
        if (hasConflict)
        {
            _logger.LogInformation("Conflict updating booking {Id} Room={RoomId} Start={Start} End={End}", id, request.RoomId, startUtc, endUtc);
            return (BookingOperationStatus.Conflict, "Room is already booked for the requested time");
        }

        var roomChanged = originalRoomId != request.RoomId;

        // Apply changes (store UTC)
        booking.RoomId = request.RoomId;
        booking.BookedBy = request.BookedBy;
        booking.BookedByEmail = request.BookedByEmail;
        booking.StartTime = startUtc;
        booking.EndTime = endUtc;
        booking.Title = request.Title;
        booking.Purpose = request.Purpose;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated booking {Id}", id);

        // Calendar sync (await so caller knows update attempt occurred)
        if (!string.IsNullOrEmpty(booking.CalendarEventId))
        {
            try
            {
                if (roomChanged && originalRoom != null)
                {
                    _logger.LogInformation("Room changed for booking {BookingId} from {OldRoom} to {NewRoom} - recreating calendar event", booking.Id, originalRoomId, request.RoomId);
                    var deleted = await _calendarService.DeleteRoomEventAsync(originalRoom, booking.CalendarEventId!);
                    if (!deleted)
                    {
                        _logger.LogWarning("Failed to delete old calendar event {EventId} for booking {BookingId} during room move", booking.CalendarEventId, booking.Id);
                    }
                    var subject = string.IsNullOrWhiteSpace(booking.Title)
                        ? (string.IsNullOrWhiteSpace(booking.Purpose) ? $"Room booking - {newRoom.Name}" : booking.Purpose)
                        : booking.Title;
                    var body = booking.Purpose ?? booking.Title;
                    var newEventId = await _calendarService.CreateRoomEventAsync(newRoom, booking.StartTime, booking.EndTime, subject!, booking.BookedBy, booking.BookedByEmail, body);
                    if (!string.IsNullOrEmpty(newEventId))
                    {
                        booking.CalendarEventId = newEventId;
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Recreated calendar event {EventId} for booking {BookingId}", newEventId, booking.Id);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to recreate calendar event for booking {BookingId} after room change", booking.Id);
                    }
                }
                else
                {
                    var subject = string.IsNullOrWhiteSpace(booking.Title)
                        ? (string.IsNullOrWhiteSpace(booking.Purpose) ? $"Room booking - {newRoom.Name}" : booking.Purpose)
                        : booking.Title;
                    var body = booking.Purpose ?? booking.Title;
                    var updated = await _calendarService.UpdateRoomEventAsync(newRoom, booking.CalendarEventId!, booking.StartTime, booking.EndTime, subject!, booking.BookedBy, booking.BookedByEmail, body);
                    if (updated)
                    {
                        _logger.LogInformation("Updated calendar event {EventId} for booking {BookingId}", booking.CalendarEventId, booking.Id);
                    }
                    else
                    {
                        _logger.LogWarning("Calendar update failed for booking {BookingId} Event {EventId}", booking.Id, booking.CalendarEventId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error synchronising calendar event for booking {BookingId}", booking.Id);
            }
        }

        return (BookingOperationStatus.Success, null);
    }

    public async Task<bool> DeleteBookingAsync(int id)
    {
        _logger.LogInformation("Deleting booking {Id}", id);
        // Need room + calendar event id to delete external event
        var booking = await _context.Bookings.Include(b => b.Room).FirstOrDefaultAsync(b => b.Id == id);
        if (booking == null)
        {
            _logger.LogInformation("Booking {Id} not found for delete", id);
            return false;
        }

        // Attempt to delete associated calendar event first (best-effort)
        if (!string.IsNullOrEmpty(booking.CalendarEventId) && booking.Room != null)
        {
            try
            {
                var deletedEvent = await _calendarService.DeleteRoomEventAsync(booking.Room, booking.CalendarEventId);
                if (deletedEvent)
                {
                    _logger.LogInformation("Deleted calendar event {EventId} for booking {BookingId}", booking.CalendarEventId, booking.Id);
                }
                else
                {
                    _logger.LogWarning("Calendar event {EventId} for booking {BookingId} not deleted (may have been removed or not a Bookify event)", booking.CalendarEventId, booking.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete calendar event {EventId} for booking {BookingId}", booking.CalendarEventId, booking.Id);
            }
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
                StartTime = AsUtc(b.StartTime),
                EndTime = AsUtc(b.EndTime),
                Title = b.Title,
                Purpose = b.Purpose,
                CreatedAt = AsUtc(b.CreatedAt),
                CalendarEventId = b.CalendarEventId
            })
            .ToListAsync();
        _logger.LogDebug("Found {Count} bookings for user {Email}", list.Count, email);
        return list;
    }


    public async Task<bool> ApplyCalendarEventDeletedAsync(string eventId, CancellationToken ct = default)
    {
        var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.CalendarEventId == eventId, ct);
        if (booking == null) return false;
        _context.Bookings.Remove(booking);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Removed booking {BookingId} due to external calendar event deletion", booking.Id);
        return true;
    }

    public async Task<bool> ApplyCalendarEventUpdateFromFragmentAsync(string eventId, Microsoft.Graph.Models.Event eventUpdate, CancellationToken ct)
    {
        var booking = await _context.Bookings.Include(b => b.Room).FirstOrDefaultAsync(b => b.CalendarEventId == eventId, ct);
        if (booking?.Room == null) return false;
        var changed = false;
        if (eventUpdate.Start != null && DateTime.TryParse(eventUpdate.Start.DateTime, out var start))
        {
            var startUtc = start.Kind == DateTimeKind.Utc ? start : DateTime.SpecifyKind(start, DateTimeKind.Utc);
            if (booking.StartTime != startUtc)
            {
                booking.StartTime = startUtc;
                changed = true;
            }
        }
        if (eventUpdate.End != null && DateTime.TryParse(eventUpdate.End.DateTime, out var end))
        {
            var endUtc = end.Kind == DateTimeKind.Utc ? end : DateTime.SpecifyKind(end, DateTimeKind.Utc);
            if (booking.EndTime != endUtc)
            {
                booking.EndTime = endUtc;
                changed = true;
            }
        }
        if (!string.IsNullOrWhiteSpace(eventUpdate.Subject) && booking.Title != eventUpdate.Subject)
        {
            booking.Title = eventUpdate.Subject;
            changed = true;
        }
        if (changed)
        {
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Applied external event update to booking {BookingId}", booking.Id);
        }
        return changed;
    }

    public async Task<bool> ApplyCalendarEventUpdatedAsync(string eventId, CancellationToken ct = default)
    {
        var booking = await _context.Bookings.Include(b => b.Room).FirstOrDefaultAsync(b => b.CalendarEventId == eventId, ct);
        if (booking?.Room == null) return false;
        var (success, startUtc, endUtc, subject) = await _calendarService.GetRoomEventAsync(booking.Room, eventId, ct);
        if (!success) return false;
        var changed = false;
        if (startUtc.HasValue && endUtc.HasValue)
        {
            if (booking.StartTime != startUtc.Value || booking.EndTime != endUtc.Value)
            {
                booking.StartTime = startUtc.Value;
                booking.EndTime = endUtc.Value;
                changed = true;
            }
        }
        if (!string.IsNullOrWhiteSpace(subject) && booking.Title != subject)
        {
            booking.Title = subject;
            changed = true;
        }
        if (changed)
        {
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Applied external event update to booking {BookingId}", booking.Id);
        }
        return changed;
    }
}
