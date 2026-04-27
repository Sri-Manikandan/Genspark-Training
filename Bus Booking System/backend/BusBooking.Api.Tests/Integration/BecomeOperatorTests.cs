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

public class BecomeOperatorTests : IClassFixture<IntegrationFixture>, IAsyncLifetime
{
    private readonly IntegrationFixture _fx;

    public BecomeOperatorTests(IntegrationFixture fx) => _fx = fx;

    public async Task InitializeAsync() => await _fx.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Customer_can_submit_operator_request()
    {
        var (_, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        var client = _fx.CreateClient();
        client.AttachBearer(token);

        var resp = await client.PostAsJsonAsync(
            "/api/v1/me/become-operator",
            new BecomeOperatorRequest { CompanyName = "Shakti Travels" });

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await resp.Content.ReadFromJsonAsync<OperatorRequestDto>();
        dto!.Status.Should().Be(OperatorRequestStatus.Pending);
        dto.CompanyName.Should().Be("Shakti Travels");
    }

    [Fact]
    public async Task Second_pending_request_returns_422()
    {
        var (_, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        var client = _fx.CreateClient();
        client.AttachBearer(token);

        var first = await client.PostAsJsonAsync("/api/v1/me/become-operator",
            new BecomeOperatorRequest { CompanyName = "First Co" });
        first.EnsureSuccessStatusCode();

        var second = await client.PostAsJsonAsync("/api/v1/me/become-operator",
            new BecomeOperatorRequest { CompanyName = "Second Co" });

        second.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await second.Content.ReadAsStringAsync();
        body.Should().Contain("REQUEST_ALREADY_PENDING");
    }

    [Fact]
    public async Task Already_operator_returns_422()
    {
        var (_, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer, Roles.Operator]);
        var client = _fx.CreateClient();
        client.AttachBearer(token);

        var resp = await client.PostAsJsonAsync("/api/v1/me/become-operator",
            new BecomeOperatorRequest { CompanyName = "Doesn't matter" });

        resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await resp.Content.ReadAsStringAsync()).Should().Contain("ALREADY_OPERATOR");
    }

    [Fact]
    public async Task Anonymous_request_is_401()
    {
        var client = _fx.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/v1/me/become-operator",
            new BecomeOperatorRequest { CompanyName = "Anon" });
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Admin_only_token_is_403()
    {
        var (_, token) = await AdminTokenFactory.CreateAdminAsync(_fx, email: "x-admin@t.local");
        var client = _fx.CreateClient();
        client.AttachAdminBearer(token);

        var resp = await client.PostAsJsonAsync("/api/v1/me/become-operator",
            new BecomeOperatorRequest { CompanyName = "No" });

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
