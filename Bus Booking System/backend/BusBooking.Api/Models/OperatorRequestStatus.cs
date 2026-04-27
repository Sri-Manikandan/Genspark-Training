namespace BusBooking.Api.Models;

public static class OperatorRequestStatus
{
    public const string Pending = "pending";
    public const string Approved = "approved";
    public const string Rejected = "rejected";

    public static readonly IReadOnlySet<string> All = new HashSet<string> { Pending, Approved, Rejected };
}
