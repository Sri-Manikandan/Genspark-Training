using System.IdentityModel.Tokens.Jwt;
using System.Text;
using BusBooking.Api.Infrastructure.Auth;
using BusBooking.Api.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BusBooking.Api.Tests.Unit;

public class JwtTokenServiceTests
{
    private static IJwtTokenService CreateService()
    {
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-aud",
                ["Jwt:SigningKey"] = "test-only-32-byte-signing-key-xxxxxxxxx",
                ["Jwt:ExpiryMinutes"] = "60"
            })
            .Build();
        return new JwtTokenService(cfg);
    }

    [Fact]
    public void Generate_embeds_expected_claims_and_expiry()
    {
        var svc = CreateService();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = "test@example.com",
            PasswordHash = "ignored"
        };
        var roles = new[] { Roles.Customer };

        var result = svc.Generate(user, roles);

        result.Token.Should().NotBeNullOrWhiteSpace();
        result.ExpiresAtUtc.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(60), TimeSpan.FromMinutes(1));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        jwt.Issuer.Should().Be("test-issuer");
        jwt.Audiences.Should().Contain("test-aud");
        jwt.Claims.Should().ContainSingle(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
        jwt.Claims.Should().ContainSingle(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
        jwt.Claims.Should().ContainSingle(c => c.Type == "name" && c.Value == user.Name);
        jwt.Claims.Should().Contain(c => c.Type == "role" && c.Value == Roles.Customer);
    }

    [Fact]
    public void Generate_includes_every_role()
    {
        var svc = CreateService();
        var user = new User { Id = Guid.NewGuid(), Name = "n", Email = "e@e.com", PasswordHash = "x" };

        var result = svc.Generate(user, new[] { Roles.Customer, Roles.Operator });

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        jwt.Claims.Where(c => c.Type == "role")
            .Select(c => c.Value)
            .Should()
            .BeEquivalentTo(new[] { Roles.Customer, Roles.Operator });
    }
}
