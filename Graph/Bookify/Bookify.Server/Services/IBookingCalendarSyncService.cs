using Microsoft.Graph.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Bookify.Server.Services;

public interface IBookingCalendarSyncService
{
    Task<bool> ApplyCalendarEventDeletedAsync(string eventId, CancellationToken ct = default);
    Task<bool> ApplyBookingFromExternalFragmentAsync(string eventId, Event eventUpdateFragment, CancellationToken ct);
    Task<bool> UpdateBookingFromCalendarEventAsync(string eventId, CancellationToken ct = default);
}
