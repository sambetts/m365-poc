using Bookify.Server.Application.Rooms.Contracts;
using FluentValidation;

namespace Bookify.Server.Application.Rooms.Validation;

public class RoomAvailabilityRequestValidator : AbstractValidator<RoomAvailabilityRequest>
{
    public RoomAvailabilityRequestValidator()
    {
        RuleFor(x => x.StartTime).LessThan(x => x.EndTime).WithMessage("StartTime must be before EndTime");
        RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime).WithMessage("EndTime must be after StartTime");
    }
}
