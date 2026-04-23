# M6 — Bookings List, User Cancellation & Refund Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. **Work directly on `main` — do NOT create a feature branch.** Commit messages MUST NOT include a `Co-Authored-By: Claude` trailer.

**Goal:** Deliver the M6 demoable outcome: a logged-in customer opens a "My Bookings" page with three tabs (Upcoming / Past / Cancelled), opens a booking, sees the projected refund for cancelling now, confirms cancellation, and receives an email confirming the cancellation and refund. The Razorpay test-mode refund API is called for realism.

**Architecture:** Three new endpoints on `BookingsController` (`GET /bookings`, `GET /bookings/{id}/refund-preview`, `POST /bookings/{id}/cancel`) backed by three new methods on `IBookingService`. Refund tier maths lives in a single-purpose `RefundPolicyService` driven by `appsettings.RefundPolicy` (the section is already there, never wired). `IRazorpayClient` gains a `CreateRefundAsync` call, mirrored in the test fake. `BookingService.CancelAsync` runs the state mutation in one DB transaction (booking → `cancelled`, refund_amount snapshotted, refund_status `pending`) then makes the Razorpay refund + cancellation email post-commit (so an external outage cannot strand the booking in a half-cancelled state). `BookingDetailDto` is widened with `cancelledAt`, `cancellationReason`, `refundAmount`, `refundStatus` so the same record drives the new UI. Frontend gains a `BookingsApiService` extension, `BookingStatusBadgeComponent`, `BookingsListPageComponent` (mat-tabs over `MatTable`), `BookingDetailPageComponent`, and a `CancelBookingDialogComponent` (refund preview + reason input). The "My Bookings" link is added to the navbar; the existing confirmation screen gets a link back to the list.

**Tech Stack:** .NET 9 · EF Core 9 · FluentValidation · `IOptions<RefundPolicyOptions>` · `HMACSHA256` · QuestPDF (already wired) · Resend (already wired) · xUnit · FluentAssertions · `Microsoft.AspNetCore.Mvc.Testing` · Angular 20 (standalone + Signals) · Angular Material (`MatTabs`, `MatTable`, `MatDialog`, `MatChips`, `MatTooltip`, `MatPaginator`).

---

## File map

### New backend files

| Path | Responsibility |
|---|---|
| `backend/BusBooking.Api/Models/RefundStatus.cs` | refund-status string constants |
| `backend/BusBooking.Api/Services/IRefundPolicyService.cs` | contract for refund tier lookup + quoting |
| `backend/BusBooking.Api/Services/RefundPolicyService.cs` | reads `RefundPolicyOptions`, returns `RefundQuote` |
| `backend/BusBooking.Api/Services/RefundQuote.cs` | record carrying refund-percent, refund-amount, hours-to-departure, blocked-flag |
| `backend/BusBooking.Api/Infrastructure/RefundPolicy/RefundPolicyOptions.cs` | options bound to `appsettings.RefundPolicy` |
| `backend/BusBooking.Api/Dtos/BookingListItemDto.cs` | row in the bookings list |
| `backend/BusBooking.Api/Dtos/BookingListResponseDto.cs` | `{items, page, pageSize, totalCount}` |
| `backend/BusBooking.Api/Dtos/RefundPreviewDto.cs` | response of refund-preview endpoint |
| `backend/BusBooking.Api/Dtos/CancelBookingRequest.cs` | POST cancel body — `{reason}` |
| `backend/BusBooking.Api/Validators/CancelBookingRequestValidator.cs` | FluentValidation |

### Modified backend files

- `backend/BusBooking.Api/Models/BookingStatus.cs` — `IsCancelled` helper
- `backend/BusBooking.Api/Dtos/BookingDetailDto.cs` — add `CancelledAt`, `CancellationReason`, `RefundAmount`, `RefundStatus`
- `backend/BusBooking.Api/Infrastructure/Razorpay/IRazorpayClient.cs` — add `CreateRefundAsync`
- `backend/BusBooking.Api/Infrastructure/Razorpay/RazorpayClient.cs` — implement refund call
- `backend/BusBooking.Api/Services/IBookingService.cs` — add `ListAsync`, `GetRefundPreviewAsync`, `CancelAsync`
- `backend/BusBooking.Api/Services/BookingService.cs` — implement the three methods + project new fields in `MapDetail`
- `backend/BusBooking.Api/Services/INotificationSender.cs` — add `SendBookingCancelledAsync`
- `backend/BusBooking.Api/Services/LoggingNotificationSender.cs` — implement cancellation email
- `backend/BusBooking.Api/Controllers/BookingsController.cs` — three new actions
- `backend/BusBooking.Api/Program.cs` — bind `RefundPolicyOptions`, register `IRefundPolicyService`

### New backend test files

| Path | Responsibility |
|---|---|
| `backend/BusBooking.Api.Tests/Unit/RefundPolicyServiceTests.cs` | tier maths matrix |
| `backend/BusBooking.Api.Tests/Integration/BookingListTests.cs` | filter (upcoming / past / cancelled) + paging |
| `backend/BusBooking.Api.Tests/Integration/BookingRefundPreviewTests.cs` | three tiers + blocked + 404 + foreign-user 403 |
| `backend/BusBooking.Api.Tests/Integration/BookingCancellationTests.cs` | cancel happy paths (80%, 50%), blocked (<12h), idempotency, refund call recorded, email sent, seat freed |

### Modified backend test files

- `backend/BusBooking.Api.Tests/Support/FakeRazorpayClient.cs` — track refund calls

### New frontend files

| Path | Responsibility |
|---|---|
| `frontend/bus-booking-web/src/app/shared/components/booking-status-badge/booking-status-badge.component.ts` | small `mat-chip` showing status with colour |
| `frontend/bus-booking-web/src/app/features/customer/bookings-list/bookings-list-page.component.ts` | mat-tabs over Upcoming / Past / Cancelled |
| `frontend/bus-booking-web/src/app/features/customer/bookings-list/bookings-list-page.component.html` | template |
| `frontend/bus-booking-web/src/app/features/customer/booking-detail/booking-detail-page.component.ts` | full detail + cancel button |
| `frontend/bus-booking-web/src/app/features/customer/booking-detail/booking-detail-page.component.html` | template |
| `frontend/bus-booking-web/src/app/features/customer/booking-detail/cancel-booking-dialog.component.ts` | refund preview + reason dialog |
| `frontend/bus-booking-web/src/app/features/customer/booking-detail/cancel-booking-dialog.component.html` | template |

### Modified frontend files

- `frontend/bus-booking-web/src/app/core/api/bookings.api.ts` — add list, preview, cancel methods + new DTOs; widen `BookingDetailDto`
- `frontend/bus-booking-web/src/app/app.routes.ts` — add `/my-bookings` and `/my-bookings/:id`
- `frontend/bus-booking-web/src/app/shared/components/navbar/navbar.component.html` — add "My Bookings" link for customers
- `frontend/bus-booking-web/src/app/features/customer/booking-confirmation/booking-confirmation.component.html` — link "View all bookings"

---

## Prerequisites

- M5 complete: `Booking`, `BookingSeat`, `Payment`, `Notification` entities live; `BookingsController` exposes create / verify-payment / get / ticket; `BookingDetailDto` exists.
- The `RefundPolicy` section in `appsettings.json` already has the tier values (24h → 80%, 12h → 50%, block < 12h). **Do not redefine** — bind it.
- Local Postgres reachable; `bus_booking_test` schema exists for the integration tests.
- Tests pass clean: `dotnet test backend/BusBooking.Api.Tests/BusBooking.Api.Tests.csproj` is green before starting.

---

## Task 1: Refund policy options + service + unit tests

**Files:**
- Create: `backend/BusBooking.Api/Infrastructure/RefundPolicy/RefundPolicyOptions.cs`
- Create: `backend/BusBooking.Api/Services/RefundQuote.cs`
- Create: `backend/BusBooking.Api/Services/IRefundPolicyService.cs`
- Create: `backend/BusBooking.Api/Services/RefundPolicyService.cs`
- Create: `backend/BusBooking.Api.Tests/Unit/RefundPolicyServiceTests.cs`

- [ ] **Step 1: Write the failing unit test**

```csharp
// backend/BusBooking.Api.Tests/Unit/RefundPolicyServiceTests.cs
using BusBooking.Api.Infrastructure.RefundPolicy;
using BusBooking.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace BusBooking.Api.Tests.Unit;

public class RefundPolicyServiceTests
{
    private static RefundPolicyService Build()
    {
        var options = Options.Create(new RefundPolicyOptions
        {
            Tiers =
            [
                new RefundPolicyTier { MinHoursBeforeDeparture = 24, RefundPercent = 80 },
                new RefundPolicyTier { MinHoursBeforeDeparture = 12, RefundPercent = 50 }
            ],
            BlockBelowHours = 12
        });
        return new RefundPolicyService(options);
    }

    [Theory]
    [InlineData(72, 80, 800)]   // ≥24h → 80%
    [InlineData(24, 80, 800)]   // boundary
    [InlineData(23.5, 50, 500)] // 12–24h → 50%
    [InlineData(12, 50, 500)]   // boundary
    public void Quote_returns_expected_refund(double hoursAhead, int expectedPercent, int expectedAmount)
    {
        var svc = Build();
        var now = new DateTime(2026, 04, 23, 12, 0, 0, DateTimeKind.Utc);
        var departure = now.AddHours(hoursAhead);

        var quote = svc.Quote(totalAmount: 1000m, departureUtc: departure, nowUtc: now);

        quote.Blocked.Should().BeFalse();
        quote.RefundPercent.Should().Be(expectedPercent);
        quote.RefundAmount.Should().Be(expectedAmount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(11.99)]
    public void Quote_blocks_under_window(double hoursAhead)
    {
        var svc = Build();
        var now = new DateTime(2026, 04, 23, 12, 0, 0, DateTimeKind.Utc);
        var quote = svc.Quote(1000m, now.AddHours(hoursAhead), now);

        quote.Blocked.Should().BeTrue();
        quote.RefundPercent.Should().Be(0);
        quote.RefundAmount.Should().Be(0m);
    }

    [Fact]
    public void Quote_after_departure_is_blocked()
    {
        var svc = Build();
        var now = new DateTime(2026, 04, 23, 12, 0, 0, DateTimeKind.Utc);
        var quote = svc.Quote(1000m, now.AddHours(-1), now);
        quote.Blocked.Should().BeTrue();
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test backend/BusBooking.Api.Tests/BusBooking.Api.Tests.csproj --filter FullyQualifiedName~RefundPolicyServiceTests`
Expected: build error — `RefundPolicyOptions`, `RefundPolicyTier`, `RefundPolicyService` do not exist.

