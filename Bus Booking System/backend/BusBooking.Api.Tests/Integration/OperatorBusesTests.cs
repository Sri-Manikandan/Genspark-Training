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

public class OperatorBusesTests : IClassFixture<IntegrationFixture>, IAsyncLifetime
{
    private readonly IntegrationFixture _fx;

    public OperatorBusesTests(IntegrationFixture fx) => _fx = fx;

    public async Task InitializeAsync() => await _fx.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private static CreateBusRequest SampleBus(string reg = "TN-37-AB-1234") => new()
    {
        RegistrationNumber = reg,
        BusName = "Shakti Express",
        BusType = BusType.Seater,
        Rows = 3,
        Columns = 4
    };

    [Fact]
    public async Task Create_generates_seats_and_marks_pending()
    {
        var (op, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Operator]);
        var client = _fx.CreateClient(); client.AttachBearer(token);

        var resp = await client.PostAsJsonAsync("/api/v1/operator/buses", SampleBus());
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await resp.Content.ReadFromJsonAsync<BusDto>();
        dto!.ApprovalStatus.Should().Be(BusApprovalStatus.Pending);
        dto.OperationalStatus.Should().Be(BusOperationalStatus.Active);
        dto.Capacity.Should().Be(12);

        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seats = await db.SeatDefinitions.Where(s => s.BusId == dto.Id).ToListAsync();
        seats.Should().HaveCount(12);
        seats.Select(s => s.SeatNumber).Should().Contain(new[] { "A1", "C4" });
    }

    [Fact]
    public async Task Duplicate_registration_returns_409()
    {
        var (_, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Operator]);
        var client = _fx.CreateClient(); client.AttachBearer(token);

        (await client.PostAsJsonAsync("/api/v1/operator/buses", SampleBus())).EnsureSuccessStatusCode();
        var dup = await client.PostAsJsonAsync("/api/v1/operator/buses", SampleBus());
        dup.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await dup.Content.ReadAsStringAsync()).Should().Contain("REGISTRATION_TAKEN");
    }

    [Fact]
    public async Task List_scopes_to_current_operator()
    {
        var (_, tokenA) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Operator], email: "a@t.local");
        var (_, tokenB) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Operator], email: "b@t.local");

        var clientA = _fx.CreateClient(); clientA.AttachBearer(tokenA);
        var clientB = _fx.CreateClient(); clientB.AttachBearer(tokenB);

        (await clientA.PostAsJsonAsync("/api/v1/operator/buses", SampleBus("OP-A-01"))).EnsureSuccessStatusCode();
        (await clientB.PostAsJsonAsync("/api/v1/operator/buses", SampleBus("OP-B-01"))).EnsureSuccessStatusCode();

        var listA = await (await clientA.GetAsync("/api/v1/operator/buses"))
            .Content.ReadFromJsonAsync<List<BusDto>>();
        listA!.Should().HaveCount(1);
        listA![0].RegistrationNumber.Should().Be("OP-A-01");
    }

    [Fact]
    public async Task Patch_status_toggles_maintenance()
    {
        var (_, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Operator]);
        var client = _fx.CreateClient(); client.AttachBearer(token);

        var created = await (await client.PostAsJsonAsync("/api/v1/operator/buses", SampleBus()))
            .Content.ReadFromJsonAsync<BusDto>();

        var resp = await client.PatchAsJsonAsync($"/api/v1/operator/buses/{created!.Id}/status",
            new UpdateBusStatusRequest { OperationalStatus = BusOperationalStatus.UnderMaintenance });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await resp.Content.ReadFromJsonAsync<BusDto>();
        dto!.OperationalStatus.Should().Be(BusOperationalStatus.UnderMaintenance);
    }

    [Fact]
    public async Task Delete_soft_retires_bus()
    {
        var (_, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Operator]);
        var client = _fx.CreateClient(); client.AttachBearer(token);

        var created = await (await client.PostAsJsonAsync("/api/v1/operator/buses", SampleBus()))
            .Content.ReadFromJsonAsync<BusDto>();

        var resp = await client.DeleteAsync($"/api/v1/operator/buses/{created!.Id}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await resp.Content.ReadFromJsonAsync<BusDto>();
        dto!.OperationalStatus.Should().Be(BusOperationalStatus.Retired);

        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Buses.FirstAsync(b => b.Id == created.Id)).OperationalStatus
            .Should().Be(BusOperationalStatus.Retired);
    }

    [Fact]
    public async Task Customer_only_token_is_403()
    {
        var (_, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        var client = _fx.CreateClient(); client.AttachBearer(token);

        var resp = await client.PostAsJsonAsync("/api/v1/operator/buses", SampleBus());
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
