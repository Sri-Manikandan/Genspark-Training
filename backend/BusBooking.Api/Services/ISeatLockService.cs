using BusBooking.Api.Dtos;
using BusBooking.Api.Models;

namespace BusBooking.Api.Services;

public interface ISeatLockService
{
    Task<SeatLockResponseDto> LockAsync(Guid tripId, Guid? userId, LockSeatsRequest req, CancellationToken ct);
    Task ReleaseAsync(Guid lockId, Guid sessionId, Guid? userId, CancellationToken ct);
    Task<IReadOnlyList<SeatLock>> GetActiveLocksAsync(Guid lockId, CancellationToken ct);
}