- [ ] **Step 3: Create the options + tier records**

```csharp
// backend/BusBooking.Api/Infrastructure/RefundPolicy/RefundPolicyOptions.cs
namespace BusBooking.Api.Infrastructure.RefundPolicy;

public class RefundPolicyOptions
{
    public const string SectionName = "RefundPolicy";

    public List<RefundPolicyTier> Tiers { get; set; } = new();
    public int BlockBelowHours { get; set; } = 12;
}

public class RefundPolicyTier
{
    public int MinHoursBeforeDeparture { get; set; }
    public int RefundPercent { get; set; }
}
```

- [ ] **Step 4: Create the quote record**

```csharp
// backend/BusBooking.Api/Services/RefundQuote.cs
namespace BusBooking.Api.Services;

public record RefundQuote(
    int RefundPercent,
    decimal RefundAmount,
    double HoursUntilDeparture,
    bool Blocked);
```

- [ ] **Step 5: Define the service contract**

```csharp
// backend/BusBooking.Api/Services/IRefundPolicyService.cs
namespace BusBooking.Api.Services;

public interface IRefundPolicyService
{
    RefundQuote Quote(decimal totalAmount, DateTime departureUtc, DateTime nowUtc);
}
```

- [ ] **Step 6: Implement the service**

```csharp
// backend/BusBooking.Api/Services/RefundPolicyService.cs
using BusBooking.Api.Infrastructure.RefundPolicy;
using Microsoft.Extensions.Options;

namespace BusBooking.Api.Services;

public class RefundPolicyService : IRefundPolicyService
{
    private readonly RefundPolicyOptions _options;

    public RefundPolicyService(IOptions<RefundPolicyOptions> options)
    {
        _options = options.Value;
    }

    public RefundQuote Quote(decimal totalAmount, DateTime departureUtc, DateTime nowUtc)
    {
        var hours = (departureUtc - nowUtc).TotalHours;

        if (hours < _options.BlockBelowHours)
            return new RefundQuote(0, 0m, hours, Blocked: true);

        var tier = _options.Tiers
            .OrderByDescending(t => t.MinHoursBeforeDeparture)
            .FirstOrDefault(t => hours >= t.MinHoursBeforeDeparture);

        if (tier is null)
            return new RefundQuote(0, 0m, hours, Blocked: false);

        var amount = Math.Round(totalAmount * tier.RefundPercent / 100m, 2);
        return new RefundQuote(tier.RefundPercent, amount, hours, Blocked: false);
    }
}
```

- [ ] **Step 7: Re-run the test, verify pass**

Run: `dotnet test backend/BusBooking.Api.Tests/BusBooking.Api.Tests.csproj --filter FullyQualifiedName~RefundPolicyServiceTests`
Expected: PASS for all 8 cases.

- [ ] **Step 8: Commit**

```bash
git add backend/BusBooking.Api/Infrastructure/RefundPolicy/ \
        backend/BusBooking.Api/Services/IRefundPolicyService.cs \
        backend/BusBooking.Api/Services/RefundPolicyService.cs \
        backend/BusBooking.Api/Services/RefundQuote.cs \
        backend/BusBooking.Api.Tests/Unit/RefundPolicyServiceTests.cs
git commit -m "feat(m6): add refund-policy service with tier-based quoting"
```

---

## Task 2: Razorpay refund client method (interface + impl + fake)

**Files:**
- Modify: `backend/BusBooking.Api/Infrastructure/Razorpay/IRazorpayClient.cs`
- Modify: `backend/BusBooking.Api/Infrastructure/Razorpay/RazorpayClient.cs`
- Modify: `backend/BusBooking.Api.Tests/Support/FakeRazorpayClient.cs`

- [ ] **Step 1: Add the refund record + interface method**

In `backend/BusBooking.Api/Infrastructure/Razorpay/IRazorpayClient.cs`, add the new record above the interface and the new method to the interface:

```csharp
namespace BusBooking.Api.Infrastructure.Razorpay;

public record RazorpayOrder(string Id, long Amount, string Currency, string Receipt);

public record RazorpayRefund(string Id, string PaymentId, long Amount, string Status);

public interface IRazorpayClient
{
    string KeyId { get; }
    Task<RazorpayOrder> CreateOrderAsync(long amountInPaise, string receipt, CancellationToken ct);
    bool VerifySignature(string orderId, string paymentId, string signature);
    Task<RazorpayRefund> CreateRefundAsync(string paymentId, long amountInPaise, CancellationToken ct);
}
```

- [ ] **Step 2: Implement the refund call in `RazorpayClient`**

In `backend/BusBooking.Api/Infrastructure/Razorpay/RazorpayClient.cs`, add the method body and the inner `RefundResponse` record. Place the method directly after `VerifySignature`:

```csharp
public async Task<RazorpayRefund> CreateRefundAsync(string paymentId, long amountInPaise, CancellationToken ct)
{
    var body = new { amount = amountInPaise, speed = "normal" };
    var resp = await _http.PostAsJsonAsync($"/v1/payments/{paymentId}/refund", body, ct);
    if (!resp.IsSuccessStatusCode)
    {
        var text = await resp.Content.ReadAsStringAsync(ct);
        _log.LogError("Razorpay refund failed {Status} {Body}", resp.StatusCode, text);
        throw new InvalidOperationException($"Razorpay refund failed: {resp.StatusCode}");
    }

    var dto = await resp.Content.ReadFromJsonAsync<RefundResponse>(cancellationToken: ct)
        ?? throw new InvalidOperationException("Razorpay refund response was empty");

    return new RazorpayRefund(dto.id, dto.payment_id, dto.amount, dto.status);
}
```

Add to the bottom of the class (next to the existing `OrderResponse` record):

```csharp
private record RefundResponse(string id, string payment_id, long amount, string status);
```

- [ ] **Step 3: Update the test fake to record refund calls**

Replace the contents of `backend/BusBooking.Api.Tests/Support/FakeRazorpayClient.cs` with:

```csharp
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using BusBooking.Api.Infrastructure.Razorpay;

namespace BusBooking.Api.Tests.Support;

public class FakeRazorpayClient : IRazorpayClient
{
    public const string TestKeyId = "rzp_test_fake";
    public const string TestKeySecret = "fake_secret_123";

    public readonly ConcurrentDictionary<string, long> CreatedOrders = new();
    public readonly ConcurrentBag<RazorpayRefund> CreatedRefunds = new();
    public bool ThrowOnRefund { get; set; }

    public string KeyId => TestKeyId;

    public Task<RazorpayOrder> CreateOrderAsync(long amountInPaise, string receipt, CancellationToken ct)
    {
        var id = "order_" + Guid.NewGuid().ToString("N")[..14];
        CreatedOrders[id] = amountInPaise;
        return Task.FromResult(new RazorpayOrder(id, amountInPaise, "INR", receipt));
    }

    public bool VerifySignature(string orderId, string paymentId, string signature)
    {
        var expected = BuildSignature(orderId, paymentId);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature.ToLowerInvariant()));
    }

    public Task<RazorpayRefund> CreateRefundAsync(string paymentId, long amountInPaise, CancellationToken ct)
    {
        if (ThrowOnRefund)
            throw new InvalidOperationException("Razorpay refund unavailable (test fault injection)");

        var refund = new RazorpayRefund(
            Id: "rfnd_" + Guid.NewGuid().ToString("N")[..14],
            PaymentId: paymentId,
            Amount: amountInPaise,
            Status: "processed");
        CreatedRefunds.Add(refund);
        return Task.FromResult(refund);
    }

    public static string BuildSignature(string orderId, string paymentId)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(TestKeySecret));
        var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{orderId}|{paymentId}"));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
```

- [ ] **Step 4: Build to confirm everything compiles**

