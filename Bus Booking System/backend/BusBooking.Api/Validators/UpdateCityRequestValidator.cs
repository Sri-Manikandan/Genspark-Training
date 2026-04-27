using BusBooking.Api.Dtos;
using FluentValidation;

namespace BusBooking.Api.Validators;

public class UpdateCityRequestValidator : AbstractValidator<UpdateCityRequest>
{
    public UpdateCityRequestValidator()
    {
        RuleFor(x => x.Name!).NotEmpty().MaximumLength(120).When(x => x.Name is not null);
        RuleFor(x => x.State!).NotEmpty().MaximumLength(120).When(x => x.State is not null);
    }
}
