namespace BusBooking.Api.Dtos;

public record RouteOptionDto(
    Guid Id,
    string SourceCityName,
    string DestinationCityName,
    decimal? DistanceKm
);
