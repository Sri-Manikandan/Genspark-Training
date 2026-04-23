namespace BusBooking.Api.Dtos;

public record VerifyPaymentRequest(string RazorpayPaymentId, string RazorpaySignature);

