# M8 — Admin Operator Cascade, Platform Revenue & Cross-Operator Bookings Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. **Work directly on `main` — do NOT create a feature branch.** Commit messages MUST NOT include a `Co-Authored-By: Claude` trailer.

**Goal:** Deliver the M8 demoable outcome — an admin can (1) disable an operator, which atomically retires their buses, cancels future confirmed bookings as `cancelled_by_operator`, queues full refunds + emails, (2) re-enable the operator (role flag only, no booking reinstatement), (3) see cross-operator bookings, and (4) see platform revenue (GMV + platform-fee income) with a date-range filter.

**Architecture:** Three new operator-domain service interfaces (`IAdminOperatorService`, `IAdminRevenueService`, `IAdminBookingService`) scoped under `/api/v1/admin/*`. The disable flow is one DB transaction (stamp `users.operator_disabled_at`, set bus `operational_status='retired'`, flip each future confirmed booking to `cancelled_by_operator` with `RefundAmount = TotalAmount` + `RefundStatus='pending'`, write audit rows), followed by **post-commit** async side effects (Razorpay refund per booking, Resend emails to the operator and each affected customer). Side effects never roll back the DB commit — a Razorpay outage leaves refunds in `pending`/`failed` status for manual recovery, consistent with the existing customer-cancel flow. Two new notification methods + two new audit action constants are added. Revenue and cross-operator booking reads use `AsNoTracking` LINQ over existing tables; no new migrations.

**Tech Stack:** .NET 9 · EF Core 9 · Npgsql · xUnit · FluentAssertions · `Microsoft.AspNetCore.Mvc.Testing` · Angular 20 (standalone + Signals) · Angular Material (`MatTable`, `MatFormField`, `MatSelect`, `MatDatepicker`, `MatDialog`) · Tailwind.

---

## File map

### New backend files

| Path | Responsibility |
|---|---|
| `backend/BusBooking.Api/Dtos/AdminOperatorListItemDto.cs` | Per-operator row: id, name, email, disabled flag, bus counts |
| `backend/BusBooking.Api/Dtos/DisableOperatorRequest.cs` | `{ reason? }` body for disable |
| `backend/BusBooking.Api/Dtos/AdminRevenueResponseDto.cs` | `{ dateFrom, dateTo, gmv, platformFeeIncome, confirmedBookings, byOperator[] }` |
| `backend/BusBooking.Api/Dtos/AdminRevenueOperatorItemDto.cs` | Per-operator revenue row |
| `backend/BusBooking.Api/Dtos/AdminBookingListItemDto.cs` | Cross-operator booking row (adds operator name vs. `OperatorBookingListItemDto`) |
| `backend/BusBooking.Api/Dtos/AdminBookingListResponseDto.cs` | Paginated wrapper |
| `backend/BusBooking.Api/Services/IAdminOperatorService.cs` | List / disable / enable contract |
| `backend/BusBooking.Api/Services/AdminOperatorService.cs` | Cascade implementation |
| `backend/BusBooking.Api/Services/IAdminRevenueService.cs` | Revenue contract |
| `backend/BusBooking.Api/Services/AdminRevenueService.cs` | GMV + platform-fee income query |
| `backend/BusBooking.Api/Services/IAdminBookingService.cs` | Cross-operator bookings contract |
| `backend/BusBooking.Api/Services/AdminBookingService.cs` | Cross-operator bookings query |
| `backend/BusBooking.Api/Controllers/AdminOperatorsController.cs` | `GET/POST /api/v1/admin/operators[…/disable][…/enable]` |
| `backend/BusBooking.Api/Controllers/AdminRevenueController.cs` | `GET /api/v1/admin/revenue` |
| `backend/BusBooking.Api/Controllers/AdminBookingsController.cs` | `GET /api/v1/admin/bookings` |

### Modified backend files

- `backend/BusBooking.Api/Models/AuditAction.cs` — add `OperatorDisabled`, `OperatorEnabled` constants
- `backend/BusBooking.Api/Services/INotificationSender.cs` — add `SendOperatorDisabledAsync`, `SendBookingCancelledByOperatorAsync`
- `backend/BusBooking.Api/Services/LoggingNotificationSender.cs` — implement the two new methods
- `backend/BusBooking.Api/Program.cs` — register the three new services

### New test files

| Path | Responsibility |
|---|---|
| `backend/BusBooking.Api.Tests/Integration/AdminOperatorsTests.cs` | List, disable cascade, re-enable (no reinstatement), auth |
| `backend/BusBooking.Api.Tests/Integration/AdminRevenueTests.cs` | GMV, platform-fee income, date range, exclude cancelled, auth |
| `backend/BusBooking.Api.Tests/Integration/AdminBookingsTests.cs` | Cross-operator listing, filters, auth |

### New frontend files

| Path | Responsibility |
|---|---|
| `frontend/bus-booking-web/src/app/core/api/admin-operators.api.ts` | Admin operators HTTP client |
| `frontend/bus-booking-web/src/app/core/api/admin-revenue.api.ts` | Admin revenue HTTP client |
| `frontend/bus-booking-web/src/app/core/api/admin-bookings.api.ts` | Admin bookings HTTP client |
| `frontend/bus-booking-web/src/app/features/admin/operators/admin-operators-page.component.ts` | Operators table with disable/enable actions |
| `frontend/bus-booking-web/src/app/features/admin/operators/admin-operators-page.component.html` | Template |
| `frontend/bus-booking-web/src/app/features/admin/operators/disable-operator-dialog.component.ts` | Confirmation dialog with reason textarea |
| `frontend/bus-booking-web/src/app/features/admin/revenue/admin-revenue-page.component.ts` | Revenue summary + per-operator breakdown |
| `frontend/bus-booking-web/src/app/features/admin/revenue/admin-revenue-page.component.html` | Template |
| `frontend/bus-booking-web/src/app/features/admin/bookings/admin-bookings-page.component.ts` | Cross-operator bookings table |
| `frontend/bus-booking-web/src/app/features/admin/bookings/admin-bookings-page.component.html` | Template |

### Modified frontend files

- `frontend/bus-booking-web/src/app/app.routes.ts` — add `operators`, `revenue`, `bookings` children under `admin`
- `frontend/bus-booking-web/src/app/features/admin/admin-dashboard/admin-dashboard.component.html` — add three new tiles

---

## Task 1: Audit-action constants + notification interface

**Files:**
- Modify: `backend/BusBooking.Api/Models/AuditAction.cs`
- Modify: `backend/BusBooking.Api/Services/INotificationSender.cs`

- [ ] **Step 1: Add two new constants to `AuditAction.cs`**

Replace the file contents with:

```csharp
namespace BusBooking.Api.Models;

public static class AuditAction
{
    public const string OperatorRequestApproved = "OPERATOR_REQUEST_APPROVED";
    public const string OperatorRequestRejected = "OPERATOR_REQUEST_REJECTED";
    public const string OperatorOfficeCreated = "OFFICE_CREATED";
    public const string OperatorOfficeDeleted = "OFFICE_DELETED";
    public const string BusCreated = "BUS_CREATED";
    public const string BusApproved = "BUS_APPROVED";
    public const string BusRejected = "BUS_REJECTED";
    public const string BusStatusChanged = "BUS_STATUS_CHANGED";
    public const string OperatorDisabled = "OPERATOR_DISABLED";
    public const string OperatorEnabled = "OPERATOR_ENABLED";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        OperatorRequestApproved, OperatorRequestRejected,
        OperatorOfficeCreated, OperatorOfficeDeleted,
        BusCreated, BusApproved, BusRejected, BusStatusChanged,
        OperatorDisabled, OperatorEnabled
    };
}
```

- [ ] **Step 2: Add two methods to `INotificationSender.cs`**

