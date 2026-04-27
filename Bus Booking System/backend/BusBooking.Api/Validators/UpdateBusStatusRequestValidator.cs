using BusBooking.Api.Dtos;
using BusBooking.Api.Models;
using FluentValidation;

namespace BusBooking.Api.Validators;

public class UpdateBusStatusRequestValidator : AbstractValidator<UpdateBusStatusRequest>
{
    public UpdateBusStatusRequestValidator()
    {
        RuleFor(r => r.OperationalStatus)
            .NotEmpty()
            .Must(s => s == BusOperationalStatus.Active || s == BusOperationalStatus.UnderMaintenance)
            .WithMessage("OperationalStatus must be 'active' or 'under_maintenance'");
    }
}
