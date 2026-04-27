using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BusBooking.Api.Infrastructure.Errors;
using Microsoft.AspNetCore.Http;

namespace BusBooking.Api.Infrastructure.Auth;

public interface ICurrentUserAccessor
{
    Guid UserId { get; }
    bool TryGetUserId(out Guid userId);
}

public class CurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _http;

    public CurrentUserAccessor(IHttpContextAccessor http)
    {
        _http = http;
    }

    public Guid UserId
    {
        get
        {
            if (!TryGetUserId(out var id))
                throw new UnauthorizedException("UNAUTHORIZED", "Missing or invalid subject claim");
            return id;
        }
    }

    public bool TryGetUserId(out Guid userId)
    {
        userId = Guid.Empty;
        var user = _http.HttpContext?.User;
        if (user is null) return false;
        var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out userId);
    }
}
