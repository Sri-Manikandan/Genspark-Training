namespace BusBooking.Api.Models;

public static class Roles
{
    public const string Customer = "customer";
    public const string Operator = "operator";
    public const string Admin = "admin";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        Customer, Operator, Admin
    };
}
