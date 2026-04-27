namespace BusBooking.Api.Dtos;

public record LoginResponse(string Token, DateTime ExpiresAtUtc, UserDto User);
