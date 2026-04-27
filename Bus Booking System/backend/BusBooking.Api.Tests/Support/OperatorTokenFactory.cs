using System.Net.Http.Headers;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Infrastructure.Auth;
using BusBooking.Api.Models;
using Microsoft.Extensions.DependencyInjection;

namespace BusBooking.Api.Tests.Support;

public static class OperatorTokenFactory
{
    public static async Task<(User user, string token)> CreateAsync(
        IntegrationFixture fx,
        string[] roles,
        string? email = null,
        string? name = null,
        CancellationToken ct = default)
    {
        using var scope = fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var tokens = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        var id = Guid.NewGuid();
        var user = new User
        {
            Id = id,
            Name = name ?? $"User-{id:N}".Substring(0, 12),
            Email = email ?? $"u-{id:N}@busbooking.local".Substring(0, 40),
            PasswordHash = hasher.Hash("x-not-used"),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        foreach (var r in roles)
            user.Roles.Add(new UserRole { UserId = id, Role = r });

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        var token = tokens.Generate(user, roles);
        return (user, token.Token);
    }

    public static void AttachBearer(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
