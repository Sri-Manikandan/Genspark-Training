using BusBooking.Api.Dtos;
using BusBooking.Api.Models;
using FluentValidation;

namespace BusBooking.Api.Validators;

public class UpdatePlatformFeeRequestValidator : AbstractValidator<UpdatePlatformFeeRequest>
{
    public UpdatePlatformFeeRequestValidator()
    {
        RuleFor(x => x.FeeType).NotEmpty().Must(v => PlatformFeeType.All.Contains(v))
            .WithMessage("feeType must be 'fixed' or 'percent'");
        RuleFor(x => x.Value).GreaterThanOrEqualTo(0).LessThanOrEqualTo(10000);
    }
}
