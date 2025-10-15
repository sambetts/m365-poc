using Bookify.Server.Models;

namespace Bookify.Server.Services;

public interface IExternalCalendarService
{
    Task<string?> CreateRoomEventAsync(Bookify.Server.Models.Room room, DateTime startUtc, DateTime endUtc, string subject, string organiserName, string organiserEmail, string? body = null, CancellationToken ct = default);
    Task<bool> UpdateRoomEventAsync(Bookify.Server.Models.Room room, string eventId, DateTime startUtc, DateTime endUtc, string subject, string organiserName, string organiserEmail, string? body = null, CancellationToken ct = default);
    Task<bool> DeleteRoomEventAsync(Bookify.Server.Models.Room room, string eventId, CancellationToken ct = default);
    // Fetch an event and return basic details (UTC start/end + subject). Returns success flag.
    Task<(bool success, DateTime? startUtc, DateTime? endUtc, string? subject)> GetRoomEventAsync(Room room, string eventId, CancellationToken ct = default);
}
