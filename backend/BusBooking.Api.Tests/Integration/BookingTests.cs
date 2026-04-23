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
public class BookingTests : IAsyncLifetime
{
    private readonly IntegrationFixture _fx;
    public BookingTests(IntegrationFixture fx) => _fx = fx;
    public async Task InitializeAsync() => await _fx.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task HappyPath_CreateVerify_ConfirmsBookingAndSendsEmail()
    {
        var trip = await TripTestSeed.CreateAsync(_fx);
        var (customer, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        var client = _fx.CreateClient(); client.AttachBearer(token);

        var sessionId = Guid.NewGuid();
        var lockResp = await client.PostAsJsonAsync(
            $"/api/v1/trips/{trip.TripId}/seat-locks",
            new LockSeatsRequest(sessionId, new List<string> { "A1" }));
        lockResp.EnsureSuccessStatusCode();
        var lockDto = await lockResp.Content.ReadFromJsonAsync<SeatLockResponseDto>();

        var createResp = await client.PostAsJsonAsync("/api/v1/bookings",
            new CreateBookingRequest(trip.TripId, lockDto!.LockId, sessionId,
                new List<PassengerDto>
                {
                    new("A1", "Asha", 30, PassengerGender.Female)
                }));
        createResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = await createResp.Content.ReadFromJsonAsync<CreateBookingResponseDto>();
        created!.KeyId.Should().Be(FakeRazorpayClient.TestKeyId);
        _fx.Razorpay.CreatedOrders.Should().ContainKey(created.RazorpayOrderId);

        var paymentId = "pay_" + Guid.NewGuid().ToString("N")[..10];
        var signature = FakeRazorpayClient.BuildSignature(created.RazorpayOrderId, paymentId);

        var verifyResp = await client.PostAsJsonAsync(
            $"/api/v1/bookings/{created.BookingId}/verify-payment",
            new VerifyPaymentRequest(paymentId, signature));
        verifyResp.EnsureSuccessStatusCode();
        var detail = await verifyResp.Content.ReadFromJsonAsync<BookingDetailDto>();

        detail!.Status.Should().Be(BookingStatus.Confirmed);
        detail.Seats.Should().ContainSingle(s => s.SeatNumber == "A1");
        detail.ConfirmedAt.Should().NotBeNull();

        _fx.Email.Sent.Should().ContainSingle(e => e.Subject.Contains(detail.BookingCode));

        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.SeatLocks.CountAsync(l => l.LockId == lockDto.LockId)).Should().Be(0);
        (await db.Notifications.CountAsync(n => n.UserId == customer.Id)).Should().Be(1);
    }

