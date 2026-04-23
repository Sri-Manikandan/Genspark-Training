namespace BusBooking.Api.Models;

public static class BookingStatus
{
    public const string PendingPayment = "pending_payment";
    public const string Confirmed = "confirmed";
    public const string Cancelled = "cancelled";
    public const string CancelledByOperator = "cancelled_by_operator";
    public const string Completed = "completed";

    public static readonly string[] All =
        [PendingPayment, Confirmed, Cancelled, CancelledByOperator, Completed];
}

