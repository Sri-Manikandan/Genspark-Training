using BusBooking.Api.Dtos;

namespace BusBooking.Api.Services;

public interface IScheduleService
{
    Task<IReadOnlyList<BusScheduleDto>> ListAsync(Guid operatorUserId, Guid? busId, CancellationToken ct);
    Task<BusScheduleDto> CreateAsync(Guid operatorUserId, CreateBusScheduleRequest req, CancellationToken ct);
    Task<BusScheduleDto> UpdateAsync(Guid operatorUserId, Guid scheduleId, UpdateBusScheduleRequest req, CancellationToken ct);
    Task DeleteAsync(Guid operatorUserId, Guid scheduleId, CancellationToken ct);
    Task<IReadOnlyList<RouteOptionDto>> ListActiveRoutesAsync(CancellationToken ct);
}
