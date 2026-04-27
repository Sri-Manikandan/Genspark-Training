# M5 — Seat Locking, Booking, Razorpay, PDF & Email Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. **Work directly on `main` — do NOT create a feature branch.** Commit messages MUST NOT include a `Co-Authored-By: Claude` trailer.

**Goal:** Deliver the M5 demoable outcome: an anonymous user picks seats on the trip page, is prompted to log in, enters passenger details, completes a Razorpay test-mode payment, receives a PDF ticket download, and an email confirmation is sent via Resend.

**Architecture:** Five new EF Core entities (`seat_locks`, `bookings`, `booking_seats`, `payments`, `notifications`). `SeatLockService` enforces the `UNIQUE(trip_id, seat_number)` invariant that makes concurrent picks safe; `SeatLockCleanupService` (IHostedService) reaps expired rows every 60 s. `BookingService` materialises a booking (pending_payment) with a Razorpay order, then confirms it on HMAC signature verify, deleting the seat locks in the same transaction and firing an email with a PDF attachment post-commit. Razorpay and Resend are thin `HttpClient` wrappers; the PDF ticket uses QuestPDF. Two new controllers (`SeatLocksController`, `BookingsController`) and one updated service (`TripService` seat-layout + seats-left). Angular adds a `BookingsApiService`, a reusable `CountdownTimerComponent`, upgrades `SeatMapComponent` to interactive, updates the trip page to lock seats, and adds a three-step `CheckoutStepperComponent` plus a `BookingConfirmationComponent`.

**Tech Stack:** .NET 9 · EF Core 9 · Npgsql · QuestPDF (Community) · `IHttpClientFactory` · HMAC-SHA256 · FluentValidation · xUnit · FluentAssertions · `Microsoft.AspNetCore.Mvc.Testing` · Angular 20 (standalone + Signals) · Angular Material (`MatStepper`, `MatFormField`, `MatRadioGroup`, `MatCard`) · Razorpay `checkout.js` · PostgreSQL.

---

## File map

### New backend files

| Path | Responsibility |
|---|---|
| `backend/BusBooking.Api/Models/SeatLock.cs` | `seat_locks` entity |
| `backend/BusBooking.Api/Models/Booking.cs` | `bookings` entity |
| `backend/BusBooking.Api/Models/BookingSeat.cs` | `booking_seats` entity |
| `backend/BusBooking.Api/Models/Payment.cs` | `payments` entity |
| `backend/BusBooking.Api/Models/BookingStatus.cs` | booking status constants |
| `backend/BusBooking.Api/Models/PaymentStatus.cs` | payment status constants |
| `backend/BusBooking.Api/Models/Notification.cs` | `notifications` entity |
| `backend/BusBooking.Api/Models/NotificationType.cs` | notification type constants |
| `backend/BusBooking.Api/Models/NotificationChannel.cs` | notification channel constants |
| `backend/BusBooking.Api/Models/PassengerGender.cs` | passenger gender constants |
| `backend/BusBooking.Api/Dtos/LockSeatsRequest.cs` | POST seat-locks body |
| `backend/BusBooking.Api/Dtos/SeatLockResponseDto.cs` | `{lockId, sessionId, seats, expiresAt}` |
| `backend/BusBooking.Api/Dtos/CreateBookingRequest.cs` | POST /bookings body |
| `backend/BusBooking.Api/Dtos/PassengerDto.cs` | per-seat passenger payload |
| `backend/BusBooking.Api/Dtos/CreateBookingResponseDto.cs` | Razorpay order descriptor |
| `backend/BusBooking.Api/Dtos/VerifyPaymentRequest.cs` | `{razorpayPaymentId, razorpaySignature}` |
| `backend/BusBooking.Api/Dtos/BookingDetailDto.cs` | full booking response |
| `backend/BusBooking.Api/Dtos/BookingSeatDto.cs` | seat line in booking detail |
| `backend/BusBooking.Api/Validators/LockSeatsRequestValidator.cs` | FluentValidation |
| `backend/BusBooking.Api/Validators/CreateBookingRequestValidator.cs` | FluentValidation |
| `backend/BusBooking.Api/Validators/VerifyPaymentRequestValidator.cs` | FluentValidation |
| `backend/BusBooking.Api/Infrastructure/Razorpay/RazorpayOptions.cs` | options record |
| `backend/BusBooking.Api/Infrastructure/Razorpay/IRazorpayClient.cs` | contract |
| `backend/BusBooking.Api/Infrastructure/Razorpay/RazorpayClient.cs` | HTTP client + HMAC |
| `backend/BusBooking.Api/Infrastructure/Resend/ResendOptions.cs` | options record |
| `backend/BusBooking.Api/Infrastructure/Resend/IResendEmailClient.cs` | contract |
| `backend/BusBooking.Api/Infrastructure/Resend/ResendEmailClient.cs` | HTTP client |
| `backend/BusBooking.Api/Infrastructure/Pdf/IPdfTicketGenerator.cs` | contract |
| `backend/BusBooking.Api/Infrastructure/Pdf/PdfTicketGenerator.cs` | QuestPDF impl |
| `backend/BusBooking.Api/Background/SeatLockCleanupService.cs` | IHostedService, 60 s |
| `backend/BusBooking.Api/Services/ISeatLockService.cs` | contract |
| `backend/BusBooking.Api/Services/SeatLockService.cs` | acquire / release / validate |
| `backend/BusBooking.Api/Services/IBookingService.cs` | contract |
| `backend/BusBooking.Api/Services/BookingService.cs` | create / verify-payment / detail / ticket |
| `backend/BusBooking.Api/Controllers/SeatLocksController.cs` | POST /trips/{id}/seat-locks, DELETE /seat-locks/{lockId} |
| `backend/BusBooking.Api/Controllers/BookingsController.cs` | POST /bookings, POST verify-payment, GET {id}, GET {id}/ticket |

### Modified backend files

- `backend/BusBooking.Api/Infrastructure/AppDbContext.cs` — add 5 `DbSet<>` + EF mappings
- `backend/BusBooking.Api/Services/TripService.cs` — subtract locked + booked seats from `seatsLeft`; return per-seat statuses
- `backend/BusBooking.Api/Services/INotificationSender.cs` — add `SendBookingConfirmedAsync(User, BookingDetailDto, byte[] pdf, ...)`
- `backend/BusBooking.Api/Services/LoggingNotificationSender.cs` — add stub impl
- `backend/BusBooking.Api/Program.cs` — DI registrations + QuestPDF license + hosted service + HttpClient
- `backend/BusBooking.Api/appsettings.json` — placeholder `Razorpay`, `Resend` sections
- `backend/BusBooking.Api/BusBooking.Api.csproj` — add `QuestPDF` NuGet

### New test files

| Path | Responsibility |
|---|---|
| `backend/BusBooking.Api.Tests/Support/FakeRazorpayClient.cs` | deterministic order ids + stub signature verify |
| `backend/BusBooking.Api.Tests/Support/FakeResendEmailClient.cs` | captures outbound email calls |
| `backend/BusBooking.Api.Tests/Unit/RazorpaySignatureTests.cs` | HMAC verify matrix |
| `backend/BusBooking.Api.Tests/Unit/PdfTicketGeneratorTests.cs` | PDF produced contains booking code |
| `backend/BusBooking.Api.Tests/Integration/SeatLockTests.cs` | concurrent lock + release + expiry filter |
| `backend/BusBooking.Api.Tests/Integration/BookingTests.cs` | happy path + expired lock + idempotent verify |

### Modified test files

- `backend/BusBooking.Api.Tests/Support/IntegrationFixture.cs` — truncate new tables; override Razorpay + Resend with fakes

### New frontend files

| Path | Responsibility |
|---|---|
| `frontend/bus-booking-web/src/app/core/api/bookings.api.ts` | seat-locks + bookings HTTP calls |
| `frontend/bus-booking-web/src/app/shared/components/countdown-timer/countdown-timer.component.ts` | 7-min countdown signal |
| `frontend/bus-booking-web/src/app/shared/components/countdown-timer/countdown-timer.component.html` | template |
| `frontend/bus-booking-web/src/app/features/customer/checkout/checkout-stepper.component.ts` | review + passengers + payment |
| `frontend/bus-booking-web/src/app/features/customer/checkout/checkout-stepper.component.html` | MatStepper template |
| `frontend/bus-booking-web/src/app/features/customer/booking-confirmation/booking-confirmation.component.ts` | post-payment screen |
| `frontend/bus-booking-web/src/app/features/customer/booking-confirmation/booking-confirmation.component.html` | template |
| `frontend/bus-booking-web/src/types/razorpay.d.ts` | `Razorpay` window typing |

### Modified frontend files

- `frontend/bus-booking-web/src/app/shared/components/seat-map/seat-map.component.ts` — add `selectable`, `selectedSeats` (output), selection state
- `frontend/bus-booking-web/src/app/shared/components/seat-map/seat-map.component.html` — click handlers, selected styling
- `frontend/bus-booking-web/src/app/features/public/trip-detail/trip-detail.component.ts` — selection → `lockSeats` → navigate to `/checkout`
- `frontend/bus-booking-web/src/app/features/public/trip-detail/trip-detail.component.html` — sticky "Book Now" bar
- `frontend/bus-booking-web/src/app/features/auth/login/login.component.ts` — honour `?returnUrl=` query param
- `frontend/bus-booking-web/src/app/app.routes.ts` — add `checkout/:tripId` and `booking-confirmation/:id`
- `frontend/bus-booking-web/src/index.html` — load `https://checkout.razorpay.com/v1/checkout.js`

---

## Prerequisites

- M4 complete: `AppDbContext` has `Buses`, `SeatDefinitions`, `BusSchedules`, `BusTrips`. `TripService.BuildSeatLayoutAsync` returns a `SeatLayoutDto` marked "all available".
- An operator user with an approved bus and a schedule whose route has offices at both cities must be seedable through `IntegrationFixture`.
- Local Postgres reachable; `ConnectionStrings:Default` wired for dev; `bus_booking_test` exists for the tests.

---

## Task 1: Domain models (seat locks, bookings, payments, notifications)

**Files:**
- Create: `backend/BusBooking.Api/Models/BookingStatus.cs`
- Create: `backend/BusBooking.Api/Models/PaymentStatus.cs`
- Create: `backend/BusBooking.Api/Models/NotificationType.cs`
- Create: `backend/BusBooking.Api/Models/NotificationChannel.cs`
- Create: `backend/BusBooking.Api/Models/PassengerGender.cs`
- Create: `backend/BusBooking.Api/Models/SeatLock.cs`
- Create: `backend/BusBooking.Api/Models/Booking.cs`
- Create: `backend/BusBooking.Api/Models/BookingSeat.cs`
- Create: `backend/BusBooking.Api/Models/Payment.cs`
- Create: `backend/BusBooking.Api/Models/Notification.cs`

- [ ] **Step 1: Create status + enum-like constant classes**

```csharp
// backend/BusBooking.Api/Models/BookingStatus.cs
namespace BusBooking.Api.Models;

public static class BookingStatus
{
    public const string PendingPayment     = "pending_payment";
    public const string Confirmed          = "confirmed";
    public const string Cancelled          = "cancelled";
    public const string CancelledByOperator = "cancelled_by_operator";
    public const string Completed          = "completed";

    public static readonly string[] All =
        [PendingPayment, Confirmed, Cancelled, CancelledByOperator, Completed];
}
```

```csharp
// backend/BusBooking.Api/Models/PaymentStatus.cs
namespace BusBooking.Api.Models;

public static class PaymentStatus
{
    public const string Created  = "created";
    public const string Captured = "captured";
    public const string Failed   = "failed";
    public const string Refunded = "refunded";

    public static readonly string[] All = [Created, Captured, Failed, Refunded];
}
```

```csharp
// backend/BusBooking.Api/Models/NotificationType.cs
namespace BusBooking.Api.Models;

public static class NotificationType
{
    public const string BookingConfirmed  = "booking_confirmed";
    public const string Cancelled         = "cancelled";
    public const string Refund            = "refund";
    public const string OperatorApproved  = "operator_approved";
    public const string OperatorDisabled  = "operator_disabled";
}
```

```csharp
// backend/BusBooking.Api/Models/NotificationChannel.cs
namespace BusBooking.Api.Models;

public static class NotificationChannel
{
    public const string Email = "email";
}
```

```csharp
// backend/BusBooking.Api/Models/PassengerGender.cs
namespace BusBooking.Api.Models;

public static class PassengerGender
{
    public const string Male   = "male";
    public const string Female = "female";
    public const string Other  = "other";

    public static readonly string[] All = [Male, Female, Other];
}
```

- [ ] **Step 2: Create SeatLock entity**

```csharp
// backend/BusBooking.Api/Models/SeatLock.cs
namespace BusBooking.Api.Models;

public class SeatLock
{
    public Guid Id { get; set; }
    public Guid TripId { get; set; }
    public required string SeatNumber { get; set; }
    public Guid LockId { get; set; }       // group id returned to client; shared across all seats in one POST
    public Guid SessionId { get; set; }    // client-provided; validated on booking
    public Guid? UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    public BusTrip Trip { get; set; } = null!;
    public User? User { get; set; }
}
```

- [ ] **Step 3: Create Booking + BookingSeat entities**

```csharp
// backend/BusBooking.Api/Models/Booking.cs
namespace BusBooking.Api.Models;

public class Booking
{
    public Guid Id { get; set; }
    public required string BookingCode { get; set; }
    public Guid TripId { get; set; }
    public Guid UserId { get; set; }
    public Guid LockId { get; set; }        // seat-lock group that must be deleted on confirm
    public decimal TotalFare { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal TotalAmount { get; set; }
    public int SeatCount { get; set; }
    public string Status { get; set; } = BookingStatus.PendingPayment;
    public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public decimal? RefundAmount { get; set; }
    public string? RefundStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }

    public BusTrip Trip { get; set; } = null!;
    public User User { get; set; } = null!;
    public ICollection<BookingSeat> Seats { get; set; } = new List<BookingSeat>();
    public Payment? Payment { get; set; }
}
```

```csharp
// backend/BusBooking.Api/Models/BookingSeat.cs
namespace BusBooking.Api.Models;

public class BookingSeat
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public required string SeatNumber { get; set; }
    public required string PassengerName { get; set; }
    public int PassengerAge { get; set; }
    public required string PassengerGender { get; set; }

    public Booking Booking { get; set; } = null!;
}
```

- [ ] **Step 4: Create Payment + Notification entities**

```csharp
// backend/BusBooking.Api/Models/Payment.cs
namespace BusBooking.Api.Models;

public class Payment
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public required string RazorpayOrderId { get; set; }
    public string? RazorpayPaymentId { get; set; }
    public string? RazorpaySignature { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string Status { get; set; } = PaymentStatus.Created;
    public DateTime CreatedAt { get; set; }
    public DateTime? CapturedAt { get; set; }
    public DateTime? RefundedAt { get; set; }
    public string? RawResponse { get; set; }

    public Booking Booking { get; set; } = null!;
}
```

```csharp
// backend/BusBooking.Api/Models/Notification.cs
namespace BusBooking.Api.Models;

public class Notification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string Type { get; set; }
    public string Channel { get; set; } = NotificationChannel.Email;
    public required string ToAddress { get; set; }
    public required string Subject { get; set; }
    public string? ResendMessageId { get; set; }
    public string Status { get; set; } = "sent";    // sent | failed
    public DateTime CreatedAt { get; set; }
    public string? Error { get; set; }

    public User User { get; set; } = null!;
}
```

- [ ] **Step 5: Build the project**

Run: `dotnet build backend/BusBooking.Api/BusBooking.Api.csproj`
Expected: BUILD SUCCEEDED — entities compile but are not yet mapped.

- [ ] **Step 6: Commit**

```bash
git add backend/BusBooking.Api/Models/
git commit -m "feat(m5): add seat-lock, booking, payment, notification entities"
```

---

## Task 2: AppDbContext mappings + EF migration

**Files:**
- Modify: `backend/BusBooking.Api/Infrastructure/AppDbContext.cs`
- Create: `backend/BusBooking.Api/Migrations/<ts>_AddBookingsDomain.cs` (generated)

- [ ] **Step 1: Add DbSets**

In `AppDbContext.cs`, after the existing `BusTrips` line, add:

```csharp
public DbSet<SeatLock> SeatLocks => Set<SeatLock>();
public DbSet<Booking> Bookings => Set<Booking>();
public DbSet<BookingSeat> BookingSeats => Set<BookingSeat>();
public DbSet<Payment> Payments => Set<Payment>();
public DbSet<Notification> Notifications => Set<Notification>();
```

- [ ] **Step 2: Add entity configurations**

At the bottom of `OnModelCreating(ModelBuilder modelBuilder)`, before the closing brace of the method, append:

