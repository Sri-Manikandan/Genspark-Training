namespace BusBooking.Api.Models;

public class Payment
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public required string RazorpayOrderId { get; set; }
    public string? RazorpayPaymentId { get; set; }
    public string? RazorpaySignature { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string Status { get; set; } = PaymentStatus.Created;
    public DateTime CreatedAt { get; set; }
    public DateTime? CapturedAt { get; set; }
    public DateTime? RefundedAt { get; set; }
    public string? RawResponse { get; set; }

    public Booking Booking { get; set; } = null!;
}

