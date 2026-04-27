namespace BusBooking.Api.Dtos;

public record BusScheduleDto(
    Guid Id,
    Guid BusId,
    string BusName,
    Guid RouteId,
    string SourceCityName,
    string DestinationCityName,
    TimeOnly DepartureTime,
    TimeOnly ArrivalTime,
    decimal FarePerSeat,
    DateOnly ValidFrom,
    DateOnly ValidTo,
    int DaysOfWeek,
    bool IsActive
);
