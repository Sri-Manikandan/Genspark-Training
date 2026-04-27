namespace BusBooking.Api.Dtos;

public record OperatorRevenueResponseDto(
    DateOnly DateFrom,
    DateOnly DateTo,
    decimal GrandTotalFare,
    List<OperatorRevenueItemDto> ByBus);
