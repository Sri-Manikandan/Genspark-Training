namespace BusBooking.Api.Dtos;

public record CreateBookingResponseDto(
    Guid BookingId,
    string BookingCode,
    string RazorpayOrderId,
    string KeyId,
    long Amount,
    string Currency,
    decimal TotalFare,
    decimal PlatformFee,
    decimal TotalAmount);

