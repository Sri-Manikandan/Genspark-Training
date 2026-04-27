using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BusBooking.Api.Tests.Support;

namespace BusBooking.Api.Tests.Integration;

public class MeEndpointTests : IClassFixture<IntegrationFixture>, IAsyncLifetime
{
    private readonly IntegrationFixture _fixture;
    private readonly HttpClient _client;

    public MeEndpointTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    public Task InitializeAsync() => _fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private record RegisterRequest(string Name, string Email, string Password, string? Phone);
    private record LoginRequest(string Email, string Password);
    private record UserDto(Guid Id, string Name, string Email, string? Phone, string[] Roles);
    private record LoginResponse(string Token, DateTime ExpiresAtUtc, UserDto User);

    [Fact]
    public async Task Me_without_token_returns_401()
    {
        var response = await _client.GetAsync("/api/v1/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_with_valid_token_returns_200_with_user_and_roles()
    {
        (await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest("Grace Hopper", "grace@example.com", "Compiler!1959", null)))
            .EnsureSuccessStatusCode();

        var login = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("grace@example.com", "Compiler!1959"));
        var loginBody = await login.Content.ReadFromJsonAsync<LoginResponse>();
        loginBody.Should().NotBeNull();

        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/me");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginBody!.Token);
        var response = await _client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var me = await response.Content.ReadFromJsonAsync<UserDto>();
        me.Should().NotBeNull();
        me!.Email.Should().Be("grace@example.com");
        me.Name.Should().Be("Grace Hopper");
        me.Roles.Should().BeEquivalentTo(new[] { "customer" });
    }
}
