using System.Net;
using System.Net.Http.Json;
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Models;
using BusBooking.Api.Tests.Support;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BusBooking.Api.Tests.Integration;

[Collection("Integration")]
public class AdminRevenueTests : IAsyncLifetime
{
    private readonly IntegrationFixture _fx;
    public AdminRevenueTests(IntegrationFixture fx) { _fx = fx; }

    public Task InitializeAsync() => _fx.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Returns_gmv_platform_fee_income_grouped_by_operator()
    {
        var seed1 = await TripTestSeed.CreateAsync(_fx, capacity: 4, daysAhead: 3);
        var seed2 = await TripTestSeed.CreateAsync(_fx, capacity: 4, daysAhead: 4);
        var (cust, _) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);

        await SeedConfirmedBookingAsync(seed1.TripId, cust.Id, totalFare: 1000m, platformFee: 50m);
        await SeedConfirmedBookingAsync(seed2.TripId, cust.Id, totalFare: 500m, platformFee: 25m);

        var (admin, adminToken) = await AdminTokenFactory.CreateAdminAsync(_fx);
        var client = _fx.CreateClient();
        client.AttachAdminBearer(adminToken);

        var from = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)).ToString("yyyy-MM-dd");
        var to   = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)).ToString("yyyy-MM-dd");

        var resp = await client.GetFromJsonAsync<AdminRevenueResponseDto>(
            $"/api/v1/admin/revenue?from={from}&to={to}");

        resp!.Gmv.Should().Be(1500m);
        resp.PlatformFeeIncome.Should().Be(75m);
        resp.ConfirmedBookings.Should().Be(2);
        resp.ByOperator.Should().HaveCount(2);
        resp.ByOperator.Sum(x => x.Gmv).Should().Be(1500m);
    }

    [Fact]
    public async Task Excludes_cancelled_and_pending_bookings()
    {
        var seed = await TripTestSeed.CreateAsync(_fx, capacity: 4, daysAhead: 3);
        var (cust, _) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);

        await SeedConfirmedBookingAsync(seed.TripId, cust.Id, totalFare: 1000m, platformFee: 50m);
        await SeedCancelledBookingAsync(seed.TripId, cust.Id, totalFare: 500m, platformFee: 25m);

        using (var scope = _fx.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Bookings.Add(new Booking
            {
                Id = Guid.NewGuid(), BookingCode = "BK-PENDING",
                TripId = seed.TripId, UserId = cust.Id, LockId = Guid.NewGuid(),
                TotalFare = 300m, PlatformFee = 10m, TotalAmount = 310m,
                SeatCount = 1, Status = BookingStatus.PendingPayment,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var (admin, adminToken) = await AdminTokenFactory.CreateAdminAsync(_fx);
        var client = _fx.CreateClient();
        client.AttachAdminBearer(adminToken);

        var from = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)).ToString("yyyy-MM-dd");
        var to   = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)).ToString("yyyy-MM-dd");

        var resp = await client.GetFromJsonAsync<AdminRevenueResponseDto>(
            $"/api/v1/admin/revenue?from={from}&to={to}");
        resp!.Gmv.Should().Be(1000m);
        resp.PlatformFeeIncome.Should().Be(50m);
        resp.ConfirmedBookings.Should().Be(1);
    }

    [Fact]
    public async Task Date_range_filter_excludes_trips_outside_range()
    {
        var seedIn = await TripTestSeed.CreateAsync(_fx, capacity: 4, daysAhead: 3);
        var seedOut = await TripTestSeed.CreateAsync(_fx, capacity: 4, daysAhead: 30);
        var (cust, _) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);

        await SeedConfirmedBookingAsync(seedIn.TripId, cust.Id, totalFare: 100m, platformFee: 10m);
        await SeedConfirmedBookingAsync(seedOut.TripId, cust.Id, totalFare: 999m, platformFee: 99m);

        var (admin, adminToken) = await AdminTokenFactory.CreateAdminAsync(_fx);
        var client = _fx.CreateClient();
        client.AttachAdminBearer(adminToken);

        var from = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var to   = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)).ToString("yyyy-MM-dd");

        var resp = await client.GetFromJsonAsync<AdminRevenueResponseDto>(
            $"/api/v1/admin/revenue?from={from}&to={to}");
        resp!.Gmv.Should().Be(100m);
        resp.PlatformFeeIncome.Should().Be(10m);
    }

    [Fact]
    public async Task Defaults_to_last_30_days_when_no_range_supplied()
    {
        var (_, adminToken) = await AdminTokenFactory.CreateAdminAsync(_fx);
        var client = _fx.CreateClient();
        client.AttachAdminBearer(adminToken);

        var resp = await client.GetFromJsonAsync<AdminRevenueResponseDto>("/api/v1/admin/revenue");
        resp.Should().NotBeNull();
        resp!.ByOperator.Should().NotBeNull();
    }

    [Fact]
    public async Task Requires_admin_role()
    {
        var (_, custToken) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        var client = _fx.CreateClient();
        client.AttachBearer(custToken);

        (await client.GetAsync("/api/v1/admin/revenue")).StatusCode
            .Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task SeedConfirmedBookingAsync(
        Guid tripId, Guid userId, decimal totalFare, decimal platformFee)
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
            TotalFare = totalFare,
            PlatformFee = platformFee,
            TotalAmount = totalFare + platformFee,
            SeatCount = 1,
            Status = BookingStatus.Confirmed,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            ConfirmedAt = DateTime.UtcNow.AddMinutes(-4)
        });
        db.BookingSeats.Add(new BookingSeat
        {
            Id = Guid.NewGuid(), BookingId = id, SeatNumber = "A1",
            PassengerName = "Pat", PassengerAge = 30, PassengerGender = PassengerGender.Male
        });
        db.Payments.Add(new Payment
        {
            Id = Guid.NewGuid(), BookingId = id,
            RazorpayOrderId = $"order_{Guid.NewGuid():N}"[..20],
            RazorpayPaymentId = $"pay_{Guid.NewGuid():N}"[..18],
            Amount = totalFare + platformFee, Currency = "INR",
            Status = PaymentStatus.Captured,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            CapturedAt = DateTime.UtcNow.AddMinutes(-4)
        });
        await db.SaveChangesAsync();
    }

    private async Task SeedCancelledBookingAsync(
        Guid tripId, Guid userId, decimal totalFare, decimal platformFee)
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
            TotalFare = totalFare,
            PlatformFee = platformFee,
            TotalAmount = totalFare + platformFee,
            SeatCount = 1,
            Status = BookingStatus.Cancelled,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            ConfirmedAt = DateTime.UtcNow.AddMinutes(-9),
            CancelledAt = DateTime.UtcNow.AddMinutes(-2),
            RefundAmount = (totalFare + platformFee) * 0.8m,
            RefundStatus = RefundStatus.Processed
        });
        db.BookingSeats.Add(new BookingSeat
        {
            Id = Guid.NewGuid(), BookingId = id, SeatNumber = "A2",
            PassengerName = "Pat", PassengerAge = 30, PassengerGender = PassengerGender.Male
        });
        await db.SaveChangesAsync();
    }
}
