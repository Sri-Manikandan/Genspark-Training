namespace BusBooking.Api.Infrastructure.Razorpay;

public record RazorpayOrder(string Id, long Amount, string Currency, string Receipt);

public interface IRazorpayClient
{
    string KeyId { get; }
    Task<RazorpayOrder> CreateOrderAsync(long amountInPaise, string receipt, CancellationToken ct);
    bool VerifySignature(string orderId, string paymentId, string signature);
}

