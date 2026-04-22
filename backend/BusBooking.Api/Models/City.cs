namespace BusBooking.Api.Models;

public class City
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string State { get; set; }
    public bool IsActive { get; set; } = true;
}
