using Bookify.Server.Application.Bookings.Contracts;
using FluentValidation;

namespace Bookify.Server.Application.Bookings.Validation;

public class CreateBookingRequestValidator : AbstractValidator<CreateBookingRequest>
{
    public CreateBookingRequestValidator()
    {
        RuleFor(x => x.RoomId).NotEmpty();
        RuleFor(x => x.BookedBy).NotEmpty();
        RuleFor(x => x.BookedByEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.StartTime)
        .Must(dt => dt.Kind == DateTimeKind.Utc || dt.Kind == DateTimeKind.Unspecified)
        .WithMessage("StartTime must be UTC or unspecified")
        .LessThan(x => x.EndTime).WithMessage("StartTime must be before EndTime")
        .GreaterThan(DateTime.UtcNow).WithMessage("Cannot book a room in the past");
        RuleFor(x => x.EndTime)
        .Must(dt => dt.Kind == DateTimeKind.Utc || dt.Kind == DateTimeKind.Unspecified)
        .WithMessage("EndTime must be UTC or unspecified")
        .GreaterThan(x => x.StartTime).WithMessage("EndTime must be after StartTime");
    }
}
