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
public class AdminBookingsTests : IAsyncLifetime
{
    private readonly IntegrationFixture _fx;
    public AdminBookingsTests(IntegrationFixture fx) { _fx = fx; }

    public Task InitializeAsync() => _fx.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Returns_bookings_across_all_operators()
    {
        var seed1 = await TripTestSeed.CreateAsync(_fx, capacity: 4, daysAhead: 3);
        var seed2 = await TripTestSeed.CreateAsync(_fx, capacity: 4, daysAhead: 3);
        var (cust, _) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);

        await SeedConfirmedBookingAsync(seed1.TripId, cust.Id);
        await SeedConfirmedBookingAsync(seed2.TripId, cust.Id);

        var (admin, adminToken) = await AdminTokenFactory.CreateAdminAsync(_fx);
        var client = _fx.CreateClient();
        client.AttachAdminBearer(adminToken);

        var resp = await client.GetFromJsonAsync<AdminBookingListResponseDto>(
            "/api/v1/admin/bookings");
        resp!.TotalCount.Should().Be(2);
        resp.Items.Select(i => i.OperatorUserId).Distinct().Should().HaveCount(2);
    }

    [Fact]
    public async Task Filters_by_operator()
    {
        var seed1 = await TripTestSeed.CreateAsync(_fx, capacity: 4, daysAhead: 3);
        var seed2 = await TripTestSeed.CreateAsync(_fx, capacity: 4, daysAhead: 3);
        var (cust, _) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        await SeedConfirmedBookingAsync(seed1.TripId, cust.Id);
        await SeedConfirmedBookingAsync(seed2.TripId, cust.Id);

        var (_, adminToken) = await AdminTokenFactory.CreateAdminAsync(_fx);
        var client = _fx.CreateClient();
        client.AttachAdminBearer(adminToken);

        var resp = await client.GetFromJsonAsync<AdminBookingListResponseDto>(
            $"/api/v1/admin/bookings?operatorUserId={seed1.OperatorId}");
        resp!.TotalCount.Should().Be(1);
        resp.Items[0].OperatorUserId.Should().Be(seed1.OperatorId);
    }

    [Fact]
    public async Task Filters_by_status()
    {
        var seed = await TripTestSeed.CreateAsync(_fx, capacity: 4, daysAhead: 3);
        var (cust, _) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        await SeedConfirmedBookingAsync(seed.TripId, cust.Id);
        await SeedCancelledBookingAsync(seed.TripId, cust.Id);

        var (_, adminToken) = await AdminTokenFactory.CreateAdminAsync(_fx);
        var client = _fx.CreateClient();
        client.AttachAdminBearer(adminToken);

        var resp = await client.GetFromJsonAsync<AdminBookingListResponseDto>(
            $"/api/v1/admin/bookings?status={BookingStatus.Cancelled}");
        resp!.TotalCount.Should().Be(1);
        resp.Items[0].Status.Should().Be(BookingStatus.Cancelled);
    }

    [Fact]
    public async Task Excludes_pending_payment_by_default()
    {
        var seed = await TripTestSeed.CreateAsync(_fx, capacity: 4, daysAhead: 3);
        var (cust, _) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        await SeedConfirmedBookingAsync(seed.TripId, cust.Id);

        using (var scope = _fx.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Bookings.Add(new Booking
            {
                Id = Guid.NewGuid(), BookingCode = "BK-PENDING",
                TripId = seed.TripId, UserId = cust.Id, LockId = Guid.NewGuid(),
                TotalFare = 100m, PlatformFee = 10m, TotalAmount = 110m,
                SeatCount = 1, Status = BookingStatus.PendingPayment,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var (_, adminToken) = await AdminTokenFactory.CreateAdminAsync(_fx);
        var client = _fx.CreateClient();
        client.AttachAdminBearer(adminToken);

        var resp = await client.GetFromJsonAsync<AdminBookingListResponseDto>(
            "/api/v1/admin/bookings");
        resp!.TotalCount.Should().Be(1);
        resp.Items[0].Status.Should().Be(BookingStatus.Confirmed);
    }

    [Fact]
    public async Task Requires_admin_role()
    {
        var (_, custToken) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        var client = _fx.CreateClient();
        client.AttachBearer(custToken);

        (await client.GetAsync("/api/v1/admin/bookings")).StatusCode
            .Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task SeedConfirmedBookingAsync(Guid tripId, Guid userId)
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
            TotalFare = 500m, PlatformFee = 25m, TotalAmount = 525m,
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
        await db.SaveChangesAsync();
    }

    private async Task SeedCancelledBookingAsync(Guid tripId, Guid userId)
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
            TotalFare = 500m, PlatformFee = 25m, TotalAmount = 525m,
            SeatCount = 1,
            Status = BookingStatus.Cancelled,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            ConfirmedAt = DateTime.UtcNow.AddMinutes(-9),
            CancelledAt = DateTime.UtcNow.AddMinutes(-1),
            RefundAmount = 420m, RefundStatus = RefundStatus.Processed
        });
        db.BookingSeats.Add(new BookingSeat
        {
            Id = Guid.NewGuid(), BookingId = id, SeatNumber = "A2",
            PassengerName = "Pat", PassengerAge = 30, PassengerGender = PassengerGender.Male
        });
        await db.SaveChangesAsync();
    }
}
