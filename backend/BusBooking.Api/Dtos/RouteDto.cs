namespace BusBooking.Api.Dtos;

public record RouteDto(
    Guid Id,
    CityDto Source,
    CityDto Destination,
    int? DistanceKm,
    bool IsActive);
