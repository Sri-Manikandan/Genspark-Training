using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using BusBooking.Api.Tests.Support;

namespace BusBooking.Api.Tests.Integration;

public class AuthLoginTests : IClassFixture<IntegrationFixture>, IAsyncLifetime
{
    private readonly IntegrationFixture _fixture;
    private readonly HttpClient _client;

    public AuthLoginTests(IntegrationFixture fixture)
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

    private async Task RegisterAsync(string email, string password)
    {
        var body = new RegisterRequest("Test", email, password, null);
        (await _client.PostAsJsonAsync("/api/v1/auth/register", body)).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Login_with_valid_credentials_returns_token_and_user()
    {
        await RegisterAsync("grace@example.com", "Compiler!1959");

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("grace@example.com", "Compiler!1959"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        payload.Should().NotBeNull();
        payload!.Token.Should().NotBeNullOrWhiteSpace();
        payload.User.Email.Should().Be("grace@example.com");
        payload.User.Roles.Should().Contain("customer");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(payload.Token);
        jwt.Claims.Should().Contain(c => c.Type == "role" && c.Value == "customer");
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401_INVALID_CREDENTIALS()
    {
        await RegisterAsync("grace@example.com", "Compiler!1959");

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("grace@example.com", "WrongPassword!"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync()).Should().Contain("INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task Login_with_unknown_email_returns_401_INVALID_CREDENTIALS()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("nobody@example.com", "whatever"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync()).Should().Contain("INVALID_CREDENTIALS");
    }
}