Replace the file contents with:

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
    Task SendOperatorDisabledAsync(User operatorUser, string? reason, CancellationToken ct = default);
    Task SendBookingCancelledByOperatorAsync(
        User customer,
        BookingDetailDto booking,
        decimal refundAmount,
        CancellationToken ct = default);
}
```

- [ ] **Step 3: Build to confirm the project still compiles after interface changes (implementation stub comes next)**

Run:

```bash
cd backend && dotnet build BusBooking.Api/BusBooking.Api.csproj 2>&1 | tail -10
```

Expected: build **fails** with `CS0535` — `LoggingNotificationSender` does not implement the two new methods. That is the signal to proceed to Task 2.

---

## Task 2: Implement the two new notification methods

**Files:**
- Modify: `backend/BusBooking.Api/Services/LoggingNotificationSender.cs`

- [ ] **Step 1: Append the two method implementations**

Open `backend/BusBooking.Api/Services/LoggingNotificationSender.cs`. Just before the final closing `}` of the class (after the existing `BuildBookingConfirmedHtml` method), insert:

```csharp
    public async Task SendOperatorDisabledAsync(User operatorUser, string? reason, CancellationToken ct = default)
    {
        var subject = "Your operator account has been disabled";
        var html = BuildOperatorDisabledHtml(operatorUser, reason);
        var result = await _email.SendAsync(
            operatorUser.Email,
            subject,
            html,
            Array.Empty<ResendAttachment>(),
            ct);

        _db.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = operatorUser.Id,
            Type = NotificationType.OperatorDisabled,
            Channel = NotificationChannel.Email,
            ToAddress = operatorUser.Email,
            Subject = subject,
            ResendMessageId = result.MessageId,
            Status = result.Success ? "sent" : "failed",
            Error = result.Error,
            CreatedAt = _time.GetUtcNow().UtcDateTime
        });
        await _db.SaveChangesAsync(ct);

        if (!result.Success)
            _log.LogWarning("Operator-disabled email failed for {Email}: {Error}", operatorUser.Email, result.Error);
    }

    public async Task SendBookingCancelledByOperatorAsync(
        User customer,
        BookingDetailDto booking,
        decimal refundAmount,
        CancellationToken ct = default)
    {
        var subject = $"Booking cancelled by operator — {booking.BookingCode}";
        var html = BuildOperatorCancelledBookingHtml(customer, booking, refundAmount);

        var result = await _email.SendAsync(
            customer.Email,
            subject,
            html,
            Array.Empty<ResendAttachment>(),
            ct);

        _db.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = customer.Id,
            Type = NotificationType.Cancelled,
            Channel = NotificationChannel.Email,
            ToAddress = customer.Email,
            Subject = subject,
            ResendMessageId = result.MessageId,
            Status = result.Success ? "sent" : "failed",
            Error = result.Error,
            CreatedAt = _time.GetUtcNow().UtcDateTime
        });
        await _db.SaveChangesAsync(ct);

        if (!result.Success)
            _log.LogWarning("Operator-cancel email failed for {BookingCode}: {Error}",
                booking.BookingCode, result.Error);
    }

    private static string BuildOperatorDisabledHtml(User user, string? reason)
    {
        var sb = new StringBuilder();
        sb.Append("<div style=\"font-family:Arial,sans-serif\">");
        sb.Append($"<h2>Operator account disabled</h2>");
        sb.Append($"<p>Hi {System.Net.WebUtility.HtmlEncode(user.Name)},</p>");
        sb.Append("<p>Your operator account has been disabled by an administrator. Your buses have been retired and all upcoming confirmed bookings have been cancelled with full refunds to the customers.</p>");
        if (!string.IsNullOrWhiteSpace(reason))
            sb.Append($"<p><b>Reason:</b> {System.Net.WebUtility.HtmlEncode(reason)}</p>");
        sb.Append("<p>Your customer account remains active. Contact support if you believe this is an error.</p>");
        sb.Append("</div>");
        return sb.ToString();
    }

    private static string BuildOperatorCancelledBookingHtml(User user, BookingDetailDto b, decimal refundAmount)
    {
        var sb = new StringBuilder();
        sb.Append("<div style=\"font-family:Arial,sans-serif\">");
        sb.Append($"<h2>Booking cancelled by operator: {b.BookingCode}</h2>");
        sb.Append($"<p>Hi {System.Net.WebUtility.HtmlEncode(user.Name)},</p>");
        sb.Append("<p>Your booking has been cancelled because the operator is no longer available on our platform.</p>");
        sb.Append("<hr/>");
        sb.Append($"<p><b>Trip:</b> {System.Net.WebUtility.HtmlEncode(b.SourceCity)} → {System.Net.WebUtility.HtmlEncode(b.DestinationCity)}</p>");
        sb.Append($"<p><b>Date:</b> {b.TripDate}</p>");
        sb.Append($"<p><b>Bus:</b> {System.Net.WebUtility.HtmlEncode(b.BusName)} (Operator: {System.Net.WebUtility.HtmlEncode(b.OperatorName)})</p>");
        sb.Append($"<p><b>Seats:</b> {string.Join(", ", b.Seats.Select(s => System.Net.WebUtility.HtmlEncode(s.SeatNumber)))}</p>");
        sb.Append($"<p><b>Full refund:</b> ₹{refundAmount:0.00}</p>");
        sb.Append("<p>Refunds typically reflect in your account in 5–7 business days.</p>");
        sb.Append("</div>");
        return sb.ToString();
    }
```

- [ ] **Step 2: Build to confirm the project compiles**

```bash
cd backend && dotnet build BusBooking.Api/BusBooking.Api.csproj 2>&1 | tail -5
```

Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add backend/BusBooking.Api/Models/AuditAction.cs \
        backend/BusBooking.Api/Services/INotificationSender.cs \
        backend/BusBooking.Api/Services/LoggingNotificationSender.cs
git commit -m "feat(m8): add operator-disabled audit actions and notification methods"
```

---

## Task 3: Admin operator DTOs + service interface

**Files:**
- Create: `backend/BusBooking.Api/Dtos/AdminOperatorListItemDto.cs`
- Create: `backend/BusBooking.Api/Dtos/DisableOperatorRequest.cs`
- Create: `backend/BusBooking.Api/Services/IAdminOperatorService.cs`

- [ ] **Step 1: Create `AdminOperatorListItemDto.cs`**

```csharp
namespace BusBooking.Api.Dtos;

public record AdminOperatorListItemDto(
    Guid UserId,
    string Name,
    string Email,
    DateTime CreatedAt,
    bool IsDisabled,
    DateTime? DisabledAt,
    int TotalBuses,
    int ActiveBuses,
    int RetiredBuses);
```

- [ ] **Step 2: Create `DisableOperatorRequest.cs`**

```csharp
namespace BusBooking.Api.Dtos;

public record DisableOperatorRequest(string? Reason);
```

- [ ] **Step 3: Create `IAdminOperatorService.cs`**

```csharp
using BusBooking.Api.Dtos;

namespace BusBooking.Api.Services;

public interface IAdminOperatorService
{
    Task<IReadOnlyList<AdminOperatorListItemDto>> ListAsync(CancellationToken ct);
    Task<AdminOperatorListItemDto> DisableAsync(
        Guid adminId, Guid operatorUserId, string? reason, CancellationToken ct);
    Task<AdminOperatorListItemDto> EnableAsync(
        Guid adminId, Guid operatorUserId, CancellationToken ct);
}
```

- [ ] **Step 4: Build**

```bash
cd backend && dotnet build BusBooking.Api/BusBooking.Api.csproj 2>&1 | tail -3
```

Expected: `Build succeeded.`

---

## Task 4: Write failing integration tests for admin operators (list + disable cascade + enable)

**Files:**
- Create: `backend/BusBooking.Api.Tests/Integration/AdminOperatorsTests.cs`

- [ ] **Step 1: Create the test file**

```csharp
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
        booking.RefundAmount.Should().Be(1000m); // full refund
        booking.RefundStatus.Should().Be(RefundStatus.Processed);
        booking.Payment!.Status.Should().Be(PaymentStatus.Refunded);

        _fx.Razorpay.CreatedRefunds.Should().ContainSingle()
            .Which.Amount.Should().Be(100000); // paise

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
```

- [ ] **Step 2: Run — confirm all seven tests FAIL (no controller yet)**

```bash
cd backend && dotnet test BusBooking.Api.Tests \
  --filter "FullyQualifiedName~AdminOperatorsTests" 2>&1 | tail -20
```

Expected: build succeeds (the test only references DTOs already defined in Task 3), all 7 tests fail — most with `404 NotFound`.

---

## Task 5: Implement `AdminOperatorService` (list + disable cascade + enable)

**Files:**
- Create: `backend/BusBooking.Api/Services/AdminOperatorService.cs`

- [ ] **Step 1: Create the service**

