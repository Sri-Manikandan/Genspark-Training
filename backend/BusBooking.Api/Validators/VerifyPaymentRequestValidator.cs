using BusBooking.Api.Dtos;
using FluentValidation;

namespace BusBooking.Api.Validators;

public class VerifyPaymentRequestValidator : AbstractValidator<VerifyPaymentRequest>
{
    public VerifyPaymentRequestValidator()
    {
        RuleFor(r => r.RazorpayPaymentId).NotEmpty().MaximumLength(64);
        RuleFor(r => r.RazorpaySignature).NotEmpty().MaximumLength(200);
    }
}

