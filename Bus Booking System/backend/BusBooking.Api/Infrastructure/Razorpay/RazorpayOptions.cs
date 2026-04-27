namespace BusBooking.Api.Infrastructure.Razorpay;

public class RazorpayOptions
{
    public const string SectionName = "Razorpay";
    public string KeyId { get; set; } = "";
    public string KeySecret { get; set; } = "";
    public string BaseUrl { get; set; } = "https://api.razorpay.com";
}

