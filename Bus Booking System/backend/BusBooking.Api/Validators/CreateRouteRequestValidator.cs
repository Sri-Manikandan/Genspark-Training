using BusBooking.Api.Dtos;
using FluentValidation;

namespace BusBooking.Api.Validators;

public class CreateRouteRequestValidator : AbstractValidator<CreateRouteRequest>
{
    public CreateRouteRequestValidator()
    {
        RuleFor(x => x.SourceCityId).NotEmpty();
        RuleFor(x => x.DestinationCityId).NotEmpty()
            .NotEqual(x => x.SourceCityId)
            .WithMessage("Destination city must differ from source city");
        RuleFor(x => x.DistanceKm!.Value).GreaterThan(0).LessThanOrEqualTo(5000)
            .When(x => x.DistanceKm is not null);
    }
}