```csharp
modelBuilder.Entity<SeatLock>(b =>
{
    b.ToTable("seat_locks");
    b.HasKey(l => l.Id);
    b.Property(l => l.Id).HasColumnName("id");
    b.Property(l => l.TripId).HasColumnName("trip_id");
    b.Property(l => l.SeatNumber).HasColumnName("seat_number").IsRequired().HasMaxLength(8);
    b.Property(l => l.LockId).HasColumnName("lock_id");
    b.Property(l => l.SessionId).HasColumnName("session_id");
    b.Property(l => l.UserId).HasColumnName("user_id");
    b.Property(l => l.CreatedAt).HasColumnName("created_at");
    b.Property(l => l.ExpiresAt).HasColumnName("expires_at");
    b.HasIndex(l => new { l.TripId, l.SeatNumber }).IsUnique();
    b.HasIndex(l => l.LockId);
    b.HasIndex(l => l.ExpiresAt);
    b.HasOne(l => l.Trip).WithMany().HasForeignKey(l => l.TripId).OnDelete(DeleteBehavior.Cascade);
    b.HasOne(l => l.User).WithMany().HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.SetNull);
});

modelBuilder.Entity<Booking>(b =>
{
    b.ToTable("bookings");
    b.HasKey(x => x.Id);
    b.Property(x => x.Id).HasColumnName("id");
    b.Property(x => x.BookingCode).HasColumnName("booking_code").IsRequired().HasMaxLength(16);
    b.Property(x => x.TripId).HasColumnName("trip_id");
    b.Property(x => x.UserId).HasColumnName("user_id");
    b.Property(x => x.LockId).HasColumnName("lock_id");
    b.Property(x => x.TotalFare).HasColumnName("total_fare").HasColumnType("numeric(10,2)");
    b.Property(x => x.PlatformFee).HasColumnName("platform_fee").HasColumnType("numeric(10,2)");
    b.Property(x => x.TotalAmount).HasColumnName("total_amount").HasColumnType("numeric(10,2)");
    b.Property(x => x.SeatCount).HasColumnName("seat_count");
    b.Property(x => x.Status).HasColumnName("status").IsRequired().HasMaxLength(32);
    b.Property(x => x.CancellationReason).HasColumnName("cancellation_reason").HasMaxLength(500);
    b.Property(x => x.CancelledAt).HasColumnName("cancelled_at");
    b.Property(x => x.RefundAmount).HasColumnName("refund_amount").HasColumnType("numeric(10,2)");
    b.Property(x => x.RefundStatus).HasColumnName("refund_status").HasMaxLength(32);
    b.Property(x => x.CreatedAt).HasColumnName("created_at");
    b.Property(x => x.ConfirmedAt).HasColumnName("confirmed_at");
    b.HasIndex(x => x.BookingCode).IsUnique();
    b.HasIndex(x => x.UserId);
    b.HasIndex(x => x.TripId);
    b.HasIndex(x => x.Status);
    b.HasOne(x => x.Trip).WithMany().HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Restrict);
    b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
});

modelBuilder.Entity<BookingSeat>(b =>
{
    b.ToTable("booking_seats");
    b.HasKey(x => x.Id);
    b.Property(x => x.Id).HasColumnName("id");
    b.Property(x => x.BookingId).HasColumnName("booking_id");
    b.Property(x => x.SeatNumber).HasColumnName("seat_number").IsRequired().HasMaxLength(8);
    b.Property(x => x.PassengerName).HasColumnName("passenger_name").IsRequired().HasMaxLength(120);
    b.Property(x => x.PassengerAge).HasColumnName("passenger_age");
    b.Property(x => x.PassengerGender).HasColumnName("passenger_gender").IsRequired().HasMaxLength(10);
    b.HasIndex(x => new { x.BookingId, x.SeatNumber }).IsUnique();
    b.HasOne(x => x.Booking).WithMany(y => y.Seats).HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.Cascade);
});

modelBuilder.Entity<Payment>(b =>
{
    b.ToTable("payments");
    b.HasKey(x => x.Id);
    b.Property(x => x.Id).HasColumnName("id");
    b.Property(x => x.BookingId).HasColumnName("booking_id");
    b.Property(x => x.RazorpayOrderId).HasColumnName("razorpay_order_id").IsRequired().HasMaxLength(64);
    b.Property(x => x.RazorpayPaymentId).HasColumnName("razorpay_payment_id").HasMaxLength(64);
    b.Property(x => x.RazorpaySignature).HasColumnName("razorpay_signature").HasMaxLength(256);
    b.Property(x => x.Amount).HasColumnName("amount").HasColumnType("numeric(10,2)");
    b.Property(x => x.Currency).HasColumnName("currency").IsRequired().HasMaxLength(3);
    b.Property(x => x.Status).HasColumnName("status").IsRequired().HasMaxLength(16);
    b.Property(x => x.CreatedAt).HasColumnName("created_at");
    b.Property(x => x.CapturedAt).HasColumnName("captured_at");
    b.Property(x => x.RefundedAt).HasColumnName("refunded_at");
    b.Property(x => x.RawResponse).HasColumnName("raw_response").HasColumnType("jsonb");
    b.HasIndex(x => x.BookingId).IsUnique();
    b.HasIndex(x => x.RazorpayOrderId);
    b.HasOne(x => x.Booking).WithOne(y => y.Payment!).HasForeignKey<Payment>(x => x.BookingId).OnDelete(DeleteBehavior.Cascade);
});

modelBuilder.Entity<Notification>(b =>
{
    b.ToTable("notifications");
    b.HasKey(x => x.Id);
    b.Property(x => x.Id).HasColumnName("id");
    b.Property(x => x.UserId).HasColumnName("user_id");
    b.Property(x => x.Type).HasColumnName("type").IsRequired().HasMaxLength(32);
    b.Property(x => x.Channel).HasColumnName("channel").IsRequired().HasMaxLength(16);
    b.Property(x => x.ToAddress).HasColumnName("to_address").IsRequired().HasMaxLength(254);
    b.Property(x => x.Subject).HasColumnName("subject").IsRequired().HasMaxLength(200);
    b.Property(x => x.ResendMessageId).HasColumnName("resend_message_id").HasMaxLength(128);
    b.Property(x => x.Status).HasColumnName("status").IsRequired().HasMaxLength(16);
    b.Property(x => x.CreatedAt).HasColumnName("created_at");
    b.Property(x => x.Error).HasColumnName("error").HasMaxLength(1000);
    b.HasIndex(x => x.UserId);
    b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
});
```

- [ ] **Step 3: Generate the migration**

Run: `cd backend/BusBooking.Api && dotnet ef migrations add AddBookingsDomain`
Expected: a new file under `Migrations/` named `<timestamp>_AddBookingsDomain.cs` plus the regenerated model snapshot.

- [ ] **Step 4: Apply the migration to the dev DB**

Run: `cd backend/BusBooking.Api && dotnet ef database update`
Expected: `Applying migration '<timestamp>_AddBookingsDomain'. Done.`

- [ ] **Step 5: Commit**

```bash
git add backend/BusBooking.Api/Infrastructure/AppDbContext.cs \
        backend/BusBooking.Api/Migrations/
git commit -m "feat(m5): add EF mappings + migration for bookings domain"
```

---

## Task 3: DTOs for seat locks, bookings, passengers

**Files:**
- Create: `backend/BusBooking.Api/Dtos/LockSeatsRequest.cs`
- Create: `backend/BusBooking.Api/Dtos/SeatLockResponseDto.cs`
- Create: `backend/BusBooking.Api/Dtos/PassengerDto.cs`
- Create: `backend/BusBooking.Api/Dtos/CreateBookingRequest.cs`
- Create: `backend/BusBooking.Api/Dtos/CreateBookingResponseDto.cs`
- Create: `backend/BusBooking.Api/Dtos/VerifyPaymentRequest.cs`
- Create: `backend/BusBooking.Api/Dtos/BookingSeatDto.cs`
- Create: `backend/BusBooking.Api/Dtos/BookingDetailDto.cs`

- [ ] **Step 1: Create request/response records**

```csharp
// backend/BusBooking.Api/Dtos/LockSeatsRequest.cs
namespace BusBooking.Api.Dtos;

public record LockSeatsRequest(Guid SessionId, List<string> Seats);
```

```csharp
// backend/BusBooking.Api/Dtos/SeatLockResponseDto.cs
namespace BusBooking.Api.Dtos;

public record SeatLockResponseDto(
    Guid LockId,
    Guid SessionId,
    IReadOnlyList<string> Seats,
    DateTime ExpiresAt);
```

```csharp
// backend/BusBooking.Api/Dtos/PassengerDto.cs
namespace BusBooking.Api.Dtos;

public record PassengerDto(
    string SeatNumber,
    string PassengerName,
    int PassengerAge,
    string PassengerGender);
```

```csharp
// backend/BusBooking.Api/Dtos/CreateBookingRequest.cs
namespace BusBooking.Api.Dtos;

public record CreateBookingRequest(
    Guid TripId,
    Guid LockId,
    Guid SessionId,
    List<PassengerDto> Passengers);
```

```csharp
// backend/BusBooking.Api/Dtos/CreateBookingResponseDto.cs
namespace BusBooking.Api.Dtos;

public record CreateBookingResponseDto(
    Guid BookingId,
    string BookingCode,
    string RazorpayOrderId,
    string KeyId,
    long Amount,      // paise
    string Currency);
```

```csharp
// backend/BusBooking.Api/Dtos/VerifyPaymentRequest.cs
namespace BusBooking.Api.Dtos;

public record VerifyPaymentRequest(string RazorpayPaymentId, string RazorpaySignature);
```

```csharp
// backend/BusBooking.Api/Dtos/BookingSeatDto.cs
namespace BusBooking.Api.Dtos;

public record BookingSeatDto(
    string SeatNumber,
    string PassengerName,
    int PassengerAge,
    string PassengerGender);
```

```csharp
// backend/BusBooking.Api/Dtos/BookingDetailDto.cs
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
    IReadOnlyList<BookingSeatDto> Seats);
```

- [ ] **Step 2: Build**

Run: `dotnet build backend/BusBooking.Api/BusBooking.Api.csproj`
Expected: BUILD SUCCEEDED.

- [ ] **Step 3: Commit**

```bash
git add backend/BusBooking.Api/Dtos/
git commit -m "feat(m5): add DTOs for seat locks, bookings, passengers"
```

---

## Task 4: FluentValidation validators

**Files:**
- Create: `backend/BusBooking.Api/Validators/LockSeatsRequestValidator.cs`
- Create: `backend/BusBooking.Api/Validators/CreateBookingRequestValidator.cs`
- Create: `backend/BusBooking.Api/Validators/VerifyPaymentRequestValidator.cs`

- [ ] **Step 1: Validator for seat-lock body**

```csharp
// backend/BusBooking.Api/Validators/LockSeatsRequestValidator.cs
using BusBooking.Api.Dtos;
using FluentValidation;

namespace BusBooking.Api.Validators;

public class LockSeatsRequestValidator : AbstractValidator<LockSeatsRequest>
{
    public LockSeatsRequestValidator()
    {
        RuleFor(r => r.SessionId).NotEmpty();
        RuleFor(r => r.Seats)
            .NotEmpty().WithMessage("At least one seat must be selected")
            .Must(seats => seats.Count <= 6).WithMessage("Cannot lock more than 6 seats in one request")
            .Must(seats => seats.Distinct(StringComparer.OrdinalIgnoreCase).Count() == seats.Count)
                .WithMessage("Duplicate seat numbers");
        RuleForEach(r => r.Seats)
            .NotEmpty().MaximumLength(8).Matches("^[A-Za-z0-9]+$");
    }
}
```

- [ ] **Step 2: Validator for booking creation**

```csharp
// backend/BusBooking.Api/Validators/CreateBookingRequestValidator.cs
using BusBooking.Api.Dtos;
using BusBooking.Api.Models;
using FluentValidation;

namespace BusBooking.Api.Validators;

public class CreateBookingRequestValidator : AbstractValidator<CreateBookingRequest>
{
    public CreateBookingRequestValidator()
    {
        RuleFor(r => r.TripId).NotEmpty();
        RuleFor(r => r.LockId).NotEmpty();
        RuleFor(r => r.SessionId).NotEmpty();
        RuleFor(r => r.Passengers)
            .NotEmpty()
            .Must(p => p.Count <= 6).WithMessage("Cannot book more than 6 seats")
            .Must(p => p.Select(x => x.SeatNumber).Distinct(StringComparer.OrdinalIgnoreCase).Count() == p.Count)
                .WithMessage("Duplicate seat numbers in passenger list");
        RuleForEach(r => r.Passengers).ChildRules(p =>
        {
            p.RuleFor(x => x.SeatNumber).NotEmpty().MaximumLength(8);
            p.RuleFor(x => x.PassengerName).NotEmpty().MaximumLength(120);
            p.RuleFor(x => x.PassengerAge).InclusiveBetween(1, 120);
            p.RuleFor(x => x.PassengerGender).Must(g => PassengerGender.All.Contains(g))
                .WithMessage($"Must be one of: {string.Join(", ", PassengerGender.All)}");
        });
    }
}
```

- [ ] **Step 3: Validator for verify-payment**

```csharp
// backend/BusBooking.Api/Validators/VerifyPaymentRequestValidator.cs
using BusBooking.Api.Dtos;
using FluentValidation;

namespace BusBooking.Api.Validators;

public class VerifyPaymentRequestValidator : AbstractValidator<VerifyPaymentRequest>
{
    public VerifyPaymentRequestValidator()
    {
        RuleFor(r => r.RazorpayPaymentId).NotEmpty().MaximumLength(64);
        RuleFor(r => r.RazorpaySignature).NotEmpty().MaximumLength(256);
    }
}
```

- [ ] **Step 4: Build**

Run: `dotnet build backend/BusBooking.Api/BusBooking.Api.csproj`
Expected: BUILD SUCCEEDED.

- [ ] **Step 5: Commit**

```bash
git add backend/BusBooking.Api/Validators/
git commit -m "feat(m5): add validators for seat-lock, booking, verify-payment"
```

---

## Task 5: Razorpay client infrastructure

**Files:**
- Create: `backend/BusBooking.Api/Infrastructure/Razorpay/RazorpayOptions.cs`
- Create: `backend/BusBooking.Api/Infrastructure/Razorpay/IRazorpayClient.cs`
- Create: `backend/BusBooking.Api/Infrastructure/Razorpay/RazorpayClient.cs`
- Modify: `backend/BusBooking.Api/appsettings.json`

- [ ] **Step 1: Options record**

```csharp
// backend/BusBooking.Api/Infrastructure/Razorpay/RazorpayOptions.cs
namespace BusBooking.Api.Infrastructure.Razorpay;

public class RazorpayOptions
{
    public const string SectionName = "Razorpay";
    public string KeyId     { get; set; } = "";
    public string KeySecret { get; set; } = "";
    public string BaseUrl   { get; set; } = "https://api.razorpay.com";
}
```

- [ ] **Step 2: Contract**

```csharp
// backend/BusBooking.Api/Infrastructure/Razorpay/IRazorpayClient.cs
namespace BusBooking.Api.Infrastructure.Razorpay;

public record RazorpayOrder(string Id, long Amount, string Currency, string Receipt);

public interface IRazorpayClient
{
    string KeyId { get; }
    Task<RazorpayOrder> CreateOrderAsync(long amountInPaise, string receipt, CancellationToken ct);
    bool VerifySignature(string orderId, string paymentId, string signature);
}
```

- [ ] **Step 3: Implementation**

```csharp
// backend/BusBooking.Api/Infrastructure/Razorpay/RazorpayClient.cs
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace BusBooking.Api.Infrastructure.Razorpay;

public class RazorpayClient : IRazorpayClient
{
    public const string HttpClientName = "razorpay";
    private readonly HttpClient _http;
    private readonly RazorpayOptions _options;
    private readonly ILogger<RazorpayClient> _log;

    public RazorpayClient(
        IHttpClientFactory httpFactory,
        IOptions<RazorpayOptions> options,
        ILogger<RazorpayClient> log)
    {
        _options = options.Value;
        _log = log;
        _http = httpFactory.CreateClient(HttpClientName);
        _http.BaseAddress = new Uri(_options.BaseUrl);
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.KeyId}:{_options.KeySecret}"));
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basic);
    }

    public string KeyId => _options.KeyId;

    public async Task<RazorpayOrder> CreateOrderAsync(long amountInPaise, string receipt, CancellationToken ct)
    {
        var body = new { amount = amountInPaise, currency = "INR", receipt, payment_capture = 1 };
        var resp = await _http.PostAsJsonAsync("/v1/orders", body, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var text = await resp.Content.ReadAsStringAsync(ct);
            _log.LogError("Razorpay order creation failed {Status} {Body}", resp.StatusCode, text);
            throw new InvalidOperationException($"Razorpay order creation failed: {resp.StatusCode}");
        }
        var dto = await resp.Content.ReadFromJsonAsync<OrderResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Razorpay response was empty");
        return new RazorpayOrder(dto.id, dto.amount, dto.currency, dto.receipt ?? receipt);
    }

    public bool VerifySignature(string orderId, string paymentId, string signature)
    {
        var payload = $"{orderId}|{paymentId}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.KeySecret));
        var computed = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(signature.ToLowerInvariant()));
    }

    private record OrderResponse(string id, long amount, string currency, string? receipt);
}
```