```csharp
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Infrastructure.Errors;
using BusBooking.Api.Infrastructure.Razorpay;
using BusBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Api.Services;

public class AdminOperatorService : IAdminOperatorService
{
    private readonly AppDbContext _db;
    private readonly IAuditLogWriter _audit;
    private readonly INotificationSender _notifier;
    private readonly IRazorpayClient _razorpay;
    private readonly TimeProvider _time;
    private readonly ILogger<AdminOperatorService> _log;

    public AdminOperatorService(
        AppDbContext db,
        IAuditLogWriter audit,
        INotificationSender notifier,
        IRazorpayClient razorpay,
        TimeProvider time,
        ILogger<AdminOperatorService> log)
    {
        _db = db;
        _audit = audit;
        _notifier = notifier;
        _razorpay = razorpay;
        _time = time;
        _log = log;
    }

    public async Task<IReadOnlyList<AdminOperatorListItemDto>> ListAsync(CancellationToken ct)
    {
        var rows = await _db.Users
            .AsNoTracking()
            .Where(u => u.Roles.Any(r => r.Role == Roles.Operator))
            .Select(u => new
            {
                u.Id, u.Name, u.Email, u.CreatedAt, u.OperatorDisabledAt,
                Buses = _db.Buses.Where(b => b.OperatorUserId == u.Id)
                    .Select(b => b.OperationalStatus).ToList()
            })
            .ToListAsync(ct);

        return rows.Select(r => new AdminOperatorListItemDto(
            r.Id, r.Name, r.Email, r.CreatedAt,
            IsDisabled: r.OperatorDisabledAt.HasValue,
            DisabledAt: r.OperatorDisabledAt,
            TotalBuses: r.Buses.Count,
            ActiveBuses: r.Buses.Count(s => s == BusOperationalStatus.Active),
            RetiredBuses: r.Buses.Count(s => s == BusOperationalStatus.Retired)))
            .OrderBy(o => o.Name)
            .ToList();
    }

    public async Task<AdminOperatorListItemDto> DisableAsync(
        Guid adminId, Guid operatorUserId, string? reason, CancellationToken ct)
    {
        var op = await _db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == operatorUserId, ct)
            ?? throw new NotFoundException("Operator not found");

        if (!op.Roles.Any(r => r.Role == Roles.Operator))
            throw new NotFoundException("User is not an operator");

        // Idempotent: already disabled → return current state without cascading again.
        if (op.OperatorDisabledAt.HasValue)
            return await LoadDtoAsync(op.Id, ct);

        var now = _time.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);

        op.OperatorDisabledAt = now;

        var buses = await _db.Buses
            .Where(b => b.OperatorUserId == op.Id
                     && b.OperationalStatus != BusOperationalStatus.Retired)
            .ToListAsync(ct);
        foreach (var bus in buses) bus.OperationalStatus = BusOperationalStatus.Retired;

        var bookingsToCancel = await _db.Bookings
            .Include(b => b.Payment)
            .Include(b => b.User)
            .Include(b => b.Seats)
            .Include(b => b.Trip).ThenInclude(t => t!.Schedule).ThenInclude(s => s!.Bus).ThenInclude(b => b!.Operator)
            .Include(b => b.Trip).ThenInclude(t => t!.Schedule).ThenInclude(s => s!.Route).ThenInclude(r => r!.SourceCity)
            .Include(b => b.Trip).ThenInclude(t => t!.Schedule).ThenInclude(s => s!.Route).ThenInclude(r => r!.DestinationCity)
            .Where(b => b.Trip!.Schedule!.Bus!.OperatorUserId == op.Id
                     && b.Status == BookingStatus.Confirmed
                     && b.Trip!.TripDate >= today)
            .ToListAsync(ct);

        foreach (var b in bookingsToCancel)
        {
            b.Status = BookingStatus.CancelledByOperator;
            b.CancelledAt = now;
            b.CancellationReason = reason ?? "Operator disabled by admin";
            b.RefundAmount = b.TotalAmount; // full refund on operator-disable
            b.RefundStatus = RefundStatus.Pending;
        }

        await _db.SaveChangesAsync(ct);
        await _audit.WriteAsync(adminId, AuditAction.OperatorDisabled,
            "user", op.Id,
            new { reason, cascadedBookings = bookingsToCancel.Count, retiredBuses = buses.Count }, ct);

        // Post-commit side effects. An outage here does not unwind the DB state.
        foreach (var b in bookingsToCancel)
        {
            await RefundBookingPostCommitAsync(b, now, ct);
        }

        try { await _notifier.SendOperatorDisabledAsync(op, reason, ct); }
        catch (Exception ex) { _log.LogError(ex, "Operator-disabled email failed for {Email}", op.Email); }

        foreach (var b in bookingsToCancel)
        {
            try
            {
                var detail = MapBookingDetail(b);
                await _notifier.SendBookingCancelledByOperatorAsync(
                    b.User, detail, b.RefundAmount ?? 0m, ct);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Operator-cancel email failed for booking {Code}", b.BookingCode);
            }
        }

        return await LoadDtoAsync(op.Id, ct);
    }

    public async Task<AdminOperatorListItemDto> EnableAsync(
        Guid adminId, Guid operatorUserId, CancellationToken ct)
    {
        var op = await _db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == operatorUserId, ct)
            ?? throw new NotFoundException("Operator not found");

        if (!op.Roles.Any(r => r.Role == Roles.Operator))
            throw new NotFoundException("User is not an operator");

        if (!op.OperatorDisabledAt.HasValue)
            return await LoadDtoAsync(op.Id, ct);

        op.OperatorDisabledAt = null;
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(adminId, AuditAction.OperatorEnabled,
            "user", op.Id, metadata: null, ct);

        return await LoadDtoAsync(op.Id, ct);
    }

    private async Task RefundBookingPostCommitAsync(Booking b, DateTime now, CancellationToken ct)
    {
        var refundAmount = b.RefundAmount ?? 0m;
        if (refundAmount <= 0m
            || b.Payment is not { Status: PaymentStatus.Captured, RazorpayPaymentId: { } payId })
        {
            b.RefundStatus = RefundStatus.Processed;
            await _db.SaveChangesAsync(ct);
            return;
        }

        try
        {
            await _razorpay.CreateRefundAsync(payId, (long)(refundAmount * 100m), ct);
            b.Payment.Status = PaymentStatus.Refunded;
            b.Payment.RefundedAt = now;
            b.RefundStatus = RefundStatus.Processed;
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Operator-cascade Razorpay refund failed for booking {Code}", b.BookingCode);
            b.RefundStatus = RefundStatus.Failed;
            await _db.SaveChangesAsync(ct);
        }
    }

    private async Task<AdminOperatorListItemDto> LoadDtoAsync(Guid operatorUserId, CancellationToken ct)
    {
        var row = (await ListAsync(ct)).FirstOrDefault(o => o.UserId == operatorUserId)
            ?? throw new NotFoundException("Operator not found");
        return row;
    }

    private static BookingDetailDto MapBookingDetail(Booking b)
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
}
```

- [ ] **Step 2: Build**

```bash
cd backend && dotnet build BusBooking.Api/BusBooking.Api.csproj 2>&1 | tail -5
```

Expected: `Build succeeded.`

---

## Task 6: Admin operators controller + registration + tests pass

**Files:**
- Create: `backend/BusBooking.Api/Controllers/AdminOperatorsController.cs`
- Modify: `backend/BusBooking.Api/Program.cs`

- [ ] **Step 1: Create the controller**

```csharp
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure.Auth;
using BusBooking.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.Api.Controllers;

[ApiController]
[Route("api/v1/admin/operators")]
[Authorize(Roles = "admin")]
public class AdminOperatorsController : ControllerBase
{
    private readonly IAdminOperatorService _service;
    private readonly ICurrentUserAccessor _me;

    public AdminOperatorsController(IAdminOperatorService service, ICurrentUserAccessor me)
    {
        _service = service;
        _me = me;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminOperatorListItemDto>>> List(
        CancellationToken ct)
        => Ok(await _service.ListAsync(ct));

    [HttpPost("{id:guid}/disable")]
    public async Task<ActionResult<AdminOperatorListItemDto>> Disable(
        Guid id,
        [FromBody] DisableOperatorRequest? body,
        CancellationToken ct)
        => Ok(await _service.DisableAsync(_me.UserId, id, body?.Reason, ct));

    [HttpPost("{id:guid}/enable")]
    public async Task<ActionResult<AdminOperatorListItemDto>> Enable(
        Guid id, CancellationToken ct)
        => Ok(await _service.EnableAsync(_me.UserId, id, ct));
}
```

- [ ] **Step 2: Register `IAdminOperatorService` in `Program.cs`**

In `backend/BusBooking.Api/Program.cs`, immediately after the line

```csharp
builder.Services.AddScoped<IOperatorBookingService, OperatorBookingService>();
```

add:

```csharp
builder.Services.AddScoped<IAdminOperatorService, AdminOperatorService>();
```

- [ ] **Step 3: Run the operator tests**

```bash
cd backend && dotnet test BusBooking.Api.Tests \
  --filter "FullyQualifiedName~AdminOperatorsTests" 2>&1 | tail -20
```

Expected: all 7 tests pass.

- [ ] **Step 4: Run the full backend test suite to check for regressions**

```bash
cd backend && dotnet test BusBooking.Api.Tests 2>&1 | tail -10
```

Expected: all tests pass.

- [ ] **Step 5: Commit**

```bash
git add backend/BusBooking.Api/Dtos/AdminOperatorListItemDto.cs \
        backend/BusBooking.Api/Dtos/DisableOperatorRequest.cs \
        backend/BusBooking.Api/Services/IAdminOperatorService.cs \
        backend/BusBooking.Api/Services/AdminOperatorService.cs \
        backend/BusBooking.Api/Controllers/AdminOperatorsController.cs \
        backend/BusBooking.Api/Program.cs \
        backend/BusBooking.Api.Tests/Integration/AdminOperatorsTests.cs
git commit -m "feat(m8): admin operator list + disable cascade + enable endpoints"
```

---

## Task 7: Admin revenue DTOs, service interface, failing tests

**Files:**
- Create: `backend/BusBooking.Api/Dtos/AdminRevenueOperatorItemDto.cs`
- Create: `backend/BusBooking.Api/Dtos/AdminRevenueResponseDto.cs`
- Create: `backend/BusBooking.Api/Services/IAdminRevenueService.cs`
- Create: `backend/BusBooking.Api.Tests/Integration/AdminRevenueTests.cs`

- [ ] **Step 1: Create `AdminRevenueOperatorItemDto.cs`**

```csharp
namespace BusBooking.Api.Dtos;

public record AdminRevenueOperatorItemDto(
    Guid OperatorUserId,
    string OperatorName,
    int ConfirmedBookings,
    decimal Gmv,
    decimal PlatformFeeIncome);
```

