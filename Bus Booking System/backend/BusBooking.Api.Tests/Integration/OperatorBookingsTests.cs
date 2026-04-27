using System.Net;
using System.Net.Http.Json;
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Models;
using BusBooking.Api.Tests.Support;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace BusBooking.Api.Tests.Integration;

public class OperatorBookingsTests : IClassFixture<IntegrationFixture>, IAsyncLifetime
{
    private readonly IntegrationFixture _fx;
    public OperatorBookingsTests(IntegrationFixture fx) => _fx = fx;

    public Task InitializeAsync() => _fx.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Returns_only_bookings_for_operators_own_buses()
    {
        var seed1 = await TripTestSeed.CreateAsync(_fx, capacity: 4, daysAhead: 5);
        var seed2 = await TripTestSeed.CreateAsync(_fx, capacity: 4, daysAhead: 5);

        var (cust, _) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        await SeedConfirmedBookingAsync(seed1.TripId, cust.Id, ["A1"]);
        await SeedConfirmedBookingAsync(seed2.TripId, cust.Id, ["A1"]);

        var (op1, op1Token) = await GetOperatorForBusAsync(seed1.BusId);

        var client = _fx.CreateClient();
        client.AttachBearer(op1Token);

        var resp = await client.GetFromJsonAsync<OperatorBookingListResponseDto>(
            "/api/v1/operator/bookings");

        resp!.Items.Should().OnlyContain(i => i.BusId == seed1.BusId);
        resp.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Filters_by_bus_id()
    {
        var seed1 = await TripTestSeed.CreateAsync(_fx, capacity: 4, daysAhead: 5);
        var (op, opToken) = await GetOperatorForBusAsync(seed1.BusId);

        var seed2 = await TripTestSeed.CreateWithOperatorAsync(_fx, op, capacity: 4, daysAhead: 5);

        var (cust, _) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        await SeedConfirmedBookingAsync(seed1.TripId, cust.Id, ["A1"]);
        await SeedConfirmedBookingAsync(seed2.TripId, cust.Id, ["A1"]);

        var client = _fx.CreateClient();
        client.AttachBearer(opToken);

        var resp = await client.GetFromJsonAsync<OperatorBookingListResponseDto>(
            $"/api/v1/operator/bookings?busId={seed1.BusId}");

        resp!.Items.Should().HaveCount(1);
        resp.Items[0].BusId.Should().Be(seed1.BusId);
    }

    [Fact]
    public async Task Filters_by_date()
    {
        var seed = await TripTestSeed.CreateAsync(_fx, capacity: 4, daysAhead: 5);
        var (op, opToken) = await GetOperatorForBusAsync(seed.BusId);
        var (cust, _) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        await SeedConfirmedBookingAsync(seed.TripId, cust.Id, ["A1"]);

        var client = _fx.CreateClient();
        client.AttachBearer(opToken);

        var tripDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)).ToString("yyyy-MM-dd");
        var respMatch = await client.GetFromJsonAsync<OperatorBookingListResponseDto>(
            $"/api/v1/operator/bookings?date={tripDate}");
        respMatch!.Items.Should().HaveCount(1);

        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)).ToString("yyyy-MM-dd");
        var respNoMatch = await client.GetFromJsonAsync<OperatorBookingListResponseDto>(
            $"/api/v1/operator/bookings?date={yesterday}");
        respNoMatch!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Excludes_pending_payment_bookings()
    {
        var seed = await TripTestSeed.CreateAsync(_fx, capacity: 4, daysAhead: 5);
        var (op, opToken) = await GetOperatorForBusAsync(seed.BusId);
        var (cust, _) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);

        await SeedPendingBookingAsync(seed.TripId, cust.Id, "A1");
        await SeedConfirmedBookingAsync(seed.TripId, cust.Id, ["A2"]);

        var client = _fx.CreateClient();
        client.AttachBearer(opToken);

        var resp = await client.GetFromJsonAsync<OperatorBookingListResponseDto>(
            "/api/v1/operator/bookings");
        resp!.Items.Should().HaveCount(1);
        resp.Items[0].Status.Should().Be(BookingStatus.Confirmed);
    }

    [Fact]
    public async Task Requires_operator_role()
    {
        var (_, custToken) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        var client = _fx.CreateClient();
        client.AttachBearer(custToken);

        var resp = await client.GetAsync("/api/v1/operator/bookings");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
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

    private async Task SeedConfirmedBookingAsync(Guid tripId, Guid userId, string[] seats)
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
            TotalFare = 500m * seats.Length,
            PlatformFee = 25m,
            TotalAmount = 500m * seats.Length + 25m,
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
            Amount = 500m * seats.Length + 25m, Currency = "INR",
            Status = PaymentStatus.Captured,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            CapturedAt = DateTime.UtcNow.AddMinutes(-4)
        });
        await db.SaveChangesAsync();
    }

    private async Task SeedPendingBookingAsync(Guid tripId, Guid userId, string seat)
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
            TotalFare = 500m,
            PlatformFee = 25m,
            TotalAmount = 525m,
            SeatCount = 1,
            Status = BookingStatus.PendingPayment,
            CreatedAt = DateTime.UtcNow
        });
        db.BookingSeats.Add(new BookingSeat
        {
            Id = Guid.NewGuid(), BookingId = id, SeatNumber = seat,
            PassengerName = "Test", PassengerAge = 25, PassengerGender = PassengerGender.Male
        });
        await db.SaveChangesAsync();
    }
}
