namespace BusBooking.Api.Dtos;

public record BookingListResponseDto(
    IReadOnlyList<BookingListItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount);