- [ ] **Step 2: Create `AdminRevenueResponseDto.cs`**

```csharp
namespace BusBooking.Api.Dtos;

public record AdminRevenueResponseDto(
    DateOnly DateFrom,
    DateOnly DateTo,
    int ConfirmedBookings,
    decimal Gmv,
    decimal PlatformFeeIncome,
    List<AdminRevenueOperatorItemDto> ByOperator);
```

- [ ] **Step 3: Create `IAdminRevenueService.cs`**

```csharp
using BusBooking.Api.Dtos;

namespace BusBooking.Api.Services;

public interface IAdminRevenueService
{
    Task<AdminRevenueResponseDto> GetAsync(DateOnly from, DateOnly to, CancellationToken ct);
}
```

- [ ] **Step 4: Create `AdminRevenueTests.cs`**

```csharp
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

        resp!.Gmv.Should().Be(1500m);           // 1000 + 500
        resp.PlatformFeeIncome.Should().Be(75m); // 50 + 25
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
```

- [ ] **Step 5: Build + run tests; confirm all FAIL (no controller yet)**

```bash
cd backend && dotnet test BusBooking.Api.Tests \
  --filter "FullyQualifiedName~AdminRevenueTests" 2>&1 | tail -15
```

Expected: all 5 tests fail with `404 NotFound`.

---

## Task 8: Implement `AdminRevenueService` + controller → tests pass

**Files:**
- Create: `backend/BusBooking.Api/Services/AdminRevenueService.cs`
- Create: `backend/BusBooking.Api/Controllers/AdminRevenueController.cs`
- Modify: `backend/BusBooking.Api/Program.cs`

- [ ] **Step 1: Create `AdminRevenueService.cs`**

```csharp
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Api.Services;

public class AdminRevenueService : IAdminRevenueService
{
    private readonly AppDbContext _db;

    public AdminRevenueService(AppDbContext db) => _db = db;

    public async Task<AdminRevenueResponseDto> GetAsync(
        DateOnly from, DateOnly to, CancellationToken ct)
    {
        var rows = await _db.Bookings
            .AsNoTracking()
            .Where(b => (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Completed)
                     && b.Trip!.TripDate >= from
                     && b.Trip!.TripDate <= to)
            .Include(b => b.Trip).ThenInclude(t => t!.Schedule).ThenInclude(s => s!.Bus).ThenInclude(b => b!.Operator)
            .ToListAsync(ct);

        var byOperator = rows
            .GroupBy(b => new
            {
                OpId = b.Trip!.Schedule!.Bus!.Operator!.Id,
                OpName = b.Trip!.Schedule!.Bus!.Operator!.Name
            })
            .Select(g => new AdminRevenueOperatorItemDto(
                g.Key.OpId,
                g.Key.OpName,
                g.Count(),
                g.Sum(b => b.TotalFare),
                g.Sum(b => b.PlatformFee)))
            .OrderByDescending(x => x.Gmv)
            .ToList();

        return new AdminRevenueResponseDto(
            from,
            to,
            rows.Count,
            rows.Sum(b => b.TotalFare),
            rows.Sum(b => b.PlatformFee),
            byOperator);
    }
}
```

- [ ] **Step 2: Create `AdminRevenueController.cs`**

```csharp
using BusBooking.Api.Dtos;
using BusBooking.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.Api.Controllers;

[ApiController]
[Route("api/v1/admin/revenue")]
[Authorize(Roles = "admin")]
public class AdminRevenueController : ControllerBase
{
    private readonly IAdminRevenueService _service;

    public AdminRevenueController(IAdminRevenueService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<AdminRevenueResponseDto>> Get(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var f = from ?? today.AddMonths(-1);
        var t = to ?? today;
        return Ok(await _service.GetAsync(f, t, ct));
    }
}
```

- [ ] **Step 3: Register `IAdminRevenueService` in `Program.cs`**

Immediately after the line added in Task 6 (`builder.Services.AddScoped<IAdminOperatorService, AdminOperatorService>();`), add:

```csharp
builder.Services.AddScoped<IAdminRevenueService, AdminRevenueService>();
```

- [ ] **Step 4: Run revenue tests**

```bash
cd backend && dotnet test BusBooking.Api.Tests \
  --filter "FullyQualifiedName~AdminRevenueTests" 2>&1 | tail -15
```

Expected: all 5 tests pass.

- [ ] **Step 5: Commit**

```bash
git add backend/BusBooking.Api/Dtos/AdminRevenueOperatorItemDto.cs \
        backend/BusBooking.Api/Dtos/AdminRevenueResponseDto.cs \
        backend/BusBooking.Api/Services/IAdminRevenueService.cs \
        backend/BusBooking.Api/Services/AdminRevenueService.cs \
        backend/BusBooking.Api/Controllers/AdminRevenueController.cs \
        backend/BusBooking.Api/Program.cs \
        backend/BusBooking.Api.Tests/Integration/AdminRevenueTests.cs
git commit -m "feat(m8): add admin revenue endpoint with per-operator breakdown"
```

---

## Task 9: Admin cross-operator bookings — DTOs, service, failing tests

**Files:**
- Create: `backend/BusBooking.Api/Dtos/AdminBookingListItemDto.cs`
- Create: `backend/BusBooking.Api/Dtos/AdminBookingListResponseDto.cs`
- Create: `backend/BusBooking.Api/Services/IAdminBookingService.cs`
- Create: `backend/BusBooking.Api.Tests/Integration/AdminBookingsTests.cs`

- [ ] **Step 1: Create `AdminBookingListItemDto.cs`**

```csharp
namespace BusBooking.Api.Dtos;

public record AdminBookingListItemDto(
    Guid BookingId,
    string BookingCode,
    Guid TripId,
    DateOnly TripDate,
    string SourceCity,
    string DestinationCity,
    Guid BusId,
    string BusName,
    Guid OperatorUserId,
    string OperatorName,
    string CustomerName,
    string CustomerEmail,
    int SeatCount,
    decimal TotalFare,
    decimal PlatformFee,
    decimal TotalAmount,
    string Status,
    DateTime CreatedAt);
```

- [ ] **Step 2: Create `AdminBookingListResponseDto.cs`**

```csharp
namespace BusBooking.Api.Dtos;

public record AdminBookingListResponseDto(
    List<AdminBookingListItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount);
```

- [ ] **Step 3: Create `IAdminBookingService.cs`**

```csharp
using BusBooking.Api.Dtos;

namespace BusBooking.Api.Services;

public interface IAdminBookingService
{
    Task<AdminBookingListResponseDto> ListAsync(
        Guid? operatorUserId,
        string? status,
        DateOnly? date,
        int page,
        int pageSize,
        CancellationToken ct);
}
```

- [ ] **Step 4: Create `AdminBookingsTests.cs`**

```csharp
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
```

- [ ] **Step 5: Run tests — confirm all FAIL (no controller yet)**

```bash
cd backend && dotnet test BusBooking.Api.Tests \
  --filter "FullyQualifiedName~AdminBookingsTests" 2>&1 | tail -15
```

Expected: all 5 tests fail.

---

## Task 10: Implement `AdminBookingService` + controller → tests pass

**Files:**
- Create: `backend/BusBooking.Api/Services/AdminBookingService.cs`
- Create: `backend/BusBooking.Api/Controllers/AdminBookingsController.cs`
- Modify: `backend/BusBooking.Api/Program.cs`

- [ ] **Step 1: Create `AdminBookingService.cs`**

```csharp
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Infrastructure.Errors;
using BusBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Api.Services;

public class AdminBookingService : IAdminBookingService
{
    private readonly AppDbContext _db;

    public AdminBookingService(AppDbContext db) => _db = db;

    public async Task<AdminBookingListResponseDto> ListAsync(
        Guid? operatorUserId,
        string? status,
        DateOnly? date,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var p = page < 1 ? 1 : page;
        var size = pageSize is < 1 or > 100 ? 20 : pageSize;

        if (!string.IsNullOrEmpty(status) && !BookingStatus.All.Contains(status))
            throw new BusinessRuleException("INVALID_STATUS", "Unknown booking status filter");

        var q = _db.Bookings
            .AsNoTracking()
            .Include(b => b.Trip).ThenInclude(t => t!.Schedule).ThenInclude(s => s!.Bus).ThenInclude(b => b!.Operator)
            .Include(b => b.Trip).ThenInclude(t => t!.Schedule).ThenInclude(s => s!.Route).ThenInclude(r => r!.SourceCity)
            .Include(b => b.Trip).ThenInclude(t => t!.Schedule).ThenInclude(s => s!.Route).ThenInclude(r => r!.DestinationCity)
            .Include(b => b.User)
            .AsQueryable();

        if (string.IsNullOrEmpty(status))
            q = q.Where(b => b.Status != BookingStatus.PendingPayment);
        else
            q = q.Where(b => b.Status == status);

        if (operatorUserId.HasValue)
            q = q.Where(b => b.Trip!.Schedule!.Bus!.OperatorUserId == operatorUserId.Value);

        if (date.HasValue)
            q = q.Where(b => b.Trip!.TripDate == date.Value);

        var total = await q.CountAsync(ct);
        var rows = await q
            .OrderByDescending(b => b.CreatedAt)
            .Skip((p - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        var items = rows.Select(b =>
        {
            var sched = b.Trip!.Schedule!;
            var route = sched.Route!;
            var bus = sched.Bus!;
            var op = bus.Operator!;
            return new AdminBookingListItemDto(
                b.Id,
                b.BookingCode,
                b.TripId,
                b.Trip.TripDate,
                route.SourceCity!.Name,
                route.DestinationCity!.Name,
                bus.Id,
                bus.BusName,
                op.Id,
                op.Name,
                b.User.Name,
                b.User.Email,
                b.SeatCount,
                b.TotalFare,
                b.PlatformFee,
                b.TotalAmount,
                b.Status,
                b.CreatedAt);
        }).ToList();

        return new AdminBookingListResponseDto(items, p, size, total);
    }
}
```

