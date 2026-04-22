namespace BusBooking.Api.Dtos;

public class UpdateCityRequest
{
    public string? Name { get; set; }
    public string? State { get; set; }
    public bool? IsActive { get; set; }
}
