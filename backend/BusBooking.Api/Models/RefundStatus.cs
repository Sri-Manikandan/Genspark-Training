namespace BusBooking.Api.Models;

public static class RefundStatus
{
    public const string None      = "none";       // never set in DB; used in DTOs when refund_status is null
    public const string Pending   = "pending";    // committed, not yet acknowledged by Razorpay
    public const string Processed = "processed";  // Razorpay accepted the refund
    public const string Failed    = "failed";     // post-commit Razorpay call failed; manual recovery
}