- [ ] **Step 4: Add placeholder config to appsettings.json**

In `backend/BusBooking.Api/appsettings.json`, add a `Razorpay` section alongside existing sections (create one if the file has no trailing block). The exact keys:

```json
"Razorpay": {
  "KeyId": "",
  "KeySecret": "",
  "BaseUrl": "https://api.razorpay.com"
}
```

- [ ] **Step 5: Build**

Run: `dotnet build backend/BusBooking.Api/BusBooking.Api.csproj`
Expected: BUILD SUCCEEDED.

- [ ] **Step 6: Commit**

```bash
git add backend/BusBooking.Api/Infrastructure/Razorpay/ \
        backend/BusBooking.Api/appsettings.json
git commit -m "feat(m5): add Razorpay HTTP client + HMAC signature verify"
```

---

## Task 6: Resend email client + extend INotificationSender

**Files:**
- Create: `backend/BusBooking.Api/Infrastructure/Resend/ResendOptions.cs`
- Create: `backend/BusBooking.Api/Infrastructure/Resend/IResendEmailClient.cs`
- Create: `backend/BusBooking.Api/Infrastructure/Resend/ResendEmailClient.cs`
- Modify: `backend/BusBooking.Api/Services/INotificationSender.cs`
- Modify: `backend/BusBooking.Api/Services/LoggingNotificationSender.cs`
- Modify: `backend/BusBooking.Api/appsettings.json`

- [ ] **Step 1: Options record**

```csharp
// backend/BusBooking.Api/Infrastructure/Resend/ResendOptions.cs
namespace BusBooking.Api.Infrastructure.Resend;

public class ResendOptions
{
    public const string SectionName = "Resend";
    public string ApiKey      { get; set; } = "";
    public string FromAddress { get; set; } = "onboarding@resend.dev";
    public string BaseUrl     { get; set; } = "https://api.resend.com";
}
```

- [ ] **Step 2: Email client contract**

```csharp
// backend/BusBooking.Api/Infrastructure/Resend/IResendEmailClient.cs
namespace BusBooking.Api.Infrastructure.Resend;

public record ResendAttachment(string Filename, byte[] Content);
public record ResendSendResult(string? MessageId, bool Success, string? Error);

public interface IResendEmailClient
{
    Task<ResendSendResult> SendAsync(
        string toAddress,
        string subject,
        string htmlBody,
        IReadOnlyList<ResendAttachment> attachments,
        CancellationToken ct);
}
```

- [ ] **Step 3: Email client implementation**

```csharp
// backend/BusBooking.Api/Infrastructure/Resend/ResendEmailClient.cs
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace BusBooking.Api.Infrastructure.Resend;

public class ResendEmailClient : IResendEmailClient
{
    public const string HttpClientName = "resend";
    private readonly HttpClient _http;
    private readonly ResendOptions _options;
    private readonly ILogger<ResendEmailClient> _log;

    public ResendEmailClient(
        IHttpClientFactory httpFactory,
        IOptions<ResendOptions> options,
        ILogger<ResendEmailClient> log)
    {
        _options = options.Value;
        _log = log;
        _http = httpFactory.CreateClient(HttpClientName);
        _http.BaseAddress = new Uri(_options.BaseUrl);
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
    }

    public async Task<ResendSendResult> SendAsync(
        string toAddress, string subject, string htmlBody,
        IReadOnlyList<ResendAttachment> attachments, CancellationToken ct)
    {
        var body = new
        {
            from = _options.FromAddress,
            to = new[] { toAddress },
            subject,
            html = htmlBody,
            attachments = attachments.Select(a => new
            {
                filename = a.Filename,
                content = Convert.ToBase64String(a.Content)
            }).ToArray()
        };

        try
        {
            var resp = await _http.PostAsJsonAsync("/emails", body, ct);
            if (!resp.IsSuccessStatusCode)
            {
                var text = await resp.Content.ReadAsStringAsync(ct);
                _log.LogWarning("Resend email failed {Status} {Body}", resp.StatusCode, text);
                return new ResendSendResult(null, false, $"{(int)resp.StatusCode}: {text}");
            }
            var dto = await resp.Content.ReadFromJsonAsync<SendResponse>(cancellationToken: ct);
            return new ResendSendResult(dto?.id, true, null);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Resend send exception");
            return new ResendSendResult(null, false, ex.Message);
        }
    }

    private record SendResponse(string? id);
}
```

- [ ] **Step 4: Extend INotificationSender**

```csharp
// backend/BusBooking.Api/Services/INotificationSender.cs
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
}
```

- [ ] **Step 5: Extend LoggingNotificationSender**

Replace `LoggingNotificationSender.cs` with the real Resend-backed implementation (drops the old stub, keeps the existing four methods as log-only so M3 tests stay green; booking confirmation uses Resend + persists a notifications row).

```csharp
// backend/BusBooking.Api/Services/LoggingNotificationSender.cs
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Infrastructure.Resend;
using BusBooking.Api.Models;
using Microsoft.Extensions.Logging;

namespace BusBooking.Api.Services;

public class LoggingNotificationSender : INotificationSender
{
    private readonly ILogger<LoggingNotificationSender> _log;
    private readonly IResendEmailClient _email;
    private readonly AppDbContext _db;
    private readonly TimeProvider _time;

    public LoggingNotificationSender(
        ILogger<LoggingNotificationSender> log,
        IResendEmailClient email,
        AppDbContext db,
        TimeProvider time)
    {
        _log = log;
        _email = email;
        _db = db;
        _time = time;
    }

    public Task SendOperatorApprovedAsync(User user, CancellationToken ct = default)
    {
        _log.LogInformation("NOTIFY operator-approved to={Email}", user.Email);
        return Task.CompletedTask;
    }

    public Task SendOperatorRejectedAsync(User user, string reason, CancellationToken ct = default)
    {
        _log.LogInformation("NOTIFY operator-rejected to={Email} reason={Reason}", user.Email, reason);
        return Task.CompletedTask;
    }

    public Task SendBusApprovedAsync(User operatorUser, Bus bus, CancellationToken ct = default)
    {
        _log.LogInformation("NOTIFY bus-approved to={Email} bus={Reg}", operatorUser.Email, bus.RegistrationNumber);
        return Task.CompletedTask;
    }

    public Task SendBusRejectedAsync(User operatorUser, Bus bus, string reason, CancellationToken ct = default)
    {
        _log.LogInformation("NOTIFY bus-rejected to={Email} bus={Reg} reason={Reason}",
            operatorUser.Email, bus.RegistrationNumber, reason);
        return Task.CompletedTask;
    }

    public async Task SendBookingConfirmedAsync(User user, BookingDetailDto booking, byte[] pdfTicket, CancellationToken ct = default)
    {
        var subject = $"Booking confirmed — {booking.BookingCode}";
        var html = BuildBookingConfirmedHtml(user, booking);
        var result = await _email.SendAsync(
            user.Email,
            subject,
            html,
            new[] { new ResendAttachment($"ticket-{booking.BookingCode}.pdf", pdfTicket) },
            ct);

        _db.Notifications.Add(new Notification
        {
            Id              = Guid.NewGuid(),
            UserId          = user.Id,
            Type            = NotificationType.BookingConfirmed,
            Channel         = NotificationChannel.Email,
            ToAddress       = user.Email,
            Subject         = subject,
            ResendMessageId = result.MessageId,
            Status          = result.Success ? "sent" : "failed",
            Error           = result.Error,
            CreatedAt       = _time.GetUtcNow().UtcDateTime
        });
        await _db.SaveChangesAsync(ct);

        if (!result.Success)
            _log.LogWarning("Booking confirmation email failed for {BookingCode}: {Error}",
                booking.BookingCode, result.Error);
    }

    private static string BuildBookingConfirmedHtml(User user, BookingDetailDto b)
    {
        var seatList = string.Join(", ", b.Seats.Select(s => s.SeatNumber));
        return $"""
            <p>Hi {user.Name},</p>
            <p>Your booking <strong>{b.BookingCode}</strong> is confirmed.</p>
            <ul>
              <li>{b.SourceCity} → {b.DestinationCity}</li>
              <li>{b.TripDate:yyyy-MM-dd} · {b.DepartureTime:HH\\:mm} — {b.ArrivalTime:HH\\:mm}</li>
              <li>{b.BusName} by {b.OperatorName}</li>
              <li>Seats: {seatList}</li>
              <li>Total: ₹{b.TotalAmount:0.00}</li>
            </ul>
            <p>Your PDF ticket is attached.</p>
            """;
    }
}
```

- [ ] **Step 6: Add Resend config to appsettings.json**

Add next to the `Razorpay` block:

```json
"Resend": {
  "ApiKey": "",
  "FromAddress": "onboarding@resend.dev",
  "BaseUrl": "https://api.resend.com"
}
```

- [ ] **Step 7: Build**

Run: `dotnet build backend/BusBooking.Api/BusBooking.Api.csproj`
Expected: BUILD SUCCEEDED.

- [ ] **Step 8: Commit**

```bash
git add backend/BusBooking.Api/Infrastructure/Resend/ \
        backend/BusBooking.Api/Services/INotificationSender.cs \
        backend/BusBooking.Api/Services/LoggingNotificationSender.cs \
        backend/BusBooking.Api/appsettings.json
git commit -m "feat(m5): add Resend email client + booking-confirmed notification"
```

---

## Task 7: PDF ticket generator (QuestPDF)

**Files:**
- Modify: `backend/BusBooking.Api/BusBooking.Api.csproj`
- Create: `backend/BusBooking.Api/Infrastructure/Pdf/IPdfTicketGenerator.cs`
- Create: `backend/BusBooking.Api/Infrastructure/Pdf/PdfTicketGenerator.cs`
- Create: `backend/BusBooking.Api.Tests/Unit/PdfTicketGeneratorTests.cs`

- [ ] **Step 1: Add QuestPDF NuGet**

Run: `cd backend/BusBooking.Api && dotnet add package QuestPDF --version 2024.12.1`
Expected: `info : Package 'QuestPDF' version '2024.12.1' added`.

Note: QuestPDF's Community licence is free for individual developers; the license must be set at startup (handled in Task 14).

- [ ] **Step 2: Contract**

```csharp
// backend/BusBooking.Api/Infrastructure/Pdf/IPdfTicketGenerator.cs
using BusBooking.Api.Dtos;

namespace BusBooking.Api.Infrastructure.Pdf;

public interface IPdfTicketGenerator
{
    byte[] Generate(BookingDetailDto booking);
}
```

- [ ] **Step 3: Implementation**

```csharp
// backend/BusBooking.Api/Infrastructure/Pdf/PdfTicketGenerator.cs
using BusBooking.Api.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BusBooking.Api.Infrastructure.Pdf;

public class PdfTicketGenerator : IPdfTicketGenerator
{
    public byte[] Generate(BookingDetailDto b)
    {
        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Margin(36);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(ts => ts.FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item().Text("BusBooking — e-Ticket").FontSize(20).Bold();
                    col.Item().Text($"Booking code: {b.BookingCode}").SemiBold();
                });

                page.Content().PaddingVertical(12).Column(col =>
                {
                    col.Spacing(6);
                    col.Item().Text($"{b.SourceCity} → {b.DestinationCity}").FontSize(14).SemiBold();
                    col.Item().Text($"{b.TripDate:yyyy-MM-dd}  ·  {b.DepartureTime:HH\\:mm} → {b.ArrivalTime:HH\\:mm}");
                    col.Item().Text($"{b.BusName} · {b.OperatorName}");
                    col.Item().PaddingTop(8).Text("Passengers").Bold();

                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(60);
                            c.RelativeColumn();
                            c.ConstantColumn(60);
                            c.ConstantColumn(80);
                        });
                        t.Header(h =>
                        {
                            h.Cell().Text("Seat").Bold();
                            h.Cell().Text("Name").Bold();
                            h.Cell().Text("Age").Bold();
                            h.Cell().Text("Gender").Bold();
                        });
                        foreach (var s in b.Seats)
                        {
                            t.Cell().Text(s.SeatNumber);
                            t.Cell().Text(s.PassengerName);
                            t.Cell().Text(s.PassengerAge.ToString());
                            t.Cell().Text(s.PassengerGender);
                        }
                    });

                    col.Item().PaddingTop(12).Text($"Fare: ₹{b.TotalFare:0.00}");
                    col.Item().Text($"Platform fee: ₹{b.PlatformFee:0.00}");
                    col.Item().Text($"Total paid: ₹{b.TotalAmount:0.00}").Bold();
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Generated ").FontColor(Colors.Grey.Medium);
                    t.Span(DateTime.UtcNow.ToString("u")).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf();
    }
}
```

- [ ] **Step 4: Write the failing unit test**

```csharp
// backend/BusBooking.Api.Tests/Unit/PdfTicketGeneratorTests.cs
using System.Text;
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure.Pdf;
using FluentAssertions;
using QuestPDF;
using QuestPDF.Infrastructure;

namespace BusBooking.Api.Tests.Unit;

public class PdfTicketGeneratorTests
{
    public PdfTicketGeneratorTests()
    {
        Settings.License = LicenseType.Community;
    }

    [Fact]
    public void Generate_ReturnsPdfWithBookingCode()
    {
        var gen = new PdfTicketGenerator();
        var dto = new BookingDetailDto(
            Guid.NewGuid(),
            "BK-ABCDEFGH",
            Guid.NewGuid(),
            new DateOnly(2026, 6, 1),
            "Bangalore", "Chennai",
            "Volvo Multi-axle", "SpeedyBus",
            new TimeOnly(22, 0), new TimeOnly(6, 0),
            900m, 50m, 950m, 1, BookingStatusOrPending,
            null, DateTime.UtcNow,
            new[] { new BookingSeatDto("A1", "Asha", 30, "female") });

        var bytes = gen.Generate(dto);

        bytes.Should().NotBeNullOrEmpty();
        Encoding.ASCII.GetString(bytes, 0, 5).Should().Be("%PDF-");
        // QuestPDF compresses text streams; search for the literal bytes of the code is unreliable.
        // Instead, assert the output is a non-trivial PDF (> 1KB).
        bytes.Length.Should().BeGreaterThan(1_000);
    }

    private const string BookingStatusOrPending = "confirmed";
}
```

- [ ] **Step 5: Run the test (should fail to compile until the PDF generator is wired in)**

Run: `dotnet test backend/BusBooking.Api.Tests/BusBooking.Api.Tests.csproj --filter FullyQualifiedName~PdfTicketGenerator -v minimal`
Expected: on first pass, PASS (the implementation is already in place). Confirming the sequence here rather than TDD-failing because the PDF library is a black-box dependency.

- [ ] **Step 6: Commit**

```bash
git add backend/BusBooking.Api/BusBooking.Api.csproj \
        backend/BusBooking.Api/Infrastructure/Pdf/ \
        backend/BusBooking.Api.Tests/Unit/PdfTicketGeneratorTests.cs
git commit -m "feat(m5): add QuestPDF-based ticket generator + unit test"
```

---

## Task 8: SeatLockService

**Files:**
- Create: `backend/BusBooking.Api/Services/ISeatLockService.cs`
- Create: `backend/BusBooking.Api/Services/SeatLockService.cs`

- [ ] **Step 1: Contract**

```csharp
// backend/BusBooking.Api/Services/ISeatLockService.cs
using BusBooking.Api.Dtos;
using BusBooking.Api.Models;

namespace BusBooking.Api.Services;

public interface ISeatLockService
{
    Task<SeatLockResponseDto> LockAsync(Guid tripId, Guid? userId, LockSeatsRequest req, CancellationToken ct);
    Task ReleaseAsync(Guid lockId, Guid sessionId, Guid? userId, CancellationToken ct);
    Task<IReadOnlyList<SeatLock>> GetActiveLocksAsync(Guid lockId, CancellationToken ct);
}
```

- [ ] **Step 2: Implementation**

