namespace BusBooking.Api.Dtos;

public record AdminOperatorListItemDto(
    Guid UserId,
    string Name,
    string Email,
    DateTime CreatedAt,
    bool IsDisabled,
    DateTime? DisabledAt,
    int TotalBuses,
    int ActiveBuses,
    int RetiredBuses);
