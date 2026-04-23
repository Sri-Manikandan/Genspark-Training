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
public class OperatorSchedulesTests : IAsyncLifetime
{
    private readonly IntegrationFixture _fx;
    public OperatorSchedulesTests(IntegrationFixture fx) => _fx = fx;
    public async Task InitializeAsync() => await _fx.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // Seeds: operator user + approved bus + two cities + route + offices at both cities
    private async Task<(User op, string token, Bus bus, Models.Route route)> SeedOperatorWithApprovedBus()
    {
        var (op, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Operator]);

        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var srcCity = new City { Id = Guid.NewGuid(), Name = "Chennai",   State = "TN", IsActive = true };
        var dstCity = new City { Id = Guid.NewGuid(), Name = "Bangalore", State = "KA", IsActive = true };
        db.Cities.AddRange(srcCity, dstCity);

        var route = new Models.Route
        {
            Id = Guid.NewGuid(), SourceCityId = srcCity.Id, DestinationCityId = dstCity.Id, IsActive = true
        };
        db.Routes.Add(route);

        db.OperatorOffices.AddRange(
            new OperatorOffice { Id = Guid.NewGuid(), OperatorUserId = op.Id, CityId = srcCity.Id, AddressLine = "1 Main St", Phone = "9999999999", IsActive = true },
            new OperatorOffice { Id = Guid.NewGuid(), OperatorUserId = op.Id, CityId = dstCity.Id, AddressLine = "2 MG Road",  Phone = "8888888888", IsActive = true }
        );

        var bus = new Bus
        {
            Id = Guid.NewGuid(), OperatorUserId = op.Id,
            RegistrationNumber = "TN-01-AA-0001", BusName = "Express One",
            BusType = BusType.Seater, Capacity = 40,
            ApprovalStatus = BusApprovalStatus.Approved,
            OperationalStatus = BusOperationalStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        db.Buses.Add(bus);
        await db.SaveChangesAsync();

        return (op, token, bus, route);
    }

    private static CreateBusScheduleRequest SampleSchedule(Guid busId, Guid routeId) => new(
        busId, routeId,
        new TimeOnly(8, 0), new TimeOnly(14, 0),
        350m,
        DateOnly.FromDateTime(DateTime.UtcNow),
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
        127   // all days
    );

    [Fact]
    public async Task Create_valid_schedule_returns_201()
    {
        var (_, token, bus, route) = await SeedOperatorWithApprovedBus();
        var client = _fx.CreateClient(); client.AttachBearer(token);

        var resp = await client.PostAsJsonAsync("/api/v1/operator/schedules", SampleSchedule(bus.Id, route.Id));
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await resp.Content.ReadFromJsonAsync<BusScheduleDto>();
        dto!.BusId.Should().Be(bus.Id);
        dto.DaysOfWeek.Should().Be(127);
        dto.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Create_with_unapproved_bus_returns_422()
    {
        var (op, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Operator], email: "unapproved@t.local");
        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var srcCity = new City { Id = Guid.NewGuid(), Name = "CityA", State = "XX", IsActive = true };
        var dstCity = new City { Id = Guid.NewGuid(), Name = "CityB", State = "XX", IsActive = true };
        db.Cities.AddRange(srcCity, dstCity);
        var route = new Models.Route { Id = Guid.NewGuid(), SourceCityId = srcCity.Id, DestinationCityId = dstCity.Id, IsActive = true };
        db.Routes.Add(route);
        var bus = new Bus
        {
            Id = Guid.NewGuid(), OperatorUserId = op.Id,
            RegistrationNumber = "TN-PENDING", BusName = "Pending Bus",
            BusType = BusType.Seater, Capacity = 20,
            ApprovalStatus = BusApprovalStatus.Pending,
            OperationalStatus = BusOperationalStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        db.Buses.Add(bus);
        await db.SaveChangesAsync();

        var client = _fx.CreateClient(); client.AttachBearer(token);
        var resp = await client.PostAsJsonAsync("/api/v1/operator/schedules", SampleSchedule(bus.Id, route.Id));
        resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await resp.Content.ReadAsStringAsync()).Should().Contain("BUS_NOT_APPROVED");
    }

