using BusBooking.Api.Dtos;

namespace BusBooking.Api.Services;

public interface ICityService
{
    Task<IReadOnlyList<CityDto>> SearchActiveAsync(string query, int limit, CancellationToken ct);
    Task<IReadOnlyList<CityDto>> ListAllAsync(CancellationToken ct);
    Task<CityDto> GetAsync(Guid id, CancellationToken ct);
    Task<CityDto> CreateAsync(CreateCityRequest request, CancellationToken ct);
    Task<CityDto> UpdateAsync(Guid id, UpdateCityRequest request, CancellationToken ct);
}
