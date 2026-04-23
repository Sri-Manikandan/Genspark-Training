using BusBooking.Api.Dtos;

namespace BusBooking.Api.Services;

public interface IOperatorBookingService
{
    Task<OperatorBookingListResponseDto> ListBookingsAsync(
        Guid operatorUserId,
        Guid? busId,
        DateOnly? date,
        int page,
        int pageSize,
        CancellationToken ct);

    Task<OperatorRevenueResponseDto> GetRevenueAsync(
        Guid operatorUserId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct);
}
