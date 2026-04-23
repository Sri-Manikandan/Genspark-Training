using BusBooking.Api.Dtos;

namespace BusBooking.Api.Services;

public interface IAdminRevenueService
{
    Task<AdminRevenueResponseDto> GetAsync(DateOnly from, DateOnly to, CancellationToken ct);
}