Run: `dotnet build backend/BusBooking.Api/BusBooking.Api.csproj && dotnet build backend/BusBooking.Api.Tests/BusBooking.Api.Tests.csproj`
Expected: 0 errors. (M5 booking tests still pass — they didn't touch `CreateRefundAsync`.)

- [ ] **Step 5: Run the full backend test suite to confirm no regression**

Run: `dotnet test backend/BusBooking.Api.Tests/BusBooking.Api.Tests.csproj`
Expected: all green.

- [ ] **Step 6: Commit**

```bash
git add backend/BusBooking.Api/Infrastructure/Razorpay/ \
        backend/BusBooking.Api.Tests/Support/FakeRazorpayClient.cs
git commit -m "feat(m6): add Razorpay refund client method + fake"
```

---

## Task 3: Refund/cancellation status constants + new DTOs + enriched detail DTO

**Files:**
- Create: `backend/BusBooking.Api/Models/RefundStatus.cs`
- Modify: `backend/BusBooking.Api/Models/BookingStatus.cs`
- Modify: `backend/BusBooking.Api/Dtos/BookingDetailDto.cs`
- Create: `backend/BusBooking.Api/Dtos/BookingListItemDto.cs`
- Create: `backend/BusBooking.Api/Dtos/BookingListResponseDto.cs`
- Create: `backend/BusBooking.Api/Dtos/RefundPreviewDto.cs`
- Create: `backend/BusBooking.Api/Dtos/CancelBookingRequest.cs`

- [ ] **Step 1: Add refund-status constants**

```csharp
// backend/BusBooking.Api/Models/RefundStatus.cs
namespace BusBooking.Api.Models;

public static class RefundStatus
{
    public const string None      = "none";       // never set in DB; used in DTOs when refund_status is null
    public const string Pending   = "pending";    // committed, not yet acknowledged by Razorpay
    public const string Processed = "processed";  // Razorpay accepted the refund
    public const string Failed    = "failed";     // post-commit Razorpay call failed; manual recovery
}
```

- [ ] **Step 2: Add a small helper on `BookingStatus`**

In `backend/BusBooking.Api/Models/BookingStatus.cs`, add the helper method at the bottom of the class (keep all existing constants intact):

```csharp
public static bool IsCancelled(string status) =>
    status == Cancelled || status == CancelledByOperator;
```

- [ ] **Step 3: Widen `BookingDetailDto` with cancellation/refund fields**

Replace the contents of `backend/BusBooking.Api/Dtos/BookingDetailDto.cs` with:

```csharp
namespace BusBooking.Api.Dtos;

public record BookingDetailDto(
    Guid BookingId,
    string BookingCode,
    Guid TripId,
    DateOnly TripDate,
    string SourceCity,
    string DestinationCity,
    string BusName,
    string OperatorName,
    TimeOnly DepartureTime,
    TimeOnly ArrivalTime,
    decimal TotalFare,
    decimal PlatformFee,
    decimal TotalAmount,
    int SeatCount,
    string Status,
    DateTime? ConfirmedAt,
    DateTime CreatedAt,
    DateTime? CancelledAt,
    string? CancellationReason,
    decimal? RefundAmount,
    string? RefundStatus,
    IReadOnlyList<BookingSeatDto> Seats);
```

(Adds four trailing fields before `Seats`. Order matters — call sites construct positionally.)

- [ ] **Step 4: Add the list item DTO**

```csharp
// backend/BusBooking.Api/Dtos/BookingListItemDto.cs
namespace BusBooking.Api.Dtos;

public record BookingListItemDto(
    Guid BookingId,
    string BookingCode,
    Guid TripId,
    DateOnly TripDate,
    TimeOnly DepartureTime,
    TimeOnly ArrivalTime,
    string SourceCity,
    string DestinationCity,
    string BusName,
    string OperatorName,
    int SeatCount,
    decimal TotalAmount,
    string Status,
    DateTime CreatedAt,
    DateTime? CancelledAt,
    decimal? RefundAmount,
    string? RefundStatus);
```

- [ ] **Step 5: Add the list response wrapper**

```csharp
// backend/BusBooking.Api/Dtos/BookingListResponseDto.cs
namespace BusBooking.Api.Dtos;

public record BookingListResponseDto(
    IReadOnlyList<BookingListItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount);
```

- [ ] **Step 6: Add the refund-preview DTO**

```csharp
// backend/BusBooking.Api/Dtos/RefundPreviewDto.cs
namespace BusBooking.Api.Dtos;

public record RefundPreviewDto(
    Guid BookingId,
    decimal TotalAmount,
    int RefundPercent,
    decimal RefundAmount,
    double HoursUntilDeparture,
    bool Cancellable,
    string? BlockReason);
```

- [ ] **Step 7: Add the cancel request DTO**

```csharp
// backend/BusBooking.Api/Dtos/CancelBookingRequest.cs
namespace BusBooking.Api.Dtos;

public record CancelBookingRequest(string? Reason);
```

- [ ] **Step 8: Update `BookingService.MapDetail` so it projects the new fields**

In `backend/BusBooking.Api/Services/BookingService.cs`, replace the existing `MapDetail` method (the static method at the bottom of the class) with:

```csharp
private static BookingDetailDto MapDetail(Booking b)
{
    var schedule = b.Trip!.Schedule!;
    var route = schedule.Route!;
    var bus = schedule.Bus!;

    return new BookingDetailDto(
        b.Id,
        b.BookingCode,
        b.TripId,
        b.Trip.TripDate,
        route.SourceCity!.Name,
        route.DestinationCity!.Name,
        bus.BusName,
        bus.Operator!.Name,
        schedule.DepartureTime,
        schedule.ArrivalTime,
        b.TotalFare,
        b.PlatformFee,
        b.TotalAmount,
        b.SeatCount,
        b.Status,
        b.ConfirmedAt,
        b.CreatedAt,
        b.CancelledAt,
        b.CancellationReason,
        b.RefundAmount,
        b.RefundStatus,
        b.Seats
            .OrderBy(s => s.SeatNumber)
            .Select(s => new BookingSeatDto(s.SeatNumber, s.PassengerName, s.PassengerAge, s.PassengerGender))
            .ToList());
}
```

- [ ] **Step 9: Build + run M5 booking integration tests to confirm DTO change is backward-safe**

Run: `dotnet build backend/BusBooking.Api/BusBooking.Api.csproj && dotnet test backend/BusBooking.Api.Tests/BusBooking.Api.Tests.csproj --filter FullyQualifiedName~BookingTests`
Expected: 0 build errors, all booking tests pass. The DTO is JSON-serialized by name on the wire so the new fields appear as `null` for legacy responses; M5 tests asserting individual fields still match.

- [ ] **Step 10: Commit**

```bash
git add backend/BusBooking.Api/Models/RefundStatus.cs \
        backend/BusBooking.Api/Models/BookingStatus.cs \
        backend/BusBooking.Api/Dtos/ \
        backend/BusBooking.Api/Services/BookingService.cs
git commit -m "feat(m6): widen BookingDetailDto + add cancel/list/refund-preview DTOs"
```

---

## Task 4: Cancellation notification sender extension

**Files:**
- Modify: `backend/BusBooking.Api/Services/INotificationSender.cs`
- Modify: `backend/BusBooking.Api/Services/LoggingNotificationSender.cs`

- [ ] **Step 1: Add the contract**

In `backend/BusBooking.Api/Services/INotificationSender.cs`, add the new method to the interface (keep all existing members):

```csharp
Task SendBookingCancelledAsync(
    User user,
    BookingDetailDto booking,
    decimal refundAmount,
    int refundPercent,
    CancellationToken ct = default);
```

The full file then reads:

```csharp
using BusBooking.Api.Dtos;
using BusBooking.Api.Models;

namespace BusBooking.Api.Services;

public interface INotificationSender
{
    Task SendOperatorApprovedAsync(User user, CancellationToken ct = default);
    Task SendOperatorRejectedAsync(User user, string reason, CancellationToken ct = default);
    Task SendBusApprovedAsync(User operatorUser, Bus bus, CancellationToken ct = default);
    Task SendBusRejectedAsync(User operatorUser, Bus bus, string reason, CancellationToken ct = default);
    Task SendBookingConfirmedAsync(User user, BookingDetailDto booking, byte[] pdfTicket, CancellationToken ct = default);
    Task SendBookingCancelledAsync(
        User user,
        BookingDetailDto booking,
        decimal refundAmount,
        int refundPercent,
        CancellationToken ct = default);
}
```

- [ ] **Step 2: Implement the cancellation email**

In `backend/BusBooking.Api/Services/LoggingNotificationSender.cs`, append the new method to the class (just before `BuildBookingConfirmedHtml`):

```csharp
public async Task SendBookingCancelledAsync(
    User user,
    BookingDetailDto booking,
    decimal refundAmount,
    int refundPercent,
    CancellationToken ct = default)
{
    var subject = $"Booking cancelled — {booking.BookingCode}";
    var html = BuildBookingCancelledHtml(user, booking, refundAmount, refundPercent);

    var result = await _email.SendAsync(
        user.Email,
        subject,
        html,
        Array.Empty<ResendAttachment>(),
        ct);

    _db.Notifications.Add(new Notification
    {
        Id = Guid.NewGuid(),
        UserId = user.Id,
        Type = NotificationType.Cancelled,
        Channel = NotificationChannel.Email,
        ToAddress = user.Email,
        Subject = subject,
        ResendMessageId = result.MessageId,
        Status = result.Success ? "sent" : "failed",
        Error = result.Error,
        CreatedAt = _time.GetUtcNow().UtcDateTime
    });
    await _db.SaveChangesAsync(ct);

    if (!result.Success)
        _log.LogWarning("Booking cancellation email failed for {BookingCode}: {Error}",
            booking.BookingCode, result.Error);
}

private static string BuildBookingCancelledHtml(User user, BookingDetailDto b, decimal refundAmount, int refundPercent)
{
    var sb = new StringBuilder();
    sb.Append("<div style=\"font-family:Arial,sans-serif\">");
    sb.Append($"<h2>Booking cancelled: {b.BookingCode}</h2>");
    sb.Append($"<p>Hi {System.Net.WebUtility.HtmlEncode(user.Name)},</p>");
    sb.Append("<p>Your booking has been cancelled at your request.</p>");
    sb.Append("<hr/>");
    sb.Append($"<p><b>Trip:</b> {System.Net.WebUtility.HtmlEncode(b.SourceCity)} → {System.Net.WebUtility.HtmlEncode(b.DestinationCity)}</p>");
    sb.Append($"<p><b>Date:</b> {b.TripDate}</p>");
    sb.Append($"<p><b>Bus:</b> {System.Net.WebUtility.HtmlEncode(b.BusName)} (Operator: {System.Net.WebUtility.HtmlEncode(b.OperatorName)})</p>");
    sb.Append($"<p><b>Seats:</b> {string.Join(", ", b.Seats.Select(s => System.Net.WebUtility.HtmlEncode(s.SeatNumber)))}</p>");
    sb.Append($"<p><b>Refund:</b> ₹{refundAmount:0.00} ({refundPercent}% of ₹{b.TotalAmount:0.00})</p>");
    sb.Append("<p>Refunds typically reflect in your account in 5–7 business days.</p>");
    sb.Append("</div>");
    return sb.ToString();
}
```

- [ ] **Step 3: Build to confirm**

Run: `dotnet build backend/BusBooking.Api/BusBooking.Api.csproj`
Expected: 0 errors.

- [ ] **Step 4: Commit**

```bash
git add backend/BusBooking.Api/Services/INotificationSender.cs \
        backend/BusBooking.Api/Services/LoggingNotificationSender.cs
git commit -m "feat(m6): add SendBookingCancelledAsync notification"
```

---

## Task 5: BookingService.ListAsync + endpoint + integration tests

**Files:**
- Modify: `backend/BusBooking.Api/Services/IBookingService.cs`
- Modify: `backend/BusBooking.Api/Services/BookingService.cs`
- Modify: `backend/BusBooking.Api/Controllers/BookingsController.cs`
- Create: `backend/BusBooking.Api.Tests/Integration/BookingListTests.cs`

- [ ] **Step 1: Write the failing integration test**

```csharp
// backend/BusBooking.Api.Tests/Integration/BookingListTests.cs
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

[Collection(IntegrationCollection.Name)]
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
```

- [ ] **Step 2: Run the test, verify it fails**

Run: `dotnet test backend/BusBooking.Api.Tests/BusBooking.Api.Tests.csproj --filter FullyQualifiedName~BookingListTests`
Expected: build fails — endpoint and `IBookingService.ListAsync` do not exist.

- [ ] **Step 3: Add the contract**

In `backend/BusBooking.Api/Services/IBookingService.cs`, add the new method to the interface (keep all existing members):

```csharp
Task<BookingListResponseDto> ListAsync(
    Guid userId,
    string filter,
    int page,
    int pageSize,
    CancellationToken ct);
```

- [ ] **Step 4: Implement the service method**

In `backend/BusBooking.Api/Services/BookingService.cs`, add the method body (place before `LoadForDetailAsync`). It pulls bookings filtered by user, date, and status, and pages them:

```csharp
public async Task<BookingListResponseDto> ListAsync(
    Guid userId, string filter, int page, int pageSize, CancellationToken ct)
{
    var today = DateOnly.FromDateTime(_time.GetUtcNow().UtcDateTime.Date);
    var p = page < 1 ? 1 : page;
    var size = pageSize is < 1 or > 100 ? 20 : pageSize;

    var q = _db.Bookings
        .AsNoTracking()
        .Where(b => b.UserId == userId)
        .Include(b => b.Trip).ThenInclude(t => t!.Schedule).ThenInclude(s => s!.Bus).ThenInclude(b => b!.Operator)
        .Include(b => b.Trip).ThenInclude(t => t!.Schedule).ThenInclude(s => s!.Route).ThenInclude(r => r!.SourceCity)
        .Include(b => b.Trip).ThenInclude(t => t!.Schedule).ThenInclude(s => s!.Route).ThenInclude(r => r!.DestinationCity)
        .AsQueryable();

    q = (filter ?? "upcoming").ToLowerInvariant() switch
    {
        "past" => q.Where(b =>
            (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Completed)
            && b.Trip!.TripDate < today),
        "cancelled" => q.Where(b =>
            b.Status == BookingStatus.Cancelled || b.Status == BookingStatus.CancelledByOperator),
        _ /* upcoming */ => q.Where(b =>
            b.Status == BookingStatus.Confirmed && b.Trip!.TripDate >= today),
    };

    var total = await q.CountAsync(ct);

    var rows = await q
        .OrderByDescending(b => b.CreatedAt)
        .Skip((p - 1) * size)
        .Take(size)
        .ToListAsync(ct);

    var items = rows.Select(b =>
    {
        var schedule = b.Trip!.Schedule!;
        var route = schedule.Route!;
        var bus = schedule.Bus!;
        return new BookingListItemDto(
            b.Id,
            b.BookingCode,
            b.TripId,
            b.Trip.TripDate,
            schedule.DepartureTime,
            schedule.ArrivalTime,
            route.SourceCity!.Name,
            route.DestinationCity!.Name,
            bus.BusName,
            bus.Operator!.Name,
            b.SeatCount,
            b.TotalAmount,
            b.Status,
            b.CreatedAt,
            b.CancelledAt,
            b.RefundAmount,
            b.RefundStatus);
    }).ToList();

    return new BookingListResponseDto(items, p, size, total);
}
```

- [ ] **Step 5: Add the controller action**

In `backend/BusBooking.Api/Controllers/BookingsController.cs`, add the new action above the existing `Create` method:

```csharp
[HttpGet]
public async Task<ActionResult<BookingListResponseDto>> List(
    [FromQuery] string? filter,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    CancellationToken ct = default)
{
    var result = await _bookings.ListAsync(_currentUser.UserId, filter ?? "upcoming", page, pageSize, ct);
    return Ok(result);
}
```

- [ ] **Step 6: Run the BookingListTests, verify pass**

Run: `dotnet test backend/BusBooking.Api.Tests/BusBooking.Api.Tests.csproj --filter FullyQualifiedName~BookingListTests`
Expected: all 5 tests PASS.

- [ ] **Step 7: Commit**

```bash
git add backend/BusBooking.Api/Services/IBookingService.cs \
        backend/BusBooking.Api/Services/BookingService.cs \
        backend/BusBooking.Api/Controllers/BookingsController.cs \
        backend/BusBooking.Api.Tests/Integration/BookingListTests.cs
git commit -m "feat(m6): GET /bookings list endpoint with filter+paging"
```

---

## Task 6: BookingService.GetRefundPreviewAsync + endpoint + integration tests

**Files:**
- Modify: `backend/BusBooking.Api/Services/IBookingService.cs`
- Modify: `backend/BusBooking.Api/Services/BookingService.cs` (constructor + new method)
- Modify: `backend/BusBooking.Api/Controllers/BookingsController.cs`
- Create: `backend/BusBooking.Api.Tests/Integration/BookingRefundPreviewTests.cs`

- [ ] **Step 1: Write the failing integration test**

```csharp
// backend/BusBooking.Api.Tests/Integration/BookingRefundPreviewTests.cs
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

[Collection(IntegrationCollection.Name)]
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
        // Trip today at 22:00 — but if 'now' is past 10:00 the test machine, hours < 12.
        // Force a near-departure trip by seeding a trip dated today and shifting schedule departure to one hour ahead via raw update.
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
    public async Task Preview_404_for_other_user_booking()
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
```

- [ ] **Step 2: Run the test, verify it fails**

Run: `dotnet test backend/BusBooking.Api.Tests/BusBooking.Api.Tests.csproj --filter FullyQualifiedName~BookingRefundPreviewTests`
Expected: build fails — endpoint and method do not exist.

- [ ] **Step 3: Add the contract**

In `backend/BusBooking.Api/Services/IBookingService.cs`, add:

```csharp
Task<RefundPreviewDto> GetRefundPreviewAsync(Guid userId, Guid bookingId, CancellationToken ct);
```

- [ ] **Step 4: Inject `IRefundPolicyService` into `BookingService`**

In `backend/BusBooking.Api/Services/BookingService.cs`, add the new dependency to the existing constructor + field. The full updated constructor + fields block:

```csharp
private readonly AppDbContext _db;
private readonly ISeatLockService _locks;
private readonly IPlatformFeeService _platformFee;
private readonly IRazorpayClient _razorpay;
private readonly IPdfTicketGenerator _pdf;
private readonly INotificationSender _notifications;
private readonly IRefundPolicyService _refundPolicy;
private readonly TimeProvider _time;
private readonly ILogger<BookingService> _log;

public BookingService(
    AppDbContext db,
    ISeatLockService locks,
    IPlatformFeeService platformFee,
    IRazorpayClient razorpay,
    IPdfTicketGenerator pdf,
    INotificationSender notifications,
    IRefundPolicyService refundPolicy,
    TimeProvider time,
    ILogger<BookingService> log)
{
    _db = db;
    _locks = locks;
    _platformFee = platformFee;
    _razorpay = razorpay;
    _pdf = pdf;
    _notifications = notifications;
    _refundPolicy = refundPolicy;
    _time = time;
    _log = log;
}
```

- [ ] **Step 5: Implement the preview method**

In `BookingService.cs`, add this method just before `LoadForDetailAsync`:

```csharp
public async Task<RefundPreviewDto> GetRefundPreviewAsync(Guid userId, Guid bookingId, CancellationToken ct)
{
    var booking = await _db.Bookings
        .AsNoTracking()
        .Include(b => b.Trip).ThenInclude(t => t!.Schedule)
        .FirstOrDefaultAsync(b => b.Id == bookingId, ct)
        ?? throw new NotFoundException("Booking not found");

    if (booking.UserId != userId)
        throw new ForbiddenException("Not your booking");

    var schedule = booking.Trip!.Schedule!;
    var departureUtc = booking.Trip.TripDate.ToDateTime(schedule.DepartureTime, DateTimeKind.Utc);
    var now = _time.GetUtcNow().UtcDateTime;
    var quote = _refundPolicy.Quote(booking.TotalAmount, departureUtc, now);

    string? blockReason = null;
    if (booking.Status != BookingStatus.Confirmed)
        blockReason = $"Booking is in status '{booking.Status}', not cancellable";
    else if (quote.Blocked)
        blockReason = "Cancellation window has closed (less than the policy minimum hours before departure)";

    return new RefundPreviewDto(
        booking.Id,
        booking.TotalAmount,
        quote.RefundPercent,
        quote.RefundAmount,
        quote.HoursUntilDeparture,
        Cancellable: blockReason is null,
        BlockReason: blockReason);
}
```

- [ ] **Step 6: Add the controller action**

In `BookingsController.cs`, add this action below `Get(Guid id…)`:

```csharp
[HttpGet("{id:guid}/refund-preview")]
public async Task<ActionResult<RefundPreviewDto>> RefundPreview(Guid id, CancellationToken ct)
{
    var preview = await _bookings.GetRefundPreviewAsync(_currentUser.UserId, id, ct);
    return Ok(preview);
}
```

- [ ] **Step 7: Wire `IRefundPolicyService` into DI**

In `backend/BusBooking.Api/Program.cs`, add these two lines next to the other `Configure<…>` and `AddScoped<…>` calls (just before the line that adds `IScheduleService`):

```csharp
builder.Services.Configure<BusBooking.Api.Infrastructure.RefundPolicy.RefundPolicyOptions>(
    builder.Configuration.GetSection(BusBooking.Api.Infrastructure.RefundPolicy.RefundPolicyOptions.SectionName));
builder.Services.AddScoped<IRefundPolicyService, RefundPolicyService>();
```

- [ ] **Step 8: Run the preview tests, verify pass**

Run: `dotnet test backend/BusBooking.Api.Tests/BusBooking.Api.Tests.csproj --filter FullyQualifiedName~BookingRefundPreviewTests`
Expected: all 4 tests PASS.

- [ ] **Step 9: Run the full test suite to confirm no regression**

Run: `dotnet test backend/BusBooking.Api.Tests/BusBooking.Api.Tests.csproj`
Expected: all green (M5 booking tests still pass — the constructor change is backward-compatible from DI's perspective; resolved automatically).

- [ ] **Step 10: Commit**

```bash
git add backend/BusBooking.Api/Services/IBookingService.cs \
        backend/BusBooking.Api/Services/BookingService.cs \
        backend/BusBooking.Api/Controllers/BookingsController.cs \
        backend/BusBooking.Api/Program.cs \
        backend/BusBooking.Api.Tests/Integration/BookingRefundPreviewTests.cs
git commit -m "feat(m6): GET /bookings/{id}/refund-preview endpoint"
```

---

## Task 7: BookingService.CancelAsync + validator + endpoint + integration tests

**Files:**
- Modify: `backend/BusBooking.Api/Services/IBookingService.cs`
- Modify: `backend/BusBooking.Api/Services/BookingService.cs`
- Modify: `backend/BusBooking.Api/Controllers/BookingsController.cs`
- Create: `backend/BusBooking.Api/Validators/CancelBookingRequestValidator.cs`
- Create: `backend/BusBooking.Api.Tests/Integration/BookingCancellationTests.cs`

- [ ] **Step 1: Write the failing integration tests**

```csharp
// backend/BusBooking.Api.Tests/Integration/BookingCancellationTests.cs
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

[Collection(IntegrationCollection.Name)]
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

        // Razorpay refund call recorded
        _fx.Razorpay.CreatedRefunds.Should().ContainSingle()
            .Which.Amount.Should().Be(80000); // paise

        // Email sent
        _fx.Email.Sent.Should().ContainSingle(e => e.Subject.Contains("cancelled"));

        // Payment + DB state
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

        // Booking unchanged
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
            resp.StatusCode.Should().Be(HttpStatusCode.OK); // booking is still cancelled
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
```

- [ ] **Step 2: Run the cancel tests, verify they fail**

Run: `dotnet test backend/BusBooking.Api.Tests/BusBooking.Api.Tests.csproj --filter FullyQualifiedName~BookingCancellationTests`
Expected: build fails — endpoint and method do not exist.

- [ ] **Step 3: Add the validator**

```csharp
// backend/BusBooking.Api/Validators/CancelBookingRequestValidator.cs
using BusBooking.Api.Dtos;
using FluentValidation;

namespace BusBooking.Api.Validators;

public class CancelBookingRequestValidator : AbstractValidator<CancelBookingRequest>
{
    public CancelBookingRequestValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason must be 500 characters or fewer");
    }
}
```

- [ ] **Step 4: Add the contract**

In `backend/BusBooking.Api/Services/IBookingService.cs`, add:

```csharp
Task<BookingDetailDto> CancelAsync(Guid userId, Guid bookingId, CancelBookingRequest req, CancellationToken ct);
```

- [ ] **Step 5: Implement `CancelAsync`**

In `backend/BusBooking.Api/Services/BookingService.cs`, add this method just before `LoadForDetailAsync`:

```csharp
public async Task<BookingDetailDto> CancelAsync(
    Guid userId, Guid bookingId, CancelBookingRequest req, CancellationToken ct)
{
    var booking = await LoadForDetailAsync(bookingId, ct)
        ?? throw new NotFoundException("Booking not found");
    if (booking.UserId != userId)
        throw new ForbiddenException("Not your booking");

    // Idempotent: re-cancelling an already-cancelled booking returns current state.
    if (BookingStatus.IsCancelled(booking.Status))
        return MapDetail(booking);

    if (booking.Status != BookingStatus.Confirmed)
        throw new BusinessRuleException("CANCEL_NOT_ALLOWED",
            $"Booking cannot be cancelled from status '{booking.Status}'");

    var schedule = booking.Trip!.Schedule!;
    var departureUtc = booking.Trip.TripDate.ToDateTime(schedule.DepartureTime, DateTimeKind.Utc);
    var now = _time.GetUtcNow().UtcDateTime;
    var quote = _refundPolicy.Quote(booking.TotalAmount, departureUtc, now);

    if (quote.Blocked)
        throw new BusinessRuleException("CANCEL_WINDOW_CLOSED",
            "Cancellation window has closed for this booking",
            new { hoursUntilDeparture = quote.HoursUntilDeparture });

    // Mutate state in a single transaction
    booking.Status = BookingStatus.Cancelled;
    booking.CancelledAt = now;
    booking.CancellationReason = req.Reason;
    booking.RefundAmount = quote.RefundAmount;
    booking.RefundStatus = RefundStatus.Pending;
    await _db.SaveChangesAsync(ct);

    var dto = MapDetail(booking);

    // Side effects post-commit. A Razorpay outage must not strand the booking.
    if (quote.RefundAmount > 0m
        && booking.Payment is { Status: PaymentStatus.Captured, RazorpayPaymentId: { } payId })
    {
        try
        {
            var refund = await _razorpay.CreateRefundAsync(payId, (long)(quote.RefundAmount * 100m), ct);
            booking.Payment.Status = PaymentStatus.Refunded;
            booking.Payment.RefundedAt = now;
            booking.RefundStatus = RefundStatus.Processed;
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Razorpay refund failed for booking {BookingCode}", booking.BookingCode);
            booking.RefundStatus = RefundStatus.Failed;
            await _db.SaveChangesAsync(ct);
        }
    }
    else
    {
        // Zero refund or unpaid booking — nothing to call.
        booking.RefundStatus = RefundStatus.Processed;
        await _db.SaveChangesAsync(ct);
    }

    var refreshed = MapDetail(booking);

    try
    {
        await _notifications.SendBookingCancelledAsync(
            booking.User, refreshed, quote.RefundAmount, quote.RefundPercent, ct);
    }
    catch (Exception ex)
    {
        _log.LogError(ex, "Cancellation email failed for {BookingCode}", booking.BookingCode);
    }

    return refreshed;
}
```

- [ ] **Step 6: Inject the validator + add the controller action**

In `backend/BusBooking.Api/Controllers/BookingsController.cs`, add the validator field, constructor parameter, and the new action. The constructor + fields block becomes:

```csharp
private readonly IBookingService _bookings;
private readonly IValidator<CreateBookingRequest> _createValidator;
private readonly IValidator<VerifyPaymentRequest> _verifyValidator;
private readonly IValidator<CancelBookingRequest> _cancelValidator;
private readonly ICurrentUserAccessor _currentUser;

public BookingsController(
    IBookingService bookings,
    IValidator<CreateBookingRequest> createValidator,
    IValidator<VerifyPaymentRequest> verifyValidator,
    IValidator<CancelBookingRequest> cancelValidator,
    ICurrentUserAccessor currentUser)
{
    _bookings = bookings;
    _createValidator = createValidator;
    _verifyValidator = verifyValidator;
    _cancelValidator = cancelValidator;
    _currentUser = currentUser;
}
```

Then add this action below `RefundPreview`:

```csharp
[HttpPost("{id:guid}/cancel")]
public async Task<ActionResult<BookingDetailDto>> Cancel(
    Guid id,
    [FromBody] CancelBookingRequest req,
    CancellationToken ct)
{
    await _cancelValidator.ValidateAndThrowAsync(req, ct);
    var detail = await _bookings.CancelAsync(_currentUser.UserId, id, req, ct);
    return Ok(detail);
}
```

- [ ] **Step 7: Run the cancel tests, verify pass**

Run: `dotnet test backend/BusBooking.Api.Tests/BusBooking.Api.Tests.csproj --filter FullyQualifiedName~BookingCancellationTests`
Expected: all 6 tests PASS.

- [ ] **Step 8: Run the full backend test suite**

Run: `dotnet test backend/BusBooking.Api.Tests/BusBooking.Api.Tests.csproj`
Expected: all green.

- [ ] **Step 9: Commit**

```bash
git add backend/BusBooking.Api/Services/IBookingService.cs \
        backend/BusBooking.Api/Services/BookingService.cs \
        backend/BusBooking.Api/Controllers/BookingsController.cs \
        backend/BusBooking.Api/Validators/CancelBookingRequestValidator.cs \
        backend/BusBooking.Api.Tests/Integration/BookingCancellationTests.cs
git commit -m "feat(m6): POST /bookings/{id}/cancel with refund + email"
```

---

## Task 8: Backend quality gate

- [ ] **Step 1: Build**

Run: `dotnet build backend/BusBooking.Api/BusBooking.Api.csproj`
Expected: 0 errors, 0 warnings (other than pre-existing).

- [ ] **Step 2: Test**

Run: `dotnet test backend/BusBooking.Api.Tests/BusBooking.Api.Tests.csproj`
Expected: every M0–M5 test still green; new M6 unit + integration tests green.

- [ ] **Step 3: Manual smoke against the dev DB (optional but recommended)**

```bash
cd backend/BusBooking.Api
dotnet ef database update           # no schema change in M6 — should be a no-op
dotnet run                          # serve on :5080
# In another shell, with a confirmed booking, hit:
curl -H "Authorization: Bearer <TOKEN>" http://localhost:5080/api/v1/bookings?filter=upcoming
curl -H "Authorization: Bearer <TOKEN>" http://localhost:5080/api/v1/bookings/<id>/refund-preview
```

Expected: the list returns the user's bookings; the preview returns a quote.

(No commit — verification only.)

---

## Task 9: Frontend — `bookings.api.ts` extensions

**Files:**
- Modify: `frontend/bus-booking-web/src/app/core/api/bookings.api.ts`

- [ ] **Step 1: Replace `bookings.api.ts` with the widened version**

Open the file and overwrite with the content below. The existing types are kept; new types/methods are appended; `BookingDetailDto` is widened to mirror the backend.

```typescript
// frontend/bus-booking-web/src/app/core/api/bookings.api.ts
import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface LockSeatsRequest {
  sessionId: string;
  seats: string[];
}

export interface SeatLockResponseDto {
  lockId: string;
  sessionId: string;
  seats: string[];
  expiresAt: string;
}

export interface PassengerDto {
  seatNumber: string;
  passengerName: string;
  passengerAge: number;
  passengerGender: 'male' | 'female' | 'other';
}

export interface CreateBookingRequest {
  tripId: string;
  lockId: string;
  sessionId: string;
  passengers: PassengerDto[];
}

export interface CreateBookingResponseDto {
  bookingId: string;
  bookingCode: string;
  razorpayOrderId: string;
  keyId: string;
  amount: number;
  currency: string;
}

export interface VerifyPaymentRequest {
  razorpayPaymentId: string;
  razorpaySignature: string;
}

export interface BookingSeatDto {
  seatNumber: string;
  passengerName: string;
  passengerAge: number;
  passengerGender: string;
}

export interface BookingDetailDto {
  bookingId: string;
  bookingCode: string;
  tripId: string;
  tripDate: string;
  sourceCity: string;
  destinationCity: string;
  busName: string;
  operatorName: string;
  departureTime: string;
  arrivalTime: string;
  totalFare: number;
  platformFee: number;
  totalAmount: number;
  seatCount: number;
  status: string;
  confirmedAt: string | null;
  createdAt: string;
  cancelledAt: string | null;
  cancellationReason: string | null;
  refundAmount: number | null;
  refundStatus: string | null;
  seats: BookingSeatDto[];
}

export type BookingFilter = 'upcoming' | 'past' | 'cancelled';

export interface BookingListItemDto {
  bookingId: string;
  bookingCode: string;
  tripId: string;
  tripDate: string;
  departureTime: string;
  arrivalTime: string;
  sourceCity: string;
  destinationCity: string;
  busName: string;
  operatorName: string;
  seatCount: number;
  totalAmount: number;
  status: string;
  createdAt: string;
  cancelledAt: string | null;
  refundAmount: number | null;
  refundStatus: string | null;
}

export interface BookingListResponseDto {
  items: BookingListItemDto[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface RefundPreviewDto {
  bookingId: string;
  totalAmount: number;
  refundPercent: number;
  refundAmount: number;
  hoursUntilDeparture: number;
  cancellable: boolean;
  blockReason: string | null;
}

export interface CancelBookingRequest {
  reason: string | null;
}

@Injectable({ providedIn: 'root' })
export class BookingsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  lockSeats(tripId: string, body: LockSeatsRequest): Observable<SeatLockResponseDto> {
    return this.http.post<SeatLockResponseDto>(`${this.base}/trips/${tripId}/seat-locks`, body);
  }

  releaseLock(lockId: string, sessionId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/seat-locks/${lockId}`, {
      params: { sessionId }
    });
  }

  createBooking(body: CreateBookingRequest): Observable<CreateBookingResponseDto> {
    return this.http.post<CreateBookingResponseDto>(`${this.base}/bookings`, body);
  }

  verifyPayment(bookingId: string, body: VerifyPaymentRequest): Observable<BookingDetailDto> {
    return this.http.post<BookingDetailDto>(`${this.base}/bookings/${bookingId}/verify-payment`, body);
  }

  getBooking(bookingId: string): Observable<BookingDetailDto> {
    return this.http.get<BookingDetailDto>(`${this.base}/bookings/${bookingId}`);
  }

  getTicketUrl(bookingId: string): string {
    return `${this.base}/bookings/${bookingId}/ticket`;
  }

  listBookings(filter: BookingFilter = 'upcoming', page = 1, pageSize = 20): Observable<BookingListResponseDto> {
    const params = new HttpParams()
      .set('filter', filter)
      .set('page', String(page))
      .set('pageSize', String(pageSize));
    return this.http.get<BookingListResponseDto>(`${this.base}/bookings`, { params });
  }

  getRefundPreview(bookingId: string): Observable<RefundPreviewDto> {
    return this.http.get<RefundPreviewDto>(`${this.base}/bookings/${bookingId}/refund-preview`);
  }

  cancelBooking(bookingId: string, body: CancelBookingRequest): Observable<BookingDetailDto> {
    return this.http.post<BookingDetailDto>(`${this.base}/bookings/${bookingId}/cancel`, body);
  }
}
```

- [ ] **Step 2: Type-check**

Run: `cd frontend/bus-booking-web && npx tsc --noEmit -p tsconfig.app.json`
Expected: no errors. (Existing consumers — checkout stepper, booking confirmation — only read fields they already used; new fields are additive.)

- [ ] **Step 3: Commit**

```bash
git add frontend/bus-booking-web/src/app/core/api/bookings.api.ts
git commit -m "feat(m6): extend bookings.api.ts with list/preview/cancel methods"
```

---

## Task 10: Frontend — `BookingStatusBadgeComponent`

**Files:**
- Create: `frontend/bus-booking-web/src/app/shared/components/booking-status-badge/booking-status-badge.component.ts`

A tiny, theme-aware mat-chip that maps a booking status string to a coloured pill.

- [ ] **Step 1: Write the component**

```typescript
// frontend/bus-booking-web/src/app/shared/components/booking-status-badge/booking-status-badge.component.ts
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatChipsModule } from '@angular/material/chips';

