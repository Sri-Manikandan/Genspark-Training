using BusBooking.Api.Dtos;

namespace BusBooking.Api.Services;

public interface IRouteService
{
    Task<IReadOnlyList<RouteDto>> ListAllAsync(CancellationToken ct);
    Task<RouteDto> GetAsync(Guid id, CancellationToken ct);
    Task<RouteDto> CreateAsync(CreateRouteRequest request, CancellationToken ct);
    Task<RouteDto> UpdateAsync(Guid id, UpdateRouteRequest request, CancellationToken ct);
}
