namespace BusBooking.Api.Dtos;

public class CreateBusRequest
{
    public required string RegistrationNumber { get; set; }
    public required string BusName { get; set; }
    public required string BusType { get; set; }
    public int Rows { get; set; }
    public int Columns { get; set; }
}