interface StatusStyle {
  label: string;
  classes: string;
}

@Component({
  selector: 'app-booking-status-badge',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, MatChipsModule],
  template: `
    <mat-chip [class]="style().classes" disableRipple>
      {{ style().label }}
    </mat-chip>
  `
})
export class BookingStatusBadgeComponent {
  readonly status = input.required<string>();

  readonly style = computed<StatusStyle>(() => {
    switch (this.status()) {
      case 'confirmed':
        return { label: 'Confirmed', classes: 'bg-emerald-100 text-emerald-800' };
      case 'pending_payment':
        return { label: 'Pending payment', classes: 'bg-amber-100 text-amber-800' };
      case 'cancelled':
        return { label: 'Cancelled', classes: 'bg-rose-100 text-rose-800' };
      case 'cancelled_by_operator':
        return { label: 'Cancelled (operator)', classes: 'bg-rose-100 text-rose-800' };
      case 'completed':
        return { label: 'Completed', classes: 'bg-slate-200 text-slate-800' };
      default:
        return { label: this.status(), classes: 'bg-slate-200 text-slate-800' };
    }
  });
}
```

- [ ] **Step 2: Type-check**

Run: `cd frontend/bus-booking-web && npx tsc --noEmit -p tsconfig.app.json`
Expected: no errors.

- [ ] **Step 3: Commit**

```bash
git add frontend/bus-booking-web/src/app/shared/components/booking-status-badge/
git commit -m "feat(m6): add BookingStatusBadgeComponent"
```

---

## Task 11: Frontend — `BookingsListPageComponent`

**Files:**
- Create: `frontend/bus-booking-web/src/app/features/customer/bookings-list/bookings-list-page.component.ts`
- Create: `frontend/bus-booking-web/src/app/features/customer/bookings-list/bookings-list-page.component.html`

The page: title + three mat-tabs over the same dataset; clicking a row navigates to the detail page.

- [ ] **Step 1: TypeScript**

```typescript
// frontend/bus-booking-web/src/app/features/customer/bookings-list/bookings-list-page.component.ts
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { MatTabsModule, MatTabChangeEvent } from '@angular/material/tabs';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import {
  BookingFilter,
  BookingListItemDto,
  BookingsApiService
} from '../../../core/api/bookings.api';
import { BookingStatusBadgeComponent } from '../../../shared/components/booking-status-badge/booking-status-badge.component';

