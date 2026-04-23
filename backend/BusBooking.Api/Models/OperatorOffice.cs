namespace BusBooking.Api.Models;

public class OperatorOffice
{
    public Guid Id { get; set; }
    public Guid OperatorUserId { get; set; }
    public Guid CityId { get; set; }
    public required string AddressLine { get; set; }
    public required string Phone { get; set; }
    public bool IsActive { get; set; } = true;

    public User? Operator { get; set; }
    public City? City { get; set; }
}
