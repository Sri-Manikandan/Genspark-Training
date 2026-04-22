namespace BusBooking.Api.Models;

public static class PlatformFeeType
{
    public const string Fixed = "fixed";
    public const string Percent = "percent";

    public static readonly string[] All = [Fixed, Percent];
}
