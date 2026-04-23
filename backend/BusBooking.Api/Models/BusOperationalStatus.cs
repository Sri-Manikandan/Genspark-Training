namespace BusBooking.Api.Models;

public static class BusOperationalStatus
{
    public const string Active = "active";
    public const string UnderMaintenance = "under_maintenance";
    public const string Retired = "retired";

    public static readonly string[] All = [Active, UnderMaintenance, Retired];
}
