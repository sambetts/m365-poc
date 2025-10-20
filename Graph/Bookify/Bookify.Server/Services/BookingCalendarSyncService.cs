using Bookify.Server.Data;
using Bookify.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph.Models;

namespace Bookify.Server.Services;

public class BookingCalendarSyncService(BookifyDbContext context, ILogger<BookingCalendarSyncService> logger, IExternalCalendarService calendarService) : IBookingCalendarSyncService
{
    private static DateTime AsUtc(DateTime dt) => dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Utc);

    public async Task<bool> ApplyCalendarEventDeletedAsync(string eventId, CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var booking = await context.Bookings.FirstOrDefaultAsync(b => b.CalendarEventId == eventId, ct);
        if (booking == null)
        {
            sw.Stop();
            logger.LogDebug(ServiceLogEvents.ExternalDelete, "No booking found for deleted external event {EventId} (Elapsed {ElapsedMs}ms)", eventId, sw.ElapsedMilliseconds);
            return false;
        }
        context.Bookings.Remove(booking);
        await context.SaveChangesAsync(ct);
        sw.Stop();
        logger.LogInformation(ServiceLogEvents.ExternalDelete, "Removed booking {BookingId} due to external calendar event deletion {EventId} in {ElapsedMs}ms", booking.Id, eventId, sw.ElapsedMilliseconds);
        return true;
    }

    public async Task<bool> ApplyBookingFromExternalFragmentAsync(string eventId, Event eventUpdateFragment, CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var booking = await context.Bookings.Include(b => b.Room).FirstOrDefaultAsync(b => b.CalendarEventId == eventId, ct);
        if (booking?.Room == null)
        {
            sw.Stop();
            logger.LogDebug(ServiceLogEvents.ExternalUpdate, "No booking found for external event fragment update {EventId} (Elapsed {ElapsedMs}ms)", eventId, sw.ElapsedMilliseconds);
            return false;
        }

        var changed = false;
        if (eventUpdateFragment.Start != null && DateTime.TryParse(eventUpdateFragment.Start.DateTime, out var start))
        {
            var startUtc = AsUtc(start);
            if (booking.StartTime != startUtc)
            {
                booking.StartTime = startUtc;
                changed = true;
            }
        }
        if (eventUpdateFragment.End != null && DateTime.TryParse(eventUpdateFragment.End.DateTime, out var end))
        {
            var endUtc = AsUtc(end);
            if (booking.EndTime != endUtc)
            {
                booking.EndTime = endUtc;
                changed = true;
            }
        }
        if (!string.IsNullOrWhiteSpace(eventUpdateFragment.Subject) && booking.Title != eventUpdateFragment.Subject)
        {
            booking.Title = eventUpdateFragment.Subject;
            changed = true;
        }

        if (changed)
        {
            await context.SaveChangesAsync(ct);
            logger.LogInformation(ServiceLogEvents.ExternalUpdate, "Applied external event update to booking {BookingId} from fragment {EventId}", booking.Id, eventId);
        }
        else
        {
            logger.LogInformation(ServiceLogEvents.ExternalUpdate, "Skipped updating booking {BookingId} from fragment {EventId} - no changes detected", booking.Id, eventId);
        }
        sw.Stop();
        logger.LogDebug(ServiceLogEvents.ExternalUpdate, "Processed fragment update for event {EventId} (Changed={Changed}) in {ElapsedMs}ms", eventId, changed, sw.ElapsedMilliseconds);
        return changed;
    }

    public async Task<bool> UpdateBookingFromCalendarEventAsync(string eventId, CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var booking = await context.Bookings.Include(b => b.Room).FirstOrDefaultAsync(b => b.CalendarEventId == eventId, ct);
        if (booking?.Room == null)
        {
            sw.Stop();
            logger.LogDebug(ServiceLogEvents.ExternalUpdate, "No booking found for external event update {EventId} (Elapsed {ElapsedMs}ms)", eventId, sw.ElapsedMilliseconds);
            return false;
        }

        var (success, startUtc, endUtc, subject) = await calendarService.GetRoomEventAsync(booking.Room, eventId, ct);
        if (!success)
        {
            sw.Stop();
            logger.LogWarning(ServiceLogEvents.ExternalFetch, "Failed to fetch external event {EventId} for booking {BookingId} (Elapsed {ElapsedMs}ms)", eventId, booking.Id, sw.ElapsedMilliseconds);
            return false;
        }

        var changed = false;
        if (startUtc.HasValue && endUtc.HasValue && (booking.StartTime != startUtc.Value || booking.EndTime != endUtc.Value))
        {
            booking.StartTime = startUtc.Value;
            booking.EndTime = endUtc.Value;
            changed = true;
        }
        if (!string.IsNullOrWhiteSpace(subject) && booking.Title != subject)
        {
            booking.Title = subject;
            changed = true;
        }

        if (changed)
        {
            await context.SaveChangesAsync(ct);
            logger.LogInformation(ServiceLogEvents.ExternalUpdate, "Applied external event update to booking {BookingId} from full fetch {EventId}", booking.Id, eventId);
        }
        else
        {
            logger.LogInformation(ServiceLogEvents.ExternalUpdate, "Skipped updating booking {BookingId} from full fetch {EventId} - no changes detected", booking.Id, eventId);
        }
        sw.Stop();
        logger.LogDebug(ServiceLogEvents.ExternalUpdate, "Processed full update for event {EventId} (Changed={Changed}) in {ElapsedMs}ms", eventId, changed, sw.ElapsedMilliseconds);
        return changed;
    }
}
