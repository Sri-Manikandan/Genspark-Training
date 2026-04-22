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
public class CitiesSearchTests : IAsyncLifetime
{
    private readonly IntegrationFixture _fx;

    public CitiesSearchTests(IntegrationFixture fx)
    {
        _fx = fx;
    }

    public async Task InitializeAsync()
    {
        await _fx.ResetAsync();
        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // Until IntegrationFixture.ResetAsync clears this table (Task 8), do it manually:
        await db.Database.ExecuteSqlRawAsync("DELETE FROM cities");

        db.Cities.AddRange(
            new City { Id = Guid.NewGuid(), Name = "Bangalore", State = "Karnataka", IsActive = true },
            new City { Id = Guid.NewGuid(), Name = "Bengaluru",  State = "Karnataka", IsActive = true },
            new City { Id = Guid.NewGuid(), Name = "Chennai",    State = "Tamil Nadu", IsActive = true },
            new City { Id = Guid.NewGuid(), Name = "Mumbai",     State = "Maharashtra", IsActive = false });
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Returns_empty_when_query_shorter_than_two_chars()
    {
        var resp = await _fx.Client.GetFromJsonAsync<List<CityDto>>("/api/v1/cities?q=b");
        resp.Should().NotBeNull();
        resp!.Should().BeEmpty();
    }

    [Fact]
    public async Task Fuzzy_matches_prefix_case_insensitively()
    {
        var resp = await _fx.Client.GetFromJsonAsync<List<CityDto>>("/api/v1/cities?q=ban");
        resp!.Should().ContainSingle(c => c.Name == "Bangalore");
    }

    [Fact]
    public async Task Excludes_inactive_cities()
    {
        var resp = await _fx.Client.GetFromJsonAsync<List<CityDto>>("/api/v1/cities?q=mum");
        resp!.Should().BeEmpty();
    }

    [Fact]
    public async Task Respects_limit_parameter()
    {
        var resp = await _fx.Client.GetAsync("/api/v1/cities?q=a&limit=1");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<List<CityDto>>();
        body!.Count.Should().BeLessOrEqualTo(1);
    }
}
