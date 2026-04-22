using System.Net;
using System.Net.Http.Json;
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Tests.Support;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BusBooking.Api.Tests.Integration;

[Collection("Integration")]
public class AdminCitiesTests : IAsyncLifetime
{
    private readonly IntegrationFixture _fx;

    public AdminCitiesTests(IntegrationFixture fx)
    {
        _fx = fx;
    }

    public async Task InitializeAsync()
    {
        await _fx.ResetAsync();
        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlRawAsync("DELETE FROM cities");
    }

    public Task DisposeAsync()
    {
        _fx.Client.DefaultRequestHeaders.Authorization = null;
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Anonymous_list_returns_401()
    {
        var resp = await _fx.Client.GetAsync("/api/v1/admin/cities");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Admin_can_create_update_and_deactivate()
    {
        var (_, token) = await AdminTokenFactory.CreateAdminAsync(_fx);
        _fx.Client.AttachAdminBearer(token);

        var created = await _fx.Client.PostAsJsonAsync("/api/v1/admin/cities",
            new { name = "Hyderabad", state = "Telangana" });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdBody = await created.Content.ReadFromJsonAsync<CityDto>();
        createdBody!.Name.Should().Be("Hyderabad");
        createdBody.IsActive.Should().BeTrue();

        var patched = await _fx.Client.PatchAsJsonAsync(
            $"/api/v1/admin/cities/{createdBody.Id}",
            new { isActive = false });
        patched.StatusCode.Should().Be(HttpStatusCode.OK);
        var patchedBody = await patched.Content.ReadFromJsonAsync<CityDto>();
        patchedBody!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Duplicate_name_returns_409()
    {
        var (_, token) = await AdminTokenFactory.CreateAdminAsync(_fx);
        _fx.Client.AttachAdminBearer(token);

        (await _fx.Client.PostAsJsonAsync("/api/v1/admin/cities",
            new { name = "Pune", state = "Maharashtra" })).EnsureSuccessStatusCode();

        var dup = await _fx.Client.PostAsJsonAsync("/api/v1/admin/cities",
            new { name = "pune", state = "Maharashtra" });
        dup.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Customer_role_receives_403()
    {
        // Create a customer with the existing /auth/register flow
        var uniqueEmail = $"alice-{Guid.NewGuid():N}@example.com";
        var registered = await _fx.Client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            name = "Alice",
            email = uniqueEmail,
            password = "Abcdef1!",
            phone = (string?)null
        });
        registered.EnsureSuccessStatusCode();

        var login = await _fx.Client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = uniqueEmail,
            password = "Abcdef1!"
        });
        login.EnsureSuccessStatusCode();
        var loginBody = await login.Content.ReadFromJsonAsync<LoginResponse>();

        _fx.Client.AttachAdminBearer(loginBody!.Token);
        var forbidden = await _fx.Client.GetAsync("/api/v1/admin/cities");
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
