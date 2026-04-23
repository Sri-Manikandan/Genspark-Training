namespace BusBooking.Api.Dtos;

public record TripDetailDto(
    Guid TripId,
    Guid BusId,
    string BusName,
    string BusType,
    string OperatorName,
    DateOnly TripDate,
    TimeOnly DepartureTime,
    TimeOnly ArrivalTime,
    decimal FarePerSeat,
    int SeatsLeft,
    string SourceCityName,
    string DestinationCityName,
    string? PickupAddress,
    string? DropAddress,
    SeatLayoutDto SeatLayout
);
