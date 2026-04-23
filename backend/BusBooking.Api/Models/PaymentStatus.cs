namespace BusBooking.Api.Models;

public static class PaymentStatus
{
    public const string Created = "created";
    public const string Captured = "captured";
    public const string Failed = "failed";
    public const string Refunded = "refunded";

    public static readonly string[] All = [Created, Captured, Failed, Refunded];
}

