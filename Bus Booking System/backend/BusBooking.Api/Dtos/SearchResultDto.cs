namespace BusBooking.Api.Dtos;

public record SearchResultDto(
    Guid TripId,
    string BusName,
    string BusType,
    string OperatorName,
    TimeOnly DepartureTime,
    TimeOnly ArrivalTime,
    decimal FarePerSeat,
    int SeatsLeft,
    string PickupAddress,
    string DropAddress
);
