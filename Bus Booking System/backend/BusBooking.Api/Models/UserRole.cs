namespace BusBooking.Api.Models;

public class UserRole
{
    public Guid UserId { get; set; }
    public required string Role { get; set; }

    public User User { get; set; } = null!;
}
