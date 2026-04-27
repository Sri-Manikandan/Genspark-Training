using BusBooking.Api.Dtos;
using FluentValidation;

namespace BusBooking.Api.Validators;

public class UpdateBusScheduleRequestValidator : AbstractValidator<UpdateBusScheduleRequest>
{
    public UpdateBusScheduleRequestValidator()
    {
        When(x => x.FarePerSeat.HasValue, () =>
            RuleFor(x => x.FarePerSeat!.Value).GreaterThan(0));
        When(x => x.DaysOfWeek.HasValue, () =>
            RuleFor(x => x.DaysOfWeek!.Value).InclusiveBetween(1, 127)
                .WithMessage("DaysOfWeek must be a bitmask between 1 and 127"));
        When(x => x.ValidFrom.HasValue && x.ValidTo.HasValue, () =>
            RuleFor(x => x.ValidTo!.Value).GreaterThanOrEqualTo(x => x.ValidFrom!.Value)
                .WithMessage("ValidTo must be on or after ValidFrom"));
    }
}
