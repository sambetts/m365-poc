namespace Bookify.Server.Application.Bookings;

using Bookify.Server.Application.Bookings.Contracts;
using Bookify.Server.Models;

public static class BookingMapper
{
    public static (string subject, string? body) BuildSubjectAndBody(string? title, string? body, string roomName)
    {
        var subject = string.IsNullOrWhiteSpace(title)
        ? (string.IsNullOrWhiteSpace(body) ? $"Room booking - {roomName}" : body!)
        : title!;
        var bodyContent = body ?? title;
        return (subject, bodyContent);
    }

    public static BookingResponse MapToResponse(Booking b) => new()
    {
        Id = b.Id,
        RoomId = b.RoomId,
        RoomName = b.Room!.Name,
        BookedBy = b.BookedBy,
        BookedByEmail = b.BookedByEmail,
        StartTime = EnsureUtc(b.StartTime),
        EndTime = EnsureUtc(b.EndTime),
        Title = b.Title,
        Body = b.Body,
        CreatedAt = EnsureUtc(b.CreatedAt),
        CalendarEventId = b.CalendarEventId
    };

    private static DateTime EnsureUtc(DateTime dt) => dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Utc);
}
