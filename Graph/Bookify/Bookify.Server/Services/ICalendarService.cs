using Bookify.Server.Models;

namespace Bookify.Server.Services;

public interface ICalendarService
{
    Task<string?> CreateRoomEventAsync(Bookify.Server.Models.Room room, DateTime startUtc, DateTime endUtc, string subject, string organiserName, string organiserEmail, string? body = null, CancellationToken ct = default);
    Task<bool> UpdateRoomEventAsync(Bookify.Server.Models.Room room, string eventId, DateTime startUtc, DateTime endUtc, string subject, string organiserName, string organiserEmail, string? body = null, CancellationToken ct = default);
    Task<bool> DeleteRoomEventAsync(Bookify.Server.Models.Room room, string eventId, CancellationToken ct = default);
}
