using BusBooking.Api.Dtos;
using BusBooking.Api.Models;
using BusBooking.Api.Services;
using FluentValidation;

namespace BusBooking.Api.Validators;

public class CreateBusRequestValidator : AbstractValidator<CreateBusRequest>
{
    public CreateBusRequestValidator()
    {
        RuleFor(r => r.RegistrationNumber).NotEmpty().MinimumLength(4).MaximumLength(32);
        RuleFor(r => r.BusName).NotEmpty().MinimumLength(2).MaximumLength(120);
        RuleFor(r => r.BusType).NotEmpty().Must(t => BusType.All.Contains(t))
            .WithMessage($"BusType must be one of: {string.Join(", ", BusType.All)}");
        RuleFor(r => r.Rows).InclusiveBetween(1, SeatLayoutGenerator.MaxRows);
        RuleFor(r => r.Columns).InclusiveBetween(1, SeatLayoutGenerator.MaxColumns);
    }
}
