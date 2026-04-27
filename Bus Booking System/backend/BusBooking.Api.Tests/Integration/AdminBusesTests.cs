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

public class AdminBusesTests : IClassFixture<IntegrationFixture>, IAsyncLifetime
{
    private readonly IntegrationFixture _fx;

    public AdminBusesTests(IntegrationFixture fx) => _fx = fx;

    public async Task InitializeAsync() => await _fx.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<Guid> SeedPendingBusAsync(string reg = "TN-99-ZZ-0001")
    {
        var (op, opToken) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Operator]);
        var client = _fx.CreateClient(); client.AttachBearer(opToken);
        var created = await (await client.PostAsJsonAsync("/api/v1/operator/buses",
            new CreateBusRequest
            {
                RegistrationNumber = reg,
                BusName = "Test Bus",
                BusType = BusType.Seater,
                Rows = 2, Columns = 2
            })).Content.ReadFromJsonAsync<BusDto>();
        return created!.Id;
    }

    [Fact]
    public async Task List_filters_by_pending()
    {
        await SeedPendingBusAsync("PEND-1");
        await SeedPendingBusAsync("PEND-2");

        var (_, token) = await AdminTokenFactory.CreateAdminAsync(_fx, email: "admin-list-b@t.local");
        var client = _fx.CreateClient(); client.AttachAdminBearer(token);

        var resp = await client.GetAsync("/api/v1/admin/buses?status=pending");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await resp.Content.ReadFromJsonAsync<List<BusDto>>();
        list!.Should().HaveCount(2);
        list.Should().OnlyContain(b => b.ApprovalStatus == BusApprovalStatus.Pending);
    }

    [Fact]
    public async Task Approve_flips_status_and_writes_audit()
    {
        var busId = await SeedPendingBusAsync("APP-1");
        var (admin, token) = await AdminTokenFactory.CreateAdminAsync(_fx, email: "admin-app-b@t.local");
        var client = _fx.CreateClient(); client.AttachAdminBearer(token);

        var resp = await client.PostAsync($"/api/v1/admin/buses/{busId}/approve", null);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await resp.Content.ReadFromJsonAsync<BusDto>();
        dto!.ApprovalStatus.Should().Be(BusApprovalStatus.Approved);

        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var audit = await db.AuditLog.FirstOrDefaultAsync(a =>
            a.Action == AuditAction.BusApproved && a.TargetId == busId);
        audit.Should().NotBeNull();
        audit!.ActorUserId.Should().Be(admin.Id);
    }

    [Fact]
    public async Task Reject_stores_reason()
    {
        var busId = await SeedPendingBusAsync("REJ-1");
        var (_, token) = await AdminTokenFactory.CreateAdminAsync(_fx, email: "admin-r-b@t.local");
        var client = _fx.CreateClient(); client.AttachAdminBearer(token);

        var resp = await client.PostAsJsonAsync(
            $"/api/v1/admin/buses/{busId}/reject",
            new RejectBusRequest { Reason = "Insurance expired" });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bus = await db.Buses.FirstAsync(b => b.Id == busId);
        bus.ApprovalStatus.Should().Be(BusApprovalStatus.Rejected);
        bus.RejectReason.Should().Be("Insurance expired");
    }

    [Fact]
    public async Task Approve_already_approved_returns_422()
    {
        var busId = await SeedPendingBusAsync("DBL-1");
        var (_, token) = await AdminTokenFactory.CreateAdminAsync(_fx, email: "admin-dbl@t.local");
        var client = _fx.CreateClient(); client.AttachAdminBearer(token);

        (await client.PostAsync($"/api/v1/admin/buses/{busId}/approve", null)).EnsureSuccessStatusCode();
        var second = await client.PostAsync($"/api/v1/admin/buses/{busId}/approve", null);
        second.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await second.Content.ReadAsStringAsync()).Should().Contain("BUS_NOT_PENDING");
    }

    [Fact]
    public async Task Non_admin_token_is_403()
    {
        var busId = await SeedPendingBusAsync("FORB-1");
        var (_, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Operator], email: "op-forb@t.local");
        var client = _fx.CreateClient(); client.AttachBearer(token);

        var resp = await client.PostAsync($"/api/v1/admin/buses/{busId}/approve", null);
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