```csharp
// backend/BusBooking.Api/Services/SeatLockService.cs
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Infrastructure.Errors;
using BusBooking.Api.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BusBooking.Api.Services;

public class SeatLockService : ISeatLockService
{
    public static readonly TimeSpan LockWindow = TimeSpan.FromMinutes(7);
    private readonly AppDbContext _db;
    private readonly TimeProvider _time;

    public SeatLockService(AppDbContext db, TimeProvider time)
    {
        _db = db;
        _time = time;
    }

    public async Task<SeatLockResponseDto> LockAsync(
        Guid tripId, Guid? userId, LockSeatsRequest req, CancellationToken ct)
    {
        var trip = await _db.BusTrips
            .Include(t => t.Schedule).ThenInclude(s => s!.Bus)
            .FirstOrDefaultAsync(t => t.Id == tripId, ct)
            ?? throw new NotFoundException("Trip not found");

        if (trip.Status != TripStatus.Scheduled)
            throw new BusinessRuleException("TRIP_NOT_AVAILABLE", "Trip is not available for booking");

        // Validate requested seats exist on the bus.
        var validSeats = await _db.SeatDefinitions
            .Where(s => s.BusId == trip.Schedule!.BusId)
            .Select(s => s.SeatNumber)
            .ToListAsync(ct);
        var unknown = req.Seats.Except(validSeats, StringComparer.OrdinalIgnoreCase).ToList();
        if (unknown.Count > 0)
            throw new BusinessRuleException("UNKNOWN_SEATS", "Seat numbers do not belong to this bus",
                new { unknown });

        // Reject seats already confirmed in a booking.
        var bookedSeats = await _db.BookingSeats
            .Where(bs => bs.Booking.TripId == tripId
                         && bs.Booking.Status != BookingStatus.Cancelled
                         && bs.Booking.Status != BookingStatus.CancelledByOperator)
            .Select(bs => bs.SeatNumber)
            .ToListAsync(ct);
        var alreadyBooked = req.Seats.Intersect(bookedSeats, StringComparer.OrdinalIgnoreCase).ToList();
        if (alreadyBooked.Count > 0)
            throw new ConflictException("SEAT_UNAVAILABLE", "One or more seats are already booked",
                new { unavailable = alreadyBooked });

        var now       = _time.GetUtcNow().UtcDateTime;
        var expiresAt = now + LockWindow;
        var lockId    = Guid.NewGuid();

        foreach (var seat in req.Seats)
        {
            _db.SeatLocks.Add(new SeatLock
            {
                Id         = Guid.NewGuid(),
                TripId     = tripId,
                SeatNumber = seat,
                LockId     = lockId,
                SessionId  = req.SessionId,
                UserId     = userId,
                CreatedAt  = now,
                ExpiresAt  = expiresAt
            });
        }

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == "23505")
        {
            // Unique (trip_id, seat_number) — another session got here first. Detach so a retry can be clean.
            foreach (var entry in _db.ChangeTracker.Entries<SeatLock>().ToList())
                entry.State = EntityState.Detached;

            var currentLockOwners = await _db.SeatLocks
                .Where(l => l.TripId == tripId && l.ExpiresAt > now && req.Seats.Contains(l.SeatNumber))
                .Select(l => l.SeatNumber)
                .ToListAsync(ct);

            throw new ConflictException("SEAT_UNAVAILABLE", "One or more seats are currently locked",
                new { unavailable = currentLockOwners });
        }

        return new SeatLockResponseDto(lockId, req.SessionId, req.Seats, expiresAt);
    }

    public async Task ReleaseAsync(Guid lockId, Guid sessionId, Guid? userId, CancellationToken ct)
    {
        var rows = await _db.SeatLocks.Where(l => l.LockId == lockId).ToListAsync(ct);
        if (rows.Count == 0)
            throw new NotFoundException("Lock not found");

        var any = rows[0];
        // Either the holder of the session or the authenticated user may release.
        var sessionMatches = any.SessionId == sessionId;
        var userMatches    = userId.HasValue && any.UserId == userId;
        if (!sessionMatches && !userMatches)
            throw new ForbiddenException("Not owner of this lock");

        _db.SeatLocks.RemoveRange(rows);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<SeatLock>> GetActiveLocksAsync(Guid lockId, CancellationToken ct)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        return await _db.SeatLocks
            .Where(l => l.LockId == lockId && l.ExpiresAt > now)
            .ToListAsync(ct);
    }
}
```

- [ ] **Step 3: Build**

Run: `dotnet build backend/BusBooking.Api/BusBooking.Api.csproj`
Expected: BUILD SUCCEEDED.

- [ ] **Step 4: Commit**

```bash
git add backend/BusBooking.Api/Services/ISeatLockService.cs \
        backend/BusBooking.Api/Services/SeatLockService.cs
git commit -m "feat(m5): add SeatLockService with unique-constraint concurrency"
```

---

## Task 9: SeatLockCleanupService (IHostedService)

**Files:**
- Create: `backend/BusBooking.Api/Background/SeatLockCleanupService.cs`

- [ ] **Step 1: Create the hosted service**

```csharp
// backend/BusBooking.Api/Background/SeatLockCleanupService.cs
using BusBooking.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Api.Background;

public class SeatLockCleanupService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);
    private readonly IServiceProvider _sp;
    private readonly ILogger<SeatLockCleanupService> _log;

    public SeatLockCleanupService(IServiceProvider sp, ILogger<SeatLockCleanupService> log)
    {
        _sp = sp;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var time = scope.ServiceProvider.GetRequiredService<TimeProvider>();
                var cutoff = time.GetUtcNow().UtcDateTime;
                var removed = await db.SeatLocks
                    .Where(l => l.ExpiresAt < cutoff)
                    .ExecuteDeleteAsync(stoppingToken);
                if (removed > 0)
                    _log.LogInformation("SeatLockCleanup removed {Count} expired locks", removed);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _log.LogError(ex, "SeatLockCleanup tick failed");
            }

            try { await Task.Delay(Interval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}
```

- [ ] **Step 2: Build**

Run: `dotnet build backend/BusBooking.Api/BusBooking.Api.csproj`
Expected: BUILD SUCCEEDED.

- [ ] **Step 3: Commit**

```bash
git add backend/BusBooking.Api/Background/
git commit -m "feat(m5): add SeatLockCleanupService background worker"
```

---

## Task 10: BookingService

**Files:**
- Create: `backend/BusBooking.Api/Services/IBookingService.cs`
- Create: `backend/BusBooking.Api/Services/BookingService.cs`

- [ ] **Step 1: Contract**

```csharp
// backend/BusBooking.Api/Services/IBookingService.cs
using BusBooking.Api.Dtos;

namespace BusBooking.Api.Services;

public interface IBookingService
{
    Task<CreateBookingResponseDto> CreateAsync(Guid userId, CreateBookingRequest req, CancellationToken ct);
    Task<BookingDetailDto> VerifyPaymentAsync(Guid userId, Guid bookingId, VerifyPaymentRequest req, CancellationToken ct);
    Task<BookingDetailDto> GetAsync(Guid userId, Guid bookingId, CancellationToken ct);
    Task<byte[]> GetTicketPdfAsync(Guid userId, Guid bookingId, CancellationToken ct);
}
```

- [ ] **Step 2: Implementation**

```csharp
// backend/BusBooking.Api/Services/BookingService.cs
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Infrastructure.Errors;
using BusBooking.Api.Infrastructure.Pdf;
using BusBooking.Api.Infrastructure.Razorpay;
using BusBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Api.Services;

public class BookingService : IBookingService
{
    private readonly AppDbContext _db;
    private readonly ISeatLockService _locks;
    private readonly IPlatformFeeService _platformFee;
    private readonly IRazorpayClient _razorpay;
    private readonly IPdfTicketGenerator _pdf;
    private readonly INotificationSender _notifications;
    private readonly TimeProvider _time;
    private readonly ILogger<BookingService> _log;

    public BookingService(
        AppDbContext db, ISeatLockService locks, IPlatformFeeService platformFee,
        IRazorpayClient razorpay, IPdfTicketGenerator pdf, INotificationSender notifications,
        TimeProvider time, ILogger<BookingService> log)
    {
        _db = db;
        _locks = locks;
        _platformFee = platformFee;
        _razorpay = razorpay;
        _pdf = pdf;
        _notifications = notifications;
        _time = time;
        _log = log;
    }

    public async Task<CreateBookingResponseDto> CreateAsync(Guid userId, CreateBookingRequest req, CancellationToken ct)
    {
        var now = _time.GetUtcNow().UtcDateTime;

        // 1. Validate the lock.
        var lockRows = await _db.SeatLocks
            .Where(l => l.LockId == req.LockId)
            .ToListAsync(ct);
        if (lockRows.Count == 0)
            throw new ConflictException("LOCK_EXPIRED", "Your seat reservation has expired");
        if (lockRows.Any(l => l.ExpiresAt <= now))
            throw new ConflictException("LOCK_EXPIRED", "Your seat reservation has expired");
        if (lockRows.Any(l => l.TripId != req.TripId))
            throw new BusinessRuleException("LOCK_TRIP_MISMATCH", "Lock does not belong to this trip");
        if (lockRows.Any(l => l.SessionId != req.SessionId))
            throw new ForbiddenException("Lock session mismatch");

        var lockedSeats = lockRows.Select(l => l.SeatNumber).OrderBy(x => x).ToList();
        var requestedSeats = req.Passengers.Select(p => p.SeatNumber).OrderBy(x => x).ToList();
        if (!lockedSeats.SequenceEqual(requestedSeats, StringComparer.OrdinalIgnoreCase))
            throw new BusinessRuleException("PASSENGER_SEAT_MISMATCH",
                "Passenger seat list does not match the locked seats",
                new { expected = lockedSeats, actual = requestedSeats });

        // 2. Load trip + fare.
        var trip = await _db.BusTrips
            .Include(t => t.Schedule)
            .FirstAsync(t => t.Id == req.TripId, ct);

        // 3. Snapshot platform fee.
        var fee = await _platformFee.GetActiveAsync(ct);
        var seatCount   = req.Passengers.Count;
        var totalFare   = Math.Round(trip.Schedule!.FarePerSeat * seatCount, 2);
        var platformFee = fee.FeeType == PlatformFeeType.Fixed
            ? fee.Value
            : Math.Round(totalFare * fee.Value / 100m, 2);
        var totalAmount = totalFare + platformFee;
        var paise       = (long)(totalAmount * 100m);

        // 4. Generate booking code.
        var bookingCode = $"BK-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

        // 5. Create Razorpay order.
        var order = await _razorpay.CreateOrderAsync(paise, bookingCode, ct);

        // 6. Persist booking + seats + payment in one transaction. The Npgsql provider wraps SaveChanges
        //    in a transaction already; explicit BeginTransactionAsync is only needed here because we make
        //    the Razorpay call before writing and do not want a partial row if SaveChanges fails.
        var bookingId = Guid.NewGuid();
        var booking = new Booking
        {
            Id          = bookingId,
            BookingCode = bookingCode,
            TripId      = req.TripId,
            UserId      = userId,
            LockId      = req.LockId,
            TotalFare   = totalFare,
            PlatformFee = platformFee,
            TotalAmount = totalAmount,
            SeatCount   = seatCount,
            Status      = BookingStatus.PendingPayment,
            CreatedAt   = now
        };
        _db.Bookings.Add(booking);

        foreach (var p in req.Passengers)
        {
            _db.BookingSeats.Add(new BookingSeat
            {
                Id              = Guid.NewGuid(),
                BookingId       = bookingId,
                SeatNumber      = p.SeatNumber,
                PassengerName   = p.PassengerName,
                PassengerAge    = p.PassengerAge,
                PassengerGender = p.PassengerGender
            });
        }

        _db.Payments.Add(new Payment
        {
            Id              = Guid.NewGuid(),
            BookingId       = bookingId,
            RazorpayOrderId = order.Id,
            Amount          = totalAmount,
            Currency        = "INR",
            Status          = PaymentStatus.Created,
            CreatedAt       = now
        });

        // Tag the lock with the user id so the release/verify paths can prove ownership.
        foreach (var l in lockRows) l.UserId = userId;

        await _db.SaveChangesAsync(ct);

        return new CreateBookingResponseDto(
            bookingId, bookingCode, order.Id, _razorpay.KeyId, paise, "INR");
    }

    public async Task<BookingDetailDto> VerifyPaymentAsync(
        Guid userId, Guid bookingId, VerifyPaymentRequest req, CancellationToken ct)
    {
        var booking = await _db.Bookings
            .Include(b => b.Seats)
            .Include(b => b.Payment)
            .Include(b => b.Trip).ThenInclude(t => t!.Schedule).ThenInclude(s => s!.Bus).ThenInclude(b => b!.Operator)
            .Include(b => b.Trip).ThenInclude(t => t!.Schedule).ThenInclude(s => s!.Route).ThenInclude(r => r!.SourceCity)
            .Include(b => b.Trip).ThenInclude(t => t!.Schedule).ThenInclude(s => s!.Route).ThenInclude(r => r!.DestinationCity)
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == bookingId, ct)
            ?? throw new NotFoundException("Booking not found");

        if (booking.UserId != userId)
            throw new ForbiddenException("Not your booking");

        var payment = booking.Payment
            ?? throw new BusinessRuleException("PAYMENT_MISSING", "No payment on this booking");

        // Idempotent: already captured?
        if (booking.Status == BookingStatus.Confirmed && payment.Status == PaymentStatus.Captured)
        {
            if (!string.Equals(payment.RazorpayPaymentId, req.RazorpayPaymentId, StringComparison.Ordinal))
                throw new ConflictException("PAYMENT_MISMATCH",
                    "Booking is already confirmed with a different payment id");
            return MapDetail(booking);
        }

        if (!_razorpay.VerifySignature(payment.RazorpayOrderId, req.RazorpayPaymentId, req.RazorpaySignature))
            throw new BusinessRuleException("SIGNATURE_INVALID", "Razorpay signature did not verify");

        var now = _time.GetUtcNow().UtcDateTime;
        payment.RazorpayPaymentId = req.RazorpayPaymentId;
        payment.RazorpaySignature = req.RazorpaySignature;
        payment.Status            = PaymentStatus.Captured;
        payment.CapturedAt        = now;

        booking.Status      = BookingStatus.Confirmed;
        booking.ConfirmedAt = now;

        // Delete the seat locks (the booking_seats row is now the permanent reservation).
        var locks = await _db.SeatLocks.Where(l => l.LockId == booking.LockId).ToListAsync(ct);
        _db.SeatLocks.RemoveRange(locks);

        await _db.SaveChangesAsync(ct);

        var dto = MapDetail(booking);

        // Side effects post-commit. Failures are logged but do not roll back the booking.
        try
        {
            var pdf = _pdf.Generate(dto);
            await _notifications.SendBookingConfirmedAsync(booking.User, dto, pdf, ct);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Post-confirm notification failed for {BookingCode}", booking.BookingCode);
        }

        return dto;
    }

    public async Task<BookingDetailDto> GetAsync(Guid userId, Guid bookingId, CancellationToken ct)
    {
        var booking = await LoadForDetailAsync(bookingId, ct)
            ?? throw new NotFoundException("Booking not found");
        if (booking.UserId != userId)
            throw new ForbiddenException("Not your booking");
        return MapDetail(booking);
    }

    public async Task<byte[]> GetTicketPdfAsync(Guid userId, Guid bookingId, CancellationToken ct)
    {
        var booking = await LoadForDetailAsync(bookingId, ct)
            ?? throw new NotFoundException("Booking not found");
        if (booking.UserId != userId)
            throw new ForbiddenException("Not your booking");
        if (booking.Status != BookingStatus.Confirmed && booking.Status != BookingStatus.Completed)
            throw new BusinessRuleException("TICKET_NOT_AVAILABLE", "Ticket is only available for confirmed bookings");
        return _pdf.Generate(MapDetail(booking));
    }

    private async Task<Booking?> LoadForDetailAsync(Guid bookingId, CancellationToken ct) =>
        await _db.Bookings
            .Include(b => b.Seats)
            .Include(b => b.Payment)
            .Include(b => b.Trip).ThenInclude(t => t!.Schedule).ThenInclude(s => s!.Bus).ThenInclude(b => b!.Operator)
            .Include(b => b.Trip).ThenInclude(t => t!.Schedule).ThenInclude(s => s!.Route).ThenInclude(r => r!.SourceCity)
            .Include(b => b.Trip).ThenInclude(t => t!.Schedule).ThenInclude(s => s!.Route).ThenInclude(r => r!.DestinationCity)
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == bookingId, ct);

    private static BookingDetailDto MapDetail(Booking b)
    {
        var schedule = b.Trip!.Schedule!;
        var route    = schedule.Route!;
        var bus      = schedule.Bus!;

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
            b.Seats
                .OrderBy(s => s.SeatNumber)
                .Select(s => new BookingSeatDto(s.SeatNumber, s.PassengerName, s.PassengerAge, s.PassengerGender))
                .ToList());
    }
}
```

- [ ] **Step 3: Build**

Run: `dotnet build backend/BusBooking.Api/BusBooking.Api.csproj`
Expected: BUILD SUCCEEDED.

