using System.Net;
using System.Net.Http.Json;
using BusBooking.Api.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BusBooking.Api.Tests.Integration;

public class AdminSeedTests : IClassFixture<IntegrationFixture>, IAsyncLifetime
{
    private readonly IntegrationFixture _fixture;
    private readonly HttpClient _client;

    public AdminSeedTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    // Reset BEFORE so the seeder has a clean slate when we trigger it, then rely on
    // the test-level ensure step below.
    public Task InitializeAsync() => _fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private record LoginRequest(string Email, string Password);
    private record UserDto(Guid Id, string Name, string Email, string? Phone, string[] Roles);
    private record LoginResponse(string Token, DateTime ExpiresAtUtc, UserDto User);

    [Fact]
    public async Task Admin_is_seeded_and_can_login_with_admin_role()
    {
        // Hitting any endpoint re-triggers lazy services; but seeder runs on startup.
        // Fixture's InitializeAsync truncated users. So issue a direct call to seeder
        // via Services to guarantee admin exists, then authenticate.
        using (var scope = _fixture.Services.CreateScope())
        {
            var seeder = scope.ServiceProvider.GetRequiredService<BusBooking.Api.Infrastructure.Seeding.IAdminSeeder>();
            await seeder.SeedAsync(CancellationToken.None);
        }

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("admin@busbooking.local", "ChangeMeOnFirstBoot!"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        body.Should().NotBeNull();
        body!.User.Email.Should().Be("admin@busbooking.local");
        body.User.Roles.Should().BeEquivalentTo(new[] { "admin" });
    }

    [Fact]
    public async Task Admin_seed_is_idempotent()
    {
        using var scope = _fixture.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<BusBooking.Api.Infrastructure.Seeding.IAdminSeeder>();

        await seeder.SeedAsync(CancellationToken.None);
        await seeder.SeedAsync(CancellationToken.None);
        await seeder.SeedAsync(CancellationToken.None);

        var db = scope.ServiceProvider.GetRequiredService<BusBooking.Api.Infrastructure.AppDbContext>();
        var admins = await db.Users.Where(u => u.Email == "admin@busbooking.local").ToListAsync();
        admins.Should().HaveCount(1);
    }
}
