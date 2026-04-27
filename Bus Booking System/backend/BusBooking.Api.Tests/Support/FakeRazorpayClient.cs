using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using BusBooking.Api.Infrastructure.Razorpay;

namespace BusBooking.Api.Tests.Support;

public class FakeRazorpayClient : IRazorpayClient
{
    public const string TestKeyId = "rzp_test_fake";
    public const string TestKeySecret = "fake_secret_123";

    public readonly ConcurrentDictionary<string, long> CreatedOrders = new();
    public readonly ConcurrentBag<RazorpayRefund> CreatedRefunds = new();
    public bool ThrowOnRefund { get; set; }

    public string KeyId => TestKeyId;

    public Task<RazorpayOrder> CreateOrderAsync(long amountInPaise, string receipt, CancellationToken ct)
    {
        var id = "order_" + Guid.NewGuid().ToString("N")[..14];
        CreatedOrders[id] = amountInPaise;
        return Task.FromResult(new RazorpayOrder(id, amountInPaise, "INR", receipt));
    }

    public bool VerifySignature(string orderId, string paymentId, string signature)
    {
        var expected = BuildSignature(orderId, paymentId);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature.ToLowerInvariant()));
    }

    public Task<RazorpayRefund> CreateRefundAsync(string paymentId, long amountInPaise, CancellationToken ct)
    {
        if (ThrowOnRefund)
            throw new InvalidOperationException("Razorpay refund unavailable (test fault injection)");

        var refund = new RazorpayRefund(
            Id: "rfnd_" + Guid.NewGuid().ToString("N")[..14],
            PaymentId: paymentId,
            Amount: amountInPaise,
            Status: "processed");
        CreatedRefunds.Add(refund);
        return Task.FromResult(refund);
    }

    public static string BuildSignature(string orderId, string paymentId)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(TestKeySecret));
        var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{orderId}|{paymentId}"));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
