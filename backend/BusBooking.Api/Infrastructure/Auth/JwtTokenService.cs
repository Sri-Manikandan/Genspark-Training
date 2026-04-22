using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BusBooking.Api.Models;
using Microsoft.IdentityModel.Tokens;

namespace BusBooking.Api.Infrastructure.Auth;

public class JwtTokenService : IJwtTokenService
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly SymmetricSecurityKey _signingKey;
    private readonly int _expiryMinutes;

    public JwtTokenService(IConfiguration configuration)
    {
        var section = configuration.GetSection("Jwt");
        _issuer = section["Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer missing");
        _audience = section["Audience"] ?? throw new InvalidOperationException("Jwt:Audience missing");
        var key = section["SigningKey"] ?? throw new InvalidOperationException("Jwt:SigningKey missing");
        if (Encoding.UTF8.GetByteCount(key) < 32)
            throw new InvalidOperationException("Jwt:SigningKey must be at least 32 bytes");
        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        _expiryMinutes = section.GetValue<int?>("ExpiryMinutes") ?? 60;
    }

    public AuthToken Generate(User user, IEnumerable<string> roles)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(_expiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("name", user.Name),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        foreach (var role in roles)
            claims.Add(new Claim("role", role));

        var creds = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: creds);

        return new AuthToken(new JwtSecurityTokenHandler().WriteToken(jwt), expiresAt);
    }
}
