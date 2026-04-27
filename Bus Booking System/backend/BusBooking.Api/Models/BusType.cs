namespace BusBooking.Api.Models;

public static class BusType
{
    public const string Seater = "seater";
    public const string Sleeper = "sleeper";
    public const string SemiSleeper = "semi_sleeper";

    public static readonly IReadOnlySet<string> All = new HashSet<string> { Seater, Sleeper, SemiSleeper };
}
