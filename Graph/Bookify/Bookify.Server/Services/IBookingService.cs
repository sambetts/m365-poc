using Bookify.Server.DTOs;

namespace Bookify.Server.Services;

public enum BookingOperationStatus
{
    Success,
    NotFound,
    Conflict,
    BadRequest
}

public interface IBookingService
{
    Task<IEnumerable<BookingResponse>> GetBookingsAsync(DateTime? startDate, DateTime? endDate);
    Task<BookingResponse?> GetBookingAsync(int id);
    Task<(BookingOperationStatus status, BookingResponse? response, string? errorMessage)> CreateBookingAsync(CreateBookingRequest request, bool createExternal);
    Task<(BookingOperationStatus status, string? errorMessage)> UpdateBookingAsync(int id, CreateBookingRequest request);
    Task<bool> DeleteBookingAsync(int id);
    Task<IEnumerable<BookingResponse>> GetUserBookingsAsync(string email);
    // External calendar sync operations have been removed; callers should depend directly on IBookingCalendarSyncService.
}
