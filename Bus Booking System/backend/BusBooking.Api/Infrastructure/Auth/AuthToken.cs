namespace BusBooking.Api.Infrastructure.Auth;

public record AuthToken(string Token, DateTime ExpiresAtUtc);
