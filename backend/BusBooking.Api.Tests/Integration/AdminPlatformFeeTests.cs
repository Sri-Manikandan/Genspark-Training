using System.Net;
using System.Net.Http.Json;
using BusBooking.Api.Dtos;
using BusBooking.Api.Tests.Support;
using FluentAssertions;

namespace BusBooking.Api.Tests.Integration;

[Collection("Integration")]
public class AdminPlatformFeeTests : IAsyncLifetime
{
    private readonly IntegrationFixture _fx;

    public AdminPlatformFeeTests(IntegrationFixture fx)
    {
        _fx = fx;
    }

    public async Task InitializeAsync() => await _fx.ResetAsync();

    public Task DisposeAsync()
    {
        _fx.Client.DefaultRequestHeaders.Authorization = null;
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Get_returns_the_seeded_default_after_a_fresh_reset()
    {
        var (_, token) = await AdminTokenFactory.CreateAdminAsync(_fx);
        _fx.Client.AttachAdminBearer(token);

        var resp = await _fx.Client.GetAsync("/api/v1/admin/platform-fee");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<PlatformFeeDto>();
        body!.FeeType.Should().Be("fixed");
        body.Value.Should().Be(25m);
    }

    [Fact]
    public async Task Put_inserts_new_row_and_subsequent_Get_returns_it()
    {
        var (_, token) = await AdminTokenFactory.CreateAdminAsync(_fx);
        _fx.Client.AttachAdminBearer(token);

        var put = await _fx.Client.PutAsJsonAsync("/api/v1/admin/platform-fee",
            new { feeType = "percent", value = 4.5m });
        put.StatusCode.Should().Be(HttpStatusCode.OK);

        var get = await _fx.Client.GetFromJsonAsync<PlatformFeeDto>("/api/v1/admin/platform-fee");
        get!.FeeType.Should().Be("percent");
        get.Value.Should().Be(4.5m);
    }

    [Fact]
    public async Task Invalid_fee_type_returns_400()
    {
        var (_, token) = await AdminTokenFactory.CreateAdminAsync(_fx);
        _fx.Client.AttachAdminBearer(token);

        var resp = await _fx.Client.PutAsJsonAsync("/api/v1/admin/platform-fee",
            new { feeType = "banana", value = 3m });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
