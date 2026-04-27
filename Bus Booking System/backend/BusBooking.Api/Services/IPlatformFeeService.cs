using BusBooking.Api.Dtos;

namespace BusBooking.Api.Services;

public interface IPlatformFeeService
{
    Task<PlatformFeeDto> GetActiveAsync(CancellationToken ct);
    Task<PlatformFeeDto> UpdateAsync(Guid adminUserId, UpdatePlatformFeeRequest request, CancellationToken ct);
}
