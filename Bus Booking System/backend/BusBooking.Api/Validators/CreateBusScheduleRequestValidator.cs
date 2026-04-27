using BusBooking.Api.Dtos;
using FluentValidation;

namespace BusBooking.Api.Validators;

public class CreateBusScheduleRequestValidator : AbstractValidator<CreateBusScheduleRequest>
{
    public CreateBusScheduleRequestValidator()
    {
        RuleFor(x => x.BusId).NotEmpty();
        RuleFor(x => x.RouteId).NotEmpty();
        RuleFor(x => x.FarePerSeat).GreaterThan(0);
        RuleFor(x => x.ValidFrom).NotEmpty();
        RuleFor(x => x.ValidTo).GreaterThanOrEqualTo(x => x.ValidFrom)
            .WithMessage("ValidTo must be on or after ValidFrom");
        RuleFor(x => x.DaysOfWeek).InclusiveBetween(1, 127)
            .WithMessage("DaysOfWeek must be a bitmask between 1 and 127 (at least one day selected)");
    }
}
