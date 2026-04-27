namespace BusBooking.Api.Dtos;

public record AdminBookingListResponseDto(
    List<AdminBookingListItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount);
