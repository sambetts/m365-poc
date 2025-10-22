using Bookify.Server.Application.Rooms.Contracts;
using Bookify.Server.Models;

namespace Bookify.Server.Services;

public interface IRoomService
{
    Task<IEnumerable<Room>> GetRoomsAsync();
    Task<Room?> GetRoomAsync(string id);
    Task<IEnumerable<RoomAvailabilityResponse>> CheckAvailabilityAsync(RoomAvailabilityRequest request);
    Task<IEnumerable<BookingInfo>> GetRoomBookingsAsync(string roomId, DateTime? startDate, DateTime? endDate);
}
