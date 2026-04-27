namespace BusBooking.Api.Infrastructure.Razorpay;

public record RazorpayOrder(string Id, long Amount, string Currency, string Receipt);

public record RazorpayRefund(string Id, string PaymentId, long Amount, string Status);

public interface IRazorpayClient
{
    string KeyId { get; }
    Task<RazorpayOrder> CreateOrderAsync(long amountInPaise, string receipt, CancellationToken ct);
    bool VerifySignature(string orderId, string paymentId, string signature);
    Task<RazorpayRefund> CreateRefundAsync(string paymentId, long amountInPaise, CancellationToken ct);
}

