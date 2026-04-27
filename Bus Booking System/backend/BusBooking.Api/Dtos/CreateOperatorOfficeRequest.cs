namespace BusBooking.Api.Dtos;

public class CreateOperatorOfficeRequest
{
    public required Guid CityId { get; set; }
    public required string AddressLine { get; set; }
    public required string Phone { get; set; }
}
