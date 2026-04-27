namespace BusBooking.Api.Dtos;

public record UpdateBusScheduleRequest(
    TimeOnly? DepartureTime,
    TimeOnly? ArrivalTime,
    decimal? FarePerSeat,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    int? DaysOfWeek,
    bool? IsActive
);
