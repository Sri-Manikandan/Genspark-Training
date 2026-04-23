using BusBooking.Api.Dtos;
using FluentValidation;

namespace BusBooking.Api.Validators;

public class CreateOperatorOfficeRequestValidator : AbstractValidator<CreateOperatorOfficeRequest>
{
    public CreateOperatorOfficeRequestValidator()
    {
        RuleFor(r => r.CityId).NotEmpty();
        RuleFor(r => r.AddressLine).NotEmpty().MinimumLength(5).MaximumLength(300);
        RuleFor(r => r.Phone).NotEmpty().MinimumLength(6).MaximumLength(32);
    }
}