    [Fact]
    public async Task Create_without_office_at_source_city_returns_422()
    {
        var (op, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Operator], email: "nooffice@t.local");
        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var srcCity = new City { Id = Guid.NewGuid(), Name = "CityC", State = "XX", IsActive = true };
        var dstCity = new City { Id = Guid.NewGuid(), Name = "CityD", State = "XX", IsActive = true };
        db.Cities.AddRange(srcCity, dstCity);
        var route = new Models.Route { Id = Guid.NewGuid(), SourceCityId = srcCity.Id, DestinationCityId = dstCity.Id, IsActive = true };
        db.Routes.Add(route);
        // Only office at dstCity — missing srcCity
        db.OperatorOffices.Add(new OperatorOffice { Id = Guid.NewGuid(), OperatorUserId = op.Id, CityId = dstCity.Id, AddressLine = "Addr", Phone = "0000", IsActive = true });
        var bus = new Bus
        {
            Id = Guid.NewGuid(), OperatorUserId = op.Id,
            RegistrationNumber = "TN-NOOFFICE", BusName = "No Office Bus",
            BusType = BusType.Seater, Capacity = 20,
            ApprovalStatus = BusApprovalStatus.Approved,
            OperationalStatus = BusOperationalStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        db.Buses.Add(bus);
        await db.SaveChangesAsync();

        var client = _fx.CreateClient(); client.AttachBearer(token);
        var resp = await client.PostAsJsonAsync("/api/v1/operator/schedules", SampleSchedule(bus.Id, route.Id));
        resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await resp.Content.ReadAsStringAsync()).Should().Contain("NO_OFFICE_AT_CITY");
    }

    [Fact]
    public async Task List_scopes_to_operator()
    {
        var (_, token, bus, route) = await SeedOperatorWithApprovedBus();
        var client = _fx.CreateClient(); client.AttachBearer(token);
        await client.PostAsJsonAsync("/api/v1/operator/schedules", SampleSchedule(bus.Id, route.Id));
        await client.PostAsJsonAsync("/api/v1/operator/schedules", SampleSchedule(bus.Id, route.Id) with { DepartureTime = new TimeOnly(18, 0) });

        var list = await (await client.GetAsync("/api/v1/operator/schedules"))
            .Content.ReadFromJsonAsync<List<BusScheduleDto>>();
        list!.Should().HaveCount(2);
        list.Should().OnlyContain(s => s.BusId == bus.Id);
    }

    [Fact]
    public async Task Update_fare_returns_200_with_new_fare()
    {
        var (_, token, bus, route) = await SeedOperatorWithApprovedBus();
        var client = _fx.CreateClient(); client.AttachBearer(token);
        var created = await (await client.PostAsJsonAsync("/api/v1/operator/schedules", SampleSchedule(bus.Id, route.Id)))
            .Content.ReadFromJsonAsync<BusScheduleDto>();

        var resp = await client.PatchAsJsonAsync($"/api/v1/operator/schedules/{created!.Id}",
            new UpdateBusScheduleRequest(null, null, 500m, null, null, null, null));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await resp.Content.ReadFromJsonAsync<BusScheduleDto>();
        updated!.FarePerSeat.Should().Be(500m);
    }

    [Fact]
    public async Task Delete_removes_schedule()
    {
        var (_, token, bus, route) = await SeedOperatorWithApprovedBus();
        var client = _fx.CreateClient(); client.AttachBearer(token);
        var created = await (await client.PostAsJsonAsync("/api/v1/operator/schedules", SampleSchedule(bus.Id, route.Id)))
            .Content.ReadFromJsonAsync<BusScheduleDto>();

        var del = await client.DeleteAsync($"/api/v1/operator/schedules/{created!.Id}");
        del.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var list = await (await client.GetAsync("/api/v1/operator/schedules"))
            .Content.ReadFromJsonAsync<List<BusScheduleDto>>();
        list!.Should().BeEmpty();
    }

    [Fact]
    public async Task ListRoutes_returns_active_routes()
    {
        var (_, token, _, route) = await SeedOperatorWithApprovedBus();
        var client = _fx.CreateClient(); client.AttachBearer(token);
        var routes = await (await client.GetAsync("/api/v1/operator/routes"))
            .Content.ReadFromJsonAsync<List<RouteOptionDto>>();
        routes!.Should().ContainSingle(r => r.Id == route.Id);
    }
}
