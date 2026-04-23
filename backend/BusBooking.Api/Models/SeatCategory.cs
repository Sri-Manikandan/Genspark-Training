namespace BusBooking.Api.Models;

public static class SeatCategory
{
    public const string Regular = "regular";

    public static readonly IReadOnlySet<string> All = new HashSet<string> { Regular };
}
