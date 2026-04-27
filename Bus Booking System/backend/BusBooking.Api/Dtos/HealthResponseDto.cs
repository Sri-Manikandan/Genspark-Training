namespace BusBooking.Api.Dtos;

public record HealthResponseDto(string Status, string Service, string Version, DateTime TimestampUtc);
