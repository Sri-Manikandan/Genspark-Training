using BusBooking.Api.Dtos;

namespace BusBooking.Api.Services;

public interface IAuthService
{
    Task<UserDto> RegisterAsync(RegisterRequest request, CancellationToken ct);
}
