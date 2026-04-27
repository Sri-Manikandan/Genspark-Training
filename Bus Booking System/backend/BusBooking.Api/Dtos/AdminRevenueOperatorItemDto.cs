namespace BusBooking.Api.Dtos;

public record AdminRevenueOperatorItemDto(
    Guid OperatorUserId,
    string OperatorName,
    int ConfirmedBookings,
    decimal Gmv,
    decimal PlatformFeeIncome);
