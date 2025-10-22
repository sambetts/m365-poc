using Bookify.Server.Application.Bookings.Contracts;
using Bookify.Server.Application.Common;

namespace Bookify.Server.Services;

public interface IBookingService
{
    Task<IEnumerable<BookingResponse>> GetBookingsAsync(DateTime? startDate, DateTime? endDate);
    Task<BookingResponse?> GetBookingAsync(int id);
    Task<Result<BookingResponse>> CreateBookingAsync(CreateBookingRequest request, bool createExternal);
    Task<Result> UpdateBookingAsync(int id, CreateBookingRequest request);
    Task<bool> DeleteBookingAsync(int id);
    Task<IEnumerable<BookingResponse>> GetUserBookingsAsync(string email);
}
