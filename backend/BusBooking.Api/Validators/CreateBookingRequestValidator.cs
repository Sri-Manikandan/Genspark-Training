using BusBooking.Api.Dtos;
using BusBooking.Api.Models;
using FluentValidation;

namespace BusBooking.Api.Validators;

public class CreateBookingRequestValidator : AbstractValidator<CreateBookingRequest>
{
    public CreateBookingRequestValidator()
    {
        RuleFor(r => r.TripId).NotEmpty();
        RuleFor(r => r.LockId).NotEmpty();
        RuleFor(r => r.SessionId).NotEmpty();
        RuleFor(r => r.Passengers)
            .NotEmpty()
            .Must(p => p.Count <= 6).WithMessage("Cannot book more than 6 seats")
            .Must(p => p.Select(x => x.SeatNumber).Distinct(StringComparer.OrdinalIgnoreCase).Count() == p.Count)
            .WithMessage("Duplicate seat numbers in passenger list");
        RuleForEach(r => r.Passengers).ChildRules(p =>
        {
            p.RuleFor(x => x.SeatNumber).NotEmpty().MaximumLength(8);
            p.RuleFor(x => x.PassengerName).NotEmpty().MaximumLength(120);
            p.RuleFor(x => x.PassengerAge).InclusiveBetween(1, 120);
            p.RuleFor(x => x.PassengerGender).Must(g => PassengerGender.All.Contains(g))
                .WithMessage($"Must be one of: {string.Join(", ", PassengerGender.All)}");
        });
    }
}

