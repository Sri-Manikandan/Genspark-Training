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

public class AdminOperatorRequestsTests : IClassFixture<IntegrationFixture>, IAsyncLifetime
{
    private readonly IntegrationFixture _fx;

    public AdminOperatorRequestsTests(IntegrationFixture fx) => _fx = fx;

    public async Task InitializeAsync() => await _fx.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<Guid> SeedPendingRequestAsync(string email = "cust@t.local")
    {
        var (customer, _) = await OperatorTokenFactory.CreateAsync(
            _fx, [Roles.Customer], email: email);

        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var req = new OperatorRequest
        {
            Id = Guid.NewGuid(),
            UserId = customer.Id,
            Status = OperatorRequestStatus.Pending,
            CompanyName = "Test Co",
            RequestedAt = DateTime.UtcNow
        };
        db.OperatorRequests.Add(req);
        await db.SaveChangesAsync();
        return req.Id;
    }

    [Fact]
    public async Task List_returns_all_by_default()
    {
        await SeedPendingRequestAsync("a@t.local");
        await SeedPendingRequestAsync("b@t.local");

        var (_, token) = await AdminTokenFactory.CreateAdminAsync(_fx, email: "admin-list@t.local");
        var client = _fx.CreateClient();
        client.AttachAdminBearer(token);

        var resp = await client.GetAsync("/api/v1/admin/operator-requests");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await resp.Content.ReadFromJsonAsync<List<OperatorRequestDto>>();
        list!.Count.Should().Be(2);
    }

    [Fact]
    public async Task List_filters_by_status()
    {
        var reqId = await SeedPendingRequestAsync();
        var (_, token) = await AdminTokenFactory.CreateAdminAsync(_fx, email: "admin-f@t.local");
        var client = _fx.CreateClient();
        client.AttachAdminBearer(token);

        var resp = await client.GetAsync("/api/v1/admin/operator-requests?status=pending");
        var list = await resp.Content.ReadFromJsonAsync<List<OperatorRequestDto>>();
        list!.Should().ContainSingle(x => x.Id == reqId);

        var none = await client.GetAsync("/api/v1/admin/operator-requests?status=approved");
        (await none.Content.ReadFromJsonAsync<List<OperatorRequestDto>>())!.Should().BeEmpty();
    }

    [Fact]
    public async Task Approve_grants_operator_role_and_writes_audit()
    {
        var reqId = await SeedPendingRequestAsync("approved@t.local");

        var (admin, token) = await AdminTokenFactory.CreateAdminAsync(_fx, email: "admin-app@t.local");
        var client = _fx.CreateClient();
        client.AttachAdminBearer(token);

        var resp = await client.PostAsync($"/api/v1/admin/operator-requests/{reqId}/approve", null);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updated = await db.OperatorRequests.FirstAsync(r => r.Id == reqId);
        updated.Status.Should().Be(OperatorRequestStatus.Approved);
        updated.ReviewedByAdminId.Should().Be(admin.Id);

        var roles = await db.UserRoles.Where(r => r.UserId == updated.UserId).Select(r => r.Role).ToListAsync();
        roles.Should().Contain(Roles.Operator);

        var audit = await db.AuditLog.FirstOrDefaultAsync(a =>
            a.Action == AuditAction.OperatorRequestApproved && a.TargetId == reqId);
        audit.Should().NotBeNull();
    }

    [Fact]
    public async Task Reject_stores_reason_and_does_not_grant_role()
    {
        var reqId = await SeedPendingRequestAsync("rejected@t.local");

        var (_, token) = await AdminTokenFactory.CreateAdminAsync(_fx, email: "admin-r@t.local");
        var client = _fx.CreateClient();
        client.AttachAdminBearer(token);

        var resp = await client.PostAsJsonAsync(
            $"/api/v1/admin/operator-requests/{reqId}/reject",
            new RejectOperatorRequest { Reason = "Missing documents" });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updated = await db.OperatorRequests.FirstAsync(r => r.Id == reqId);
        updated.Status.Should().Be(OperatorRequestStatus.Rejected);
        updated.RejectReason.Should().Be("Missing documents");

        var roles = await db.UserRoles.Where(r => r.UserId == updated.UserId).Select(r => r.Role).ToListAsync();
        roles.Should().NotContain(Roles.Operator);
    }

    [Fact]
    public async Task Non_admin_token_is_403()
    {
        var reqId = await SeedPendingRequestAsync();
        var (_, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        var client = _fx.CreateClient();
        client.AttachBearer(token);

        var resp = await client.PostAsync($"/api/v1/admin/operator-requests/{reqId}/approve", null);
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