- [ ] **Step 2: Create `AdminBookingsController.cs`**

```csharp
using BusBooking.Api.Dtos;
using BusBooking.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.Api.Controllers;

[ApiController]
[Route("api/v1/admin/bookings")]
[Authorize(Roles = "admin")]
public class AdminBookingsController : ControllerBase
{
    private readonly IAdminBookingService _service;

    public AdminBookingsController(IAdminBookingService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<AdminBookingListResponseDto>> List(
        [FromQuery] Guid? operatorUserId,
        [FromQuery] string? status,
        [FromQuery] DateOnly? date,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await _service.ListAsync(operatorUserId, status, date, page, pageSize, ct));
}
```

- [ ] **Step 3: Register `IAdminBookingService` in `Program.cs`**

After `builder.Services.AddScoped<IAdminRevenueService, AdminRevenueService>();` added in Task 8, add:

```csharp
builder.Services.AddScoped<IAdminBookingService, AdminBookingService>();
```

- [ ] **Step 4: Run admin bookings tests**

```bash
cd backend && dotnet test BusBooking.Api.Tests \
  --filter "FullyQualifiedName~AdminBookingsTests" 2>&1 | tail -15
```

Expected: all 5 tests pass.

- [ ] **Step 5: Run the full backend test suite — check for regressions**

```bash
cd backend && dotnet test BusBooking.Api.Tests 2>&1 | tail -10
```

Expected: all tests pass.

- [ ] **Step 6: Commit**

```bash
git add backend/BusBooking.Api/Dtos/AdminBookingListItemDto.cs \
        backend/BusBooking.Api/Dtos/AdminBookingListResponseDto.cs \
        backend/BusBooking.Api/Services/IAdminBookingService.cs \
        backend/BusBooking.Api/Services/AdminBookingService.cs \
        backend/BusBooking.Api/Controllers/AdminBookingsController.cs \
        backend/BusBooking.Api/Program.cs \
        backend/BusBooking.Api.Tests/Integration/AdminBookingsTests.cs
git commit -m "feat(m8): add admin cross-operator bookings endpoint with filters"
```

---

## Task 11: Frontend API clients

**Files:**
- Create: `frontend/bus-booking-web/src/app/core/api/admin-operators.api.ts`
- Create: `frontend/bus-booking-web/src/app/core/api/admin-revenue.api.ts`
- Create: `frontend/bus-booking-web/src/app/core/api/admin-bookings.api.ts`

- [ ] **Step 1: Create `admin-operators.api.ts`**

```typescript
import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface AdminOperatorListItemDto {
  userId: string;
  name: string;
  email: string;
  createdAt: string;
  isDisabled: boolean;
  disabledAt: string | null;
  totalBuses: number;
  activeBuses: number;
  retiredBuses: number;
}

export interface DisableOperatorRequest {
  reason?: string | null;
}

@Injectable({ providedIn: 'root' })
export class AdminOperatorsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/admin/operators`;

  list(): Observable<AdminOperatorListItemDto[]> {
    return this.http.get<AdminOperatorListItemDto[]>(this.base);
  }

  disable(id: string, body: DisableOperatorRequest): Observable<AdminOperatorListItemDto> {
    return this.http.post<AdminOperatorListItemDto>(`${this.base}/${id}/disable`, body);
  }

  enable(id: string): Observable<AdminOperatorListItemDto> {
    return this.http.post<AdminOperatorListItemDto>(`${this.base}/${id}/enable`, {});
  }
}
```

- [ ] **Step 2: Create `admin-revenue.api.ts`**

```typescript
import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface AdminRevenueOperatorItemDto {
  operatorUserId: string;
  operatorName: string;
  confirmedBookings: number;
  gmv: number;
  platformFeeIncome: number;
}

export interface AdminRevenueResponseDto {
  dateFrom: string;
  dateTo: string;
  confirmedBookings: number;
  gmv: number;
  platformFeeIncome: number;
  byOperator: AdminRevenueOperatorItemDto[];
}

@Injectable({ providedIn: 'root' })
export class AdminRevenueApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/admin/revenue`;

  get(from?: string, to?: string): Observable<AdminRevenueResponseDto> {
    let params = new HttpParams();
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    return this.http.get<AdminRevenueResponseDto>(this.base, { params });
  }
}
```

- [ ] **Step 3: Create `admin-bookings.api.ts`**

```typescript
import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface AdminBookingListItemDto {
  bookingId: string;
  bookingCode: string;
  tripId: string;
  tripDate: string;
  sourceCity: string;
  destinationCity: string;
  busId: string;
  busName: string;
  operatorUserId: string;
  operatorName: string;
  customerName: string;
  customerEmail: string;
  seatCount: number;
  totalFare: number;
  platformFee: number;
  totalAmount: number;
  status: string;
  createdAt: string;
}

export interface AdminBookingListResponseDto {
  items: AdminBookingListItemDto[];
  page: number;
  pageSize: number;
  totalCount: number;
}

@Injectable({ providedIn: 'root' })
export class AdminBookingsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/admin/bookings`;

  list(
    operatorUserId?: string,
    status?: string,
    date?: string,
    page = 1,
    pageSize = 20
  ): Observable<AdminBookingListResponseDto> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (operatorUserId) params = params.set('operatorUserId', operatorUserId);
    if (status) params = params.set('status', status);
    if (date) params = params.set('date', date);
    return this.http.get<AdminBookingListResponseDto>(this.base, { params });
  }
}
```

- [ ] **Step 4: Commit**

```bash
git add frontend/bus-booking-web/src/app/core/api/admin-operators.api.ts \
        frontend/bus-booking-web/src/app/core/api/admin-revenue.api.ts \
        frontend/bus-booking-web/src/app/core/api/admin-bookings.api.ts
git commit -m "feat(m8): add admin operators, revenue, and bookings API clients"
```

---

## Task 12: Admin operators page + disable dialog

**Files:**
- Create: `frontend/bus-booking-web/src/app/features/admin/operators/disable-operator-dialog.component.ts`
- Create: `frontend/bus-booking-web/src/app/features/admin/operators/admin-operators-page.component.ts`
- Create: `frontend/bus-booking-web/src/app/features/admin/operators/admin-operators-page.component.html`

- [ ] **Step 1: Create `disable-operator-dialog.component.ts`**

```typescript
import { Component, inject } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

export interface DisableOperatorDialogData {
  operatorName: string;
}

@Component({
  selector: 'app-disable-operator-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule, MatDialogModule, MatButtonModule,
    MatFormFieldModule, MatInputModule
  ],
  template: `
    <h2 mat-dialog-title>Disable {{ data.operatorName }}?</h2>
    <mat-dialog-content class="space-y-3">
      <p>
        This will retire all of this operator's buses, cancel every upcoming confirmed
        booking as <strong>cancelled by operator</strong>, queue full refunds, and email
        affected customers. The user's customer account stays active.
      </p>
      <mat-form-field class="w-full">
        <mat-label>Reason (optional)</mat-label>
        <textarea matInput rows="3" [formControl]="reason" maxlength="500"></textarea>
      </mat-form-field>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button color="warn" (click)="confirm()">Disable operator</button>
    </mat-dialog-actions>
  `
})
export class DisableOperatorDialogComponent {
  readonly data = inject<DisableOperatorDialogData>(MAT_DIALOG_DATA);
  private readonly ref = inject(MatDialogRef<DisableOperatorDialogComponent>);
  readonly reason = new FormControl<string>('', { nonNullable: true });

  confirm(): void {
    this.ref.close({ reason: this.reason.value.trim() || null });
  }
}
```

- [ ] **Step 2: Create `admin-operators-page.component.ts`**

```typescript
import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import {
  AdminOperatorsApiService, AdminOperatorListItemDto
} from '../../../core/api/admin-operators.api';
import {
  DisableOperatorDialogComponent, DisableOperatorDialogData
} from './disable-operator-dialog.component';

@Component({
  selector: 'app-admin-operators-page',
  standalone: true,
  imports: [
    CommonModule, MatTableModule, MatButtonModule, MatIconModule,
    MatDialogModule, MatSnackBarModule
  ],
  templateUrl: './admin-operators-page.component.html'
})
export class AdminOperatorsPageComponent implements OnInit {
  private readonly api = inject(AdminOperatorsApiService);
  private readonly dialog = inject(MatDialog);
  private readonly snack = inject(MatSnackBar);

