using Bookify.Server.Data;
using Bookify.Server.DTOs;
using Bookify.Server.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using GraphEvent = Microsoft.Graph.Models.Event;

namespace Bookify.Server.Services;

public class BookingService(BookifyDbContext context, ILogger<BookingService> logger, IExternalCalendarService calendarService, IBookingCalendarSyncService calendarSync) : IBookingService
{
    // --- Utility helpers ----------------------------------------------------
    private static DateTime AsUtc(DateTime dt) => dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Utc);
    private static bool IsInvalidRange(DateTime start, DateTime end) => start >= end;

    private static (string subject, string? body) BuildSubjectAndBody(string? title, string? body, string roomName)
    {
        var subject = string.IsNullOrWhiteSpace(title)
            ? (string.IsNullOrWhiteSpace(body) ? $"Room booking - {roomName}" : body!)
            : title!;
        var bodyContent = body ?? title;
        return (subject, bodyContent);
    }

    private static BookingResponse MapToResponse(Booking b) => new()
    {
        Id = b.Id,
        RoomId = b.RoomId,
        RoomName = b.Room!.Name,
        BookedBy = b.BookedBy,
        BookedByEmail = b.BookedByEmail,
        StartTime = AsUtc(b.StartTime),
        EndTime = AsUtc(b.EndTime),
        Title = b.Title,
        Body = b.Body,
        CreatedAt = AsUtc(b.CreatedAt),
        CalendarEventId = b.CalendarEventId
    };

    private async Task<bool> HasConflictAsync(string roomId, DateTime startUtc, DateTime endUtc, int? excludeBookingId = null)
    {
        return await context.Bookings.AnyAsync(b => b.RoomId == roomId &&
                                                     (excludeBookingId == null || b.Id != excludeBookingId) &&
                                                     b.StartTime < endUtc &&
                                                     b.EndTime > startUtc);
    }

    private async Task LogAsync(string action, Booking booking, string source)
    {
        context.UpdateLogs.Add(new UpdateLog
        {
            BookingId = booking.Id,
            CalendarEventId = booking.CalendarEventId,
            OccurredAtUtc = DateTime.UtcNow,
            Source = source,
            Action = action
        });
        await context.SaveChangesAsync();
    }

    // --- Calendar operations (internal create/update/delete) ----------------
    private async Task LinkCalendarEventAsync(Booking booking, Room room, CancellationToken ct = default)
    {
        try
        {
            var (subject, body) = BuildSubjectAndBody(booking.Title, booking.Body, room.Name);
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

    private async Task SyncCalendarEventOnUpdateAsync(Booking booking, Room newRoom, bool roomChanged, Room? originalRoom, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(booking.CalendarEventId)) return; // Nothing to sync

        try
        {
            if (roomChanged && originalRoom != null)
            {
                logger.LogInformation(ServiceLogEvents.ExternalUpdate, "Room changed for booking {BookingId} from {OldRoom} to {NewRoom} - recreating calendar event", booking.Id, originalRoom.Id, newRoom.Id);
                var deleted = await calendarService.DeleteRoomEventAsync(originalRoom, booking.CalendarEventId!, ct);
                if (!deleted)
                {
                    logger.LogWarning(ServiceLogEvents.ExternalDelete, "Failed to delete old calendar event {EventId} for booking {BookingId} during room move", booking.CalendarEventId, booking.Id);
                }
                else
                {
                    await LogAsync("CalendarEventDeleted", booking, "web-app");
                }
                var (subject, body) = BuildSubjectAndBody(booking.Title, booking.Body, newRoom.Name);
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
                var (subject, body) = BuildSubjectAndBody(booking.Title, booking.Body, newRoom.Name);
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

    // --- Public API ---------------------------------------------------------
    public async Task<IEnumerable<BookingResponse>> GetBookingsAsync(DateTime? startDate, DateTime? endDate)
    {
        var sw = Stopwatch.StartNew();
        logger.LogDebug(ServiceLogEvents.Fetch, "Fetching bookings with filters StartDate={StartDate} EndDate={EndDate}", startDate, endDate);
        var query = context.Bookings.Include(b => b.Room).AsQueryable();

        if (startDate.HasValue) query = query.Where(b => b.EndTime >= startDate.Value);
        if (endDate.HasValue) query = query.Where(b => b.StartTime <= endDate.Value);

        var list = await query.OrderBy(b => b.StartTime).Select(b => MapToResponse(b)).ToListAsync();
        sw.Stop();
        logger.LogInformation(ServiceLogEvents.Fetch, "Returning {Count} bookings in {ElapsedMs}ms", list.Count, sw.ElapsedMilliseconds);
        return list;
    }

    public async Task<BookingResponse?> GetBookingAsync(int id)
    {
        var sw = Stopwatch.StartNew();
        logger.LogDebug(ServiceLogEvents.Fetch, "Getting booking {Id}", id);
        var booking = await context.Bookings.Include(b => b.Room).FirstOrDefaultAsync(b => b.Id == id);
        sw.Stop();
        if (booking == null)
        {
            logger.LogInformation(ServiceLogEvents.Fetch, "Booking {Id} not found after {ElapsedMs}ms", id, sw.ElapsedMilliseconds);
            return null;
        }
        logger.LogInformation(ServiceLogEvents.Fetch, "Fetched booking {Id} in {ElapsedMs}ms", id, sw.ElapsedMilliseconds);
        return MapToResponse(booking);
    }

    public async Task<(BookingOperationStatus status, BookingResponse? response, string? errorMessage)> CreateBookingAsync(CreateBookingRequest request)
    {
        var sw = Stopwatch.StartNew();
        logger.LogInformation(ServiceLogEvents.Create, "Creating booking for Room={RoomId} By={User} Start={Start:o} End={End:o}", request.RoomId, request.BookedByEmail, request.StartTime, request.EndTime);

        if (IsInvalidRange(request.StartTime, request.EndTime))
        {
            sw.Stop();
            logger.LogWarning(ServiceLogEvents.ValidationFailed, "Invalid time range for booking: Start={Start:o} End={End:o} (Elapsed {ElapsedMs}ms)", request.StartTime, request.EndTime, sw.ElapsedMilliseconds);
            return (BookingOperationStatus.BadRequest, null, "End time must be after start time");
        }

        var startUtc = AsUtc(request.StartTime);
        var endUtc = AsUtc(request.EndTime);

        if (startUtc < DateTime.UtcNow)
        {
            sw.Stop();
            logger.LogWarning(ServiceLogEvents.ValidationFailed, "Attempt to book in the past Start={Start:o} (Elapsed {ElapsedMs}ms)", startUtc, sw.ElapsedMilliseconds);
            return (BookingOperationStatus.BadRequest, null, "Cannot book a room in the past");
        }

        var room = await context.Rooms.FindAsync(request.RoomId);
        if (room == null)
        {
            sw.Stop();
            logger.LogWarning(ServiceLogEvents.Fetch, "Room {RoomId} not found when creating booking (Elapsed {ElapsedMs}ms)", request.RoomId, sw.ElapsedMilliseconds);
            return (BookingOperationStatus.NotFound, null, $"Room with ID {request.RoomId} not found");
        }

        if (await HasConflictAsync(request.RoomId, startUtc, endUtc))
        {
            sw.Stop();
            logger.LogInformation(ServiceLogEvents.Conflict, "Booking conflict for Room={RoomId} Start={Start:o} End={End:o} (Elapsed {ElapsedMs}ms)", request.RoomId, startUtc, endUtc, sw.ElapsedMilliseconds);
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
            Body = request.Body,
            CreatedAt = DateTime.UtcNow
        };

        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        logger.LogInformation(ServiceLogEvents.Create, "Created booking {BookingId} for Room={RoomId}", booking.Id, booking.RoomId);
        await LogAsync("BookingCreated", booking, "web-app");

        await LinkCalendarEventAsync(booking, room);
        sw.Stop();

        var response = MapToResponse(booking);
        logger.LogInformation(ServiceLogEvents.Create, "Completed booking creation {BookingId} in {ElapsedMs}ms", booking.Id, sw.ElapsedMilliseconds);
        return (BookingOperationStatus.Success, response, null);
    }

    public async Task<(BookingOperationStatus status, string? errorMessage)> UpdateBookingAsync(int id, CreateBookingRequest request)
    {
        var sw = Stopwatch.StartNew();
        logger.LogInformation(ServiceLogEvents.Update, "Updating booking {Id} Room={RoomId} Start={Start:o} End={End:o}", id, request.RoomId, request.StartTime, request.EndTime);

        var booking = await context.Bookings.Include(b => b.Room).FirstOrDefaultAsync(b => b.Id == id);
        if (booking == null)
        {
            sw.Stop();
            logger.LogInformation(ServiceLogEvents.Fetch, "Booking {Id} not found for update (Elapsed {ElapsedMs}ms)", id, sw.ElapsedMilliseconds);
            return (BookingOperationStatus.NotFound, null);
        }

        if (IsInvalidRange(request.StartTime, request.EndTime))
        {
            sw.Stop();
            logger.LogWarning(ServiceLogEvents.ValidationFailed, "Invalid time range when updating booking {Id} Start={Start:o} End={End:o} (Elapsed {ElapsedMs}ms)", id, request.StartTime, request.EndTime, sw.ElapsedMilliseconds);
            return (BookingOperationStatus.BadRequest, "End time must be after start time");
        }

        var startUtc = AsUtc(request.StartTime);
        var endUtc = AsUtc(request.EndTime);

        var newRoom = await context.Rooms.FindAsync(request.RoomId);
        if (newRoom == null)
        {
            sw.Stop();
            logger.LogWarning(ServiceLogEvents.Fetch, "Room {RoomId} not found when updating booking {BookingId} (Elapsed {ElapsedMs}ms)", request.RoomId, id, sw.ElapsedMilliseconds);
            return (BookingOperationStatus.NotFound, $"Room with ID {request.RoomId} not found");
        }

        if (await HasConflictAsync(request.RoomId, startUtc, endUtc, excludeBookingId: id))
        {
            sw.Stop();
            logger.LogInformation(ServiceLogEvents.Conflict, "Conflict updating booking {Id} Room={RoomId} Start={Start:o} End={End:o} (Elapsed {ElapsedMs}ms)", id, request.RoomId, startUtc, endUtc, sw.ElapsedMilliseconds);
            return (BookingOperationStatus.Conflict, "Room is already booked for the requested time");
        }

        var roomChanged = booking.RoomId != request.RoomId;
        var originalRoom = booking.Room;

        // Apply updates
        booking.RoomId = request.RoomId;
        booking.BookedBy = request.BookedBy;
        booking.BookedByEmail = request.BookedByEmail;
        booking.StartTime = startUtc;
        booking.EndTime = endUtc;
        booking.Title = request.Title;
        booking.Body = request.Body;

        await context.SaveChangesAsync();
        logger.LogInformation(ServiceLogEvents.Update, "Updated booking {Id}", id);
        await LogAsync("BookingUpdated", booking, "web-app");

        await SyncCalendarEventOnUpdateAsync(booking, newRoom, roomChanged, originalRoom);

        sw.Stop();
        logger.LogInformation(ServiceLogEvents.Update, "Completed booking update {BookingId} in {ElapsedMs}ms", booking.Id, sw.ElapsedMilliseconds);
        return (BookingOperationStatus.Success, null);
    }

    public async Task<bool> DeleteBookingAsync(int id)
    {
        var sw = Stopwatch.StartNew();
        logger.LogInformation(ServiceLogEvents.Delete, "Deleting booking {Id}", id);

        var booking = await context.Bookings.Include(b => b.Room).FirstOrDefaultAsync(b => b.Id == id);
        if (booking == null)
        {
            sw.Stop();
            logger.LogInformation(ServiceLogEvents.Delete, "Booking {Id} not found for delete (Elapsed {ElapsedMs}ms)", id, sw.ElapsedMilliseconds);
            return false;
        }

        await DeleteCalendarEventAsync(booking);

        context.Bookings.Remove(booking);
        await context.SaveChangesAsync();
        await LogAsync("BookingDeleted", booking, "web-app");
        sw.Stop();
        logger.LogInformation(ServiceLogEvents.Delete, "Deleted booking {Id} in {ElapsedMs}ms", id, sw.ElapsedMilliseconds);
        return true;
    }

    public async Task<IEnumerable<BookingResponse>> GetUserBookingsAsync(string email)
    {
        var sw = Stopwatch.StartNew();
        logger.LogDebug(ServiceLogEvents.Fetch, "Fetching bookings for user {Email}", email);
        var list = await context.Bookings.Include(b => b.Room)
            .Where(b => b.BookedByEmail == email)
            .OrderBy(b => b.StartTime)
            .Select(b => MapToResponse(b))
            .ToListAsync();
        sw.Stop();
        logger.LogInformation(ServiceLogEvents.Fetch, "Found {Count} bookings for user {Email} in {ElapsedMs}ms", list.Count, email, sw.ElapsedMilliseconds);
        return list;
    }

    // --- External calendar sync delegated ----------------------------------
    public Task<bool> UpdateBookingFromCalendarEventAsync(string eventId, CancellationToken ct = default) => calendarSync.UpdateBookingFromCalendarEventAsync(eventId, ct);
    public Task<bool> ApplyCalendarEventDeletedAsync(string eventId, CancellationToken ct = default) => calendarSync.ApplyCalendarEventDeletedAsync(eventId, ct);
    public Task<bool> ApplyBookingFromExternalFragmentAsync(string eventId, GraphEvent eventUpdateFragment, CancellationToken ct) => calendarSync.ApplyBookingFromExternalFragmentAsync(eventId, eventUpdateFragment, ct);
}
