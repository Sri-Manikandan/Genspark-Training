using System.Net;
using System.Net.Http.Json;
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Models;
using BusBooking.Api.Tests.Support;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BusBooking.Api.Tests.Integration;

[Collection("Integration")]
public class AdminOperatorsTests : IAsyncLifetime
{
    private readonly IntegrationFixture _fx;
    public AdminOperatorsTests(IntegrationFixture fx) { _fx = fx; }

    public Task InitializeAsync() => _fx.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task List_returns_operators_with_bus_counts_and_disabled_flag()
    {
        var seed = await TripTestSeed.CreateAsync(_fx, capacity: 4, daysAhead: 7);
        var (admin, adminToken) = await AdminTokenFactory.CreateAdminAsync(_fx);

        var client = _fx.CreateClient();
        client.AttachAdminBearer(adminToken);

        var resp = await client.GetFromJsonAsync<List<AdminOperatorListItemDto>>("/api/v1/admin/operators");
        resp!.Should().ContainSingle(o => o.UserId == seed.OperatorId);
        var row = resp.First(o => o.UserId == seed.OperatorId);
        row.TotalBuses.Should().Be(1);
        row.ActiveBuses.Should().Be(1);
        row.RetiredBuses.Should().Be(0);
        row.IsDisabled.Should().BeFalse();
        row.DisabledAt.Should().BeNull();
    }

    [Fact]
    public async Task Disable_cascades_retires_buses_cancels_future_bookings_and_refunds_in_full()
    {
        var seed = await TripTestSeed.CreateAsync(_fx, capacity: 4, daysAhead: 5);
        var (cust, _) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        var bookingId = await SeedConfirmedBookingAsync(seed.TripId, cust.Id, totalAmount: 1000m);

        var (admin, adminToken) = await AdminTokenFactory.CreateAdminAsync(_fx);
        var client = _fx.CreateClient();
        client.AttachAdminBearer(adminToken);

        var resp = await client.PostAsJsonAsync(
            $"/api/v1/admin/operators/{seed.OperatorId}/disable",
            new DisableOperatorRequest("violations"));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await resp.Content.ReadFromJsonAsync<AdminOperatorListItemDto>();
        result!.IsDisabled.Should().BeTrue();
        result.DisabledAt.Should().NotBeNull();
        result.RetiredBuses.Should().Be(1);
        result.ActiveBuses.Should().Be(0);

        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var bus = await db.Buses.FirstAsync(b => b.Id == seed.BusId);
        bus.OperationalStatus.Should().Be(BusOperationalStatus.Retired);

        var op = await db.Users.FirstAsync(u => u.Id == seed.OperatorId);
        op.OperatorDisabledAt.Should().NotBeNull();

        var booking = await db.Bookings.Include(b => b.Payment).FirstAsync(b => b.Id == bookingId);
        booking.Status.Should().Be(BookingStatus.CancelledByOperator);
        booking.CancelledAt.Should().NotBeNull();
        booking.RefundAmount.Should().Be(1000m);
        booking.RefundStatus.Should().Be(RefundStatus.Processed);
        booking.Payment!.Status.Should().Be(PaymentStatus.Refunded);

        _fx.Razorpay.CreatedRefunds.Should().ContainSingle()
            .Which.Amount.Should().Be(100000);

        _fx.Email.Sent.Should().Contain(e => e.Subject.Contains("disabled"));
        _fx.Email.Sent.Should().Contain(e => e.Subject.Contains("cancelled by operator"));
    }

