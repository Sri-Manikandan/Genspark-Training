using BusBooking.Api.Dtos;

namespace BusBooking.Api.Services;

public interface IAdminBookingService
{
    Task<AdminBookingListResponseDto> ListAsync(
        Guid? operatorUserId,
        string? status,
        DateOnly? date,
        int page,
        int pageSize,
        CancellationToken ct);
}
