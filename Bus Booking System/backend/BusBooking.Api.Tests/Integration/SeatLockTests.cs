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
public class SeatLockTests : IAsyncLifetime
{
    private readonly IntegrationFixture _fx;
    public SeatLockTests(IntegrationFixture fx) => _fx = fx;
    public async Task InitializeAsync() => await _fx.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ConcurrentLock_SameSeat_OneWinsOtherGets409()
    {
        var trip = await TripTestSeed.CreateAsync(_fx);

        var body1 = new LockSeatsRequest(Guid.NewGuid(), new List<string> { "A1" });
        var body2 = new LockSeatsRequest(Guid.NewGuid(), new List<string> { "A1" });

        var t1 = _fx.Client.PostAsJsonAsync($"/api/v1/trips/{trip.TripId}/seat-locks", body1);
        var t2 = _fx.Client.PostAsJsonAsync($"/api/v1/trips/{trip.TripId}/seat-locks", body2);
        var resp1 = await t1;
        var resp2 = await t2;

        var codes = new[] { resp1.StatusCode, resp2.StatusCode };
        codes.Should().Contain(HttpStatusCode.OK);
        codes.Should().Contain(HttpStatusCode.Conflict);

        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.SeatLocks.CountAsync(l => l.TripId == trip.TripId && l.SeatNumber == "A1"))
            .Should().Be(1);
    }

    [Fact]
    public async Task ExpiredLocks_AreFilteredFromSeatLayout()
    {
        var trip = await TripTestSeed.CreateAsync(_fx);

        using (var scope = _fx.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = DateTime.UtcNow;
            db.SeatLocks.Add(new SeatLock
            {
                Id         = Guid.NewGuid(),
                TripId     = trip.TripId,
                SeatNumber = "A1",
                LockId     = Guid.NewGuid(),
                SessionId  = Guid.NewGuid(),
                CreatedAt  = now.AddMinutes(-8),
                ExpiresAt  = now.AddMinutes(-1)
            });
            await db.SaveChangesAsync();
        }

        var layout = await _fx.Client
            .GetFromJsonAsync<SeatLayoutDto>($"/api/v1/trips/{trip.TripId}/seats");
        layout!.Seats.First(s => s.SeatNumber == "A1").Status.Should().Be("available");
    }

    [Fact]
    public async Task LockMultipleSeats_AllShareSameLockId()
    {
        var trip = await TripTestSeed.CreateAsync(_fx);
        var sessionId = Guid.NewGuid();

        var resp = await _fx.Client.PostAsJsonAsync(
            $"/api/v1/trips/{trip.TripId}/seat-locks",
            new LockSeatsRequest(sessionId, new List<string> { "A1", "A2", "A3" }));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await resp.Content.ReadFromJsonAsync<SeatLockResponseDto>();

        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var rows = await db.SeatLocks.Where(l => l.LockId == dto!.LockId).ToListAsync();
        rows.Should().HaveCount(3);
        rows.Select(r => r.SeatNumber).Should().BeEquivalentTo(new[] { "A1", "A2", "A3" });
        rows.Should().OnlyContain(r => r.SessionId == sessionId);
        rows.Should().OnlyContain(r => r.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task ReleaseLock_Deletes_AllRows()
    {
        var trip = await TripTestSeed.CreateAsync(_fx);
        var sessionId = Guid.NewGuid();

        var lockResp = await _fx.Client.PostAsJsonAsync(
            $"/api/v1/trips/{trip.TripId}/seat-locks",
            new LockSeatsRequest(sessionId, new List<string> { "A1", "A2" }));
        var lockDto = await lockResp.Content.ReadFromJsonAsync<SeatLockResponseDto>();

        var del = await _fx.Client.DeleteAsync(
            $"/api/v1/seat-locks/{lockDto!.LockId}?sessionId={sessionId}");
        del.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.SeatLocks.CountAsync(l => l.LockId == lockDto.LockId)).Should().Be(0);
    }

    [Fact]
    public async Task ReleaseLock_WithWrongSession_Returns403()
    {
        var trip = await TripTestSeed.CreateAsync(_fx);
        var ownerSession = Guid.NewGuid();
        var intruderSession = Guid.NewGuid();

        var lockResp = await _fx.Client.PostAsJsonAsync(
            $"/api/v1/trips/{trip.TripId}/seat-locks",
            new LockSeatsRequest(ownerSession, new List<string> { "A1" }));
        var lockDto = await lockResp.Content.ReadFromJsonAsync<SeatLockResponseDto>();

        var del = await _fx.Client.DeleteAsync(
            $"/api/v1/seat-locks/{lockDto!.LockId}?sessionId={intruderSession}");
        del.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task LockUnknownSeat_Returns422()
    {
        var trip = await TripTestSeed.CreateAsync(_fx);
        var resp = await _fx.Client.PostAsJsonAsync(
            $"/api/v1/trips/{trip.TripId}/seat-locks",
            new LockSeatsRequest(Guid.NewGuid(), new List<string> { "Z9" }));
        resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}
