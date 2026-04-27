using BusBooking.Api.Dtos;
using FluentValidation;

namespace BusBooking.Api.Validators;

public class CreateCityRequestValidator : AbstractValidator<CreateCityRequest>
{
    public CreateCityRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.State).NotEmpty().MaximumLength(120);
    }
}
