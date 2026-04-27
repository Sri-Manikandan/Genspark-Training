namespace BusBooking.Api.Models;

public class User
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? OperatorDisabledAt { get; set; }

    public ICollection<UserRole> Roles { get; set; } = new List<UserRole>();
}
