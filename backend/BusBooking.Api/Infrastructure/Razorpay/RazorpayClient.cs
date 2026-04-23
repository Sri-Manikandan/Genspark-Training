using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace BusBooking.Api.Infrastructure.Razorpay;

public class RazorpayClient : IRazorpayClient
{
    public const string HttpClientName = "razorpay";
    private readonly HttpClient _http;
    private readonly RazorpayOptions _options;
    private readonly ILogger<RazorpayClient> _log;

    public RazorpayClient(
        IHttpClientFactory httpFactory,
        IOptions<RazorpayOptions> options,
        ILogger<RazorpayClient> log)
    {
        _options = options.Value;
        _log = log;
        _http = httpFactory.CreateClient(HttpClientName);
        _http.BaseAddress = new Uri(_options.BaseUrl);
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.KeyId}:{_options.KeySecret}"));
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basic);
    }

    public string KeyId => _options.KeyId;

    public async Task<RazorpayOrder> CreateOrderAsync(long amountInPaise, string receipt, CancellationToken ct)
    {
        var body = new { amount = amountInPaise, currency = "INR", receipt, payment_capture = 1 };
        var resp = await _http.PostAsJsonAsync("/v1/orders", body, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var text = await resp.Content.ReadAsStringAsync(ct);
            _log.LogError("Razorpay order creation failed {Status} {Body}", resp.StatusCode, text);
            throw new InvalidOperationException($"Razorpay order creation failed: {resp.StatusCode}");
        }

        var dto = await resp.Content.ReadFromJsonAsync<OrderResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Razorpay response was empty");
        return new RazorpayOrder(dto.id, dto.amount, dto.currency, dto.receipt ?? receipt);
    }

    public bool VerifySignature(string orderId, string paymentId, string signature)
    {
        var payload = $"{orderId}|{paymentId}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.KeySecret));
        var computed = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(signature.ToLowerInvariant()));
    }

    private record OrderResponse(string id, long amount, string currency, string? receipt);
}

