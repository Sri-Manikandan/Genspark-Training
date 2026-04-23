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
public class BookingRefundPreviewTests : IAsyncLifetime
{
    private readonly IntegrationFixture _fx;
    public BookingRefundPreviewTests(IntegrationFixture fx) { _fx = fx; }

    public Task InitializeAsync() => _fx.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Preview_returns_80_percent_for_far_future_trip()
    {
        // Trip 7 days ahead at 22:00 → 168h+ ⇒ 80%
        var seed = await TripTestSeed.CreateAsync(_fx, capacity: 6, daysAhead: 7);
        var (cust, custToken) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        var bookingId = await SeedConfirmedBookingAsync(seed.TripId, cust.Id, fare: 1000m);

        var client = _fx.CreateClient();
        client.AttachBearer(custToken);

        var preview = await client.GetFromJsonAsync<RefundPreviewDto>(
            $"/api/v1/bookings/{bookingId}/refund-preview");

        preview!.Cancellable.Should().BeTrue();
        preview.RefundPercent.Should().Be(80);
        preview.RefundAmount.Should().Be(800m);
        preview.BlockReason.Should().BeNull();
    }

    [Fact]
    public async Task Preview_blocks_inside_12h_window()
    {
        var seed = await TripTestSeed.CreateAsync(_fx, capacity: 6, daysAhead: 0);
        await SetScheduleDepartureNowPlusHoursAsync(seed.ScheduleId, hours: 1);

        var (cust, custToken) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        var bookingId = await SeedConfirmedBookingAsync(seed.TripId, cust.Id, fare: 1000m);

        var client = _fx.CreateClient();
        client.AttachBearer(custToken);

        var preview = await client.GetFromJsonAsync<RefundPreviewDto>(
            $"/api/v1/bookings/{bookingId}/refund-preview");

        preview!.Cancellable.Should().BeFalse();
        preview.RefundPercent.Should().Be(0);
        preview.RefundAmount.Should().Be(0m);
        preview.BlockReason.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Preview_403_for_other_user_booking()
    {
        var seed = await TripTestSeed.CreateAsync(_fx, capacity: 6, daysAhead: 7);
        var (owner, _) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        var (_, otherToken) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        var bookingId = await SeedConfirmedBookingAsync(seed.TripId, owner.Id, fare: 500m);

        var client = _fx.CreateClient();
        client.AttachBearer(otherToken);

        var resp = await client.GetAsync($"/api/v1/bookings/{bookingId}/refund-preview");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Preview_404_when_booking_missing()
    {
        var (_, custToken) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        var client = _fx.CreateClient();
        client.AttachBearer(custToken);
        var resp = await client.GetAsync($"/api/v1/bookings/{Guid.NewGuid()}/refund-preview");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<Guid> SeedConfirmedBookingAsync(Guid tripId, Guid userId, decimal fare)
    {
        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bookingId = Guid.NewGuid();
        db.Bookings.Add(new Booking
        {
            Id = bookingId,
            BookingCode = $"BK-{Guid.NewGuid():N}".Substring(0, 11),
            TripId = tripId,
            UserId = userId,
            LockId = Guid.NewGuid(),
            TotalFare = fare,
            PlatformFee = 0m,
            TotalAmount = fare,
            SeatCount = 1,
            Status = BookingStatus.Confirmed,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            ConfirmedAt = DateTime.UtcNow.AddMinutes(-4)
        });
        db.BookingSeats.Add(new BookingSeat
        {
            Id = Guid.NewGuid(),
            BookingId = bookingId,
            SeatNumber = "A1",
            PassengerName = "Test Passenger",
            PassengerAge = 30,
            PassengerGender = PassengerGender.Male
        });
        db.Payments.Add(new Payment
        {
            Id = Guid.NewGuid(),
            BookingId = bookingId,
            RazorpayOrderId = "order_" + Guid.NewGuid().ToString("N")[..14],
            RazorpayPaymentId = "pay_" + Guid.NewGuid().ToString("N")[..14],
            Amount = fare,
            Currency = "INR",
            Status = PaymentStatus.Captured,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            CapturedAt = DateTime.UtcNow.AddMinutes(-4)
        });
        await db.SaveChangesAsync();
        return bookingId;
    }

    private async Task SetScheduleDepartureNowPlusHoursAsync(Guid scheduleId, int hours)
    {
        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sched = await db.BusSchedules.FindAsync(scheduleId);
        sched!.DepartureTime = TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(hours));
        await db.SaveChangesAsync();
    }
}
