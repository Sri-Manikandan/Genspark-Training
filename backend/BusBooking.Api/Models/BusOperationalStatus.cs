namespace BusBooking.Api.Models;

public static class BusOperationalStatus
{
    public const string Active = "active";
    public const string UnderMaintenance = "under_maintenance";
    public const string Retired = "retired";

    public static readonly IReadOnlySet<string> All = new HashSet<string> { Active, UnderMaintenance, Retired };
}