  readonly operators = signal<AdminOperatorListItemDto[]>([]);
  readonly busy = signal<string | null>(null);

  readonly columns = ['name', 'email', 'buses', 'state', 'actions'];

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.api.list().subscribe(list => this.operators.set(list));
  }

  disable(op: AdminOperatorListItemDto): void {
    const ref = this.dialog.open(DisableOperatorDialogComponent, {
      width: '480px',
      data: { operatorName: op.name } satisfies DisableOperatorDialogData
    });
    ref.afterClosed().subscribe(result => {
      if (!result) return;
      this.busy.set(op.userId);
      this.api.disable(op.userId, { reason: result.reason }).subscribe({
        next: updated => {
          this.operators.update(list =>
            list.map(o => o.userId === updated.userId ? updated : o));
          this.snack.open(`Disabled ${op.name}`, 'OK', { duration: 3500 });
          this.busy.set(null);
        },
        error: () => {
          this.snack.open(`Failed to disable ${op.name}`, 'Dismiss', { duration: 5000 });
          this.busy.set(null);
        }
      });
    });
  }

  enable(op: AdminOperatorListItemDto): void {
    this.busy.set(op.userId);
    this.api.enable(op.userId).subscribe({
      next: updated => {
        this.operators.update(list =>
          list.map(o => o.userId === updated.userId ? updated : o));
        this.snack.open(`Enabled ${op.name}`, 'OK', { duration: 3500 });
        this.busy.set(null);
      },
      error: () => {
        this.snack.open(`Failed to enable ${op.name}`, 'Dismiss', { duration: 5000 });
        this.busy.set(null);
      }
    });
  }
}
```

- [ ] **Step 3: Create `admin-operators-page.component.html`**

```html
<section class="p-6 max-w-6xl mx-auto space-y-4">
  <header>
    <h1 class="text-2xl font-semibold">Operators</h1>
    <p class="text-gray-600">
      Disable an operator to retire their buses and cancel upcoming bookings with full refunds.
    </p>
  </header>

  <table mat-table [dataSource]="operators()" class="mat-elevation-z1 w-full">
    <ng-container matColumnDef="name">
      <th mat-header-cell *matHeaderCellDef>Operator</th>
      <td mat-cell *matCellDef="let o">{{ o.name }}</td>
    </ng-container>

    <ng-container matColumnDef="email">
      <th mat-header-cell *matHeaderCellDef>Email</th>
      <td mat-cell *matCellDef="let o" class="text-sm">{{ o.email }}</td>
    </ng-container>

    <ng-container matColumnDef="buses">
      <th mat-header-cell *matHeaderCellDef>Buses</th>
      <td mat-cell *matCellDef="let o">
        {{ o.activeBuses }} active · {{ o.retiredBuses }} retired
        <span class="text-gray-500">({{ o.totalBuses }} total)</span>
      </td>
    </ng-container>

    <ng-container matColumnDef="state">
      <th mat-header-cell *matHeaderCellDef>State</th>
      <td mat-cell *matCellDef="let o">
        @if (o.isDisabled) {
          <span class="px-2 py-1 rounded text-xs bg-red-100 text-red-800">
            Disabled {{ o.disabledAt | date:'mediumDate' }}
          </span>
        } @else {
          <span class="px-2 py-1 rounded text-xs bg-green-100 text-green-800">Active</span>
        }
      </td>
    </ng-container>

    <ng-container matColumnDef="actions">
      <th mat-header-cell *matHeaderCellDef>Actions</th>
      <td mat-cell *matCellDef="let o">
        @if (o.isDisabled) {
          <button mat-stroked-button color="primary"
                  [disabled]="busy() === o.userId"
                  (click)="enable(o)">
            <mat-icon>check_circle</mat-icon> Enable
          </button>
        } @else {
          <button mat-stroked-button color="warn"
                  [disabled]="busy() === o.userId"
                  (click)="disable(o)">
            <mat-icon>block</mat-icon> Disable
          </button>
        }
      </td>
    </ng-container>

    <tr mat-header-row *matHeaderRowDef="columns"></tr>
    <tr mat-row *matRowDef="let row; columns: columns;"></tr>
  </table>

  @if (operators().length === 0) {
    <div class="p-8 text-center text-slate-500 border rounded bg-slate-50">
      No operators yet.
    </div>
  }
</section>
```

- [ ] **Step 4: Commit**

```bash
git add frontend/bus-booking-web/src/app/features/admin/operators/
git commit -m "feat(m8): add admin operators page with disable/enable actions"
```

---

## Task 13: Admin revenue page

**Files:**
- Create: `frontend/bus-booking-web/src/app/features/admin/revenue/admin-revenue-page.component.ts`
- Create: `frontend/bus-booking-web/src/app/features/admin/revenue/admin-revenue-page.component.html`

- [ ] **Step 1: Create the component TypeScript**

```typescript
import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import {
  AdminRevenueApiService, AdminRevenueResponseDto
} from '../../../core/api/admin-revenue.api';

@Component({
  selector: 'app-admin-revenue-page',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatTableModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatDatepickerModule
  ],
  templateUrl: './admin-revenue-page.component.html'
})
export class AdminRevenuePageComponent implements OnInit {
  private readonly api = inject(AdminRevenueApiService);

  readonly revenue = signal<AdminRevenueResponseDto | null>(null);
  readonly columns = ['operatorName', 'confirmedBookings', 'gmv', 'platformFeeIncome'];

  readonly fromDate = new FormControl<Date | null>(
    new Date(new Date().getFullYear(), new Date().getMonth(), 1)
  );
  readonly toDate = new FormControl<Date | null>(new Date());

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    const from = this.fromDate.value?.toISOString().slice(0, 10);
    const to   = this.toDate.value?.toISOString().slice(0, 10);
    this.api.get(from, to).subscribe(res => this.revenue.set(res));
  }
}
```

- [ ] **Step 2: Create the component HTML**

```html
<section class="p-6 max-w-6xl mx-auto space-y-4">
  <header>
    <h1 class="text-2xl font-semibold">Platform revenue</h1>
    <p class="text-gray-600">GMV and platform-fee income across all operators.</p>
  </header>

  <div class="flex gap-4 items-end flex-wrap">
    <mat-form-field class="w-44">
      <mat-label>From</mat-label>
      <input matInput [matDatepicker]="fromPicker" [formControl]="fromDate">
      <mat-datepicker-toggle matIconSuffix [for]="fromPicker"></mat-datepicker-toggle>
      <mat-datepicker #fromPicker></mat-datepicker>
    </mat-form-field>

    <mat-form-field class="w-44">
      <mat-label>To</mat-label>
      <input matInput [matDatepicker]="toPicker" [formControl]="toDate">
      <mat-datepicker-toggle matIconSuffix [for]="toPicker"></mat-datepicker-toggle>
      <mat-datepicker #toPicker></mat-datepicker>
    </mat-form-field>

    <button mat-flat-button color="primary" (click)="load()">Apply</button>
  </div>

  @if (revenue(); as rev) {
    <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
      <div class="p-4 rounded bg-blue-50 border border-blue-200">
        <div class="text-xs uppercase tracking-wide text-blue-700">GMV</div>
        <div class="text-2xl font-bold text-blue-900">{{ rev.gmv | currency:'INR' }}</div>
      </div>
      <div class="p-4 rounded bg-emerald-50 border border-emerald-200">
        <div class="text-xs uppercase tracking-wide text-emerald-700">Platform-fee income</div>
        <div class="text-2xl font-bold text-emerald-900">
          {{ rev.platformFeeIncome | currency:'INR' }}
        </div>
      </div>
      <div class="p-4 rounded bg-slate-50 border border-slate-200">
        <div class="text-xs uppercase tracking-wide text-slate-700">Confirmed bookings</div>
        <div class="text-2xl font-bold text-slate-900">{{ rev.confirmedBookings }}</div>
      </div>
    </div>

    <table mat-table [dataSource]="rev.byOperator" class="mat-elevation-z1 w-full">
      <ng-container matColumnDef="operatorName">
        <th mat-header-cell *matHeaderCellDef>Operator</th>
        <td mat-cell *matCellDef="let r">{{ r.operatorName }}</td>
      </ng-container>

      <ng-container matColumnDef="confirmedBookings">
        <th mat-header-cell *matHeaderCellDef>Bookings</th>
        <td mat-cell *matCellDef="let r">{{ r.confirmedBookings }}</td>
      </ng-container>

      <ng-container matColumnDef="gmv">
        <th mat-header-cell *matHeaderCellDef>GMV</th>
        <td mat-cell *matCellDef="let r">{{ r.gmv | currency:'INR' }}</td>
      </ng-container>

      <ng-container matColumnDef="platformFeeIncome">
        <th mat-header-cell *matHeaderCellDef>Platform fee income</th>
        <td mat-cell *matCellDef="let r" class="font-semibold">
          {{ r.platformFeeIncome | currency:'INR' }}
        </td>
      </ng-container>

      <tr mat-header-row *matHeaderRowDef="columns"></tr>
      <tr mat-row *matRowDef="let row; columns: columns;"></tr>
    </table>

    @if (rev.byOperator.length === 0) {
      <div class="p-8 text-center text-slate-500 border rounded bg-slate-50">
        No confirmed bookings in this date range.
      </div>
    }
  }
