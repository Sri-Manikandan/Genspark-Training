using System.Net;
using System.Net.Http.Json;
using BusBooking.Api.Tests.Support;

namespace BusBooking.Api.Tests.Integration;

public class AuthRegisterTests : IClassFixture<IntegrationFixture>, IAsyncLifetime
{
    private readonly IntegrationFixture _fixture;
    private readonly HttpClient _client;

    public AuthRegisterTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    public Task InitializeAsync() => _fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private record RegisterRequest(string Name, string Email, string Password, string? Phone);
    private record UserDto(Guid Id, string Name, string Email, string? Phone, string[] Roles);

    [Fact]
    public async Task Register_returns_201_with_user_and_customer_role()
    {
        var body = new RegisterRequest("Ada Lovelace", "ada@example.com", "Analytical!1837", null);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        user.Should().NotBeNull();
        user!.Id.Should().NotBeEmpty();
        user.Email.Should().Be("ada@example.com");
        user.Name.Should().Be("Ada Lovelace");
        user.Roles.Should().BeEquivalentTo(new[] { "customer" });
    }

    [Fact]
    public async Task Register_duplicate_email_returns_409_EMAIL_IN_USE()
    {
        var body = new RegisterRequest("Ada Lovelace", "ada@example.com", "Analytical!1837", null);
        (await _client.PostAsJsonAsync("/api/v1/auth/register", body))
            .EnsureSuccessStatusCode();

        var second = await _client.PostAsJsonAsync("/api/v1/auth/register", body);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var raw = await second.Content.ReadAsStringAsync();
        raw.Should().Contain("EMAIL_IN_USE");
    }

    [Fact]
    public async Task Register_with_short_password_returns_400_validation_error()
    {
        var body = new RegisterRequest("X", "x@example.com", "short", null);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var raw = await response.Content.ReadAsStringAsync();
        raw.Should().Contain("VALIDATION_ERROR");
    }
}
