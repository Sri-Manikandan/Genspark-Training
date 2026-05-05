using BusBooking.Api.Dtos;

namespace BusBooking.Api.Services;

public interface ITripService
{
    // Overload 1: search today's trips (no date required)
    Task<IReadOnlyList<SearchResultDto>> SearchAsync(Guid srcCityId, Guid dstCityId, CancellationToken ct);

    // Overload 2: search a specific date
    Task<IReadOnlyList<SearchResultDto>> SearchAsync(Guid srcCityId, Guid dstCityId, DateOnly date, CancellationToken ct);

    // Overload 3: search a specific date filtered by bus type
    Task<IReadOnlyList<SearchResultDto>> SearchAsync(Guid srcCityId, Guid dstCityId, DateOnly date, string busType, CancellationToken ct);

    Task<TripDetailDto> GetDetailAsync(Guid tripId, CancellationToken ct);
    Task<SeatLayoutDto> GetSeatLayoutAsync(Guid tripId, CancellationToken ct);
}
