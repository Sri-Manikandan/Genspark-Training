namespace BusBooking.Api.Dtos;

public record OperatorBookingListResponseDto(
    List<OperatorBookingListItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount);