@Component({
  selector: 'app-bookings-list-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    CurrencyPipe,
    DatePipe,
    RouterLink,
    MatTabsModule,
    MatTableModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatIconModule,
    BookingStatusBadgeComponent
  ],
  templateUrl: './bookings-list-page.component.html'
})
export class BookingsListPageComponent implements OnInit {
  private readonly api = inject(BookingsApiService);
  private readonly router = inject(Router);

  readonly filter = signal<BookingFilter>('upcoming');
  readonly items = signal<BookingListItemDto[]>([]);
  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly columns = ['code', 'route', 'date', 'seats', 'amount', 'status', 'actions'];

  private readonly tabIndexToFilter: BookingFilter[] = ['upcoming', 'past', 'cancelled'];

  ngOnInit(): void {
    this.load();
  }

  onTabChange(e: MatTabChangeEvent): void {
    this.filter.set(this.tabIndexToFilter[e.index]);
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.api.listBookings(this.filter(), 1, 50).subscribe({
      next: (resp) => {
        this.items.set(resp.items);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(err?.error?.error?.message ?? 'Could not load your bookings');
      }
    });
  }

  open(row: BookingListItemDto): void {
    this.router.navigate(['/my-bookings', row.bookingId]);
  }
}
```

- [ ] **Step 2: Template**

```html
<!-- frontend/bus-booking-web/src/app/features/customer/bookings-list/bookings-list-page.component.html -->
<div class="max-w-5xl mx-auto p-4 md:p-6 space-y-4">
  <header class="flex items-center justify-between">
    <h1 class="text-2xl font-bold m-0">My Bookings</h1>
    <a mat-stroked-button routerLink="/">Book another trip</a>
  </header>

  <mat-tab-group [selectedIndex]="0" (selectedTabChange)="onTabChange($event)" mat-stretch-tabs="false" mat-align-tabs="start">
    <mat-tab label="Upcoming"></mat-tab>
    <mat-tab label="Past"></mat-tab>
    <mat-tab label="Cancelled"></mat-tab>
  </mat-tab-group>

  @if (loading()) {
    <div class="flex justify-center py-10">
      <mat-progress-spinner diameter="36" mode="indeterminate"></mat-progress-spinner>
    </div>
  } @else if (errorMessage()) {
    <div class="bg-rose-50 border border-rose-200 text-rose-800 p-4 rounded">
      {{ errorMessage() }}
    </div>
  } @else if (items().length === 0) {
    <div class="text-center text-slate-500 py-10 border border-dashed border-slate-200 rounded">
      <mat-icon class="text-4xl">inbox</mat-icon>
      <p class="mt-2">No bookings to show in this tab.</p>
    </div>
  } @else {
    <div class="overflow-x-auto border border-slate-200 rounded">
      <table mat-table [dataSource]="items()" class="w-full">
        <ng-container matColumnDef="code">
          <th mat-header-cell *matHeaderCellDef>Booking</th>
          <td mat-cell *matCellDef="let row" class="font-mono text-xs">{{ row.bookingCode }}</td>
        </ng-container>

        <ng-container matColumnDef="route">
          <th mat-header-cell *matHeaderCellDef>Route</th>
          <td mat-cell *matCellDef="let row">
            <div class="font-medium">{{ row.sourceCity }} → {{ row.destinationCity }}</div>
            <div class="text-xs text-slate-500">{{ row.busName }} · {{ row.operatorName }}</div>
          </td>
        </ng-container>

        <ng-container matColumnDef="date">
          <th mat-header-cell *matHeaderCellDef>Date · Time</th>
          <td mat-cell *matCellDef="let row">
            <div>{{ row.tripDate | date: 'mediumDate' }}</div>
            <div class="text-xs text-slate-500">{{ row.departureTime.substring(0,5) }} → {{ row.arrivalTime.substring(0,5) }}</div>
          </td>
        </ng-container>

        <ng-container matColumnDef="seats">
          <th mat-header-cell *matHeaderCellDef>Seats</th>
          <td mat-cell *matCellDef="let row">{{ row.seatCount }}</td>
        </ng-container>

        <ng-container matColumnDef="amount">
          <th mat-header-cell *matHeaderCellDef>Total</th>
          <td mat-cell *matCellDef="let row">
            <div>{{ row.totalAmount | currency: 'INR' }}</div>
            @if (row.refundAmount !== null && row.refundAmount > 0) {
              <div class="text-xs text-emerald-700">Refund {{ row.refundAmount | currency: 'INR' }}</div>
            }
          </td>
        </ng-container>

        <ng-container matColumnDef="status">
          <th mat-header-cell *matHeaderCellDef>Status</th>
          <td mat-cell *matCellDef="let row">
            <app-booking-status-badge [status]="row.status" />
          </td>
        </ng-container>

        <ng-container matColumnDef="actions">
          <th mat-header-cell *matHeaderCellDef></th>
          <td mat-cell *matCellDef="let row">
            <button mat-stroked-button color="primary" (click)="open(row)">View</button>
          </td>
        </ng-container>

        <tr mat-header-row *matHeaderRowDef="columns"></tr>
        <tr mat-row *matRowDef="let row; columns: columns" class="cursor-pointer" (click)="open(row)"></tr>
      </table>
    </div>
  }
