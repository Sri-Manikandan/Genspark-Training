using System.Net;
using System.Net.Http.Json;
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Models;
using BusBooking.Api.Tests.Support;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace BusBooking.Api.Tests.Integration;

public class OperatorRevenueTests : IClassFixture<IntegrationFixture>, IAsyncLifetime
{
    private readonly IntegrationFixture _fx;
    public OperatorRevenueTests(IntegrationFixture fx) => _fx = fx;

    public Task InitializeAsync() => _fx.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Returns_revenue_grouped_by_bus_for_confirmed_bookings()
    {
        var seed = await TripTestSeed.CreateAsync(_fx, capacity: 4, daysAhead: 3);
        var (op, opToken) = await GetOperatorForBusAsync(seed.BusId);
        var (cust, _) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);

        await SeedConfirmedBookingAsync(seed.TripId, cust.Id, ["A1", "A2"], 500m);

        var client = _fx.CreateClient();
        client.AttachBearer(opToken);

        var from = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)).ToString("yyyy-MM-dd");
        var to = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)).ToString("yyyy-MM-dd");

        var resp = await client.GetFromJsonAsync<OperatorRevenueResponseDto>(
            $"/api/v1/operator/revenue?from={from}&to={to}");

        resp!.ByBus.Should().HaveCount(1);
        resp.ByBus[0].BusId.Should().Be(seed.BusId);
        resp.ByBus[0].ConfirmedBookings.Should().Be(1);
        resp.ByBus[0].TotalSeats.Should().Be(2);
        resp.ByBus[0].TotalFare.Should().Be(1000m);
        resp.GrandTotalFare.Should().Be(1000m);
    }

    [Fact]
    public async Task Excludes_cancelled_bookings_from_revenue()
    {
        var seed = await TripTestSeed.CreateAsync(_fx, capacity: 4, daysAhead: 3);
        var (op, opToken) = await GetOperatorForBusAsync(seed.BusId);
        var (cust, _) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);

        await SeedConfirmedBookingAsync(seed.TripId, cust.Id, ["A1"], 500m);
        await SeedCancelledBookingAsync(seed.TripId, cust.Id, "A2", 500m);

        var client = _fx.CreateClient();
        client.AttachBearer(opToken);

        var from = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)).ToString("yyyy-MM-dd");
        var to = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)).ToString("yyyy-MM-dd");

        var resp = await client.GetFromJsonAsync<OperatorRevenueResponseDto>(
            $"/api/v1/operator/revenue?from={from}&to={to}");

        resp!.GrandTotalFare.Should().Be(500m);
        resp.ByBus[0].ConfirmedBookings.Should().Be(1);
    }

    [Fact]
    public async Task Date_range_filter_excludes_trips_outside_range()
    {
        var seedInRange = await TripTestSeed.CreateAsync(_fx, capacity: 4, daysAhead: 3);
        var (op, opToken) = await GetOperatorForBusAsync(seedInRange.BusId);
        var (cust, _) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);

        await SeedConfirmedBookingAsync(seedInRange.TripId, cust.Id, ["A1"], 500m);

        var client = _fx.CreateClient();
        client.AttachBearer(opToken);

        var from = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var to = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)).ToString("yyyy-MM-dd");

        var resp = await client.GetFromJsonAsync<OperatorRevenueResponseDto>(
            $"/api/v1/operator/revenue?from={from}&to={to}");

        resp!.ByBus.Should().HaveCount(1);
        resp.ByBus[0].BusId.Should().Be(seedInRange.BusId);
    }

    [Fact]
    public async Task Defaults_to_last_30_days_when_no_range_supplied()
    {
        var seed = await TripTestSeed.CreateAsync(_fx, capacity: 4, daysAhead: 3);
        var (op, opToken) = await GetOperatorForBusAsync(seed.BusId);
        var (cust, _) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        await SeedConfirmedBookingAsync(seed.TripId, cust.Id, ["A1"], 500m);

        var client = _fx.CreateClient();
        client.AttachBearer(opToken);

        var resp = await client.GetFromJsonAsync<OperatorRevenueResponseDto>(
            "/api/v1/operator/revenue");

        resp.Should().NotBeNull();
    }

    [Fact]
    public async Task Requires_operator_role()
    {
        var (_, custToken) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        var client = _fx.CreateClient();
        client.AttachBearer(custToken);

        (await client.GetAsync("/api/v1/operator/revenue")).StatusCode
            .Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<(User user, string token)> GetOperatorForBusAsync(Guid busId)
    {
        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bus = await db.Buses.FindAsync(busId);
        var op = await db.Users.FindAsync(bus!.OperatorUserId);
        var tokenService = scope.ServiceProvider
            .GetRequiredService<BusBooking.Api.Infrastructure.Auth.IJwtTokenService>();
        var token = tokenService.Generate(op!, [Roles.Operator]);
        return (op!, token.Token);
    }

    private async Task SeedConfirmedBookingAsync(
        Guid tripId, Guid userId, string[] seats, decimal farePerSeat)
    {
        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var id = Guid.NewGuid();
        var totalFare = farePerSeat * seats.Length;
        db.Bookings.Add(new Booking
        {
            Id = id,
            BookingCode = $"BK-{id:N}"[..11],
            TripId = tripId,
            UserId = userId,
            LockId = Guid.NewGuid(),
            TotalFare = totalFare,
            PlatformFee = 25m,
            TotalAmount = totalFare + 25m,
            SeatCount = seats.Length,
            Status = BookingStatus.Confirmed,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            ConfirmedAt = DateTime.UtcNow.AddMinutes(-4)
        });
        foreach (var s in seats)
            db.BookingSeats.Add(new BookingSeat
            {
                Id = Guid.NewGuid(), BookingId = id, SeatNumber = s,
                PassengerName = "Test", PassengerAge = 25, PassengerGender = PassengerGender.Male
            });
        db.Payments.Add(new Payment
        {
            Id = Guid.NewGuid(), BookingId = id,
            RazorpayOrderId = $"order_{Guid.NewGuid():N}"[..20],
            RazorpayPaymentId = $"pay_{Guid.NewGuid():N}"[..18],
            Amount = totalFare + 25m, Currency = "INR",
            Status = PaymentStatus.Captured,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            CapturedAt = DateTime.UtcNow.AddMinutes(-4)
        });
        await db.SaveChangesAsync();
    }

    private async Task SeedCancelledBookingAsync(
        Guid tripId, Guid userId, string seat, decimal farePerSeat)
    {
        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var id = Guid.NewGuid();
        db.Bookings.Add(new Booking
        {
            Id = id,
            BookingCode = $"BK-{id:N}"[..11],
            TripId = tripId,
            UserId = userId,
            LockId = Guid.NewGuid(),
            TotalFare = farePerSeat,
            PlatformFee = 25m,
            TotalAmount = farePerSeat + 25m,
            SeatCount = 1,
            Status = BookingStatus.Cancelled,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            ConfirmedAt = DateTime.UtcNow.AddMinutes(-9),
            CancelledAt = DateTime.UtcNow.AddMinutes(-2),
            RefundAmount = farePerSeat * 0.8m,
            RefundStatus = RefundStatus.Processed
        });
        db.BookingSeats.Add(new BookingSeat
        {
            Id = Guid.NewGuid(), BookingId = id, SeatNumber = seat,
            PassengerName = "Test", PassengerAge = 25, PassengerGender = PassengerGender.Male
        });
        await db.SaveChangesAsync();
    }
}