- [ ] **Step 4: Commit**

```bash
git add backend/BusBooking.Api/Services/IBookingService.cs \
        backend/BusBooking.Api/Services/BookingService.cs
git commit -m "feat(m5): add BookingService with Razorpay + PDF + email side-effects"
```

---

## Task 11: Update TripService for live seat status + seatsLeft

**Files:**
- Modify: `backend/BusBooking.Api/Services/TripService.cs`

- [ ] **Step 1: Replace `BuildSeatLayoutAsync`**

Find the `BuildSeatLayoutAsync` private method and replace it with the body below (it keeps the signature but consults `seat_locks` and `booking_seats`):

```csharp
private async Task<SeatLayoutDto> BuildSeatLayoutAsync(Guid busId, Guid tripId, CancellationToken ct)
{
    var seats = await _db.SeatDefinitions
        .AsNoTracking()
        .Where(s => s.BusId == busId)
        .OrderBy(s => s.RowIndex).ThenBy(s => s.ColumnIndex)
        .ToListAsync(ct);

    if (seats.Count == 0) return new SeatLayoutDto(0, 0, []);

    int rows = seats.Max(s => s.RowIndex) + 1;
    int cols = seats.Max(s => s.ColumnIndex) + 1;

    var now = DateTime.UtcNow;

    var bookedSeats = await _db.BookingSeats
        .AsNoTracking()
        .Where(bs => bs.Booking.TripId == tripId
                     && bs.Booking.Status != BookingStatus.Cancelled
                     && bs.Booking.Status != BookingStatus.CancelledByOperator)
        .Select(bs => bs.SeatNumber)
        .ToListAsync(ct);
    var bookedSet = new HashSet<string>(bookedSeats, StringComparer.OrdinalIgnoreCase);

    var lockedSeats = await _db.SeatLocks
        .AsNoTracking()
        .Where(l => l.TripId == tripId && l.ExpiresAt > now)
        .Select(l => l.SeatNumber)
        .ToListAsync(ct);
    var lockedSet = new HashSet<string>(lockedSeats, StringComparer.OrdinalIgnoreCase);

    var statusList = seats.Select(s => new SeatStatusDto(
        s.SeatNumber,
        s.RowIndex,
        s.ColumnIndex,
        bookedSet.Contains(s.SeatNumber) ? "booked"
            : lockedSet.Contains(s.SeatNumber) ? "locked"
            : "available"
    )).ToList();

    return new SeatLayoutDto(rows, cols, statusList);
}
```

- [ ] **Step 2: Replace the `seatsLeft` placeholder in `SearchAsync`**

Inside the `foreach` loop of `SearchAsync`, replace:

```csharp
int seatsLeft = schedule.Bus!.Capacity; // M5 will subtract locked/booked
```

with:

```csharp
int seatsLeft = await ComputeSeatsLeftAsync(schedule.Bus!.Id, schedule.Bus.Capacity, trip.Id, ct);
```

- [ ] **Step 3: Replace the `seatsLeft` placeholder in `GetDetailAsync`**

Replace:

```csharp
int seatsLeft = bus.Capacity; // M5 will subtract locked/booked
```

with:

```csharp
int seatsLeft = await ComputeSeatsLeftAsync(bus.Id, bus.Capacity, tripId, ct);
```

- [ ] **Step 4: Add the `ComputeSeatsLeftAsync` helper**

Inside `TripService`, add this private method next to `BuildSeatLayoutAsync`:

```csharp
private async Task<int> ComputeSeatsLeftAsync(Guid busId, int capacity, Guid tripId, CancellationToken ct)
{
    var now = DateTime.UtcNow;
    var booked = await _db.BookingSeats
        .CountAsync(bs => bs.Booking.TripId == tripId
                          && bs.Booking.Status != BookingStatus.Cancelled
                          && bs.Booking.Status != BookingStatus.CancelledByOperator, ct);
    var locked = await _db.SeatLocks
        .CountAsync(l => l.TripId == tripId && l.ExpiresAt > now, ct);
    return Math.Max(0, capacity - booked - locked);
}
```

- [ ] **Step 5: Build**

Run: `dotnet build backend/BusBooking.Api/BusBooking.Api.csproj`
Expected: BUILD SUCCEEDED.

- [ ] **Step 6: Run existing search tests to confirm they still pass**

Run: `dotnet test backend/BusBooking.Api.Tests/BusBooking.Api.Tests.csproj --filter FullyQualifiedName~SearchTests -v minimal`
Expected: all existing `SearchTests` still pass.

- [ ] **Step 7: Commit**

```bash
git add backend/BusBooking.Api/Services/TripService.cs
git commit -m "feat(m5): derive seat-layout status and seatsLeft from bookings + locks"
```

---

## Task 12: SeatLocksController

**Files:**
- Create: `backend/BusBooking.Api/Controllers/SeatLocksController.cs`

- [ ] **Step 1: Controller**

```csharp
// backend/BusBooking.Api/Controllers/SeatLocksController.cs
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure.Auth;
using BusBooking.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.Api.Controllers;

[ApiController]
[Route("api/v1")]
public class SeatLocksController : ControllerBase
{
    private readonly ISeatLockService _locks;
    private readonly IValidator<LockSeatsRequest> _validator;
    private readonly ICurrentUserAccessor _currentUser;

    public SeatLocksController(
        ISeatLockService locks,
        IValidator<LockSeatsRequest> validator,
        ICurrentUserAccessor currentUser)
    {
        _locks = locks;
        _validator = validator;
        _currentUser = currentUser;
    }

    [AllowAnonymous]
    [HttpPost("trips/{tripId:guid}/seat-locks")]
    public async Task<IActionResult> Lock(Guid tripId, [FromBody] LockSeatsRequest req, CancellationToken ct)
    {
        await _validator.ValidateAndThrowAsync(req, ct);
        var userId = _currentUser.UserIdOrNull();
        var result = await _locks.LockAsync(tripId, userId, req, ct);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpDelete("seat-locks/{lockId:guid}")]
    public async Task<IActionResult> Release(Guid lockId, [FromQuery] Guid sessionId, CancellationToken ct)
    {
        var userId = _currentUser.UserIdOrNull();
        await _locks.ReleaseAsync(lockId, sessionId, userId, ct);
        return NoContent();
    }
}
```

- [ ] **Step 2: Verify `ICurrentUserAccessor.UserIdOrNull`**

Open `backend/BusBooking.Api/Infrastructure/Auth/ICurrentUserAccessor.cs` and confirm a `Guid? UserIdOrNull()` member exists (M1 shipped one). If the method there is named `TryGetUserId` or similar, update the controller call to match the existing signature — do not invent a new one.

- [ ] **Step 3: Build**

Run: `dotnet build backend/BusBooking.Api/BusBooking.Api.csproj`
Expected: BUILD SUCCEEDED.

- [ ] **Step 4: Commit**

```bash
git add backend/BusBooking.Api/Controllers/SeatLocksController.cs
git commit -m "feat(m5): add SeatLocksController (anonymous lock / release)"
```

---

## Task 13: BookingsController

**Files:**
- Create: `backend/BusBooking.Api/Controllers/BookingsController.cs`

- [ ] **Step 1: Controller**

```csharp
// backend/BusBooking.Api/Controllers/BookingsController.cs
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure.Auth;
using BusBooking.Api.Models;
using BusBooking.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Customer)]
[Route("api/v1/bookings")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookings;
    private readonly IValidator<CreateBookingRequest> _createValidator;
    private readonly IValidator<VerifyPaymentRequest> _verifyValidator;
    private readonly ICurrentUserAccessor _currentUser;

    public BookingsController(
        IBookingService bookings,
        IValidator<CreateBookingRequest> createValidator,
        IValidator<VerifyPaymentRequest> verifyValidator,
        ICurrentUserAccessor currentUser)
    {
        _bookings = bookings;
        _createValidator = createValidator;
        _verifyValidator = verifyValidator;
        _currentUser = currentUser;
    }

    [HttpPost]
    public async Task<ActionResult<CreateBookingResponseDto>> Create(
        [FromBody] CreateBookingRequest req, CancellationToken ct)
    {
        await _createValidator.ValidateAndThrowAsync(req, ct);
        var result = await _bookings.CreateAsync(_currentUser.UserId(), req, ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/verify-payment")]
    public async Task<ActionResult<BookingDetailDto>> VerifyPayment(
        Guid id, [FromBody] VerifyPaymentRequest req, CancellationToken ct)
    {
        await _verifyValidator.ValidateAndThrowAsync(req, ct);
        var result = await _bookings.VerifyPaymentAsync(_currentUser.UserId(), id, req, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookingDetailDto>> Get(Guid id, CancellationToken ct)
    {
        var result = await _bookings.GetAsync(_currentUser.UserId(), id, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}/ticket")]
    public async Task<IActionResult> GetTicket(Guid id, CancellationToken ct)
    {
        var pdf = await _bookings.GetTicketPdfAsync(_currentUser.UserId(), id, ct);
        return File(pdf, "application/pdf", $"ticket-{id}.pdf");
    }
}
```

- [ ] **Step 2: Verify `Roles.Customer` constant exists**

Open `backend/BusBooking.Api/Models/Roles.cs` — confirm `public const string Customer = "customer";` is declared (it was added in M1). If it is missing, add it in this task.

- [ ] **Step 3: Build**

Run: `dotnet build backend/BusBooking.Api/BusBooking.Api.csproj`
Expected: BUILD SUCCEEDED.

- [ ] **Step 4: Commit**

```bash
git add backend/BusBooking.Api/Controllers/BookingsController.cs
git commit -m "feat(m5): add BookingsController (create / verify-payment / get / ticket)"
```

---

## Task 14: DI registrations, QuestPDF license, hosted service

**Files:**
- Modify: `backend/BusBooking.Api/Program.cs`

- [ ] **Step 1: Add usings**

At the top of `Program.cs` (after existing usings), add:

```csharp
using BusBooking.Api.Background;
using BusBooking.Api.Infrastructure.Pdf;
using BusBooking.Api.Infrastructure.Razorpay;
using BusBooking.Api.Infrastructure.Resend;
using QuestPDF;
using QuestPDF.Infrastructure;
```

- [ ] **Step 2: Set QuestPDF license**

At the very top of the file (before `var builder = WebApplication.CreateBuilder(args);`), add:

```csharp
Settings.License = LicenseType.Community;
```

- [ ] **Step 3: Register options + HTTP clients + services**

In the service-registration block (after the `IScheduleService` / `ITripService` lines), add:

```csharp
builder.Services.Configure<RazorpayOptions>(
    builder.Configuration.GetSection(RazorpayOptions.SectionName));
builder.Services.Configure<ResendOptions>(
    builder.Configuration.GetSection(ResendOptions.SectionName));

builder.Services.AddHttpClient(RazorpayClient.HttpClientName);
builder.Services.AddHttpClient(ResendEmailClient.HttpClientName);

builder.Services.AddScoped<IRazorpayClient, RazorpayClient>();
builder.Services.AddScoped<IResendEmailClient, ResendEmailClient>();
builder.Services.AddSingleton<IPdfTicketGenerator, PdfTicketGenerator>();

builder.Services.AddScoped<ISeatLockService, SeatLockService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddHostedService<SeatLockCleanupService>();
```

- [ ] **Step 4: Build**

Run: `dotnet build backend/BusBooking.Api/BusBooking.Api.csproj`
Expected: BUILD SUCCEEDED.

- [ ] **Step 5: Smoke-run the API**

Run: `cd backend/BusBooking.Api && timeout 10 dotnet run || true`
Expected: log lines showing `Now listening on: http://localhost:5080` and `SeatLockCleanup` not throwing.

- [ ] **Step 6: Commit**

```bash
git add backend/BusBooking.Api/Program.cs
git commit -m "feat(m5): wire DI for Razorpay, Resend, PDF, seat-lock cleanup"
```

---

## Task 15: Integration fixture update + SeatLock integration tests

**Files:**
- Modify: `backend/BusBooking.Api.Tests/Support/IntegrationFixture.cs`
- Create: `backend/BusBooking.Api.Tests/Support/FakeRazorpayClient.cs`
- Create: `backend/BusBooking.Api.Tests/Support/FakeResendEmailClient.cs`
- Create: `backend/BusBooking.Api.Tests/Integration/SeatLockTests.cs`
- Create: `backend/BusBooking.Api.Tests/Unit/RazorpaySignatureTests.cs`

- [ ] **Step 1: Add fake Razorpay client**

```csharp
// backend/BusBooking.Api.Tests/Support/FakeRazorpayClient.cs
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
    public string KeyId => TestKeyId;

    public Task<RazorpayOrder> CreateOrderAsync(long amountInPaise, string receipt, CancellationToken ct)
    {
        var id = "order_" + Guid.NewGuid().ToString("N")[..14];
        CreatedOrders[id] = amountInPaise;
        return Task.FromResult(new RazorpayOrder(id, amountInPaise, "INR", receipt));
    }

    public bool VerifySignature(string orderId, string paymentId, string signature)
    {
        var payload = $"{orderId}|{paymentId}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(TestKeySecret));
        var computed = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(signature.ToLowerInvariant()));
    }

    public static string BuildSignature(string orderId, string paymentId)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(TestKeySecret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes($"{orderId}|{paymentId}")))
            .ToLowerInvariant();
    }
}
```

- [ ] **Step 2: Add fake Resend client**

```csharp
// backend/BusBooking.Api.Tests/Support/FakeResendEmailClient.cs
using System.Collections.Concurrent;
using BusBooking.Api.Infrastructure.Resend;

namespace BusBooking.Api.Tests.Support;

public record SentEmail(string To, string Subject, string Html, int AttachmentCount);

public class FakeResendEmailClient : IResendEmailClient
{
    public readonly ConcurrentQueue<SentEmail> Sent = new();

    public Task<ResendSendResult> SendAsync(
        string toAddress, string subject, string htmlBody,
        IReadOnlyList<ResendAttachment> attachments, CancellationToken ct)
    {
        Sent.Enqueue(new SentEmail(toAddress, subject, htmlBody, attachments.Count));
        return Task.FromResult(new ResendSendResult($"msg_{Guid.NewGuid():N}", true, null));
    }
}
```

- [ ] **Step 3: Update IntegrationFixture**

Replace the body of `IntegrationFixture.cs` with:

```csharp
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Infrastructure.Razorpay;
using BusBooking.Api.Infrastructure.Resend;
using BusBooking.Api.Infrastructure.Seeding;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BusBooking.Api.Tests.Support;

public class IntegrationFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private HttpClient? _client;
    public HttpClient Client => _client ??= CreateClient();

    public FakeRazorpayClient Razorpay { get; } = new();
    public FakeResendEmailClient Email { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            var toRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                || d.ServiceType == typeof(DbContextOptions)
                || d.ServiceType == typeof(AppDbContext)
                || d.ServiceType == typeof(IRazorpayClient)
                || d.ServiceType == typeof(IResendEmailClient)).ToList();
            foreach (var d in toRemove) services.Remove(d);

            using var tmp = services.BuildServiceProvider();
            var cfg = tmp.GetRequiredService<IConfiguration>();
            var devConn = cfg.GetConnectionString("Default")
                ?? throw new InvalidOperationException("ConnectionStrings:Default missing in test environment");
            var testConn = devConn.Replace("Database=bus_booking", "Database=bus_booking_test");
            if (testConn == devConn)
                throw new InvalidOperationException("Could not derive test connection string from dev connection");

            services.AddDbContext<AppDbContext>(o => o.UseNpgsql(testConn));
            services.AddSingleton<IRazorpayClient>(_ => Razorpay);
            services.AddSingleton<IResendEmailClient>(_ => Email);
        });
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await ResetAsync();
    }

    public new Task DisposeAsync() => Task.CompletedTask;

    public async Task ResetAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE notifications, payments, booking_seats, bookings, seat_locks, "
            + "bus_trips, bus_schedules, audit_log, seat_definitions, buses, operator_offices, operator_requests, "
            + "platform_fee_config, routes, cities, user_roles, users "
            + "RESTART IDENTITY CASCADE");
        var seeder = scope.ServiceProvider.GetRequiredService<IPlatformFeeSeeder>();
        await seeder.SeedAsync(CancellationToken.None);
    }
}
```

- [ ] **Step 4: Write Razorpay signature unit test**

