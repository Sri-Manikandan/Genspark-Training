using System.Net;
using System.Net.Http.Json;
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Infrastructure.Errors;
using BusBooking.Api.Models;
using BusBooking.Api.Tests.Support;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BusBooking.Api.Tests.Integration;

[Collection("Integration")]
public class BookingCancellationTests : IAsyncLifetime
{
    private readonly IntegrationFixture _fx;
    public BookingCancellationTests(IntegrationFixture fx) { _fx = fx; }

    public Task InitializeAsync() => _fx.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Cancel_far_future_trip_refunds_80_percent_and_emails()
    {
        var seed = await TripTestSeed.CreateAsync(_fx, capacity: 6, daysAhead: 7);
        var (cust, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        var bookingId = await SeedConfirmedBookingAsync(seed.TripId, cust.Id, totalAmount: 1000m);

        var client = _fx.CreateClient();
        client.AttachBearer(token);

        var resp = await client.PostAsJsonAsync(
            $"/api/v1/bookings/{bookingId}/cancel",
            new CancelBookingRequest("changed plans"));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await resp.Content.ReadFromJsonAsync<BookingDetailDto>();
        detail!.Status.Should().Be(BookingStatus.Cancelled);
        detail.RefundAmount.Should().Be(800m);
        detail.RefundStatus.Should().Be(RefundStatus.Processed);
        detail.CancelledAt.Should().NotBeNull();
        detail.CancellationReason.Should().Be("changed plans");

        _fx.Razorpay.CreatedRefunds.Should().ContainSingle()
            .Which.Amount.Should().Be(80000); // paise

        _fx.Email.Sent.Should().ContainSingle(e => e.Subject.Contains("cancelled"));

        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dbBooking = await db.Bookings.Include(b => b.Payment).FirstAsync(b => b.Id == bookingId);
        dbBooking.Payment!.Status.Should().Be(PaymentStatus.Refunded);
        dbBooking.Payment.RefundedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Cancel_blocked_within_12h_returns_422()
    {
        var seed = await TripTestSeed.CreateAsync(_fx, capacity: 6, daysAhead: 0);
        await SetScheduleDepartureNowPlusHoursAsync(seed.ScheduleId, hours: 4);
        var (cust, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        var bookingId = await SeedConfirmedBookingAsync(seed.TripId, cust.Id, totalAmount: 1000m);

        var client = _fx.CreateClient();
        client.AttachBearer(token);

        var resp = await client.PostAsJsonAsync(
            $"/api/v1/bookings/{bookingId}/cancel",
            new CancelBookingRequest(null));
        resp.StatusCode.Should().Be((HttpStatusCode)422);
        var body = await resp.Content.ReadFromJsonAsync<ErrorResponse>();
        body!.Error.Code.Should().Be("CANCEL_WINDOW_CLOSED");

        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dbBooking = await db.Bookings.FirstAsync(b => b.Id == bookingId);
        dbBooking.Status.Should().Be(BookingStatus.Confirmed);
        _fx.Razorpay.CreatedRefunds.Should().BeEmpty();
    }

    [Fact]
    public async Task Cancel_idempotent_returns_current_state_no_double_refund()
    {
        var seed = await TripTestSeed.CreateAsync(_fx, capacity: 6, daysAhead: 7);
        var (cust, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        var bookingId = await SeedConfirmedBookingAsync(seed.TripId, cust.Id, totalAmount: 1000m);

        var client = _fx.CreateClient();
        client.AttachBearer(token);

        var first = await client.PostAsJsonAsync(
            $"/api/v1/bookings/{bookingId}/cancel", new CancelBookingRequest("first"));
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await client.PostAsJsonAsync(
            $"/api/v1/bookings/{bookingId}/cancel", new CancelBookingRequest("second"));
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await second.Content.ReadFromJsonAsync<BookingDetailDto>();
        detail!.CancellationReason.Should().Be("first"); // unchanged
        detail.Status.Should().Be(BookingStatus.Cancelled);

        _fx.Razorpay.CreatedRefunds.Should().HaveCount(1);
        _fx.Email.Sent.Count(e => e.Subject.Contains("cancelled")).Should().Be(1);
    }

    [Fact]
    public async Task Cancel_other_users_booking_403()
    {
        var seed = await TripTestSeed.CreateAsync(_fx, capacity: 6, daysAhead: 7);
        var (owner, _) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        var (_, otherToken) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        var bookingId = await SeedConfirmedBookingAsync(seed.TripId, owner.Id, totalAmount: 500m);

        var client = _fx.CreateClient();
        client.AttachBearer(otherToken);

        var resp = await client.PostAsJsonAsync(
            $"/api/v1/bookings/{bookingId}/cancel", new CancelBookingRequest(null));
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Cancel_marks_refund_failed_when_razorpay_throws()
    {
        var seed = await TripTestSeed.CreateAsync(_fx, capacity: 6, daysAhead: 7);
        var (cust, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        var bookingId = await SeedConfirmedBookingAsync(seed.TripId, cust.Id, totalAmount: 1000m);
        _fx.Razorpay.ThrowOnRefund = true;

        try
        {
            var client = _fx.CreateClient();
            client.AttachBearer(token);

            var resp = await client.PostAsJsonAsync(
                $"/api/v1/bookings/{bookingId}/cancel", new CancelBookingRequest(null));
            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            var detail = await resp.Content.ReadFromJsonAsync<BookingDetailDto>();
            detail!.Status.Should().Be(BookingStatus.Cancelled);
            detail.RefundStatus.Should().Be(RefundStatus.Failed);
        }
        finally
        {
            _fx.Razorpay.ThrowOnRefund = false;
        }
    }

    [Fact]
    public async Task Cancel_frees_seat_for_subsequent_search()
    {
        var seed = await TripTestSeed.CreateAsync(_fx, capacity: 2, daysAhead: 7);
        var (cust, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        var bookingId = await SeedConfirmedBookingAsync(seed.TripId, cust.Id, totalAmount: 500m, seatNumber: "A1");

        var client = _fx.CreateClient();
        var beforeLayout = await client.GetFromJsonAsync<SeatLayoutDto>(
            $"/api/v1/trips/{seed.TripId}/seats");
        beforeLayout!.Seats.Should().Contain(s => s.SeatNumber == "A1" && s.Status == "booked");

        client.AttachBearer(token);
        var cancelResp = await client.PostAsJsonAsync(
            $"/api/v1/bookings/{bookingId}/cancel", new CancelBookingRequest(null));
        cancelResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterLayout = await client.GetFromJsonAsync<SeatLayoutDto>(
            $"/api/v1/trips/{seed.TripId}/seats");
        afterLayout!.Seats.Should().Contain(s => s.SeatNumber == "A1" && s.Status == "available");
    }

    private async Task<Guid> SeedConfirmedBookingAsync(
        Guid tripId, Guid userId, decimal totalAmount, string seatNumber = "A1")
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
            TotalFare = totalAmount,
            PlatformFee = 0m,
            TotalAmount = totalAmount,
            SeatCount = 1,
            Status = BookingStatus.Confirmed,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            ConfirmedAt = DateTime.UtcNow.AddMinutes(-4)
        });
        db.BookingSeats.Add(new BookingSeat
        {
            Id = Guid.NewGuid(),
            BookingId = bookingId,
            SeatNumber = seatNumber,
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
            Amount = totalAmount,
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