    [Fact]
    public async Task Disable_leaves_past_and_pending_payment_bookings_untouched()
    {
        var seed = await TripTestSeed.CreateAsync(_fx, capacity: 4, daysAhead: 5);
        var (cust, _) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        var pendingId = await SeedPendingBookingAsync(seed.TripId, cust.Id);

        using (var scope = _fx.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var trip = await db.BusTrips.FirstAsync(t => t.Id == seed.TripId);
            trip.TripDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2));
            await db.SaveChangesAsync();
        }
        var pastBookingId = await SeedConfirmedBookingAsync(seed.TripId, cust.Id, totalAmount: 800m);

        var (admin, adminToken) = await AdminTokenFactory.CreateAdminAsync(_fx);
        var client = _fx.CreateClient();
        client.AttachAdminBearer(adminToken);

        var resp = await client.PostAsJsonAsync(
            $"/api/v1/admin/operators/{seed.OperatorId}/disable",
            new DisableOperatorRequest(null));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope2 = _fx.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db2.Bookings.FindAsync(pendingId))!.Status.Should().Be(BookingStatus.PendingPayment);
        (await db2.Bookings.FindAsync(pastBookingId))!.Status.Should().Be(BookingStatus.Confirmed);
        _fx.Razorpay.CreatedRefunds.Should().BeEmpty();
    }

    [Fact]
    public async Task Disable_is_idempotent_returns_current_state_without_double_refund()
    {
        var seed = await TripTestSeed.CreateAsync(_fx, capacity: 4, daysAhead: 5);
        var (cust, _) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        await SeedConfirmedBookingAsync(seed.TripId, cust.Id, totalAmount: 500m);

        var (admin, adminToken) = await AdminTokenFactory.CreateAdminAsync(_fx);
        var client = _fx.CreateClient();
        client.AttachAdminBearer(adminToken);

        var first = await client.PostAsJsonAsync(
            $"/api/v1/admin/operators/{seed.OperatorId}/disable",
            new DisableOperatorRequest(null));
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await client.PostAsJsonAsync(
            $"/api/v1/admin/operators/{seed.OperatorId}/disable",
            new DisableOperatorRequest(null));
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        _fx.Razorpay.CreatedRefunds.Should().ContainSingle();
    }

    [Fact]
    public async Task Enable_clears_disabled_flag_but_does_not_reinstate_bookings()
    {
        var seed = await TripTestSeed.CreateAsync(_fx, capacity: 4, daysAhead: 5);
        var (cust, _) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        var bookingId = await SeedConfirmedBookingAsync(seed.TripId, cust.Id, totalAmount: 700m);

        var (admin, adminToken) = await AdminTokenFactory.CreateAdminAsync(_fx);
        var client = _fx.CreateClient();
        client.AttachAdminBearer(adminToken);

        (await client.PostAsJsonAsync(
            $"/api/v1/admin/operators/{seed.OperatorId}/disable",
            new DisableOperatorRequest(null))).EnsureSuccessStatusCode();

        var enableResp = await client.PostAsync(
            $"/api/v1/admin/operators/{seed.OperatorId}/enable", content: null);
        enableResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var after = await enableResp.Content.ReadFromJsonAsync<AdminOperatorListItemDto>();
        after!.IsDisabled.Should().BeFalse();
        after.DisabledAt.Should().BeNull();

        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Bookings.FindAsync(bookingId))!.Status.Should().Be(BookingStatus.CancelledByOperator);
        (await db.Buses.FindAsync(seed.BusId))!.OperationalStatus.Should().Be(BusOperationalStatus.Retired);
    }

    [Fact]
    public async Task Disable_unknown_operator_returns_404()
    {
        var (_, adminToken) = await AdminTokenFactory.CreateAdminAsync(_fx);
        var client = _fx.CreateClient();
        client.AttachAdminBearer(adminToken);

        var resp = await client.PostAsJsonAsync(
            $"/api/v1/admin/operators/{Guid.NewGuid()}/disable",
            new DisableOperatorRequest(null));
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Requires_admin_role()
    {
        var (_, custToken) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        var client = _fx.CreateClient();
        client.AttachBearer(custToken);

        (await client.GetAsync("/api/v1/admin/operators")).StatusCode
            .Should().Be(HttpStatusCode.Forbidden);
        (await client.PostAsJsonAsync(
            $"/api/v1/admin/operators/{Guid.NewGuid()}/disable",
            new DisableOperatorRequest(null))).StatusCode
            .Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<Guid> SeedConfirmedBookingAsync(Guid tripId, Guid userId, decimal totalAmount)
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
            TotalFare = totalAmount - 25m,
            PlatformFee = 25m,
            TotalAmount = totalAmount,
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
            Amount = totalAmount, Currency = "INR",
            Status = PaymentStatus.Captured,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            CapturedAt = DateTime.UtcNow.AddMinutes(-4)
        });
        await db.SaveChangesAsync();
        return id;
    }

    private async Task<Guid> SeedPendingBookingAsync(Guid tripId, Guid userId)
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
            TotalFare = 475m,
            PlatformFee = 25m,
            TotalAmount = 500m,
            SeatCount = 1,
            Status = BookingStatus.PendingPayment,
            CreatedAt = DateTime.UtcNow
        });
        db.BookingSeats.Add(new BookingSeat
        {
            Id = Guid.NewGuid(), BookingId = id, SeatNumber = "A2",
            PassengerName = "Pat", PassengerAge = 30, PassengerGender = PassengerGender.Male
        });
        await db.SaveChangesAsync();
        return id;
    }
}
