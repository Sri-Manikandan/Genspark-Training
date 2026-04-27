namespace BusBooking.Api.Dtos;

public record SeatLockResponseDto(
    Guid LockId,
    Guid SessionId,
    IReadOnlyList<string> Seats,
    DateTime ExpiresAt);

