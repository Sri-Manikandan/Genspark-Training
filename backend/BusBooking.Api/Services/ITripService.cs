using BusBooking.Api.Dtos;

namespace BusBooking.Api.Services;

public interface ITripService
{
    Task<IReadOnlyList<SearchResultDto>> SearchAsync(Guid srcCityId, Guid dstCityId, DateOnly date, CancellationToken ct);
    Task<TripDetailDto> GetDetailAsync(Guid tripId, CancellationToken ct);
    Task<SeatLayoutDto> GetSeatLayoutAsync(Guid tripId, CancellationToken ct);
}
