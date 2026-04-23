namespace BusBooking.Api.Dtos;

public record CreateBusScheduleRequest(
    Guid BusId,
    Guid RouteId,
    TimeOnly DepartureTime,
    TimeOnly ArrivalTime,
    decimal FarePerSeat,
    DateOnly ValidFrom,
    DateOnly ValidTo,
    int DaysOfWeek
);