    [Fact]
    public async Task VerifyPayment_InvalidSignature_Returns422()
    {
        var (created, _, _, client, _) = await CreateBookingAsync();

        var verifyResp = await client.PostAsJsonAsync(
            $"/api/v1/bookings/{created.BookingId}/verify-payment",
            new VerifyPaymentRequest("pay_whatever", "badsignature"));
        verifyResp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateBooking_WithExpiredLock_Returns409()
    {
        var trip = await TripTestSeed.CreateAsync(_fx);
        var (_, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        var client = _fx.CreateClient(); client.AttachBearer(token);

        Guid lockId;
        Guid sessionId = Guid.NewGuid();
        using (var scope = _fx.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            lockId = Guid.NewGuid();
            db.SeatLocks.Add(new SeatLock
            {
                Id         = Guid.NewGuid(),
                TripId     = trip.TripId,
                SeatNumber = "A1",
                LockId     = lockId,
                SessionId  = sessionId,
                CreatedAt  = DateTime.UtcNow.AddMinutes(-10),
                ExpiresAt  = DateTime.UtcNow.AddMinutes(-3)
            });
            await db.SaveChangesAsync();
        }

        var resp = await client.PostAsJsonAsync("/api/v1/bookings",
            new CreateBookingRequest(trip.TripId, lockId, sessionId,
                new List<PassengerDto>
                {
                    new("A1", "Asha", 30, PassengerGender.Female)
                }));
        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateBooking_WithWrongSession_Returns403()
    {
        var trip = await TripTestSeed.CreateAsync(_fx);
        var (_, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        var client = _fx.CreateClient(); client.AttachBearer(token);

        var holderSession = Guid.NewGuid();
        var lockResp = await client.PostAsJsonAsync(
            $"/api/v1/trips/{trip.TripId}/seat-locks",
            new LockSeatsRequest(holderSession, new List<string> { "A1" }));
        var lockDto = await lockResp.Content.ReadFromJsonAsync<SeatLockResponseDto>();

        var resp = await client.PostAsJsonAsync("/api/v1/bookings",
            new CreateBookingRequest(trip.TripId, lockDto!.LockId, Guid.NewGuid(),
                new List<PassengerDto>
                {
                    new("A1", "Asha", 30, PassengerGender.Female)
                }));
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task VerifyPayment_IdempotentOnRepeat_ButDifferentPaymentIdReturns409()
    {
        var (created, _, _, client, _) = await CreateBookingAsync();
        var paymentId = "pay_idem";
        var sig = FakeRazorpayClient.BuildSignature(created.RazorpayOrderId, paymentId);

        var r1 = await client.PostAsJsonAsync(
            $"/api/v1/bookings/{created.BookingId}/verify-payment",
            new VerifyPaymentRequest(paymentId, sig));
        r1.EnsureSuccessStatusCode();

        var r2 = await client.PostAsJsonAsync(
            $"/api/v1/bookings/{created.BookingId}/verify-payment",
            new VerifyPaymentRequest(paymentId, sig));
        r2.EnsureSuccessStatusCode();

        var r3 = await client.PostAsJsonAsync(
            $"/api/v1/bookings/{created.BookingId}/verify-payment",
            new VerifyPaymentRequest("pay_different", sig));
        r3.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetTicketPdf_ReturnsPdfForConfirmedBooking()
    {
        var (created, _, _, client, _) = await CreateBookingAsync();
        var paymentId = "pay_ticket";
        var sig = FakeRazorpayClient.BuildSignature(created.RazorpayOrderId, paymentId);

        (await client.PostAsJsonAsync(
            $"/api/v1/bookings/{created.BookingId}/verify-payment",
            new VerifyPaymentRequest(paymentId, sig))).EnsureSuccessStatusCode();

        var resp = await client.GetAsync($"/api/v1/bookings/{created.BookingId}/ticket");
        resp.EnsureSuccessStatusCode();
        resp.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");

        var bytes = await resp.Content.ReadAsByteArrayAsync();
        bytes.Length.Should().BeGreaterThan(500);
        System.Text.Encoding.ASCII.GetString(bytes, 0, 5).Should().Be("%PDF-");
    }

    [Fact]
    public async Task GetTicketPdf_BeforePayment_Returns422()
    {
        var (created, _, _, client, _) = await CreateBookingAsync();
        var resp = await client.GetAsync($"/api/v1/bookings/{created.BookingId}/ticket");
        resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task AfterConfirm_ReusingLockFails()
    {
        // After verify-payment, the seat_locks with that LockId are deleted. A second POST
        // /bookings carrying that lockId must fail with LOCK_EXPIRED.
        var (first, lockDto, sessionId, client, tripId) = await CreateBookingAsync();
        var paymentId = "pay_first";
        var sig = FakeRazorpayClient.BuildSignature(first.RazorpayOrderId, paymentId);

        (await client.PostAsJsonAsync(
            $"/api/v1/bookings/{first.BookingId}/verify-payment",
            new VerifyPaymentRequest(paymentId, sig))).EnsureSuccessStatusCode();

        var second = await client.PostAsJsonAsync("/api/v1/bookings",
            new CreateBookingRequest(tripId, lockDto.LockId, sessionId,
                new List<PassengerDto>
                {
                    new("A1", "Other", 25, PassengerGender.Male)
                }));
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task SearchAfterConfirm_SeatsLeftDecrements()
    {
        var trip = await TripTestSeed.CreateAsync(_fx, capacity: 5);
        var (_, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        var client = _fx.CreateClient(); client.AttachBearer(token);

        var sessionId = Guid.NewGuid();
        var lockDto = (await (await client.PostAsJsonAsync(
            $"/api/v1/trips/{trip.TripId}/seat-locks",
            new LockSeatsRequest(sessionId, new List<string> { "A1" })))
            .Content.ReadFromJsonAsync<SeatLockResponseDto>())!;

        var created = (await (await client.PostAsJsonAsync("/api/v1/bookings",
            new CreateBookingRequest(trip.TripId, lockDto.LockId, sessionId,
                new List<PassengerDto>
                {
                    new("A1", "Asha", 30, PassengerGender.Female)
                })))
            .Content.ReadFromJsonAsync<CreateBookingResponseDto>())!;

        // Between create and verify, layout should still show A1 as booked (not double-available)
        // and seatsLeft should not be double-decremented.
        var midDetail = await _fx.Client.GetFromJsonAsync<TripDetailDto>($"/api/v1/trips/{trip.TripId}");
        midDetail!.SeatsLeft.Should().Be(4);

        var sig = FakeRazorpayClient.BuildSignature(created.RazorpayOrderId, "pay_s");
        (await client.PostAsJsonAsync(
            $"/api/v1/bookings/{created.BookingId}/verify-payment",
            new VerifyPaymentRequest("pay_s", sig))).EnsureSuccessStatusCode();

        var afterDetail = await _fx.Client.GetFromJsonAsync<TripDetailDto>($"/api/v1/trips/{trip.TripId}");
        afterDetail!.SeatsLeft.Should().Be(4);
        afterDetail.SeatLayout.Seats.First(s => s.SeatNumber == "A1").Status.Should().Be("booked");
    }

    private async Task<(CreateBookingResponseDto booking, SeatLockResponseDto lockDto, Guid sessionId, HttpClient client, Guid tripId)>
        CreateBookingAsync()
    {
        var trip = await TripTestSeed.CreateAsync(_fx);
        var (_, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        var client = _fx.CreateClient(); client.AttachBearer(token);

        var sessionId = Guid.NewGuid();
        var lockResp = await client.PostAsJsonAsync(
            $"/api/v1/trips/{trip.TripId}/seat-locks",
            new LockSeatsRequest(sessionId, new List<string> { "A1" }));
        lockResp.EnsureSuccessStatusCode();
        var lockDto = (await lockResp.Content.ReadFromJsonAsync<SeatLockResponseDto>())!;

        var createResp = await client.PostAsJsonAsync("/api/v1/bookings",
            new CreateBookingRequest(trip.TripId, lockDto.LockId, sessionId,
                new List<PassengerDto>
                {
                    new("A1", "Asha", 30, PassengerGender.Female)
                }));
        createResp.EnsureSuccessStatusCode();
        var created = (await createResp.Content.ReadFromJsonAsync<CreateBookingResponseDto>())!;

        return (created, lockDto, sessionId, client, trip.TripId);
    }
}