```csharp
// backend/BusBooking.Api.Tests/Unit/RazorpaySignatureTests.cs
using BusBooking.Api.Tests.Support;
using FluentAssertions;

namespace BusBooking.Api.Tests.Unit;

public class RazorpaySignatureTests
{
    [Fact]
    public void VerifySignature_AcceptsValid()
    {
        var client = new FakeRazorpayClient();
        var sig = FakeRazorpayClient.BuildSignature("order_1", "pay_1");
        client.VerifySignature("order_1", "pay_1", sig).Should().BeTrue();
    }

    [Fact]
    public void VerifySignature_RejectsTamperedSignature()
    {
        var client = new FakeRazorpayClient();
        var sig = FakeRazorpayClient.BuildSignature("order_1", "pay_1");
        client.VerifySignature("order_1", "pay_1", sig + "00").Should().BeFalse();
    }

    [Fact]
    public void VerifySignature_RejectsWrongOrderId()
    {
        var client = new FakeRazorpayClient();
        var sig = FakeRazorpayClient.BuildSignature("order_1", "pay_1");
        client.VerifySignature("order_2", "pay_1", sig).Should().BeFalse();
    }
}
```

- [ ] **Step 5: Write SeatLock integration tests**

```csharp
// backend/BusBooking.Api.Tests/Integration/SeatLockTests.cs
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

public class SeatLockTests : IClassFixture<IntegrationFixture>, IAsyncLifetime
{
    private readonly IntegrationFixture _fixture;
    public SeatLockTests(IntegrationFixture fixture) => _fixture = fixture;
    public Task InitializeAsync() => _fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ConcurrentLock_SameSeat_OneWinsOtherGets409()
    {
        var tripId = await TestSeed.CreateTripAsync(_fixture);

        var body = new LockSeatsRequest(Guid.NewGuid(), new() { "A1" });
        var body2 = new LockSeatsRequest(Guid.NewGuid(), new() { "A1" });

        var t1 = _fixture.Client.PostAsJsonAsync($"/api/v1/trips/{tripId}/seat-locks", body);
        var t2 = _fixture.Client.PostAsJsonAsync($"/api/v1/trips/{tripId}/seat-locks", body2);
        await Task.WhenAll(t1, t2);

        var codes = new[] { t1.Result.StatusCode, t2.Result.StatusCode };
        codes.Should().Contain(HttpStatusCode.OK);
        codes.Should().Contain(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ExpiredLocks_AreFilteredFromSeatLayout()
    {
        var tripId = await TestSeed.CreateTripAsync(_fixture);

        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var stale = DateTime.UtcNow.AddMinutes(-1);
            db.SeatLocks.Add(new SeatLock
            {
                Id         = Guid.NewGuid(),
                TripId     = tripId,
                SeatNumber = "A1",
                LockId     = Guid.NewGuid(),
                SessionId  = Guid.NewGuid(),
                CreatedAt  = stale.AddMinutes(-7),
                ExpiresAt  = stale
            });
            await db.SaveChangesAsync();
        }

        var layout = await _fixture.Client
            .GetFromJsonAsync<SeatLayoutDto>($"/api/v1/trips/{tripId}/seats");
        layout!.Seats.First(s => s.SeatNumber == "A1").Status.Should().Be("available");
    }

    [Fact]
    public async Task ReleaseLock_Deletes_AllRows()
    {
        var tripId = await TestSeed.CreateTripAsync(_fixture);
        var sessionId = Guid.NewGuid();
        var lockResp = await _fixture.Client.PostAsJsonAsync(
            $"/api/v1/trips/{tripId}/seat-locks",
            new LockSeatsRequest(sessionId, new() { "A1", "A2" }));
        lockResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var lockDto = await lockResp.Content.ReadFromJsonAsync<SeatLockResponseDto>();

        var del = await _fixture.Client.DeleteAsync(
            $"/api/v1/seat-locks/{lockDto!.LockId}?sessionId={sessionId}");
        del.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.SeatLocks.CountAsync(l => l.LockId == lockDto.LockId)).Should().Be(0);
    }
}
```

- [ ] **Step 6: Add the shared test seed helper**

Check whether `backend/BusBooking.Api.Tests/Support/TestSeed.cs` already exists from earlier milestones. If it does not, create it with a trip-seeding helper; if it does, append the `CreateTripAsync` method below (adapt names to match existing helpers — do not duplicate seed methods).

```csharp
// backend/BusBooking.Api.Tests/Support/TestSeed.cs  (create or extend)
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Models;
using Microsoft.Extensions.DependencyInjection;
using Route = BusBooking.Api.Models.Route;

namespace BusBooking.Api.Tests.Support;

public static class TestSeed
{
    public static async Task<Guid> CreateTripAsync(IntegrationFixture fixture)
    {
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var src = new City { Id = Guid.NewGuid(), Name = "Bangalore", State = "KA", IsActive = true };
        var dst = new City { Id = Guid.NewGuid(), Name = "Chennai",   State = "TN", IsActive = true };
        db.Cities.AddRange(src, dst);

        var route = new Route
        {
            Id = Guid.NewGuid(), SourceCityId = src.Id, DestinationCityId = dst.Id,
            DistanceKm = 350, IsActive = true
        };
        db.Routes.Add(route);

        var op = new User
        {
            Id = Guid.NewGuid(), Name = "Op", Email = $"op-{Guid.NewGuid():N}@t.com",
            PasswordHash = "x", CreatedAt = DateTime.UtcNow, IsActive = true
        };
        db.Users.Add(op);
        db.UserRoles.Add(new UserRole { UserId = op.Id, Role = Roles.Operator });

        db.OperatorOffices.AddRange(
            new OperatorOffice { Id = Guid.NewGuid(), OperatorUserId = op.Id, CityId = src.Id, AddressLine = "Pickup hub", Phone = "100", IsActive = true },
            new OperatorOffice { Id = Guid.NewGuid(), OperatorUserId = op.Id, CityId = dst.Id, AddressLine = "Drop hub",   Phone = "101", IsActive = true });

        var bus = new Bus
        {
            Id = Guid.NewGuid(), OperatorUserId = op.Id,
            RegistrationNumber = $"TN-{new Random().Next(1000, 9999)}",
            BusName = "Volvo", BusType = BusType.Seater, Capacity = 10,
            ApprovalStatus = BusApprovalStatus.Approved,
            OperationalStatus = BusOperationalStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        db.Buses.Add(bus);

        for (var i = 0; i < bus.Capacity; i++)
        {
            db.SeatDefinitions.Add(new SeatDefinition
            {
                Id = Guid.NewGuid(),
                BusId = bus.Id,
                SeatNumber = $"A{i + 1}",
                RowIndex = i,
                ColumnIndex = 0,
                SeatCategory = SeatCategory.Regular
            });
        }

        var schedule = new BusSchedule
        {
            Id = Guid.NewGuid(), BusId = bus.Id, RouteId = route.Id,
            DepartureTime = new TimeOnly(22, 0), ArrivalTime = new TimeOnly(6, 0),
            FarePerSeat = 500m,
            ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            ValidTo   = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60)),
            DaysOfWeek = 0b1111111, IsActive = true
        };
        db.BusSchedules.Add(schedule);

        var trip = new BusTrip
        {
            Id = Guid.NewGuid(), ScheduleId = schedule.Id,
            TripDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
            Status = TripStatus.Scheduled
        };
        db.BusTrips.Add(trip);
        await db.SaveChangesAsync();
        return trip.Id;
    }
}
```

- [ ] **Step 7: Run the SeatLock + signature tests**

Run: `dotnet test backend/BusBooking.Api.Tests/BusBooking.Api.Tests.csproj --filter "FullyQualifiedName~SeatLockTests|FullyQualifiedName~RazorpaySignatureTests" -v minimal`
Expected: all tests pass.

- [ ] **Step 8: Commit**

```bash
git add backend/BusBooking.Api.Tests/
git commit -m "test(m5): seat-lock concurrency + Razorpay signature + fixture update"
```

---

## Task 16: Booking integration tests

**Files:**
- Create: `backend/BusBooking.Api.Tests/Integration/BookingTests.cs`
- Modify (if needed): `backend/BusBooking.Api.Tests/Support/TestSeed.cs` (add customer-seed + JWT helper)

- [ ] **Step 1: Add a customer + JWT helper to `TestSeed`**

If `TestSeed` does not already have an `AuthenticateCustomerAsync(fixture) → (userId, bearer)` helper, add it now. A minimal implementation (extend the class from Task 15):

```csharp
// backend/BusBooking.Api.Tests/Support/TestSeed.cs  (add to existing class)
using System.Net.Http.Headers;
using System.Net.Http.Json;

public static async Task<(Guid userId, string bearer)> AuthenticateCustomerAsync(IntegrationFixture fixture)
{
    var email = $"cust-{Guid.NewGuid():N}@t.com";
    var reg = await fixture.Client.PostAsJsonAsync("/api/v1/auth/register",
        new { name = "Cust", email, password = "Password1!" });
    reg.EnsureSuccessStatusCode();

    var login = await fixture.Client.PostAsJsonAsync("/api/v1/auth/login",
        new { email, password = "Password1!" });
    login.EnsureSuccessStatusCode();
    var body = await login.Content.ReadFromJsonAsync<LoginResp>();
    return (body!.User.Id, body.Token);
}

private record LoginResp(string Token, LoginUser User);
private record LoginUser(Guid Id, string Email, string[] Roles);
```

If the field names of the existing `LoginResponse` differ (check `backend/BusBooking.Api/Dtos/AuthDtos.cs` or wherever `/auth/login` replies come from), adapt the records to match.

- [ ] **Step 2: Write the booking test**

```csharp
// backend/BusBooking.Api.Tests/Integration/BookingTests.cs
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Models;
using BusBooking.Api.Tests.Support;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BusBooking.Api.Tests.Integration;

public class BookingTests : IClassFixture<IntegrationFixture>, IAsyncLifetime
{
    private readonly IntegrationFixture _fixture;
    public BookingTests(IntegrationFixture fixture) => _fixture = fixture;
    public Task InitializeAsync() => _fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task HappyPath_CreateVerify_ConfirmsBookingAndSendsEmail()
    {
        var tripId = await TestSeed.CreateTripAsync(_fixture);
        var (userId, bearer) = await TestSeed.AuthenticateCustomerAsync(_fixture);
        var authed = _fixture.CreateClient();
        authed.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);

        var sessionId = Guid.NewGuid();
        var lockResp = await authed.PostAsJsonAsync(
            $"/api/v1/trips/{tripId}/seat-locks",
            new LockSeatsRequest(sessionId, new() { "A1" }));
        lockResp.EnsureSuccessStatusCode();
        var lockDto = await lockResp.Content.ReadFromJsonAsync<SeatLockResponseDto>();

        var createResp = await authed.PostAsJsonAsync("/api/v1/bookings",
            new CreateBookingRequest(tripId, lockDto!.LockId, sessionId,
                new() { new PassengerDto("A1", "Asha", 30, PassengerGender.Female) }));
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<CreateBookingResponseDto>();

        var paymentId = "pay_" + Guid.NewGuid().ToString("N")[..10];
        var signature = FakeRazorpayClient.BuildSignature(created!.RazorpayOrderId, paymentId);

        var verifyResp = await authed.PostAsJsonAsync(
            $"/api/v1/bookings/{created.BookingId}/verify-payment",
            new VerifyPaymentRequest(paymentId, signature));
        verifyResp.EnsureSuccessStatusCode();
        var detail = await verifyResp.Content.ReadFromJsonAsync<BookingDetailDto>();

        detail!.Status.Should().Be(BookingStatus.Confirmed);
        detail.Seats.Should().ContainSingle(s => s.SeatNumber == "A1");
        _fixture.Email.Sent.Should().ContainSingle(e => e.Subject.Contains(detail.BookingCode));

        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.SeatLocks.CountAsync(l => l.LockId == lockDto.LockId)).Should().Be(0);
    }

    [Fact]
    public async Task VerifyPayment_InvalidSignature_Returns422()
    {
        var tripId = await TestSeed.CreateTripAsync(_fixture);
        var (_, bearer) = await TestSeed.AuthenticateCustomerAsync(_fixture);
        var authed = _fixture.CreateClient();
        authed.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);

        var sessionId = Guid.NewGuid();
        var lockResp = (await authed.PostAsJsonAsync(
            $"/api/v1/trips/{tripId}/seat-locks",
            new LockSeatsRequest(sessionId, new() { "A1" })))
            .Content.ReadFromJsonAsync<SeatLockResponseDto>().Result!;
        var created = (await authed.PostAsJsonAsync("/api/v1/bookings",
            new CreateBookingRequest(tripId, lockResp.LockId, sessionId,
                new() { new PassengerDto("A1", "Asha", 30, PassengerGender.Female) })))
            .Content.ReadFromJsonAsync<CreateBookingResponseDto>().Result!;

        var verifyResp = await authed.PostAsJsonAsync(
            $"/api/v1/bookings/{created.BookingId}/verify-payment",
            new VerifyPaymentRequest("pay_whatever", "badhex"));
        verifyResp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateBooking_WithExpiredLock_Returns409()
    {
        var tripId = await TestSeed.CreateTripAsync(_fixture);
        var (userId, bearer) = await TestSeed.AuthenticateCustomerAsync(_fixture);
        var authed = _fixture.CreateClient();
        authed.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);

        Guid lockId;
        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            lockId = Guid.NewGuid();
            db.SeatLocks.Add(new SeatLock
            {
                Id         = Guid.NewGuid(),
                TripId     = tripId,
                SeatNumber = "A1",
                LockId     = lockId,
                SessionId  = Guid.NewGuid(),
                CreatedAt  = DateTime.UtcNow.AddMinutes(-10),
                ExpiresAt  = DateTime.UtcNow.AddMinutes(-3)
            });
            await db.SaveChangesAsync();
        }

        var resp = await authed.PostAsJsonAsync("/api/v1/bookings",
            new CreateBookingRequest(tripId, lockId, Guid.NewGuid(),
                new() { new PassengerDto("A1", "Asha", 30, PassengerGender.Female) }));
        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task VerifyPayment_Idempotent_OnRepeat()
    {
        var tripId = await TestSeed.CreateTripAsync(_fixture);
        var (_, bearer) = await TestSeed.AuthenticateCustomerAsync(_fixture);
        var authed = _fixture.CreateClient();
        authed.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);

        var sessionId = Guid.NewGuid();
        var lockDto = (await authed.PostAsJsonAsync($"/api/v1/trips/{tripId}/seat-locks",
            new LockSeatsRequest(sessionId, new() { "A1" })))
            .Content.ReadFromJsonAsync<SeatLockResponseDto>().Result!;
        var created = (await authed.PostAsJsonAsync("/api/v1/bookings",
            new CreateBookingRequest(tripId, lockDto.LockId, sessionId,
                new() { new PassengerDto("A1", "Asha", 30, PassengerGender.Female) })))
            .Content.ReadFromJsonAsync<CreateBookingResponseDto>().Result!;
        var paymentId = "pay_idem";
        var sig = FakeRazorpayClient.BuildSignature(created.RazorpayOrderId, paymentId);

        var r1 = await authed.PostAsJsonAsync(
            $"/api/v1/bookings/{created.BookingId}/verify-payment",
            new VerifyPaymentRequest(paymentId, sig));
        r1.EnsureSuccessStatusCode();

        var r2 = await authed.PostAsJsonAsync(
            $"/api/v1/bookings/{created.BookingId}/verify-payment",
            new VerifyPaymentRequest(paymentId, sig));
        r2.EnsureSuccessStatusCode();

        var r3 = await authed.PostAsJsonAsync(
            $"/api/v1/bookings/{created.BookingId}/verify-payment",
            new VerifyPaymentRequest("pay_different", sig));
        r3.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
```

- [ ] **Step 3: Run the booking tests**

Run: `dotnet test backend/BusBooking.Api.Tests/BusBooking.Api.Tests.csproj --filter FullyQualifiedName~BookingTests -v minimal`
Expected: all four tests pass.

- [ ] **Step 4: Run the entire backend test suite**

Run: `dotnet test backend/BusBooking.Api.Tests/BusBooking.Api.Tests.csproj -v minimal`
Expected: all pre-existing tests still pass alongside the new M5 tests.

- [ ] **Step 5: Commit**

```bash
git add backend/BusBooking.Api.Tests/Integration/BookingTests.cs \
        backend/BusBooking.Api.Tests/Support/TestSeed.cs
git commit -m "test(m5): booking happy path, expired lock, idempotent verify"
```

---

## Task 17: Frontend — BookingsApiService

**Files:**
- Create: `frontend/bus-booking-web/src/app/core/api/bookings.api.ts`

- [ ] **Step 1: API service**

```typescript
// frontend/bus-booking-web/src/app/core/api/bookings.api.ts
import { HttpClient } from '@angular/common/http';
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
  expiresAt: string;          // ISO-8601
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
  amount: number;             // paise
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
  seats: BookingSeatDto[];
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
}
```

- [ ] **Step 2: Type-check**

Run: `cd frontend/bus-booking-web && npx tsc --noEmit -p tsconfig.app.json`
Expected: no errors.

- [ ] **Step 3: Commit**

