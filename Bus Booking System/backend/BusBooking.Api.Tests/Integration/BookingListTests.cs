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
public class BookingListTests : IAsyncLifetime
{
    private readonly IntegrationFixture _fx;
    public BookingListTests(IntegrationFixture fx) { _fx = fx; }

    public Task InitializeAsync() => _fx.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Lists_only_callers_bookings_filtered_by_upcoming()
    {
        var seed = await TripTestSeed.CreateAsync(_fx, capacity: 6, daysAhead: 7);
        var (cust, custToken) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        var (other, _) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);

        // Two confirmed bookings for cust on the same trip-date (upcoming) + one for other
        await SeedConfirmedBookingAsync(seed.TripId, cust.Id, ["A1"]);
        await SeedConfirmedBookingAsync(seed.TripId, cust.Id, ["A2"]);
        await SeedConfirmedBookingAsync(seed.TripId, other.Id, ["A3"]);

        var client = _fx.CreateClient();
        client.AttachBearer(custToken);

        var resp = await client.GetAsync("/api/v1/bookings?filter=upcoming");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await resp.Content.ReadFromJsonAsync<BookingListResponseDto>();
        body!.Items.Should().HaveCount(2);
        body.Items.Should().OnlyContain(i => i.Status == BookingStatus.Confirmed);
        body.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Past_filter_returns_trips_in_the_past()
    {
        var oldSeed = await TripTestSeed.CreateAsync(_fx, capacity: 6, daysAhead: -3);
        var freshSeed = await TripTestSeed.CreateAsync(_fx, capacity: 6, daysAhead: 5);
        var (cust, custToken) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);

        await SeedConfirmedBookingAsync(oldSeed.TripId, cust.Id, ["A1"]);
        await SeedConfirmedBookingAsync(freshSeed.TripId, cust.Id, ["A1"]);

        var client = _fx.CreateClient();
        client.AttachBearer(custToken);

        var resp = await client.GetFromJsonAsync<BookingListResponseDto>("/api/v1/bookings?filter=past");

        resp!.Items.Should().HaveCount(1);
        resp.Items[0].TripDate.Should().BeBefore(DateOnly.FromDateTime(DateTime.UtcNow));
    }

    [Fact]
    public async Task Cancelled_filter_returns_only_cancelled()
    {
        var seed = await TripTestSeed.CreateAsync(_fx, capacity: 6, daysAhead: 7);
        var (cust, custToken) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);

        await SeedConfirmedBookingAsync(seed.TripId, cust.Id, ["A1"]);
        var cancelledId = await SeedConfirmedBookingAsync(seed.TripId, cust.Id, ["A2"]);
        await MarkCancelledAsync(cancelledId);

        var client = _fx.CreateClient();
        client.AttachBearer(custToken);

        var resp = await client.GetFromJsonAsync<BookingListResponseDto>("/api/v1/bookings?filter=cancelled");

        resp!.Items.Should().HaveCount(1);
        resp.Items[0].Status.Should().Be(BookingStatus.Cancelled);
    }

    [Fact]
    public async Task Requires_customer_role()
    {
        var (_, opToken) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Operator]);
        var client = _fx.CreateClient();
        client.AttachBearer(opToken);

        var resp = await client.GetAsync("/api/v1/bookings?filter=upcoming");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Defaults_to_upcoming_when_no_filter_supplied()
    {
        var seed = await TripTestSeed.CreateAsync(_fx, capacity: 6, daysAhead: 7);
        var (cust, custToken) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        await SeedConfirmedBookingAsync(seed.TripId, cust.Id, ["A1"]);

        var client = _fx.CreateClient();
        client.AttachBearer(custToken);

        var resp = await client.GetFromJsonAsync<BookingListResponseDto>("/api/v1/bookings");
        resp!.Items.Should().HaveCount(1);
    }

    private async Task<Guid> SeedConfirmedBookingAsync(Guid tripId, Guid userId, string[] seatNumbers)
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
            TotalFare = 500m * seatNumbers.Length,
            PlatformFee = 25m,
            TotalAmount = 500m * seatNumbers.Length + 25m,
            SeatCount = seatNumbers.Length,
            Status = BookingStatus.Confirmed,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            ConfirmedAt = DateTime.UtcNow.AddMinutes(-4)
        });
        foreach (var s in seatNumbers)
        {
            db.BookingSeats.Add(new BookingSeat
            {
                Id = Guid.NewGuid(),
                BookingId = bookingId,
                SeatNumber = s,
                PassengerName = "Test Passenger",
                PassengerAge = 30,
                PassengerGender = PassengerGender.Male
            });
        }
        db.Payments.Add(new Payment
        {
            Id = Guid.NewGuid(),
            BookingId = bookingId,
            RazorpayOrderId = "order_" + Guid.NewGuid().ToString("N")[..14],
            RazorpayPaymentId = "pay_" + Guid.NewGuid().ToString("N")[..14],
            Amount = 500m * seatNumbers.Length + 25m,
            Currency = "INR",
            Status = PaymentStatus.Captured,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            CapturedAt = DateTime.UtcNow.AddMinutes(-4)
        });
        await db.SaveChangesAsync();
        return bookingId;
    }

    private async Task MarkCancelledAsync(Guid bookingId)
    {
        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var b = await db.Bookings.FindAsync(bookingId);
        b!.Status = BookingStatus.Cancelled;
        b.CancelledAt = DateTime.UtcNow;
        b.CancellationReason = "test";
        b.RefundAmount = 0m;
        b.RefundStatus = RefundStatus.Processed;
        await db.SaveChangesAsync();
    }
}
