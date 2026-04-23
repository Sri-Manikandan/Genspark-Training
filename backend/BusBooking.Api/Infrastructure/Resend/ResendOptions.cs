namespace BusBooking.Api.Infrastructure.Resend;

public class ResendOptions
{
    public const string SectionName = "Resend";
    public string ApiKey { get; set; } = "";
    public string FromAddress { get; set; } = "onboarding@resend.dev";
    public string FromName { get; set; } = "Bus Booking";
    public string BaseUrl { get; set; } = "https://api.resend.com";
}