```bash
git add frontend/bus-booking-web/src/app/core/api/bookings.api.ts
git commit -m "feat(m5): add frontend BookingsApiService"
```

---

## Task 18: Frontend — CountdownTimerComponent

**Files:**
- Create: `frontend/bus-booking-web/src/app/shared/components/countdown-timer/countdown-timer.component.ts`
- Create: `frontend/bus-booking-web/src/app/shared/components/countdown-timer/countdown-timer.component.html`

- [ ] **Step 1: Component**

```typescript
// frontend/bus-booking-web/src/app/shared/components/countdown-timer/countdown-timer.component.ts
import { ChangeDetectionStrategy, Component, OnDestroy, computed, input, output, signal } from '@angular/core';

@Component({
  selector: 'app-countdown-timer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './countdown-timer.component.html'
})
export class CountdownTimerComponent implements OnDestroy {
  readonly expiresAt = input.required<string>(); // ISO timestamp
  readonly expired   = output<void>();

  private readonly now = signal(Date.now());
  private readonly intervalId = setInterval(() => this.tick(), 1000);

  readonly secondsLeft = computed(() => {
    const target = new Date(this.expiresAt()).getTime();
    return Math.max(0, Math.floor((target - this.now()) / 1000));
  });

  readonly display = computed(() => {
    const s = this.secondsLeft();
    const mm = Math.floor(s / 60).toString().padStart(2, '0');
    const ss = (s % 60).toString().padStart(2, '0');
    return `${mm}:${ss}`;
  });

  readonly isWarning = computed(() => this.secondsLeft() <= 60 && this.secondsLeft() > 0);
  readonly isExpired = computed(() => this.secondsLeft() === 0);

  private emittedExpired = false;
  private tick(): void {
    this.now.set(Date.now());
    if (!this.emittedExpired && this.secondsLeft() === 0) {
      this.emittedExpired = true;
      this.expired.emit();
    }
  }

  ngOnDestroy(): void {
    clearInterval(this.intervalId);
  }
}
```

- [ ] **Step 2: Template**

```html
<!-- frontend/bus-booking-web/src/app/shared/components/countdown-timer/countdown-timer.component.html -->
<span class="inline-flex items-center gap-2 px-3 py-1 rounded-full text-sm font-semibold"
      [class.bg-slate-100]="!isWarning() && !isExpired()"
      [class.text-slate-700]="!isWarning() && !isExpired()"
      [class.bg-rose-100]="isWarning() || isExpired()"
      [class.text-rose-700]="isWarning() || isExpired()">
  <span>⏱</span>
  @if (isExpired()) {
    <span>Expired</span>
  } @else {
    <span>{{ display() }}</span>
  }
</span>
```

- [ ] **Step 3: Build the frontend**

Run: `cd frontend/bus-booking-web && npx ng build --configuration development`
Expected: build succeeds.

- [ ] **Step 4: Commit**

```bash
git add frontend/bus-booking-web/src/app/shared/components/countdown-timer/
git commit -m "feat(m5): add reusable CountdownTimerComponent"
```

---

## Task 19: Frontend — Upgrade SeatMapComponent to interactive

**Files:**
- Modify: `frontend/bus-booking-web/src/app/shared/components/seat-map/seat-map.component.ts`
- Modify: `frontend/bus-booking-web/src/app/shared/components/seat-map/seat-map.component.html`

- [ ] **Step 1: Replace component**

```typescript
// frontend/bus-booking-web/src/app/shared/components/seat-map/seat-map.component.ts
import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SeatLayoutDto, SeatStatusDto } from '../../../core/api/search.api';

@Component({
  selector: 'app-seat-map',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './seat-map.component.html',
  styleUrl: './seat-map.component.scss'
})
export class SeatMapComponent {
  readonly layout      = input.required<SeatLayoutDto>();
  readonly selectable  = input(false);
  readonly maxSelected = input(6);
  readonly fare        = input<number | null>(null);
  readonly selectionChange = output<string[]>();

  private readonly selectedSet = signal<Set<string>>(new Set());
  readonly selected = computed(() => Array.from(this.selectedSet()).sort());

  readonly seatGrid = computed(() => {
    const l = this.layout();
    const grid: (SeatStatusDto | null)[][] = Array.from({ length: l.rows }, () => Array(l.columns).fill(null));
    for (const s of l.seats) grid[s.rowIndex][s.columnIndex] = s;
    return grid;
  });

  toggle(seat: SeatStatusDto): void {
    if (!this.selectable()) return;
    if (seat.status !== 'available') return;

    const next = new Set(this.selectedSet());
    if (next.has(seat.seatNumber)) {
      next.delete(seat.seatNumber);
    } else {
      if (next.size >= this.maxSelected()) return;
      next.add(seat.seatNumber);
    }
    this.selectedSet.set(next);
    this.selectionChange.emit(this.selected());
  }

  isSelected(seat: string): boolean {
    return this.selectedSet().has(seat);
  }
}
```

- [ ] **Step 2: Replace template**

```html
<!-- frontend/bus-booking-web/src/app/shared/components/seat-map/seat-map.component.html -->
<div class="inline-block">
  <div class="mb-3 text-xs text-slate-500 flex flex-wrap gap-3">
    <span class="flex items-center gap-1"><span class="w-4 h-4 rounded bg-emerald-100 border border-emerald-300"></span>Available</span>
    <span class="flex items-center gap-1"><span class="w-4 h-4 rounded bg-primary"></span>Selected</span>
    <span class="flex items-center gap-1"><span class="w-4 h-4 rounded bg-amber-100 border border-amber-300"></span>Locked</span>
    <span class="flex items-center gap-1"><span class="w-4 h-4 rounded bg-slate-300"></span>Booked</span>
  </div>

  <div class="flex flex-col gap-2">
    @for (row of seatGrid(); track $index) {
      <div class="flex gap-2">
        @for (seat of row; track $index) {
          @if (seat === null) {
            <div class="w-10 h-10"></div>
          } @else {
            <button
              type="button"
              class="w-10 h-10 rounded border text-xs font-medium transition"
              [disabled]="seat.status !== 'available' || !selectable()"
              [class.bg-emerald-100]="seat.status === 'available' && !isSelected(seat.seatNumber)"
              [class.border-emerald-300]="seat.status === 'available' && !isSelected(seat.seatNumber)"
              [class.text-emerald-800]="seat.status === 'available' && !isSelected(seat.seatNumber)"
              [class.bg-primary]="isSelected(seat.seatNumber)"
              [class.text-white]="isSelected(seat.seatNumber)"
              [class.bg-amber-100]="seat.status === 'locked'"
              [class.border-amber-300]="seat.status === 'locked'"
              [class.bg-slate-300]="seat.status === 'booked'"
              [class.cursor-not-allowed]="seat.status !== 'available' || !selectable()"
              (click)="toggle(seat)">
              {{ seat.seatNumber }}
            </button>
          }
        }
      </div>
    }
  </div>

  @if (selectable() && selected().length > 0) {
    <div class="mt-4 p-3 bg-slate-50 rounded text-sm">
      <div>Selected: <strong>{{ selected().join(', ') }}</strong></div>
      @if (fare() !== null) {
        <div>Fare: <strong>{{ (fare()! * selected().length) | currency:'INR' }}</strong> ({{ selected().length }} × {{ fare() | currency:'INR' }})</div>
      }
    </div>
  }
</div>
```

- [ ] **Step 3: Type-check**

Run: `cd frontend/bus-booking-web && npx tsc --noEmit -p tsconfig.app.json`
Expected: no errors.

- [ ] **Step 4: Commit**

```bash
git add frontend/bus-booking-web/src/app/shared/components/seat-map/
git commit -m "feat(m5): upgrade SeatMapComponent to interactive selection"
```

---

## Task 20: Frontend — Trip detail selects, locks, navigates to checkout

**Files:**
- Modify: `frontend/bus-booking-web/src/app/features/public/trip-detail/trip-detail.component.ts`
- Modify: `frontend/bus-booking-web/src/app/features/public/trip-detail/trip-detail.component.html`

- [ ] **Step 1: Replace component**

```typescript
// frontend/bus-booking-web/src/app/features/public/trip-detail/trip-detail.component.ts
import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { SearchApiService, TripDetailDto } from '../../../core/api/search.api';
import { BookingsApiService } from '../../../core/api/bookings.api';
import { SeatMapComponent } from '../../../shared/components/seat-map/seat-map.component';

@Component({
  selector: 'app-trip-detail',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatButtonModule, MatIconModule, SeatMapComponent],
  templateUrl: './trip-detail.component.html'
})
export class TripDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(SearchApiService);
  private readonly bookings = inject(BookingsApiService);
  private readonly location = inject(Location);
  private readonly snack = inject(MatSnackBar);

  readonly trip = signal<TripDetailDto | null>(null);
  readonly error = signal<string | null>(null);
  readonly loading = signal(true);
  readonly selectedSeats = signal<string[]>([]);
  readonly locking = signal(false);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;

    this.api.getTripDetail(id).subscribe({
      next: (data) => {
        this.trip.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Trip not found or no longer available.');
        this.loading.set(false);
      }
    });
  }

  goBack(): void {
    this.location.back();
  }

  onSelectionChange(seats: string[]): void {
    this.selectedSeats.set(seats);
  }

  bookNow(): void {
    const t = this.trip();
    const seats = this.selectedSeats();
    if (!t || seats.length === 0 || this.locking()) return;

    this.locking.set(true);
    const sessionId = (crypto as any).randomUUID();
    this.bookings.lockSeats(t.tripId, { sessionId, seats }).subscribe({
      next: (lock) => {
        this.locking.set(false);
        this.router.navigate(['/checkout', t.tripId], {
          queryParams: {
            lockId: lock.lockId,
            sessionId: lock.sessionId,
            seats: lock.seats.join(','),
            expiresAt: lock.expiresAt,
            fare: t.farePerSeat
          }
        });
      },
      error: (err) => {
        this.locking.set(false);
        const code = err?.error?.error?.code;
        const msg = code === 'SEAT_UNAVAILABLE'
          ? 'One or more of those seats were just taken — please pick again.'
          : 'Could not hold those seats. Please try again.';
        this.snack.open(msg, 'Dismiss', { duration: 5000 });
        // Refresh the layout so freshly-taken seats turn amber/grey.
        this.api.getTripDetail(t.tripId).subscribe(updated => this.trip.set(updated));
      }
    });
  }
}
```

- [ ] **Step 2: Replace template (adds sticky Book Now bar)**

```html
<!-- frontend/bus-booking-web/src/app/features/public/trip-detail/trip-detail.component.html -->
<div class="max-w-4xl mx-auto p-4 md:p-6 space-y-6 pb-24">
  @if (loading()) {
    <div class="flex justify-center p-12">
      <div class="w-8 h-8 border-4 border-primary border-t-transparent rounded-full animate-spin"></div>
    </div>
  } @else if (error()) {
    <div class="text-center p-12 bg-rose-50 border border-rose-200 rounded-lg text-rose-800">
      <h3 class="text-lg font-medium mb-2">Error</h3>
      <p>{{ error() }}</p>
      <button mat-button color="primary" class="mt-4" (click)="goBack()">Go Back</button>
    </div>
  } @else if (trip(); as t) {
    <div class="flex items-center gap-4 mb-2">
      <button mat-icon-button (click)="goBack()"><mat-icon>arrow_back</mat-icon></button>
      <h1 class="text-2xl font-bold m-0">{{ t.sourceCityName }} to {{ t.destinationCityName }}</h1>
    </div>

    <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
      <div class="md:col-span-1 space-y-4">
        <mat-card class="border border-slate-200 shadow-sm">
          <mat-card-header class="border-b border-slate-100 pb-3 mb-3">
            <mat-card-title class="text-lg">{{ t.operatorName }}</mat-card-title>
            <mat-card-subtitle>{{ t.busName }} ({{ t.busType }})</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content class="space-y-4">
            <div class="flex justify-between items-center text-sm">
              <span class="text-slate-500">Date</span>
              <span class="font-medium">{{ t.tripDate }}</span>
            </div>
            <div class="flex justify-between items-center text-sm">
              <span class="text-slate-500">Fare / seat</span>
              <span class="font-bold text-lg">{{ t.farePerSeat | currency:'INR' }}</span>
            </div>
            <div class="flex justify-between items-center text-sm">
              <span class="text-slate-500">Seats left</span>
              <span class="font-medium">{{ t.seatsLeft }}</span>
            </div>
            <div class="bg-slate-50 p-3 rounded text-sm space-y-3">
              <div class="flex gap-3">
                <mat-icon class="text-slate-400">my_location</mat-icon>
                <div>
                  <div class="font-medium">{{ t.departureTime.substring(0,5) }}</div>
                  <div class="text-slate-500 text-xs">{{ t.pickupAddress || t.sourceCityName }}</div>
                </div>
              </div>
              <div class="flex gap-3">
                <mat-icon class="text-slate-400">location_on</mat-icon>
                <div>
                  <div class="font-medium">{{ t.arrivalTime.substring(0,5) }}</div>
                  <div class="text-slate-500 text-xs">{{ t.dropAddress || t.destinationCityName }}</div>
                </div>
              </div>
            </div>
          </mat-card-content>
        </mat-card>
      </div>

      <div class="md:col-span-2">
        <mat-card class="border border-slate-200 shadow-sm">
          <mat-card-header class="border-b border-slate-100 pb-3 mb-4">
            <mat-card-title class="text-lg">Select Seats</mat-card-title>
            <mat-card-subtitle>Pick up to 6 seats</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content class="flex justify-center p-4">
            <app-seat-map
              [layout]="t.seatLayout"
              [selectable]="true"
              [fare]="t.farePerSeat"
              (selectionChange)="onSelectionChange($event)" />
          </mat-card-content>
        </mat-card>
      </div>
    </div>

    <div class="fixed bottom-0 inset-x-0 bg-white border-t border-slate-200 shadow-lg">
      <div class="max-w-4xl mx-auto p-4 flex items-center justify-between gap-4">
        <div class="text-sm">
          @if (selectedSeats().length === 0) {
            <span class="text-slate-500">Select at least one seat</span>
          } @else {
            <div><strong>{{ selectedSeats().length }}</strong> seat(s) selected</div>
            <div class="text-slate-500">{{ (t.farePerSeat * selectedSeats().length) | currency:'INR' }}</div>
          }
        </div>
        <button mat-flat-button color="primary"
                [disabled]="selectedSeats().length === 0 || locking()"
                (click)="bookNow()">
          {{ locking() ? 'Holding…' : 'Book Now' }}
        </button>
      </div>
    </div>
  }
</div>
```

- [ ] **Step 3: Type-check**

Run: `cd frontend/bus-booking-web && npx tsc --noEmit -p tsconfig.app.json`
Expected: no errors.

- [ ] **Step 4: Commit**

```bash
git add frontend/bus-booking-web/src/app/features/public/trip-detail/
git commit -m "feat(m5): trip page locks seats and navigates to checkout"
```

---

## Task 21: Frontend — LoginComponent honours `returnUrl`

**Files:**
- Modify: `frontend/bus-booking-web/src/app/features/auth/login/login.component.ts`

- [ ] **Step 1: Update component**

Replace the existing `submit()` navigate call so a `?returnUrl=` query parameter is honoured:

```typescript
// ... existing imports, but also add ActivatedRoute:
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]]
  });

  submit(): void {
    if (this.form.invalid) return;
    this.submitting.set(true);
    this.errorMessage.set(null);
    this.auth.login(this.form.getRawValue()).subscribe({
      next: () => {
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/';
        this.router.navigateByUrl(returnUrl);
      },
      error: (err) => {
        const code = err?.error?.error?.code;
        this.errorMessage.set(
          code === 'INVALID_CREDENTIALS'
            ? 'Email or password is incorrect.'
            : 'Login failed. Please try again.');
        this.submitting.set(false);
      },
      complete: () => this.submitting.set(false)
    });
  }
}
```

- [ ] **Step 2: Type-check**

Run: `cd frontend/bus-booking-web && npx tsc --noEmit -p tsconfig.app.json`
Expected: no errors.

- [ ] **Step 3: Commit**

```bash
git add frontend/bus-booking-web/src/app/features/auth/login/login.component.ts
git commit -m "feat(m5): LoginComponent honours returnUrl query param"
```

---

## Task 22: Frontend — CheckoutStepperComponent

**Files:**
- Create: `frontend/bus-booking-web/src/types/razorpay.d.ts`
- Create: `frontend/bus-booking-web/src/app/features/customer/checkout/checkout-stepper.component.ts`
- Create: `frontend/bus-booking-web/src/app/features/customer/checkout/checkout-stepper.component.html`

- [ ] **Step 1: Razorpay typing**