</div>
```

- [ ] **Step 3: Type-check**

Run: `cd frontend/bus-booking-web && npx tsc --noEmit -p tsconfig.app.json`
Expected: no errors.

- [ ] **Step 4: Commit**

```bash
git add frontend/bus-booking-web/src/app/features/customer/bookings-list/
git commit -m "feat(m6): add bookings-list page with mat-tabs filter"
```

---

## Task 12: Frontend — routes + navbar "My Bookings" link

**Files:**
- Modify: `frontend/bus-booking-web/src/app/app.routes.ts`
- Modify: `frontend/bus-booking-web/src/app/shared/components/navbar/navbar.component.html`

- [ ] **Step 1: Add the routes**

In `frontend/bus-booking-web/src/app/app.routes.ts`, add these two entries above the existing `booking-confirmation/:id` entry (or anywhere before the `**` fallback):

```typescript
{
  path: 'my-bookings',
  canMatch: [roleGuard(['customer'])],
  loadComponent: () => import('./features/customer/bookings-list/bookings-list-page.component')
    .then(m => m.BookingsListPageComponent)
},
{
  path: 'my-bookings/:id',
  canMatch: [roleGuard(['customer'])],
  loadComponent: () => import('./features/customer/booking-detail/booking-detail-page.component')
    .then(m => m.BookingDetailPageComponent)
},
```

(The detail page is created in Task 13; the route entry compiles fine without the file present until lazy-load time.)

- [ ] **Step 2: Add the navbar link**

In `frontend/bus-booking-web/src/app/shared/components/navbar/navbar.component.html`, inside the `@else` block where the logged-in links live, add a new link **before** the `Become operator` block:

```html
@if (isCustomer()) {
  <a mat-button routerLink="/my-bookings" routerLinkActive="active-link">My Bookings</a>
}
```

The full logged-in block (for context — keep the rest as-is):

```html
@if (isAdmin()) {
  <a mat-button routerLink="/admin" routerLinkActive="active-link">Admin Console</a>
}
@if (isOperator()) {
  <a mat-button routerLink="/operator" routerLinkActive="active-link">Operator portal</a>
}
@if (isCustomer()) {
  <a mat-button routerLink="/my-bookings" routerLinkActive="active-link">My Bookings</a>
}
@if (isCustomer()) {
  <a mat-button routerLink="/become-operator" routerLinkActive="active-link">Become operator</a>
}
```

- [ ] **Step 3: Type-check**

Run: `cd frontend/bus-booking-web && npx tsc --noEmit -p tsconfig.app.json`
Expected: no errors.

- [ ] **Step 4: Commit**

```bash
git add frontend/bus-booking-web/src/app/app.routes.ts \
        frontend/bus-booking-web/src/app/shared/components/navbar/navbar.component.html
