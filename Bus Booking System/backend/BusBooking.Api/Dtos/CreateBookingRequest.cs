namespace BusBooking.Api.Dtos;

public record CreateBookingRequest(
    Guid TripId,
    Guid LockId,
    Guid SessionId,
    List<PassengerDto> Passengers);