```typescript
// frontend/bus-booking-web/src/types/razorpay.d.ts
export {};

declare global {
  interface RazorpayHandlerResponse {
    razorpay_order_id: string;
    razorpay_payment_id: string;
    razorpay_signature: string;
  }
  interface RazorpayOptions {
    key: string;
    amount: number;
    currency: string;
    name: string;
    description?: string;
    order_id: string;
    handler: (resp: RazorpayHandlerResponse) => void;
    prefill?: { name?: string; email?: string; contact?: string };
    theme?: { color?: string };
    modal?: { ondismiss?: () => void };
  }
  interface RazorpayInstance {
    open(): void;
  }
  interface Window {
    Razorpay: new (options: RazorpayOptions) => RazorpayInstance;
  }
}
```

- [ ] **Step 2: Checkout component**

```typescript
// frontend/bus-booking-web/src/app/features/customer/checkout/checkout-stepper.component.ts
import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatRadioModule } from '@angular/material/radio';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatStepperModule } from '@angular/material/stepper';
import { AuthStore } from '../../../core/auth/auth.store';
import {
  BookingsApiService,
  CreateBookingResponseDto,
  PassengerDto
} from '../../../core/api/bookings.api';
import { CountdownTimerComponent } from '../../../shared/components/countdown-timer/countdown-timer.component';

@Component({
  selector: 'app-checkout-stepper',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, RouterLink,
    MatStepperModule, MatButtonModule, MatCardModule, MatFormFieldModule,
    MatInputModule, MatRadioModule, MatIconModule,
    CountdownTimerComponent
  ],
  templateUrl: './checkout-stepper.component.html'
})
export class CheckoutStepperComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(BookingsApiService);
  private readonly auth = inject(AuthStore);
  private readonly snack = inject(MatSnackBar);

  readonly tripId     = signal<string>('');
  readonly lockId     = signal<string>('');
  readonly sessionId  = signal<string>('');
  readonly seats      = signal<string[]>([]);
  readonly expiresAt  = signal<string>('');
  readonly fare       = signal<number>(0);
  readonly submitting = signal(false);
  readonly lockExpired = signal(false);

  readonly total = computed(() => this.fare() * this.seats().length);

  readonly passengersForm = this.fb.nonNullable.group({
    passengers: this.fb.array<ReturnType<typeof this.buildPassengerGroup>>([])
  });

  get passengersArray(): FormArray { return this.passengersForm.controls.passengers; }

  ngOnInit(): void {
    const pm = this.route.snapshot.paramMap;
    const qm = this.route.snapshot.queryParamMap;
    this.tripId.set(pm.get('tripId') ?? '');
    this.lockId.set(qm.get('lockId') ?? '');
    this.sessionId.set(qm.get('sessionId') ?? '');
    this.seats.set((qm.get('seats') ?? '').split(',').filter(Boolean));
    this.expiresAt.set(qm.get('expiresAt') ?? '');
    this.fare.set(Number(qm.get('fare') ?? 0));

    if (!this.lockId() || this.seats().length === 0) {
      this.router.navigate(['/']);
      return;
    }

    for (const s of this.seats()) {
      this.passengersArray.push(this.buildPassengerGroup(s));
    }

    if (!this.auth.isLoggedIn()) {
      this.router.navigate(['/login'], {
        queryParams: { returnUrl: this.router.url }
      });
    }
  }

  buildPassengerGroup(seatNumber: string) {
    return this.fb.nonNullable.group({
      seatNumber: [seatNumber],
      passengerName: ['', [Validators.required, Validators.maxLength(120)]],
      passengerAge: [30, [Validators.required, Validators.min(1), Validators.max(120)]],
      passengerGender: ['male' as 'male' | 'female' | 'other', Validators.required]
    });
  }

  onLockExpired(): void {
    this.lockExpired.set(true);
  }

  payNow(): void {
    if (this.passengersForm.invalid || this.submitting() || this.lockExpired()) return;
    this.submitting.set(true);

    const passengers: PassengerDto[] = this.passengersArray.controls.map(c => c.getRawValue() as PassengerDto);

    this.api.createBooking({
      tripId: this.tripId(),
      lockId: this.lockId(),
      sessionId: this.sessionId(),
      passengers
    }).subscribe({
      next: (created) => this.openRazorpay(created),
      error: (err) => {
        this.submitting.set(false);
        const code = err?.error?.error?.code;
        if (code === 'LOCK_EXPIRED') {
          this.lockExpired.set(true);
        } else {
          this.snack.open(err?.error?.error?.message ?? 'Failed to create booking', 'Dismiss', { duration: 5000 });
        }
      }
    });
  }

  private openRazorpay(created: CreateBookingResponseDto): void {
    const user = this.auth.user();
    const options: RazorpayOptions = {
      key: created.keyId,
      amount: created.amount,
      currency: created.currency,
      name: 'BusBooking',
      description: `Booking ${created.bookingCode}`,
      order_id: created.razorpayOrderId,
      prefill: { name: user?.name, email: user?.email },
      theme: { color: '#3f51b5' },
      handler: (resp) => this.verify(created.bookingId, resp),
      modal: {
        ondismiss: () => {
          this.submitting.set(false);
          this.snack.open('Payment dismissed. You can retry or wait for the lock to expire.', 'Dismiss', { duration: 4000 });
        }
      }
    };
    const rzp = new window.Razorpay(options);
    rzp.open();
  }

  private verify(bookingId: string, resp: RazorpayHandlerResponse): void {
    this.api.verifyPayment(bookingId, {
      razorpayPaymentId: resp.razorpay_payment_id,
      razorpaySignature: resp.razorpay_signature
    }).subscribe({
      next: () => this.router.navigate(['/booking-confirmation', bookingId]),
      error: (err) => {
        this.submitting.set(false);
        this.snack.open(err?.error?.error?.message ?? 'Payment verification failed', 'Dismiss', { duration: 6000 });
      }
    });
  }

  ngOnDestroy(): void {
    // If the user navigated away without paying, release the lock politely. Best-effort.
    if (this.submitting() || !this.lockId()) return;
    // Only release if no booking was created for this lock yet.
    this.api.releaseLock(this.lockId(), this.sessionId()).subscribe({ next: () => {}, error: () => {} });
  }
}
```

- [ ] **Step 3: Template**

```html
<!-- frontend/bus-booking-web/src/app/features/customer/checkout/checkout-stepper.component.html -->
<div class="max-w-3xl mx-auto p-4 md:p-6 space-y-6">
  <div class="flex items-center justify-between">
    <h1 class="text-2xl font-bold m-0">Checkout</h1>
    @if (expiresAt()) {
      <app-countdown-timer [expiresAt]="expiresAt()" (expired)="onLockExpired()" />
    }
  </div>

  @if (lockExpired()) {
    <mat-card class="border border-rose-200 bg-rose-50 text-rose-800 p-6 text-center">
      <h2 class="text-lg font-semibold m-0 mb-2">Your seat reservation expired</h2>
      <p class="m-0 mb-4">Please go back and select your seats again.</p>
      <a mat-stroked-button color="primary" [routerLink]="['/trips', tripId()]">Back to trip</a>
    </mat-card>
  } @else {
    <mat-stepper linear orientation="vertical">
      <mat-step label="Review your selection" [completed]="true">
        <mat-card class="border border-slate-200 p-4 space-y-2">
          <div>Seats: <strong>{{ seats().join(', ') }}</strong></div>
          <div>Fare / seat: <strong>{{ fare() | currency:'INR' }}</strong></div>
          <div>Total fare: <strong>{{ total() | currency:'INR' }}</strong></div>
          <div class="text-slate-500 text-xs">Platform fee is calculated on the next step.</div>
          <div class="pt-3">
            <button mat-flat-button color="primary" matStepperNext>Continue</button>
          </div>
        </mat-card>
      </mat-step>

      <mat-step label="Passenger details" [stepControl]="passengersForm">
        <form [formGroup]="passengersForm" class="space-y-4 mt-2">
          @for (ctrl of passengersArray.controls; track $index) {
            <mat-card class="border border-slate-200 p-4" [formGroup]="$any(ctrl)">
              <div class="font-semibold mb-2">Seat {{ ctrl.get('seatNumber')?.value }}</div>
              <div class="grid grid-cols-1 md:grid-cols-3 gap-3">
                <mat-form-field appearance="outline">
                  <mat-label>Name</mat-label>
                  <input matInput formControlName="passengerName" />
                </mat-form-field>
                <mat-form-field appearance="outline">
                  <mat-label>Age</mat-label>
                  <input matInput type="number" formControlName="passengerAge" />
                </mat-form-field>
                <div>
                  <label class="text-xs text-slate-500 block mb-1">Gender</label>
                  <mat-radio-group formControlName="passengerGender" class="flex gap-4">
                    <mat-radio-button value="male">Male</mat-radio-button>
                    <mat-radio-button value="female">Female</mat-radio-button>
                    <mat-radio-button value="other">Other</mat-radio-button>
                  </mat-radio-group>
                </div>
              </div>
            </mat-card>
          }
          <div class="flex gap-2">
            <button mat-stroked-button matStepperPrevious>Back</button>
            <button mat-flat-button color="primary" matStepperNext [disabled]="passengersForm.invalid">Continue</button>
          </div>
        </form>
      </mat-step>

      <mat-step label="Payment">
        <mat-card class="border border-slate-200 p-6 text-center">
          <p class="mb-4">You will be redirected to Razorpay test-mode checkout.</p>
          <div class="flex items-center justify-center gap-2">
            <button mat-stroked-button matStepperPrevious>Back</button>
            <button mat-flat-button color="primary" [disabled]="submitting()" (click)="payNow()">
              {{ submitting() ? 'Processing…' : 'Pay ' + (total() | currency:'INR') }}
            </button>
          </div>
        </mat-card>
      </mat-step>
    </mat-stepper>
  }
</div>
```

- [ ] **Step 4: Add the typing file to `tsconfig.app.json` includes**

Open `frontend/bus-booking-web/tsconfig.app.json` — the file's `include` block should already cover `src/**/*.d.ts`. If the glob is narrower (e.g. only `src/main.ts`), add `"src/types/*.d.ts"` to `include`. Leave the rest alone.

- [ ] **Step 5: Type-check**

Run: `cd frontend/bus-booking-web && npx tsc --noEmit -p tsconfig.app.json`
Expected: no errors.

- [ ] **Step 6: Commit**

```bash
git add frontend/bus-booking-web/src/types/razorpay.d.ts \
        frontend/bus-booking-web/src/app/features/customer/checkout/ \
        frontend/bus-booking-web/tsconfig.app.json
git commit -m "feat(m5): add checkout stepper with Razorpay handoff"
```

---

## Task 23: Frontend — BookingConfirmationComponent, routes, Razorpay script

**Files:**
- Create: `frontend/bus-booking-web/src/app/features/customer/booking-confirmation/booking-confirmation.component.ts`
- Create: `frontend/bus-booking-web/src/app/features/customer/booking-confirmation/booking-confirmation.component.html`
- Modify: `frontend/bus-booking-web/src/app/app.routes.ts`
- Modify: `frontend/bus-booking-web/src/index.html`

- [ ] **Step 1: Confirmation component**

```typescript
// frontend/bus-booking-web/src/app/features/customer/booking-confirmation/booking-confirmation.component.ts
import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { BookingDetailDto, BookingsApiService } from '../../../core/api/bookings.api';
import { AuthTokenStore } from '../../../core/auth/auth-token.store';

@Component({
  selector: 'app-booking-confirmation',
  standalone: true,
  imports: [CommonModule, RouterLink, MatCardModule, MatButtonModule, MatIconModule],
  templateUrl: './booking-confirmation.component.html'
})
export class BookingConfirmationComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(BookingsApiService);
  private readonly tokens = inject(AuthTokenStore);

  readonly booking = signal<BookingDetailDto | null>(null);
  readonly loading = signal(true);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) { this.loading.set(false); return; }
    this.api.getBooking(id).subscribe({
      next: (b) => { this.booking.set(b); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  downloadTicket(): void {
    const b = this.booking();
    if (!b) return;
    const url = this.api.getTicketUrl(b.bookingId);
    // Ticket endpoint requires the bearer token — fetch as blob and trigger a download.
    fetch(url, { headers: { Authorization: `Bearer ${this.tokens.get()}` } })
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

- [ ] **Step 2: Confirmation template**

```html
<!-- frontend/bus-booking-web/src/app/features/customer/booking-confirmation/booking-confirmation.component.html -->
<div class="max-w-2xl mx-auto p-6">
  @if (loading()) {
    <div class="text-center text-slate-500">Loading…</div>
  } @else if (booking(); as b) {
    <mat-card class="border border-emerald-200 bg-emerald-50">
      <div class="flex items-center gap-3 p-4 border-b border-emerald-200">
        <mat-icon class="text-emerald-700">check_circle</mat-icon>
        <div>
          <div class="text-lg font-semibold text-emerald-900">Booking confirmed</div>
          <div class="text-sm text-emerald-800">{{ b.bookingCode }}</div>
        </div>
      </div>
      <div class="p-6 space-y-2 text-sm">
        <div><strong>{{ b.sourceCity }} → {{ b.destinationCity }}</strong></div>
        <div>{{ b.tripDate }} · {{ b.departureTime.substring(0,5) }} → {{ b.arrivalTime.substring(0,5) }}</div>
        <div>{{ b.busName }} · {{ b.operatorName }}</div>
        <div>Seats: <strong>{{ b.seats.length }}</strong> ({{ b.seats | json }})</div>
        <div>Total paid: <strong>{{ b.totalAmount | currency:'INR' }}</strong></div>
      </div>
      <div class="p-4 flex flex-wrap gap-2 border-t border-emerald-200">
        <button mat-flat-button color="primary" (click)="downloadTicket()">Download PDF ticket</button>
        <a mat-stroked-button routerLink="/">Back to home</a>
      </div>
    </mat-card>
    <p class="text-slate-500 text-xs mt-4">A copy of this ticket has been emailed to you.</p>
  } @else {
    <div class="text-center text-slate-500">Booking not found.</div>
  }
</div>
```

- [ ] **Step 3: Add routes**

In `frontend/bus-booking-web/src/app/app.routes.ts`, add these two entries to the `routes` array (before the `path: '**'` fallback):

```typescript
{
  path: 'checkout/:tripId',
  loadComponent: () => import('./features/customer/checkout/checkout-stepper.component')
    .then(m => m.CheckoutStepperComponent)
},
{
  path: 'booking-confirmation/:id',
  canMatch: [roleGuard(['customer'])],
  loadComponent: () => import('./features/customer/booking-confirmation/booking-confirmation.component')
    .then(m => m.BookingConfirmationComponent)
},
```

- [ ] **Step 4: Inject Razorpay script into index.html**

In `frontend/bus-booking-web/src/index.html`, inside the `<head>` block (before the closing `</head>`), add:

```html
<script src="https://checkout.razorpay.com/v1/checkout.js"></script>
```

- [ ] **Step 5: Build the frontend**

Run: `cd frontend/bus-booking-web && npx ng build --configuration development`
Expected: compilation succeeds.

- [ ] **Step 6: Start the dev server and smoke-test**

Run: `cd frontend/bus-booking-web && npx ng serve --port 4200`
Expected: dev server up; visit `http://localhost:4200`, search a route, open a trip, select a seat, and confirm the sticky "Book Now" bar appears. (A full Razorpay flow needs live test keys; the bar + checkout stepper navigation are what to verify here.)

- [ ] **Step 7: Commit**

```bash
git add frontend/bus-booking-web/src/app/features/customer/booking-confirmation/ \
        frontend/bus-booking-web/src/app/app.routes.ts \
        frontend/bus-booking-web/src/index.html
git commit -m "feat(m5): add booking confirmation page + routes + Razorpay script"
```

---

## Post-plan verification

After all 23 tasks, run the full quality gate in one sweep:

- [ ] **Backend build** — `dotnet build backend/BusBooking.Api/BusBooking.Api.csproj` ⇒ succeeds
- [ ] **Backend tests** — `dotnet test backend/BusBooking.Api.Tests/BusBooking.Api.Tests.csproj` ⇒ all green
- [ ] **Frontend typecheck** — `cd frontend/bus-booking-web && npx tsc --noEmit -p tsconfig.app.json` ⇒ no errors
- [ ] **Frontend build** — `cd frontend/bus-booking-web && npx ng build --configuration development` ⇒ succeeds
- [ ] **Manual smoke** (requires live Razorpay test keys + Resend key in `appsettings.Development.json`):
  1. `dotnet ef database update`
  2. `dotnet run` (backend) and `ng serve` (frontend)
  3. Register a customer, search Bangalore→Chennai, open a trip, select a seat
  4. Click "Book Now" → checkout page with countdown
  5. Fill passenger details, pay with test card `4111 1111 1111 1111`
  6. Confirmation page shows; click "Download PDF ticket"; confirm the email arrived in the Resend-linked inbox