git commit -m "feat(m6): wire /my-bookings routes + navbar link"
```

---

## Task 13: Frontend — `BookingDetailPageComponent`

**Files:**
- Create: `frontend/bus-booking-web/src/app/features/customer/booking-detail/booking-detail-page.component.ts`
- Create: `frontend/bus-booking-web/src/app/features/customer/booking-detail/booking-detail-page.component.html`

Loads a single booking detail; shows passenger lines, payment summary, cancellation/refund summary if applicable, a Cancel CTA that opens the dialog (Task 14), and a download-PDF button for confirmed bookings.

- [ ] **Step 1: TypeScript**

```typescript
// frontend/bus-booking-web/src/app/features/customer/booking-detail/booking-detail-page.component.ts
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar } from '@angular/material/snack-bar';
import { BookingDetailDto, BookingsApiService } from '../../../core/api/bookings.api';
import { BookingStatusBadgeComponent } from '../../../shared/components/booking-status-badge/booking-status-badge.component';
import { AuthTokenStore } from '../../../core/auth/auth-token.store';
import { CancelBookingDialogComponent, CancelDialogResult } from './cancel-booking-dialog.component';

@Component({
  selector: 'app-booking-detail-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    CurrencyPipe,
    DatePipe,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatDialogModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    BookingStatusBadgeComponent
  ],
  templateUrl: './booking-detail-page.component.html'
})
export class BookingDetailPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(BookingsApiService);
  private readonly dialog = inject(MatDialog);
  private readonly tokens = inject(AuthTokenStore);
  private readonly snack = inject(MatSnackBar);

  readonly booking = signal<BookingDetailDto | null>(null);
  readonly loading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  readonly canCancel = computed(() => this.booking()?.status === 'confirmed');
  readonly canDownloadTicket = computed(() => {
    const s = this.booking()?.status;
    return s === 'confirmed' || s === 'completed';
  });
  readonly isCancelled = computed(() => {
    const s = this.booking()?.status;
    return s === 'cancelled' || s === 'cancelled_by_operator';
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) { this.loading.set(false); return; }
    this.fetch(id);
  }

  private fetch(id: string): void {
    this.loading.set(true);
    this.api.getBooking(id).subscribe({
      next: (b) => { this.booking.set(b); this.loading.set(false); },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(err?.error?.error?.message ?? 'Booking not found');
      }
    });
  }

  openCancelDialog(): void {
    const b = this.booking();
    if (!b) return;
    const ref = this.dialog.open<CancelBookingDialogComponent, { bookingId: string; bookingCode: string; totalAmount: number }, CancelDialogResult>(
      CancelBookingDialogComponent,
      {
        width: '480px',
        data: { bookingId: b.bookingId, bookingCode: b.bookingCode, totalAmount: b.totalAmount }
      }
    );
    ref.afterClosed().subscribe((result) => {
      if (result?.cancelled && result.detail) {
        this.booking.set(result.detail);
        this.snack.open('Booking cancelled. Refund email is on its way.', 'Dismiss', { duration: 4000 });
      }
    });
  }

  downloadTicket(): void {
    const b = this.booking();
    if (!b) return;
    const url = this.api.getTicketUrl(b.bookingId);
    fetch(url, { headers: { Authorization: `Bearer ${this.tokens.token() ?? ''}` } })
      .then(r => r.blob())
      .then(blob => {
        const link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = `ticket-${b.bookingCode}.pdf`;
        document.body.appendChild(link);
        link.click();
        link.remove();
      });
  }
}
```

- [ ] **Step 2: Template**

```html
<!-- frontend/bus-booking-web/src/app/features/customer/booking-detail/booking-detail-page.component.html -->
<div class="max-w-3xl mx-auto p-4 md:p-6 space-y-4">
  <a routerLink="/my-bookings" mat-button class="inline-flex items-center gap-1">
    <mat-icon>arrow_back</mat-icon>
    Back to all bookings
  </a>

  @if (loading()) {
    <div class="flex justify-center py-12">
      <mat-progress-spinner diameter="36" mode="indeterminate"></mat-progress-spinner>
    </div>
  } @else if (errorMessage()) {
    <div class="bg-rose-50 border border-rose-200 text-rose-800 p-4 rounded">
      {{ errorMessage() }}
    </div>
  } @else if (booking(); as b) {
    <mat-card class="border border-slate-200">
      <div class="p-5 border-b border-slate-200 flex flex-wrap items-start justify-between gap-3">
        <div>
          <div class="text-xs text-slate-500 font-mono">{{ b.bookingCode }}</div>
          <h1 class="text-xl md:text-2xl font-bold m-0 mt-1">
            {{ b.sourceCity }} → {{ b.destinationCity }}
          </h1>
          <div class="text-sm text-slate-600 mt-1">
            {{ b.tripDate | date: 'fullDate' }} · {{ b.departureTime.substring(0,5) }} → {{ b.arrivalTime.substring(0,5) }}
          </div>
        </div>
        <app-booking-status-badge [status]="b.status" />
      </div>

      <div class="p-5 grid gap-4 md:grid-cols-2">
        <div>
          <div class="text-xs text-slate-500 uppercase tracking-wide">Bus</div>
          <div class="font-medium">{{ b.busName }}</div>
          <div class="text-sm text-slate-600">{{ b.operatorName }}</div>
        </div>
        <div>
          <div class="text-xs text-slate-500 uppercase tracking-wide">Booked</div>
          <div class="font-medium">{{ b.createdAt | date: 'mediumDate' }}</div>
          @if (b.confirmedAt) {
            <div class="text-sm text-slate-600">Confirmed {{ b.confirmedAt | date: 'shortTime' }}</div>
          }
        </div>
      </div>

      <div class="px-5 pb-5">
        <div class="text-xs text-slate-500 uppercase tracking-wide mb-2">Passengers</div>
        <div class="border border-slate-200 rounded divide-y divide-slate-200">
          @for (s of b.seats; track s.seatNumber) {
            <div class="px-4 py-3 flex items-center justify-between">
              <div>
                <span class="font-mono text-sm bg-slate-100 px-2 py-0.5 rounded">{{ s.seatNumber }}</span>
                <span class="ml-3 font-medium">{{ s.passengerName }}</span>
              </div>
              <div class="text-xs text-slate-500">{{ s.passengerAge }} · {{ s.passengerGender }}</div>
            </div>
          }
        </div>
      </div>

      <div class="px-5 pb-5">
        <div class="text-xs text-slate-500 uppercase tracking-wide mb-2">Payment</div>
        <div class="border border-slate-200 rounded p-4 grid grid-cols-2 text-sm gap-y-1">
          <div>Fare ({{ b.seatCount }} × seat)</div><div class="text-right">{{ b.totalFare | currency: 'INR' }}</div>
          <div>Platform fee</div><div class="text-right">{{ b.platformFee | currency: 'INR' }}</div>
          <div class="font-semibold">Total</div><div class="text-right font-semibold">{{ b.totalAmount | currency: 'INR' }}</div>
        </div>
      </div>

      @if (isCancelled()) {
        <div class="mx-5 mb-5 border border-rose-200 bg-rose-50 rounded p-4 text-sm">
          <div class="font-semibold text-rose-900 mb-1">Cancelled</div>
          <div>On {{ b.cancelledAt | date: 'medium' }}</div>
          @if (b.cancellationReason) {
            <div class="mt-1">Reason: {{ b.cancellationReason }}</div>
          }
          @if (b.refundAmount !== null) {
            <div class="mt-2">
              Refund: <strong>{{ b.refundAmount | currency: 'INR' }}</strong>
              <span class="text-rose-700">({{ b.refundStatus }})</span>
            </div>
          }
        </div>
      }

      <div class="px-5 pb-5 flex flex-wrap gap-2">
        @if (canDownloadTicket()) {
          <button mat-flat-button color="primary" (click)="downloadTicket()">
            <mat-icon class="mr-1">download</mat-icon>
            Download PDF ticket
          </button>
        }
        @if (canCancel()) {
          <button mat-stroked-button color="warn" (click)="openCancelDialog()" matTooltip="See refund estimate before confirming">
            Cancel booking
          </button>
        }
      </div>
    </mat-card>
  }
</div>
```

- [ ] **Step 3: Type-check**

Run: `cd frontend/bus-booking-web && npx tsc --noEmit -p tsconfig.app.json`
Expected: error — `CancelBookingDialogComponent` not found. (That's Task 14 — keep going.)

- [ ] **Step 4: Commit (defer build verification to after Task 14)**

```bash
git add frontend/bus-booking-web/src/app/features/customer/booking-detail/booking-detail-page.component.ts \
        frontend/bus-booking-web/src/app/features/customer/booking-detail/booking-detail-page.component.html
