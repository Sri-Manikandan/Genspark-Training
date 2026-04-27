using System.Net.Http.Headers;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Infrastructure.Auth;
using BusBooking.Api.Models;
using Microsoft.Extensions.DependencyInjection;

namespace BusBooking.Api.Tests.Support;

public static class AdminTokenFactory
{
    public static async Task<(User user, string token)> CreateAdminAsync(
        IntegrationFixture fx,
        string email = "admin-test@busbooking.local",
        string name = "Admin Test",
        CancellationToken ct = default)
    {
        using var scope = fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var tokens = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        var admin = new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            PasswordHash = hasher.Hash("x-not-used"),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        admin.Roles.Add(new UserRole { UserId = admin.Id, Role = Roles.Admin });
        db.Users.Add(admin);
        await db.SaveChangesAsync(ct);

        var token = tokens.Generate(admin, [Roles.Admin]);
        return (admin, token.Token);
    }

    public static void AttachAdminBearer(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
