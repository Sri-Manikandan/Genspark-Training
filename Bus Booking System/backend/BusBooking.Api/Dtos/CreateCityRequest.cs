namespace BusBooking.Api.Dtos;

public class CreateCityRequest
{
    public required string Name { get; set; }
    public required string State { get; set; }
}
