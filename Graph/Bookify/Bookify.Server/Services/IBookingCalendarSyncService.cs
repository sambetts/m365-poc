using Microsoft.Graph.Models;

namespace Bookify.Server.Services;

public interface IBookingCalendarSyncService
{
    Task<bool> ApplyCalendarEventDeletedAsync(string eventId, CancellationToken ct = default);
    Task<bool> ApplyBookingFromExternalFragmentAsync(string eventId, Event eventUpdateFragment, CancellationToken ct);
    Task<bool> UpdateBookingFromCalendarEventAsync(string eventId, CancellationToken ct = default);
}
