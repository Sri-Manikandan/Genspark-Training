using BusBooking.Api.Dtos;
using FluentValidation;

namespace BusBooking.Api.Validators;

public class UpdateRouteRequestValidator : AbstractValidator<UpdateRouteRequest>
{
    public UpdateRouteRequestValidator()
    {
        RuleFor(x => x.DistanceKm!.Value).GreaterThan(0).LessThanOrEqualTo(5000)
            .When(x => x.DistanceKm is not null);
    }
}