</section>
```

- [ ] **Step 3: Commit**

```bash
git add frontend/bus-booking-web/src/app/features/admin/revenue/
git commit -m "feat(m8): add admin platform revenue page with per-operator breakdown"
```

---

## Task 14: Admin cross-operator bookings page

**Files:**
- Create: `frontend/bus-booking-web/src/app/features/admin/bookings/admin-bookings-page.component.ts`
- Create: `frontend/bus-booking-web/src/app/features/admin/bookings/admin-bookings-page.component.html`

- [ ] **Step 1: Create the component TypeScript**

```typescript
import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import {
  AdminBookingsApiService, AdminBookingListItemDto
} from '../../../core/api/admin-bookings.api';
import {
  AdminOperatorsApiService, AdminOperatorListItemDto
} from '../../../core/api/admin-operators.api';

const STATUS_OPTIONS: { value: string; label: string }[] = [
  { value: 'confirmed', label: 'Confirmed' },
  { value: 'completed', label: 'Completed' },
  { value: 'cancelled', label: 'Cancelled (user)' },
  { value: 'cancelled_by_operator', label: 'Cancelled (operator)' }
];

@Component({
  selector: 'app-admin-bookings-page',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatTableModule, MatFormFieldModule, MatSelectModule,
    MatInputModule, MatButtonModule, MatDatepickerModule
  ],
  templateUrl: './admin-bookings-page.component.html'
})
export class AdminBookingsPageComponent implements OnInit {
  private readonly bookingsApi = inject(AdminBookingsApiService);
  private readonly operatorsApi = inject(AdminOperatorsApiService);

  readonly bookings = signal<AdminBookingListItemDto[]>([]);
  readonly operators = signal<AdminOperatorListItemDto[]>([]);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly pageSize = 20;

  readonly statusOptions = STATUS_OPTIONS;
  readonly operatorFilter = new FormControl<string | null>(null);
  readonly statusFilter = new FormControl<string | null>(null);
  readonly dateFilter = new FormControl<Date | null>(null);

  readonly columns = [
    'bookingCode', 'date', 'route', 'operator', 'bus',
    'customer', 'seats', 'amount', 'status'
  ];

  ngOnInit(): void {
    this.operatorsApi.list().subscribe(list => this.operators.set(list));
    this.load();
  }

  load(): void {
    const opId = this.operatorFilter.value ?? undefined;
    const status = this.statusFilter.value ?? undefined;
    const date = this.dateFilter.value
      ? this.dateFilter.value.toISOString().slice(0, 10)
      : undefined;
    this.bookingsApi.list(opId, status, date, this.page(), this.pageSize).subscribe(res => {
      this.bookings.set(res.items);
      this.totalCount.set(res.totalCount);
    });
  }

  applyFilters(): void {
    this.page.set(1);
    this.load();
  }

  clearFilters(): void {
    this.operatorFilter.setValue(null);
    this.statusFilter.setValue(null);
    this.dateFilter.setValue(null);
    this.page.set(1);
    this.load();
  }

  statusClass(status: string): string {
    switch (status) {
      case 'confirmed': return 'bg-green-100 text-green-800';
      case 'completed': return 'bg-blue-100 text-blue-800';
      case 'cancelled':
      case 'cancelled_by_operator': return 'bg-red-100 text-red-800';
      default: return 'bg-slate-100 text-slate-800';
    }
  }

  formatStatus(status: string): string {
    return status.replace(/_/g, ' ');
  }
}
```

- [ ] **Step 2: Create the component HTML**

```html
<section class="p-6 max-w-7xl mx-auto space-y-4">
  <header>
    <h1 class="text-2xl font-semibold">Bookings</h1>
    <p class="text-gray-600">Cross-operator booking view.</p>
  </header>

  <div class="flex gap-4 items-end flex-wrap">
    <mat-form-field class="w-56">
      <mat-label>Operator</mat-label>
      <mat-select [formControl]="operatorFilter">
        <mat-option [value]="null">All operators</mat-option>
        @for (op of operators(); track op.userId) {
          <mat-option [value]="op.userId">{{ op.name }}</mat-option>
        }
      </mat-select>
    </mat-form-field>

    <mat-form-field class="w-48">
      <mat-label>Status</mat-label>
      <mat-select [formControl]="statusFilter">
        <mat-option [value]="null">All (except pending payment)</mat-option>
        @for (s of statusOptions; track s.value) {
          <mat-option [value]="s.value">{{ s.label }}</mat-option>
        }
      </mat-select>
    </mat-form-field>

    <mat-form-field class="w-44">
      <mat-label>Trip date</mat-label>
      <input matInput [matDatepicker]="picker" [formControl]="dateFilter">
      <mat-datepicker-toggle matIconSuffix [for]="picker"></mat-datepicker-toggle>
      <mat-datepicker #picker></mat-datepicker>
    </mat-form-field>

    <button mat-flat-button color="primary" (click)="applyFilters()">Apply</button>
    <button mat-stroked-button (click)="clearFilters()">Clear</button>
  </div>

  <table mat-table [dataSource]="bookings()" class="mat-elevation-z1 w-full">
    <ng-container matColumnDef="bookingCode">
      <th mat-header-cell *matHeaderCellDef>Code</th>
      <td mat-cell *matCellDef="let b" class="font-mono text-sm">{{ b.bookingCode }}</td>
    </ng-container>

    <ng-container matColumnDef="date">
      <th mat-header-cell *matHeaderCellDef>Trip date</th>
      <td mat-cell *matCellDef="let b">{{ b.tripDate }}</td>
    </ng-container>

    <ng-container matColumnDef="route">
      <th mat-header-cell *matHeaderCellDef>Route</th>
      <td mat-cell *matCellDef="let b">{{ b.sourceCity }} → {{ b.destinationCity }}</td>
    </ng-container>

    <ng-container matColumnDef="operator">
      <th mat-header-cell *matHeaderCellDef>Operator</th>
      <td mat-cell *matCellDef="let b">{{ b.operatorName }}</td>
    </ng-container>

    <ng-container matColumnDef="bus">
      <th mat-header-cell *matHeaderCellDef>Bus</th>
      <td mat-cell *matCellDef="let b">{{ b.busName }}</td>
    </ng-container>

    <ng-container matColumnDef="customer">
      <th mat-header-cell *matHeaderCellDef>Customer</th>
      <td mat-cell *matCellDef="let b">
        <div>{{ b.customerName }}</div>
        <div class="text-xs text-gray-500">{{ b.customerEmail }}</div>
      </td>
    </ng-container>

    <ng-container matColumnDef="seats">
      <th mat-header-cell *matHeaderCellDef>Seats</th>
      <td mat-cell *matCellDef="let b">{{ b.seatCount }}</td>
    </ng-container>

    <ng-container matColumnDef="amount">
      <th mat-header-cell *matHeaderCellDef>Amount</th>
      <td mat-cell *matCellDef="let b">{{ b.totalAmount | currency:'INR' }}</td>
    </ng-container>

    <ng-container matColumnDef="status">
      <th mat-header-cell *matHeaderCellDef>Status</th>
      <td mat-cell *matCellDef="let b">
        <span class="px-2 py-1 rounded text-xs" [ngClass]="statusClass(b.status)">
          {{ formatStatus(b.status) }}
        </span>
      </td>
    </ng-container>

    <tr mat-header-row *matHeaderRowDef="columns"></tr>
    <tr mat-row *matRowDef="let row; columns: columns;"></tr>
  </table>

  @if (bookings().length === 0) {
    <div class="p-8 text-center text-slate-500 border rounded bg-slate-50">
      No bookings match these filters.
    </div>
  }

  <p class="text-sm text-slate-500">Total: {{ totalCount() }} booking(s)</p>
</section>
```

- [ ] **Step 3: Commit**

```bash
git add frontend/bus-booking-web/src/app/features/admin/bookings/
git commit -m "feat(m8): add admin cross-operator bookings page with filters"
```

---

## Task 15: Wire routes + admin dashboard tiles + end-to-end build check

**Files:**
- Modify: `frontend/bus-booking-web/src/app/app.routes.ts`
- Modify: `frontend/bus-booking-web/src/app/features/admin/admin-dashboard/admin-dashboard.component.html`

- [ ] **Step 1: Add three admin children routes in `app.routes.ts`**

Inside the existing `{ path: 'admin', canMatch: [roleGuard(['admin'])], children: [...] }` block, append the following three entries at the end of the `children` array (after `bus-approvals`):

```typescript
      {
        path: 'operators',
        loadComponent: () => import('./features/admin/operators/admin-operators-page.component')
          .then(m => m.AdminOperatorsPageComponent)
      },
      {
        path: 'revenue',
        loadComponent: () => import('./features/admin/revenue/admin-revenue-page.component')
          .then(m => m.AdminRevenuePageComponent)
      },
      {
        path: 'bookings',
        loadComponent: () => import('./features/admin/bookings/admin-bookings-page.component')
          .then(m => m.AdminBookingsPageComponent)
      }
