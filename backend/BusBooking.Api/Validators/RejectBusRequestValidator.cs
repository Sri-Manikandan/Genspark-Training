using BusBooking.Api.Dtos;
using FluentValidation;

namespace BusBooking.Api.Validators;

public class RejectBusRequestValidator : AbstractValidator<RejectBusRequest>
{
    public RejectBusRequestValidator()
    {
        RuleFor(r => r.Reason).NotEmpty().MinimumLength(3).MaximumLength(500);
    }
}
