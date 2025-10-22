using Bookify.Server.Data;
using Bookify.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph.Models;

namespace Bookify.Server.Services;

/// <summary>
/// Synchronises external Microsoft365 calendar events with local <see cref="Booking"/> entities.
/// Implements one-way update logic in response to Graph webhook notifications or explicit fetch requests.
/// Responsibilities:
/// - Translate event fragments (encrypted webhook resource data) into local booking changes.
/// - Fetch full event data when only an event id is available.
/// - Apply deletions (remove local booking when external event deleted).
/// - Persist update audit trail via <see cref="UpdateLog"/> entries (Source = "notification").
/// </summary>
public class BookingCalendarSyncService(BookifyDbContext context, ILogger<BookingCalendarSyncService> logger, IExternalCalendarService calendarService) : IBookingCalendarSyncService
{
    private static DateTime AsUtc(DateTime dt) => dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Utc);

    /// <summary>
    /// Records a synchronisation action for diagnostics / audit history.
    /// </summary>
    /// <param name="action">Logical action applied (CalendarEventUpdated / CalendarEventDeleted).</param>
    /// <param name="booking">Affected booking.</param>
    private void LogUpdate(string action, Booking booking)
    {
        context.UpdateLogs.Add(new UpdateLog
        {
            BookingId = booking.Id,
            CalendarEventId = booking.CalendarEventId,
            OccurredAtUtc = DateTime.UtcNow,
            Source = "notification",
            Action = action
        });
    }

    /// <summary>
    /// Applies external event data (full or fragment) to a booking, updating start/end/subject/attendees where changed.
    /// Returns true if any field was modified.
    /// </summary>
    /// <param name="booking">Target local booking.</param>
    /// <param name="startUtc">Optional new UTC start time.</param>
    /// <param name="endUtc">Optional new UTC end time.</param>
    /// <param name="subject">Optional new subject/title.</param>
    /// <param name="attendees">Optional attendee email list (normalised lowercase).</param>
    /// <returns>True if at least one property changed.</returns>
    private static bool ApplyEventDataToBooking(Booking booking, DateTime? startUtc, DateTime? endUtc, string? subject, List<string>? attendees)
    {
        var changed = false;
        if (startUtc.HasValue && booking.StartTime != startUtc.Value)
        {
            booking.StartTime = startUtc.Value;
            changed = true;
        }
        if (endUtc.HasValue && booking.EndTime != endUtc.Value)
        {
            booking.EndTime = endUtc.Value;
            changed = true;
        }
        if (!string.IsNullOrWhiteSpace(subject) && booking.Title != subject)
        {
            booking.Title = subject;
            changed = true;
        }
        if (attendees != null)
        {
            // Normalise incoming list (trim + lower + distinct + sorted) so comparisons are deterministic.
            var normalisedIncoming = attendees.Select(a => a.Trim().ToLowerInvariant()).Distinct().OrderBy(a => a).ToList();
            var normalisedExisting = booking.Attendees.Select(a => a.Trim().ToLowerInvariant()).Distinct().OrderBy(a => a).ToList();
            if (!normalisedIncoming.SequenceEqual(normalisedExisting))
            {
                booking.Attendees = normalisedIncoming; // Replace entire list.
                changed = true;
            }
        }
        return changed;
    }

    /// <summary>
    /// Handles an external calendar event deletion by removing the associated local booking.
    /// </summary>
    /// <param name="eventId">External calendar event identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if a booking was found and removed; false otherwise.</returns>
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
        LogUpdate("CalendarEventDeleted", booking);
        await context.SaveChangesAsync(ct);
        sw.Stop();
        logger.LogInformation(ServiceLogEvents.ExternalDelete, "Removed booking {BookingId} due to external calendar event deletion {EventId} in {ElapsedMs}ms", booking.Id, eventId, sw.ElapsedMilliseconds);
        return true;
    }

    /// <summary>
    /// Applies a partial (fragment) update received from an encrypted Graph webhook notification.
    /// Only fields present in the fragment are considered; missing fields are ignored.
    /// </summary>
    /// <param name="eventId">External event id.</param>
    /// <param name="eventUpdateFragment">Partially populated <see cref="Event"/> instance (decrypted resource data).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if booking was updated; false if no booking found or no changes.</returns>
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

        // Extract times if present.
        DateTime? startUtc = null;
        if (eventUpdateFragment.Start != null && DateTime.TryParse(eventUpdateFragment.Start.DateTime, out var start))
        {
            startUtc = AsUtc(start);
        }
        DateTime? endUtc = null;
        if (eventUpdateFragment.End != null && DateTime.TryParse(eventUpdateFragment.End.DateTime, out var end))
        {
            endUtc = AsUtc(end);
        }
        var subject = eventUpdateFragment.Subject;

        // Extract attendees (may be omitted depending on subscription configuration).
        List<string>? attendees = null;
        if (eventUpdateFragment.Attendees?.Count >0)
        {
            attendees = eventUpdateFragment.Attendees
                .Select(a => a.EmailAddress?.Address)
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Select(a => a!.Trim().ToLowerInvariant())
                .Distinct()
                .ToList();
        }

        var changed = ApplyEventDataToBooking(booking, startUtc, endUtc, subject, attendees);

        if (changed)
        {
            LogUpdate("CalendarEventUpdated", booking);
            await context.SaveChangesAsync(ct);
            logger.LogInformation(ServiceLogEvents.ExternalUpdate, "Applied external event update to booking {BookingId} from fragment {EventId}", booking.Id, eventId);
        }
        else
        {
            logger.LogDebug(ServiceLogEvents.ExternalUpdate, "Skipped updating booking {BookingId} from fragment {EventId} - no changes detected", booking.Id, eventId);
        }
        sw.Stop();
        logger.LogDebug(ServiceLogEvents.ExternalUpdate, "Processed fragment update for event {EventId} (Changed={Changed}) in {ElapsedMs}ms", eventId, changed, sw.ElapsedMilliseconds);
        return changed;
    }

    /// <summary>
    /// Fetches full external event data (via Graph) and applies differences to a local booking.
    /// Used when webhook payload does not include resource data or when a proactive refresh is needed.
    /// </summary>
    /// <param name="eventId">External event id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if booking was updated; false if no booking found or no changes / fetch failed.</returns>
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

        // Fetch full event details from external calendar service.
        var (success, startUtc, endUtc, subject, attendees) = await calendarService.GetRoomEventAsync(booking.Room, eventId, ct);
        if (!success)
        {
            sw.Stop();
            logger.LogWarning(ServiceLogEvents.ExternalFetch, "Failed to fetch external event {EventId} for booking {BookingId} (Elapsed {ElapsedMs}ms)", eventId, booking.Id, sw.ElapsedMilliseconds);
            return false;
        }

        var changed = ApplyEventDataToBooking(booking, startUtc, endUtc, subject, attendees);

        if (changed)
        {
            LogUpdate("CalendarEventUpdated", booking);
            await context.SaveChangesAsync(ct);
            logger.LogInformation(ServiceLogEvents.ExternalUpdate, "Applied external event update to booking {BookingId} from full fetch {EventId}", booking.Id, eventId);
        }
        else
        {
            logger.LogDebug(ServiceLogEvents.ExternalUpdate, "Skipped updating booking {BookingId} from full fetch {EventId} - no changes detected", booking.Id, eventId);
        }
        sw.Stop();
        logger.LogDebug(ServiceLogEvents.ExternalUpdate, "Processed full update for event {EventId} (Changed={Changed}) in {ElapsedMs}ms", eventId, changed, sw.ElapsedMilliseconds);
        return changed;
    }
}