```

After insertion, the `admin` block should read:

```typescript
  {
    path: 'admin',
    canMatch: [roleGuard(['admin'])],
    children: [
      {
        path: '',
        loadComponent: () => import('./features/admin/admin-dashboard/admin-dashboard.component')
          .then(m => m.AdminDashboardComponent)
      },
      {
        path: 'cities',
        loadComponent: () => import('./features/admin/cities/admin-cities-page.component')
          .then(m => m.AdminCitiesPageComponent)
      },
      {
        path: 'routes',
        loadComponent: () => import('./features/admin/routes/admin-routes-page.component')
          .then(m => m.AdminRoutesPageComponent)
      },
      {
        path: 'platform-fee',
        loadComponent: () => import('./features/admin/platform-fee/admin-platform-fee-page.component')
          .then(m => m.AdminPlatformFeePageComponent)
      },
      {
        path: 'operator-requests',
        loadComponent: () => import('./features/admin/operator-requests/admin-operator-requests-page.component')
          .then(m => m.AdminOperatorRequestsPageComponent)
      },
      {
        path: 'bus-approvals',
        loadComponent: () => import('./features/admin/bus-approvals/admin-bus-approvals-page.component')
          .then(m => m.AdminBusApprovalsPageComponent)
      },
      {
        path: 'operators',
        loadComponent: () => import('./features/admin/operators/admin-operators-page.component')
          .then(m => m.AdminOperatorsPageComponent)
      },
      {
        path: 'revenue',
        loadComponent: () => import('./features/admin/revenue/admin-revenue-page.component')
          .then(m => m.AdminRevenuePageComponent)
      },
      {
        path: 'bookings',
        loadComponent: () => import('./features/admin/bookings/admin-bookings-page.component')
          .then(m => m.AdminBookingsPageComponent)
      }
    ]
  },
```

- [ ] **Step 2: Add three new tiles to the admin dashboard**

Open `frontend/bus-booking-web/src/app/features/admin/admin-dashboard/admin-dashboard.component.html` and, inside the `<div class="grid grid-cols-1 md:grid-cols-3 gap-6">...</div>`, **before** the closing `</div>` (and after the existing `bus-approvals` card), insert:

```html
    <mat-card class="hover:shadow-md transition" [routerLink]="['/admin/operators']">
      <mat-card-content class="flex items-start gap-3 cursor-pointer">
        <mat-icon>manage_accounts</mat-icon>
        <div>
          <div class="font-medium">Operators</div>
          <div class="text-sm text-gray-600">Disable or re-enable operators</div>
        </div>
      </mat-card-content>
    </mat-card>

    <mat-card class="hover:shadow-md transition" [routerLink]="['/admin/bookings']">
      <mat-card-content class="flex items-start gap-3 cursor-pointer">
        <mat-icon>receipt_long</mat-icon>
        <div>
          <div class="font-medium">Bookings</div>
          <div class="text-sm text-gray-600">Cross-operator booking history</div>
        </div>
      </mat-card-content>
    </mat-card>

    <mat-card class="hover:shadow-md transition" [routerLink]="['/admin/revenue']">
      <mat-card-content class="flex items-start gap-3 cursor-pointer">
        <mat-icon>bar_chart</mat-icon>
        <div>
          <div class="font-medium">Revenue</div>
          <div class="text-sm text-gray-600">GMV and platform-fee income</div>
        </div>
      </mat-card-content>
    </mat-card>
```

- [ ] **Step 3: Build the frontend — verify no template/TS errors**

```bash
cd frontend/bus-booking-web && ng build --configuration development 2>&1 | tail -20
```

Expected: `Application bundle generation complete.` with no errors.

- [ ] **Step 4: Run the full backend test suite one more time — nothing should have regressed**

```bash
cd backend && dotnet test BusBooking.Api.Tests 2>&1 | tail -10
```

Expected: all tests pass.

- [ ] **Step 5: Commit**

```bash
git add frontend/bus-booking-web/src/app/app.routes.ts \
        frontend/bus-booking-web/src/app/features/admin/admin-dashboard/admin-dashboard.component.html
git commit -m "feat(m8): wire admin operators, revenue, and bookings routes and dashboard tiles"
```

---

## Self-Review

### 1. Spec coverage

| Spec requirement (§5.4, §7.6) | Covered by task |
|---|---|
| `GET /admin/operators` listing with enabled/disabled state | Tasks 3–6 |
| `POST /admin/operators/{id}/disable` — cascade (retire buses, cancel future confirmed bookings as `cancelled_by_operator`, queue refunds, queue notifications, audit) | Tasks 4–6 (`AdminOperatorService.DisableAsync`) |
| Post-commit side effects: Razorpay refund + Resend emails (do not roll back DB) | Task 5 (`RefundBookingPostCommitAsync`, wrapped in try/catch after `SaveChangesAsync`) |
| `POST /admin/operators/{id}/enable` — role flag only, no booking reinstatement | Task 5 (`EnableAsync`); verified by Task 4 test `Enable_clears_disabled_flag_but_does_not_reinstate_bookings` |
| `GET /admin/revenue` — GMV + platform-fee income, filter by date range | Tasks 7–8 |
| `GET /admin/bookings` — cross-operator listing | Tasks 9–10 |
| Role enforcement: `[Authorize(Roles="admin")]` on every new endpoint | Tasks 6, 8, 10 (controllers); tests in tasks 4, 7, 9 verify 403 for customer role |
| Full refund (`amount = total_amount`) on operator-disabled cascade | Task 5 sets `RefundAmount = b.TotalAmount`; Task 4 test asserts `RefundAmount.Should().Be(1000m)` on a 1000 total |
| Operator-disabled email + per-customer cancellation email | Task 1 (interface), Task 2 (implementation), Task 5 (invocation post-commit) |
| Audit log rows for disable and enable | Task 1 (constants), Task 5 (`_audit.WriteAsync`) |
| Admin dashboard surfaces the new pages | Task 15 tiles |
| Customer email arrives on cascade (M8 demo outcome) | Task 4 test `Disable_cascades_...` asserts both `disabled` and `cancelled by operator` subjects present in `_fx.Email.Sent` |

No gaps found.

### 2. Placeholder scan

No "TBD", "TODO", "fill in", or "similar to …" text in the plan. Every step that touches code shows the exact code. Every run step shows the exact command and expected output.

### 3. Type consistency

- `AdminOperatorListItemDto` fields (`UserId, Name, Email, CreatedAt, IsDisabled, DisabledAt, TotalBuses, ActiveBuses, RetiredBuses`) match the projection built in `AdminOperatorService.ListAsync` (Task 5) and the TS interface in Task 11.
- `AdminRevenueResponseDto(DateFrom, DateTo, ConfirmedBookings, Gmv, PlatformFeeIncome, ByOperator)` parameter order matches the `new AdminRevenueResponseDto(...)` construction in Task 8 and the TS interface in Task 11.
- `AdminRevenueOperatorItemDto(OperatorUserId, OperatorName, ConfirmedBookings, Gmv, PlatformFeeIncome)` matches the `GroupBy.Select` projection in Task 8.
- `AdminBookingListItemDto` 18 fields match the `rows.Select(...)` projection in `AdminBookingService.ListAsync` (Task 10) and the TS interface in Task 11.
- `DisableOperatorRequest(string? Reason)` serialises as `{ "reason": "..." }` (ASP.NET Core default camelCase), matching the TS `DisableOperatorRequest { reason?: string | null }` in Task 11.
- `INotificationSender.SendOperatorDisabledAsync(User, string?, CancellationToken)` signature matches the invocation in `AdminOperatorService.DisableAsync` (Task 5) and the implementation in `LoggingNotificationSender` (Task 2).
- `INotificationSender.SendBookingCancelledByOperatorAsync(User, BookingDetailDto, decimal, CancellationToken)` signature matches the invocation (Task 5) and implementation (Task 2).
- `AuditAction.OperatorDisabled` / `OperatorEnabled` constants added in Task 1 are used verbatim in Task 5.
- `RefundStatus.Pending` → `Processed` transition in `RefundBookingPostCommitAsync` (Task 5) matches the existing state machine used by `BookingService.CancelAsync` — verified by referencing `RefundStatus` constants already in the codebase.
- `BookingStatus.CancelledByOperator` constant is already defined in the existing `BookingStatus` static class — used in Task 5, asserted in Task 4.
- Angular `AdminBookingsApiService.list` parameter order (`operatorUserId, status, date, page, pageSize`) matches the query-string params read by `AdminBookingsController.List` in Task 10.
- Admin dashboard tile `routerLink="/admin/operators"` matches the new route `operators` added under the `admin` parent in Task 15.

All types and call sites are consistent.

---

**Plan complete and saved to `docs/superpowers/plans/2026-04-23-m8-admin-cascade-revenue-bookings.md`. Two execution options:**

**1. Subagent-Driven (recommended)** — I dispatch a fresh subagent per task, review between tasks, fast iteration

**2. Inline Execution** — Execute tasks in this session using executing-plans, batch execution with checkpoints

**Which approach?**
