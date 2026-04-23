using BusBooking.Api.Dtos;
using FluentValidation;

namespace BusBooking.Api.Validators;

public class LockSeatsRequestValidator : AbstractValidator<LockSeatsRequest>
{
    public LockSeatsRequestValidator()
    {
        RuleFor(r => r.SessionId).NotEmpty();
        RuleFor(r => r.Seats)
            .NotEmpty().WithMessage("At least one seat must be selected")
            .Must(seats => seats.Count <= 6).WithMessage("Cannot lock more than 6 seats in one request")
            .Must(seats => seats.Distinct(StringComparer.OrdinalIgnoreCase).Count() == seats.Count)
            .WithMessage("Duplicate seat numbers");
        RuleForEach(r => r.Seats)
            .NotEmpty().MaximumLength(8).Matches("^[A-Za-z0-9]+$");
    }
}

