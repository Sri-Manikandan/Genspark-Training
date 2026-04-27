namespace BusBooking.Api.Dtos;

public record AdminRevenueResponseDto(
    DateOnly DateFrom,
    DateOnly DateTo,
    int ConfirmedBookings,
    decimal Gmv,
    decimal PlatformFeeIncome,
    List<AdminRevenueOperatorItemDto> ByOperator);
