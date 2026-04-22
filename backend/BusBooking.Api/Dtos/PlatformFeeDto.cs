namespace BusBooking.Api.Dtos;

public record PlatformFeeDto(string FeeType, decimal Value, DateTime EffectiveFrom);
