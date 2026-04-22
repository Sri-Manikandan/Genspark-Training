namespace BusBooking.Api.Dtos;

public class UpdatePlatformFeeRequest
{
    public required string FeeType { get; set; } // "fixed" or "percent"
    public decimal Value { get; set; }
}
