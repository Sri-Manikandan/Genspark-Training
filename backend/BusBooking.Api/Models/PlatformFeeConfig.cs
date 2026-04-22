namespace BusBooking.Api.Models;

public class PlatformFeeConfig
{
    public Guid Id { get; set; }
    public required string FeeType { get; set; } // PlatformFeeType.Fixed | PlatformFeeType.Percent
    public decimal Value { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public Guid CreatedByAdminId { get; set; }
}
