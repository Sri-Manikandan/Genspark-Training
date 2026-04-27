namespace BusBooking.Api.Dtos;

public record LockSeatsRequest(Guid SessionId, List<string> Seats);

