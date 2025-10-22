using Bookify.Server.DTOs;
using Microsoft.Graph.Models;
using GraphEvent = Microsoft.Graph.Models.Event;

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

    // Calendar sync operations invoked by webhook notifications
    Task<bool> UpdateBookingFromCalendarEventAsync(string eventId, CancellationToken ct = default);
    Task<bool> ApplyCalendarEventDeletedAsync(string eventId, CancellationToken ct = default);
    Task<bool> ApplyBookingFromExternalFragmentAsync(string eventId, GraphEvent eventUpdateFragment, CancellationToken ct);
}
