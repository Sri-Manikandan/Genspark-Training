using BusBooking.Api.Models;

namespace BusBooking.Api.Infrastructure.Auth;

public interface IJwtTokenService
{
    AuthToken Generate(User user, IEnumerable<string> roles);
}
