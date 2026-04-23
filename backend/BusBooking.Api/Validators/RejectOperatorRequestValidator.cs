using BusBooking.Api.Dtos;
using FluentValidation;

namespace BusBooking.Api.Validators;

public class RejectOperatorRequestValidator : AbstractValidator<RejectOperatorRequest>
{
    public RejectOperatorRequestValidator()
    {
        RuleFor(r => r.Reason)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(500);
    }
}
