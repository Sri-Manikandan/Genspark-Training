using BusBooking.Api.Dtos;
using FluentValidation;

namespace BusBooking.Api.Validators;

public class CancelBookingRequestValidator : AbstractValidator<CancelBookingRequest>
{
    public CancelBookingRequestValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason must be 500 characters or fewer");
    }
}
