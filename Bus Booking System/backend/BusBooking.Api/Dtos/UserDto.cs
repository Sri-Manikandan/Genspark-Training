namespace BusBooking.Api.Dtos;

public record UserDto(Guid Id, string Name, string Email, string? Phone, string[] Roles);
