using System.Net;
using System.Net.Http.Json;
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Models;
using BusBooking.Api.Tests.Support;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BusBooking.Api.Tests.Integration;

[Collection("Integration")]
public class AdminRoutesTests : IAsyncLifetime
{
    private readonly IntegrationFixture _fx;
    private Guid _bangalore, _chennai;

    public AdminRoutesTests(IntegrationFixture fx)
    {
        _fx = fx;
    }

    public async Task InitializeAsync()
    {
        await _fx.ResetAsync();
        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlRawAsync("DELETE FROM routes");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM cities");

        var b = new City { Id = Guid.NewGuid(), Name = "Bangalore", State = "Karnataka" };
        var c = new City { Id = Guid.NewGuid(), Name = "Chennai", State = "Tamil Nadu" };
        db.Cities.AddRange(b, c);
        await db.SaveChangesAsync();
        _bangalore = b.Id;
        _chennai = c.Id;
    }

    public Task DisposeAsync()
    {
        _fx.Client.DefaultRequestHeaders.Authorization = null;
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Admin_can_create_and_deactivate_route()
    {
        var (_, token) = await AdminTokenFactory.CreateAdminAsync(_fx);
        _fx.Client.AttachAdminBearer(token);

        var created = await _fx.Client.PostAsJsonAsync("/api/v1/admin/routes", new
        {
            sourceCityId = _bangalore,
            destinationCityId = _chennai,
            distanceKm = 350
        });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await created.Content.ReadFromJsonAsync<RouteDto>();
        dto!.Source.Name.Should().Be("Bangalore");
        dto.Destination.Name.Should().Be("Chennai");
        dto.DistanceKm.Should().Be(350);

        var patched = await _fx.Client.PatchAsJsonAsync(
            $"/api/v1/admin/routes/{dto.Id}", new { isActive = false });
        patched.StatusCode.Should().Be(HttpStatusCode.OK);
        (await patched.Content.ReadFromJsonAsync<RouteDto>())!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Duplicate_route_returns_409()
    {
        var (_, token) = await AdminTokenFactory.CreateAdminAsync(_fx);
        _fx.Client.AttachAdminBearer(token);

        var body = new { sourceCityId = _bangalore, destinationCityId = _chennai };
        (await _fx.Client.PostAsJsonAsync("/api/v1/admin/routes", body)).EnsureSuccessStatusCode();
        var dup = await _fx.Client.PostAsJsonAsync("/api/v1/admin/routes", body);
        dup.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Unknown_city_returns_404()
    {
        var (_, token) = await AdminTokenFactory.CreateAdminAsync(_fx);
        _fx.Client.AttachAdminBearer(token);

        var resp = await _fx.Client.PostAsJsonAsync("/api/v1/admin/routes", new
        {
            sourceCityId = Guid.NewGuid(),
            destinationCityId = _chennai
        });
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Source_equals_destination_returns_400()
    {
        var (_, token) = await AdminTokenFactory.CreateAdminAsync(_fx);
        _fx.Client.AttachAdminBearer(token);

        var resp = await _fx.Client.PostAsJsonAsync("/api/v1/admin/routes", new
        {
            sourceCityId = _bangalore,
            destinationCityId = _bangalore
        });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Anonymous_returns_401()
    {
        var resp = await _fx.Client.GetAsync("/api/v1/admin/routes");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
