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

public class OperatorOfficesTests : IClassFixture<IntegrationFixture>, IAsyncLifetime
{
    private readonly IntegrationFixture _fx;

    public OperatorOfficesTests(IntegrationFixture fx) => _fx = fx;

    public async Task InitializeAsync() => await _fx.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<Guid> SeedCityAsync(string name = "Coimbatore")
    {
        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var c = new City { Id = Guid.NewGuid(), Name = name, State = "Tamil Nadu", IsActive = true };
        db.Cities.Add(c);
        await db.SaveChangesAsync();
        return c.Id;
    }

    [Fact]
    public async Task Operator_can_create_list_delete_office()
    {
        var cityId = await SeedCityAsync();
        var (_, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Operator]);
        var client = _fx.CreateClient();
        client.AttachBearer(token);

        var create = await client.PostAsJsonAsync("/api/v1/operator/offices",
            new CreateOperatorOfficeRequest
            {
                CityId = cityId,
                AddressLine = "12 MG Road",
                Phone = "+91-98000-11111"
            });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await create.Content.ReadFromJsonAsync<OperatorOfficeDto>();
        dto!.CityId.Should().Be(cityId);
        dto.IsActive.Should().BeTrue();

        var list = await client.GetAsync("/api/v1/operator/offices");
        (await list.Content.ReadFromJsonAsync<List<OperatorOfficeDto>>())!.Should().HaveCount(1);

        var del = await client.DeleteAsync($"/api/v1/operator/offices/{dto.Id}");
        del.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var list2 = await client.GetAsync("/api/v1/operator/offices");
        (await list2.Content.ReadFromJsonAsync<List<OperatorOfficeDto>>())!.Should().BeEmpty();
    }

    [Fact]
    public async Task Duplicate_city_for_same_operator_returns_409()
    {
        var cityId = await SeedCityAsync();
        var (_, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Operator]);
        var client = _fx.CreateClient();
        client.AttachBearer(token);

        var body = new CreateOperatorOfficeRequest
        {
            CityId = cityId,
            AddressLine = "12 MG Road",
            Phone = "+91-98000-11111"
        };
        (await client.PostAsJsonAsync("/api/v1/operator/offices", body)).EnsureSuccessStatusCode();

        var dup = await client.PostAsJsonAsync("/api/v1/operator/offices", body);
        dup.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await dup.Content.ReadAsStringAsync()).Should().Contain("OFFICE_ALREADY_EXISTS");
    }

    [Fact]
    public async Task Unknown_city_returns_404()
    {
        var (_, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Operator]);
        var client = _fx.CreateClient();
        client.AttachBearer(token);

        var resp = await client.PostAsJsonAsync("/api/v1/operator/offices",
            new CreateOperatorOfficeRequest
            {
                CityId = Guid.NewGuid(),
                AddressLine = "Nowhere",
                Phone = "+91-0000000000"
            });
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Customer_only_token_is_403()
    {
        var cityId = await SeedCityAsync();
        var (_, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        var client = _fx.CreateClient();
        client.AttachBearer(token);

        var resp = await client.PostAsJsonAsync("/api/v1/operator/offices",
            new CreateOperatorOfficeRequest
            {
                CityId = cityId,
                AddressLine = "12 MG Road",
                Phone = "+91-98000-11111"
            });
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Deleting_another_operators_office_is_403()
    {
        var cityId = await SeedCityAsync();
        var (_, tokenA) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Operator], email: "a@t.local");
        var (_, tokenB) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Operator], email: "b@t.local");

        var clientA = _fx.CreateClient(); clientA.AttachBearer(tokenA);
        var create = await clientA.PostAsJsonAsync("/api/v1/operator/offices",
            new CreateOperatorOfficeRequest
            {
                CityId = cityId,
                AddressLine = "12 MG Road",
                Phone = "+91-98000-11111"
            });
        create.EnsureSuccessStatusCode();
        var dto = await create.Content.ReadFromJsonAsync<OperatorOfficeDto>();

        var clientB = _fx.CreateClient(); clientB.AttachBearer(tokenB);
        var resp = await clientB.DeleteAsync($"/api/v1/operator/offices/{dto!.Id}");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
