using Bookify.Server.Application.Bookings;
using Bookify.Server.Application.Bookings.Contracts;
using Bookify.Server.Application.Common;
using Bookify.Server.Data;
using Bookify.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Bookify.Server.Services;

/// <summary>
/// Core application service for managing <see cref="Booking"/> entities (CRUD + external calendar sync).
/// Responsibilities:
/// - Enforce domain invariants (conflict detection, room existence).
/// - Create / update / delete external calendar events via <see cref="IExternalCalendarService"/>.
/// - Persist audit/update trail through <see cref="UpdateLog"/> entries.
/// - Provide mapping to API contracts using <see cref="BookingMapper"/>.
/// External calendar mutation is performed best-effort; failures are logged but do not abort DB persistence.
/// </summary>
public class BookingService(BookifyDbContext context, ILogger<BookingService> logger, IExternalCalendarService calendarService) : IBookingService
{
    /// <summary>Normalises a <see cref="DateTime"/> to UTC (if Unspecified or Local).</summary>
    private static DateTime AsUtc(DateTime dt) => dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Utc);

    /// <summary>
    /// Checks if a proposed booking window overlaps an existing booking for the same room.
    /// Excludes an existing booking when updating (identified by <paramref name="excludeBookingId"/>).
    /// Uses efficient range overlap predicate: (existing.Start < proposedEnd) AND (existing.End > proposedStart).
    /// </summary>
    private async Task<bool> HasConflictAsync(string roomId, DateTime startUtc, DateTime endUtc, int? excludeBookingId = null) =>
    await context.Bookings.AnyAsync(b => b.RoomId == roomId && (excludeBookingId == null || b.Id != excludeBookingId) && b.StartTime < endUtc && b.EndTime > startUtc);

    /// <summary>
    /// Appends an <see cref="UpdateLog"/> entry for auditing external or internal mutations.
    /// Source values: "web-app" (local user initiated) or "notification" (Graph webhook initiated).
    /// </summary>
    private async Task LogAsync(string action, Booking booking, string source)
    {
        context.UpdateLogs.Add(new UpdateLog { BookingId = booking.Id, CalendarEventId = booking.CalendarEventId, OccurredAtUtc = DateTime.UtcNow, Source = source, Action = action });
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Creates a calendar event for a newly created booking and links the returned event id.
    /// Failure is logged but does not roll back the already committed booking.
    /// </summary>
    private async Task LinkCalendarEventAsync(Booking booking, Room room, CancellationToken ct = default)
    {
        try
        {
            var (subject, body) = BookingMapper.BuildSubjectAndBody(booking.Title, booking.Body, room.Name);
            var eventId = await calendarService.CreateEventAsync(room, booking.StartTime, booking.EndTime, subject, booking.BookedBy, booking.BookedByEmail, body, ct);
            if (!string.IsNullOrEmpty(eventId))
            {
                booking.CalendarEventId = eventId;
                await context.SaveChangesAsync(ct);
                logger.LogInformation(ServiceLogEvents.ExternalCreate, "Linked booking {BookingId} with calendar event {EventId}", booking.Id, eventId);
                await LogAsync("CalendarEventCreated", booking, "web-app");
            }
            else
            {
                logger.LogWarning(ServiceLogEvents.ExternalCreate, "Calendar event creation returned null for booking {BookingId}", booking.Id);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create/link calendar event for booking {BookingId}", booking.Id);
        }
    }

    /// <summary>
    /// Synchronises an existing external calendar event after a booking update.
    /// If the room changes the old event is deleted and a new one recreated in the target room.
    /// Otherwise the existing event is updated with new time / subject / body details.
    /// Errors are logged; booking data remains updated regardless of remote failures.
    /// </summary>
    private async Task SyncCalendarEventOnUpdateAsync(Booking booking, Room newRoom, bool roomChanged, Room? originalRoom, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(booking.CalendarEventId)) return; // No external linkage.
        try
        {
            if (roomChanged && originalRoom != null)
            {
                logger.LogInformation(ServiceLogEvents.ExternalUpdate, "Room changed for booking {BookingId} from {OldRoom} to {NewRoom} - recreating calendar event", booking.Id, originalRoom.Id, newRoom.Id);
                var deleted = await calendarService.DeleteRoomEventAsync(originalRoom, booking.CalendarEventId!, ct);
                if (deleted)
                {
                    await LogAsync("CalendarEventDeleted", booking, "web-app");
                }
                else
                {
                    logger.LogWarning(ServiceLogEvents.ExternalDelete, "Failed to delete old calendar event {EventId} during room move for booking {BookingId}", booking.CalendarEventId, booking.Id);
                }

                var (subject, body) = BookingMapper.BuildSubjectAndBody(booking.Title, booking.Body, newRoom.Name);
                var newEventId = await calendarService.CreateEventAsync(newRoom, booking.StartTime, booking.EndTime, subject, booking.BookedBy, booking.BookedByEmail, body, ct);
                if (!string.IsNullOrEmpty(newEventId))
                {
                    booking.CalendarEventId = newEventId;
                    await context.SaveChangesAsync(ct);
                    logger.LogInformation(ServiceLogEvents.ExternalCreate, "Recreated calendar event {EventId} for booking {BookingId}", newEventId, booking.Id);
                    await LogAsync("CalendarEventCreated", booking, "web-app");
                }
                else
                {
                    logger.LogWarning(ServiceLogEvents.ExternalCreate, "Failed to recreate calendar event for booking {BookingId} after room change", booking.Id);
                }
            }
            else
            {
                var (subject, body) = BookingMapper.BuildSubjectAndBody(booking.Title, booking.Body, newRoom.Name);
                var updated = await calendarService.UpdateEventAsync(newRoom, booking.CalendarEventId!, booking.StartTime, booking.EndTime, subject, booking.BookedBy, booking.BookedByEmail, body, ct);
                if (updated)
                {
                    logger.LogInformation(ServiceLogEvents.ExternalUpdate, "Updated calendar event {EventId} for booking {BookingId}", booking.CalendarEventId, booking.Id);
                    await LogAsync("CalendarEventUpdated", booking, "web-app");
                }
                else
                {
                    logger.LogWarning(ServiceLogEvents.ExternalUpdate, "Calendar update failed for booking {BookingId} Event {EventId}", booking.Id, booking.CalendarEventId);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error synchronising calendar event for booking {BookingId}", booking.Id);
        }
    }

    /// <summary>
    /// Deletes the external calendar event linked to a booking (if present).
    /// Logs success/failure; does not throw to calling code.
    /// </summary>
    private async Task<bool> DeleteCalendarEventAsync(Booking booking, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(booking.CalendarEventId) || booking.Room == null) return false;
        try
        {
            var deletedEvent = await calendarService.DeleteRoomEventAsync(booking.Room, booking.CalendarEventId, ct);
            if (deletedEvent)
            {
                logger.LogInformation(ServiceLogEvents.ExternalDelete, "Deleted calendar event {EventId} for booking {BookingId}", booking.CalendarEventId, booking.Id);
                await LogAsync("CalendarEventDeleted", booking, "web-app");
            }
            else
            {
                logger.LogWarning(ServiceLogEvents.ExternalDelete, "Calendar event {EventId} for booking {BookingId} not deleted", booking.CalendarEventId, booking.Id);
            }
            return deletedEvent;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete calendar event {EventId} for booking {BookingId}", booking.CalendarEventId, booking.Id);
            return false;
        }
    }

    /// <summary>
    /// Returns bookings optionally filtered by a date window (inclusive overlap) ordered by start time.
    /// </summary>
    public async Task<IEnumerable<BookingResponse>> GetBookingsAsync(DateTime? startDate, DateTime? endDate)
    {
        var query = context.Bookings.Include(b => b.Room).AsQueryable();
        if (startDate.HasValue) query = query.Where(b => b.EndTime >= startDate.Value);
        if (endDate.HasValue) query = query.Where(b => b.StartTime <= endDate.Value);
        return await query.OrderBy(b => b.StartTime).Select(b => BookingMapper.MapToResponse(b)).ToListAsync();
    }

    /// <summary>
    /// Retrieves a single booking by id including its room; returns null if not found.
    /// </summary>
    public async Task<BookingResponse?> GetBookingAsync(int id)
    {
        var booking = await context.Bookings.Include(b => b.Room).FirstOrDefaultAsync(b => b.Id == id);
        return booking == null ? null : BookingMapper.MapToResponse(booking);
    }

    /// <summary>
    /// Creates a new booking after conflict and room existence checks; optionally creates an external calendar event.
    /// Validation (time range, future start, etc.) handled via FluentValidation before invocation.
    /// </summary>
    public async Task<Result<BookingResponse>> CreateBookingAsync(CreateBookingRequest request, bool createExternal)
    {
        var startUtc = AsUtc(request.StartTime); var endUtc = AsUtc(request.EndTime);
        var room = await context.Rooms.FindAsync(request.RoomId); if (room == null) return Result<BookingResponse>.Fail($"Room with ID {request.RoomId} not found");
        if (await HasConflictAsync(request.RoomId, startUtc, endUtc)) return Result<BookingResponse>.Fail("Room is already booked for the requested time");
        var booking = new Booking { RoomId = request.RoomId, BookedBy = request.BookedBy, BookedByEmail = request.BookedByEmail, StartTime = startUtc, EndTime = endUtc, Title = request.Title, Body = request.Body, CreatedAt = DateTime.UtcNow };
        context.Bookings.Add(booking); await context.SaveChangesAsync(); await LogAsync("BookingCreated", booking, "web-app"); if (createExternal) await LinkCalendarEventAsync(booking, room!);
        return Result<BookingResponse>.Ok(BookingMapper.MapToResponse(booking));
    }

    /// <summary>
    /// Updates an existing booking (if found) and synchronises external calendar event.
    /// Returns failure result if booking/room missing or time window conflicts.
    /// </summary>
    public async Task<Result> UpdateBookingAsync(int id, CreateBookingRequest request)
    {
        var booking = await context.Bookings.Include(b => b.Room).FirstOrDefaultAsync(b => b.Id == id);
        if (booking == null) return Result.Fail("Booking not found");
        var startUtc = AsUtc(request.StartTime); var endUtc = AsUtc(request.EndTime);
        var newRoom = await context.Rooms.FindAsync(request.RoomId); if (newRoom == null) return Result.Fail($"Room with ID {request.RoomId} not found");
        if (await HasConflictAsync(request.RoomId, startUtc, endUtc, excludeBookingId: id)) return Result.Fail("Room is already booked for the requested time");
        var roomChanged = booking.RoomId != request.RoomId; var originalRoom = booking.Room;
        booking.RoomId = request.RoomId; booking.BookedBy = request.BookedBy; booking.BookedByEmail = request.BookedByEmail; booking.StartTime = startUtc; booking.EndTime = endUtc; booking.Title = request.Title; booking.Body = request.Body;
        await context.SaveChangesAsync(); await LogAsync("BookingUpdated", booking, "web-app"); await SyncCalendarEventOnUpdateAsync(booking, newRoom!, roomChanged, originalRoom);
        return Result.Ok();
    }

    /// <summary>
    /// Deletes a booking and its external calendar event (if linked). Returns false if booking not found.
    /// </summary>
    public async Task<bool> DeleteBookingAsync(int id)
    {
        var booking = await context.Bookings.Include(b => b.Room).FirstOrDefaultAsync(b => b.Id == id);
        if (booking == null) return false;
        await DeleteCalendarEventAsync(booking); context.Bookings.Remove(booking); await context.SaveChangesAsync(); await LogAsync("BookingDeleted", booking, "web-app");
        return true;
    }

    /// <summary>
    /// Lists all bookings for a specific user ordered by start time.
    /// </summary>
    public async Task<IEnumerable<BookingResponse>> GetUserBookingsAsync(string email)
    {
        return await context.Bookings.Include(b => b.Room).Where(b => b.BookedByEmail == email).OrderBy(b => b.StartTime).Select(b => BookingMapper.MapToResponse(b)).ToListAsync();
    }
}
