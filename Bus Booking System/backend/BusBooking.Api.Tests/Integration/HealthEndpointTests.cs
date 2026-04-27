using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BusBooking.Api.Tests.Integration;

public class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_health_returns_200_with_status_ok()
    {
        var response = await _client.GetAsync("/api/v1/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();
        payload.Should().NotBeNull();
        payload!.Status.Should().Be("ok");
        payload.Service.Should().Be("bus-booking-api");
    }

    private record HealthResponse(string Status, string Service, string Version, DateTime TimestampUtc);
}