git commit -m "feat(m6): add booking-detail page with cancel CTA"
```

---

## Task 14: Frontend — `CancelBookingDialogComponent`

**Files:**
- Create: `frontend/bus-booking-web/src/app/features/customer/booking-detail/cancel-booking-dialog.component.ts`
- Create: `frontend/bus-booking-web/src/app/features/customer/booking-detail/cancel-booking-dialog.component.html`

Modal dialog: loads `getRefundPreview` on open, shows the projected refund (or block reason), takes an optional reason, posts the cancel.

- [ ] **Step 1: TypeScript**

```typescript
// frontend/bus-booking-web/src/app/features/customer/booking-detail/cancel-booking-dialog.component.ts
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { BookingDetailDto, BookingsApiService, RefundPreviewDto } from '../../../core/api/bookings.api';

export interface CancelDialogData {
  bookingId: string;
  bookingCode: string;
  totalAmount: number;
}

export interface CancelDialogResult {
  cancelled: boolean;
  detail?: BookingDetailDto;
}

@Component({
  selector: 'app-cancel-booking-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    CurrencyPipe,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './cancel-booking-dialog.component.html'
})
export class CancelBookingDialogComponent implements OnInit {
  private readonly api = inject(BookingsApiService);
  private readonly fb = inject(FormBuilder);
  private readonly ref = inject(MatDialogRef<CancelBookingDialogComponent, CancelDialogResult>);
  readonly data = inject<CancelDialogData>(MAT_DIALOG_DATA);

  readonly loadingPreview = signal(true);
  readonly preview = signal<RefundPreviewDto | null>(null);
  readonly previewError = signal<string | null>(null);
  readonly submitting = signal(false);
  readonly submitError = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    reason: ['', [Validators.maxLength(500)]]
  });

  ngOnInit(): void {
    this.api.getRefundPreview(this.data.bookingId).subscribe({
      next: (p) => { this.preview.set(p); this.loadingPreview.set(false); },
      error: (err) => {
        this.loadingPreview.set(false);
        this.previewError.set(err?.error?.error?.message ?? 'Could not load refund preview');
      }
    });
  }

  confirm(): void {
    if (this.submitting() || !this.preview()?.cancellable) return;
    this.submitting.set(true);
    this.submitError.set(null);

    const reason = this.form.controls.reason.value.trim();

    this.api.cancelBooking(this.data.bookingId, { reason: reason.length > 0 ? reason : null }).subscribe({
      next: (detail) => {
        this.submitting.set(false);
        this.ref.close({ cancelled: true, detail });
      },
      error: (err) => {
        this.submitting.set(false);
        this.submitError.set(err?.error?.error?.message ?? 'Cancellation failed');
      }
    });
  }

  dismiss(): void {
    this.ref.close({ cancelled: false });
  }
}
```

- [ ] **Step 2: Template**

```html
<!-- frontend/bus-booking-web/src/app/features/customer/booking-detail/cancel-booking-dialog.component.html -->
<h2 mat-dialog-title>Cancel booking {{ data.bookingCode }}</h2>

<mat-dialog-content class="space-y-4">
  @if (loadingPreview()) {
    <div class="flex justify-center py-6">
      <mat-progress-spinner diameter="32" mode="indeterminate"></mat-progress-spinner>
    </div>
  } @else if (previewError()) {
    <div class="bg-rose-50 border border-rose-200 text-rose-800 p-3 rounded text-sm">
      {{ previewError() }}
    </div>
  } @else if (preview(); as p) {
    @if (!p.cancellable) {
      <div class="bg-amber-50 border border-amber-200 text-amber-900 p-3 rounded text-sm">
        <div class="font-semibold mb-1">Cannot cancel right now</div>
        <div>{{ p.blockReason }}</div>
      </div>
    } @else {
      <div class="border border-slate-200 rounded p-4 space-y-1 text-sm">
        <div class="flex justify-between"><span>Total paid</span><span>{{ p.totalAmount | currency: 'INR' }}</span></div>
        <div class="flex justify-between"><span>Refund percent</span><span>{{ p.refundPercent }}%</span></div>
        <div class="flex justify-between font-semibold text-emerald-800">
          <span>You'll receive</span><span>{{ p.refundAmount | currency: 'INR' }}</span>
        </div>
        <div class="text-xs text-slate-500 pt-2">
          Refunds typically reflect in your original payment method in 5–7 business days.
        </div>
      </div>

      <form [formGroup]="form" class="pt-1">
        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Reason (optional)</mat-label>
          <textarea matInput rows="3" formControlName="reason" maxlength="500"></textarea>
        </mat-form-field>
      </form>
    }

    @if (submitError()) {
      <div class="bg-rose-50 border border-rose-200 text-rose-800 p-3 rounded text-sm">
        {{ submitError() }}
      </div>
    }
  }
</mat-dialog-content>

<mat-dialog-actions align="end">
  <button mat-button (click)="dismiss()" [disabled]="submitting()">Keep booking</button>
  <button mat-flat-button color="warn"
          (click)="confirm()"
          [disabled]="submitting() || loadingPreview() || !preview()?.cancellable">
    {{ submitting() ? 'Cancelling…' : 'Confirm cancel' }}
  </button>
</mat-dialog-actions>
```

- [ ] **Step 3: Type-check + frontend build**

Run: `cd frontend/bus-booking-web && npx tsc --noEmit -p tsconfig.app.json && npx ng build --configuration development`
Expected: type-check clean; build succeeds.

- [ ] **Step 4: Commit**

```bash
git add frontend/bus-booking-web/src/app/features/customer/booking-detail/cancel-booking-dialog.component.ts \
        frontend/bus-booking-web/src/app/features/customer/booking-detail/cancel-booking-dialog.component.html
git commit -m "feat(m6): add cancel-booking dialog with refund preview"
```

---

## Task 15: Frontend — confirmation page link to "My Bookings"

**Files:**
- Modify: `frontend/bus-booking-web/src/app/features/customer/booking-confirmation/booking-confirmation.component.html`

- [ ] **Step 1: Add the link inside the action row**

Open the file. Inside the `<div class="p-4 flex flex-wrap gap-2 border-t border-emerald-200">` block (the one that already contains the "Download PDF ticket" button and the "Back to home" link), add a new link **between** the download button and the home link:

```html
<a mat-stroked-button routerLink="/my-bookings">View all bookings</a>
```

The full updated action row:

```html
<div class="p-4 flex flex-wrap gap-2 border-t border-emerald-200">
  <button mat-flat-button color="primary" (click)="downloadTicket()">Download PDF ticket</button>
  <a mat-stroked-button routerLink="/my-bookings">View all bookings</a>
  <a mat-stroked-button routerLink="/">Back to home</a>
</div>
```

- [ ] **Step 2: Type-check**

Run: `cd frontend/bus-booking-web && npx tsc --noEmit -p tsconfig.app.json`
Expected: no errors. (`RouterLink` is already imported in the confirmation component.)

- [ ] **Step 3: Commit**

```bash
git add frontend/bus-booking-web/src/app/features/customer/booking-confirmation/booking-confirmation.component.html
git commit -m "feat(m6): link 'View all bookings' from confirmation page"
```

---

## Task 16: Frontend quality gate + smoke test

- [ ] **Step 1: Type-check the whole frontend**

Run: `cd frontend/bus-booking-web && npx tsc --noEmit -p tsconfig.app.json`
Expected: clean.

- [ ] **Step 2: Production build**

Run: `cd frontend/bus-booking-web && npx ng build --configuration development`
Expected: success.

- [ ] **Step 3: Existing unit tests still pass**

Run: `cd frontend/bus-booking-web && npx ng test --watch=false --browsers=ChromeHeadless`
Expected: all green. (No new specs added in M6 — the existing three still run.)

- [ ] **Step 4: Manual smoke test (requires live Razorpay test keys + Resend key in `appsettings.Development.json`)**

1. `cd backend/BusBooking.Api && dotnet ef database update && dotnet run` (one shell)
2. `cd frontend/bus-booking-web && npx ng serve --port 4200` (another shell)
3. Log in as a seeded customer that already has a confirmed booking from M5 (or run an end-to-end M5 flow first).
4. Click the new **My Bookings** link in the navbar → list page renders with three tabs.
5. Confirm Upcoming shows the booking; Past tab is empty; Cancelled tab is empty.
6. Click **View** on a row → detail page renders with passenger lines + payment summary + Download PDF + Cancel buttons.
7. Click **Cancel booking** → dialog opens, calls `/refund-preview`, shows projected refund (≈80% if the trip is several days away).
8. Optionally type a reason, click **Confirm cancel** → dialog closes, snackbar shows confirmation, status badge flips to **Cancelled**, the page now shows the rose-coloured "Cancelled" panel with the refund amount.
9. Switch tabs: the booking is no longer in **Upcoming**, now appears in **Cancelled**.
10. Open Resend dashboard (or check the `notifications` table in the DB) — a `cancelled` notification row should be present.

(No commit — verification only.)

---

## Post-plan verification

After all 16 tasks:

- [ ] **Backend build** — `dotnet build backend/BusBooking.Api/BusBooking.Api.csproj` ⇒ succeeds
- [ ] **Backend tests** — `dotnet test backend/BusBooking.Api.Tests/BusBooking.Api.Tests.csproj` ⇒ all green (M0–M6)
- [ ] **Frontend typecheck** — `cd frontend/bus-booking-web && npx tsc --noEmit -p tsconfig.app.json` ⇒ no errors
- [ ] **Frontend build** — `cd frontend/bus-booking-web && npx ng build --configuration development` ⇒ succeeds
- [ ] **Frontend unit tests** — `cd frontend/bus-booking-web && npx ng test --watch=false --browsers=ChromeHeadless` ⇒ all green
- [ ] **Manual demo** of the M6 outcome (see Task 16 step 4):
  1. Log in as a customer with a confirmed booking
  2. Open `/my-bookings`
  3. Open the booking's detail page
  4. Cancel it through the dialog (refund preview visible)
  5. See the booking move from **Upcoming** to **Cancelled** with refund displayed
  6. Verify a cancellation email was sent (Resend dashboard or the `notifications` table)

---

## What's deferred to later milestones

- **M7** — Operator views (operator's bookings list, revenue) and the operator-facing cancel cascade are out of scope for M6.
- **M8** — Admin disable-operator cascade (which also issues refunds, but for many bookings at once) reuses `IRazorpayClient.CreateRefundAsync` and the refund-status fields introduced here, but adds a separate code path.
- **M9** — Polish, additional unit tests, README updates, seed-data improvements.
