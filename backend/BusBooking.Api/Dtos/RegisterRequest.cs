namespace BusBooking.Api.Dtos;

public record RegisterRequest(string Name, string Email, string Password, string? Phone);
