using BusBooking.Api.Dtos;
using FluentValidation;

namespace BusBooking.Api.Validators;

public class BecomeOperatorRequestValidator : AbstractValidator<BecomeOperatorRequest>
{
    public BecomeOperatorRequestValidator()
    {
        RuleFor(r => r.CompanyName)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(160);
    }
}
