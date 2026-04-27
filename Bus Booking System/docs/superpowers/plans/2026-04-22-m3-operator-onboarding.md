# M3 — Operator Onboarding (Requests, Offices, Buses, Approvals) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. **Work directly on `main` — do NOT create a feature branch.** Commit messages MUST NOT include a `Co-Authored-By: Claude` trailer.

**Goal:** Deliver the M3 demoable outcome from the spec §10: a customer submits a "become-operator" request, an admin approves it, the newly-minted operator adds city offices and a bus, and an admin approves the bus. End-to-end the user experiences the full operator-onboarding flow with role-gated UI and audit trail.

**Architecture:** Five new EF Core 9 entities (`operator_requests`, `operator_offices`, `buses`, `seat_definitions`, `audit_log`) mapped into the existing `AppDbContext`. Six REST controllers split across three surfaces: customer (`POST /me/become-operator`), operator (`/operator/offices`, `/operator/buses`), admin (`/admin/operator-requests`, `/admin/buses`). Seat definitions are auto-generated from the bus's rows × columns on creation. Audit rows are written for every state transition (approve/reject/disable). Email is stubbed behind `INotificationSender` (M5 swaps in Resend). Angular side adds operator shell + dashboard + offices + buses pages, admin operator-requests + bus-approvals pages, and a customer `become-operator` page, plus navbar avatar "Switch to Operator Console" when both roles are present.

**Tech Stack:** .NET 9 · EF Core 9 · Npgsql · FluentValidation · xUnit · FluentAssertions · `Microsoft.AspNetCore.Mvc.Testing` · Angular 20 (standalone + Signals) · Angular Material · Tailwind v3 · Jasmine/Karma · PostgreSQL (`citext` — already registered by M1).

---

## File map

### New backend files

| Path | Responsibility |
|---|---|
| `backend/BusBooking.Api/Models/OperatorRequest.cs` | `operator_requests` entity |
| `backend/BusBooking.Api/Models/OperatorRequestStatus.cs` | `pending` / `approved` / `rejected` constants |
| `backend/BusBooking.Api/Models/OperatorOffice.cs` | `operator_offices` entity |
| `backend/BusBooking.Api/Models/Bus.cs` | `buses` entity |
| `backend/BusBooking.Api/Models/BusType.cs` | `seater` / `sleeper` / `semi_sleeper` constants |
| `backend/BusBooking.Api/Models/BusApprovalStatus.cs` | `pending` / `approved` / `rejected` constants |
| `backend/BusBooking.Api/Models/BusOperationalStatus.cs` | `active` / `under_maintenance` / `retired` constants |
| `backend/BusBooking.Api/Models/SeatDefinition.cs` | `seat_definitions` entity |
| `backend/BusBooking.Api/Models/SeatCategory.cs` | `regular` constant (future-proof for M5+) |
| `backend/BusBooking.Api/Models/AuditLogEntry.cs` | `audit_log` entity |
| `backend/BusBooking.Api/Models/AuditAction.cs` | String constants: `OPERATOR_REQUEST_APPROVED`, `OPERATOR_REQUEST_REJECTED`, `BUS_APPROVED`, `BUS_REJECTED`, `OFFICE_CREATED`, `OFFICE_DELETED`, `BUS_CREATED`, `BUS_STATUS_CHANGED` |
| `backend/BusBooking.Api/Dtos/BecomeOperatorRequest.cs` | Customer payload `{ CompanyName }` |
| `backend/BusBooking.Api/Dtos/OperatorRequestDto.cs` | Public shape for an operator request |
| `backend/BusBooking.Api/Dtos/RejectOperatorRequest.cs` | Admin reject payload `{ Reason }` |
| `backend/BusBooking.Api/Dtos/OperatorOfficeDto.cs` | Office shape |
| `backend/BusBooking.Api/Dtos/CreateOperatorOfficeRequest.cs` | Create payload |
| `backend/BusBooking.Api/Dtos/BusDto.cs` | Bus shape |
| `backend/BusBooking.Api/Dtos/CreateBusRequest.cs` | `{ RegistrationNumber, BusName, BusType, Rows, Columns }` |
| `backend/BusBooking.Api/Dtos/UpdateBusStatusRequest.cs` | `{ OperationalStatus }` |
| `backend/BusBooking.Api/Dtos/RejectBusRequest.cs` | `{ Reason }` |
| `backend/BusBooking.Api/Validators/BecomeOperatorRequestValidator.cs` | FluentValidation |
| `backend/BusBooking.Api/Validators/RejectOperatorRequestValidator.cs` | FluentValidation |
| `backend/BusBooking.Api/Validators/CreateOperatorOfficeRequestValidator.cs` | FluentValidation |
| `backend/BusBooking.Api/Validators/CreateBusRequestValidator.cs` | FluentValidation |
| `backend/BusBooking.Api/Validators/UpdateBusStatusRequestValidator.cs` | FluentValidation |
| `backend/BusBooking.Api/Validators/RejectBusRequestValidator.cs` | FluentValidation |
| `backend/BusBooking.Api/Services/IAuditLogWriter.cs` | `WriteAsync(actorUserId, action, targetType, targetId, metadata?)` |
| `backend/BusBooking.Api/Services/AuditLogWriter.cs` | Simple append-only writer |
| `backend/BusBooking.Api/Services/INotificationSender.cs` | `SendOperatorApprovedAsync`, `SendOperatorRejectedAsync`, `SendBusApprovedAsync`, `SendBusRejectedAsync` |
| `backend/BusBooking.Api/Services/LoggingNotificationSender.cs` | Stub impl — logs via Serilog. M5 replaces with Resend. |
| `backend/BusBooking.Api/Services/IOperatorRequestService.cs` | Contract |
| `backend/BusBooking.Api/Services/OperatorRequestService.cs` | Impl |
| `backend/BusBooking.Api/Services/IOperatorOfficeService.cs` | Contract |
| `backend/BusBooking.Api/Services/OperatorOfficeService.cs` | Impl |
| `backend/BusBooking.Api/Services/IBusService.cs` | Contract |
| `backend/BusBooking.Api/Services/BusService.cs` | Impl — also responsible for seat-definition generation |
| `backend/BusBooking.Api/Infrastructure/Auth/CurrentUserAccessor.cs` | Reads `sub` claim, returns `Guid` — removes MeController duplication |
| `backend/BusBooking.Api/Controllers/BecomeOperatorController.cs` | `POST /api/v1/me/become-operator` |
| `backend/BusBooking.Api/Controllers/AdminOperatorRequestsController.cs` | `/api/v1/admin/operator-requests` GET + approve + reject |
| `backend/BusBooking.Api/Controllers/OperatorOfficesController.cs` | `/api/v1/operator/offices` GET/POST/DELETE |
| `backend/BusBooking.Api/Controllers/OperatorBusesController.cs` | `/api/v1/operator/buses` GET/POST + status PATCH + DELETE |
| `backend/BusBooking.Api/Controllers/AdminBusesController.cs` | `/api/v1/admin/buses` GET (approval queue) + approve + reject |
| `backend/BusBooking.Api/Migrations/<ts>_AddOperatorDomain.cs` | EF migration |

### Modified backend files

- `backend/BusBooking.Api/Infrastructure/AppDbContext.cs` — add `DbSet<OperatorRequest>`, `DbSet<OperatorOffice>`, `DbSet<Bus>`, `DbSet<SeatDefinition>`, `DbSet<AuditLogEntry>` plus their mappings.
- `backend/BusBooking.Api/Program.cs` — DI registrations for `IAuditLogWriter`, `INotificationSender`, `IOperatorRequestService`, `IOperatorOfficeService`, `IBusService`, `CurrentUserAccessor`.
- `backend/BusBooking.Api/Controllers/MeController.cs` — refactor to use `CurrentUserAccessor` (optional — include only if it keeps MeController tiny).

### New test files

| Path | Responsibility |
|---|---|
| `backend/BusBooking.Api.Tests/Support/OperatorTokenFactory.cs` | Helper: seeds a user with a given role set and returns a JWT. Generalisation of `AdminTokenFactory`. |
| `backend/BusBooking.Api.Tests/Unit/BusServiceSeatGenerationTests.cs` | Unit: rows × cols → deterministic `A1..C4` seat labels |
| `backend/BusBooking.Api.Tests/Integration/BecomeOperatorTests.cs` | Customer creates request; second request while pending → 422 `REQUEST_ALREADY_PENDING`; already-operator → 422 `ALREADY_OPERATOR` |
| `backend/BusBooking.Api.Tests/Integration/AdminOperatorRequestsTests.cs` | List by status; approve grants role + audit + notification; reject with reason |
| `backend/BusBooking.Api.Tests/Integration/OperatorOfficesTests.cs` | Create/list/delete + `UNIQUE(operator_user_id, city_id)` 409 + unknown city 404 |
| `backend/BusBooking.Api.Tests/Integration/OperatorBusesTests.cs` | Create auto-generates seats; list; status PATCH; delete soft-retires |
| `backend/BusBooking.Api.Tests/Integration/AdminBusesTests.cs` | List pending; approve flips status + audit + notification; reject with reason |

### Modified test files

- `backend/BusBooking.Api.Tests/Support/IntegrationFixture.cs` — extend `ResetAsync` to truncate the five new tables.

### New frontend files

| Path | Responsibility |
|---|---|
| `frontend/bus-booking-web/src/app/core/api/operator-requests.api.ts` | `becomeOperator(body)` (customer); `list`, `approve`, `reject` (admin) |
| `frontend/bus-booking-web/src/app/core/api/operator-offices.api.ts` | operator offices CRUD |
| `frontend/bus-booking-web/src/app/core/api/operator-buses.api.ts` | operator buses CRUD |
| `frontend/bus-booking-web/src/app/core/api/admin-buses.api.ts` | admin list pending + approve + reject |
| `frontend/bus-booking-web/src/app/features/customer/become-operator/become-operator-page.component.{ts,html,scss}` | Customer form |
| `frontend/bus-booking-web/src/app/features/operator/operator-shell/operator-shell.component.{ts,html,scss}` | Sidebar + outlet for `/operator/*` |
| `frontend/bus-booking-web/src/app/features/operator/dashboard/operator-dashboard.component.{ts,html,scss}` | Landing page with counts |
| `frontend/bus-booking-web/src/app/features/operator/offices/operator-offices-page.component.{ts,html,scss}` | List + add-office dialog + delete |
| `frontend/bus-booking-web/src/app/features/operator/buses/operator-buses-list.component.{ts,html,scss}` | List of buses with status chip |
| `frontend/bus-booking-web/src/app/features/operator/buses/operator-bus-form.component.{ts,html,scss}` | New-bus form |
| `frontend/bus-booking-web/src/app/features/admin/operator-requests/admin-operator-requests-page.component.{ts,html,scss}` | Approve / reject queue |
| `frontend/bus-booking-web/src/app/features/admin/bus-approvals/admin-bus-approvals-page.component.{ts,html,scss}` | Approve / reject queue |

### Modified frontend files

- `src/app/app.routes.ts` — add `customer/become-operator`, `operator` (with child routes), `admin/operator-requests`, `admin/bus-approvals`; all gated by `roleGuard`.
- `src/app/features/admin/admin-dashboard/admin-dashboard.component.{ts,html}` — add quick-links to the two new admin pages.
- `src/app/core/auth/role.guard.ts` — (only if `canMatch` needs extension); verify multi-role access works as-is.
- `src/app/shared/components/navbar/*` — add "Switch to Operator Console" menu item when `roles` includes `operator`. If the navbar does not yet exist in `shared/components`, find the current rendering in `app.html` and migrate inline.

---

## Prerequisites

Run once before starting (user action — credentials live in the developer's shell):

```bash
# citext extension should already exist from M1.
psql -d bus_booking      -c "CREATE EXTENSION IF NOT EXISTS citext;"
psql -d bus_booking_test -c "CREATE EXTENSION IF NOT EXISTS citext;"
```

M2 migrations (cities, routes, platform_fee_config) must be applied on both `bus_booking` and `bus_booking_test`. If the test DB has drifted:

```bash
cd backend/BusBooking.Api
DOTNET_ROLL_FORWARD=Major dotnet ef database update --connection "Host=localhost;Database=bus_booking_test;Username=postgres;Password=postgres"
```

Every `dotnet` command below runs from `backend/BusBooking.Api` (or `backend/BusBooking.Api.Tests` for test runs). Every `ng` / `npm` command runs from `frontend/bus-booking-web`.

---

## Task 1: Operator-domain entities + DbContext mappings

**Files:**
- Create: `backend/BusBooking.Api/Models/OperatorRequest.cs`
- Create: `backend/BusBooking.Api/Models/OperatorRequestStatus.cs`
- Create: `backend/BusBooking.Api/Models/OperatorOffice.cs`
- Create: `backend/BusBooking.Api/Models/Bus.cs`
- Create: `backend/BusBooking.Api/Models/BusType.cs`
- Create: `backend/BusBooking.Api/Models/BusApprovalStatus.cs`
- Create: `backend/BusBooking.Api/Models/BusOperationalStatus.cs`
- Create: `backend/BusBooking.Api/Models/SeatDefinition.cs`
- Create: `backend/BusBooking.Api/Models/SeatCategory.cs`
- Create: `backend/BusBooking.Api/Models/AuditLogEntry.cs`
- Create: `backend/BusBooking.Api/Models/AuditAction.cs`
- Modify: `backend/BusBooking.Api/Infrastructure/AppDbContext.cs`

- [ ] **Step 1: Create `OperatorRequestStatus.cs`**

```csharp
namespace BusBooking.Api.Models;

public static class OperatorRequestStatus
{
    public const string Pending = "pending";
    public const string Approved = "approved";
    public const string Rejected = "rejected";

    public static readonly string[] All = [Pending, Approved, Rejected];
}
```

- [ ] **Step 2: Create `OperatorRequest.cs`**

```csharp
namespace BusBooking.Api.Models;

public class OperatorRequest
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string Status { get; set; }
    public required string CompanyName { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedByAdminId { get; set; }
    public string? RejectReason { get; set; }

    public User? User { get; set; }
}
```

- [ ] **Step 3: Create `OperatorOffice.cs`**

```csharp
namespace BusBooking.Api.Models;

public class OperatorOffice
{
    public Guid Id { get; set; }
    public Guid OperatorUserId { get; set; }
    public Guid CityId { get; set; }
    public required string AddressLine { get; set; }
    public required string Phone { get; set; }
    public bool IsActive { get; set; } = true;

    public User? Operator { get; set; }
    public City? City { get; set; }
}
```

- [ ] **Step 4: Create `BusType.cs`**

```csharp
namespace BusBooking.Api.Models;

public static class BusType
{
    public const string Seater = "seater";
    public const string Sleeper = "sleeper";
    public const string SemiSleeper = "semi_sleeper";

    public static readonly string[] All = [Seater, Sleeper, SemiSleeper];
}
```

- [ ] **Step 5: Create `BusApprovalStatus.cs`**

```csharp
namespace BusBooking.Api.Models;

public static class BusApprovalStatus
{
    public const string Pending = "pending";
    public const string Approved = "approved";
    public const string Rejected = "rejected";

    public static readonly string[] All = [Pending, Approved, Rejected];
}
```

- [ ] **Step 6: Create `BusOperationalStatus.cs`**

```csharp
namespace BusBooking.Api.Models;

public static class BusOperationalStatus
{
    public const string Active = "active";
    public const string UnderMaintenance = "under_maintenance";
    public const string Retired = "retired";

    public static readonly string[] All = [Active, UnderMaintenance, Retired];
}
```

- [ ] **Step 7: Create `Bus.cs`**

```csharp
namespace BusBooking.Api.Models;

public class Bus
{
    public Guid Id { get; set; }
    public Guid OperatorUserId { get; set; }
    public required string RegistrationNumber { get; set; }
    public required string BusName { get; set; }
    public required string BusType { get; set; }
    public int Capacity { get; set; }
    public required string ApprovalStatus { get; set; } = Models.BusApprovalStatus.Pending;
    public required string OperationalStatus { get; set; } = Models.BusOperationalStatus.Active;
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedByAdminId { get; set; }
    public string? RejectReason { get; set; }

    public User? Operator { get; set; }
    public ICollection<SeatDefinition> Seats { get; set; } = new List<SeatDefinition>();
}
```

- [ ] **Step 8: Create `SeatCategory.cs`**

```csharp
namespace BusBooking.Api.Models;

public static class SeatCategory
{
    public const string Regular = "regular";

    public static readonly string[] All = [Regular];
}
```

- [ ] **Step 9: Create `SeatDefinition.cs`**

```csharp
namespace BusBooking.Api.Models;

public class SeatDefinition
{
    public Guid Id { get; set; }
    public Guid BusId { get; set; }
    public required string SeatNumber { get; set; }
    public int RowIndex { get; set; }
    public int ColumnIndex { get; set; }
    public required string SeatCategory { get; set; } = Models.SeatCategory.Regular;

    public Bus? Bus { get; set; }
}
```

- [ ] **Step 10: Create `AuditAction.cs`**

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
}
```

- [ ] **Step 11: Create `AuditLogEntry.cs`**

```csharp
namespace BusBooking.Api.Models;

public class AuditLogEntry
{
    public Guid Id { get; set; }
    public Guid ActorUserId { get; set; }
    public required string Action { get; set; }
    public required string TargetType { get; set; }
    public Guid TargetId { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

- [ ] **Step 12: Extend `AppDbContext.cs`**

Open `backend/BusBooking.Api/Infrastructure/AppDbContext.cs`. After the `DbSet<PlatformFeeConfig>` line, add:

```csharp
    public DbSet<OperatorRequest> OperatorRequests => Set<OperatorRequest>();
    public DbSet<OperatorOffice> OperatorOffices => Set<OperatorOffice>();
    public DbSet<Bus> Buses => Set<Bus>();
    public DbSet<SeatDefinition> SeatDefinitions => Set<SeatDefinition>();
    public DbSet<AuditLogEntry> AuditLog => Set<AuditLogEntry>();
```

Inside `OnModelCreating`, after the `PlatformFeeConfig` block and before the closing `}`, add:

```csharp
        modelBuilder.Entity<OperatorRequest>(b =>
        {
            b.ToTable("operator_requests");
            b.HasKey(r => r.Id);
            b.Property(r => r.Id).HasColumnName("id");
            b.Property(r => r.UserId).HasColumnName("user_id");
            b.Property(r => r.Status).HasColumnName("status").IsRequired().HasMaxLength(16);
            b.Property(r => r.CompanyName).HasColumnName("company_name").IsRequired().HasMaxLength(160);
            b.Property(r => r.RequestedAt).HasColumnName("requested_at");
            b.Property(r => r.ReviewedAt).HasColumnName("reviewed_at");
            b.Property(r => r.ReviewedByAdminId).HasColumnName("reviewed_by_admin_id");
            b.Property(r => r.RejectReason).HasColumnName("reject_reason").HasMaxLength(500);
            b.HasIndex(r => new { r.UserId, r.Status });
            b.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OperatorOffice>(b =>
        {
            b.ToTable("operator_offices");
            b.HasKey(o => o.Id);
            b.Property(o => o.Id).HasColumnName("id");
            b.Property(o => o.OperatorUserId).HasColumnName("operator_user_id");
            b.Property(o => o.CityId).HasColumnName("city_id");
            b.Property(o => o.AddressLine).HasColumnName("address_line").IsRequired().HasMaxLength(300);
            b.Property(o => o.Phone).HasColumnName("phone").IsRequired().HasMaxLength(32);
            b.Property(o => o.IsActive).HasColumnName("is_active");
            b.HasIndex(o => new { o.OperatorUserId, o.CityId }).IsUnique();
            b.HasOne(o => o.Operator)
                .WithMany()
                .HasForeignKey(o => o.OperatorUserId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(o => o.City)
                .WithMany()
                .HasForeignKey(o => o.CityId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Bus>(b =>
        {
            b.ToTable("buses");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.OperatorUserId).HasColumnName("operator_user_id");
            b.Property(x => x.RegistrationNumber).HasColumnName("registration_number")
                .HasColumnType("citext").IsRequired().HasMaxLength(32);
            b.Property(x => x.BusName).HasColumnName("bus_name").IsRequired().HasMaxLength(120);
            b.Property(x => x.BusType).HasColumnName("bus_type").IsRequired().HasMaxLength(16);
            b.Property(x => x.Capacity).HasColumnName("capacity");
            b.Property(x => x.ApprovalStatus).HasColumnName("approval_status").IsRequired().HasMaxLength(16);
            b.Property(x => x.OperationalStatus).HasColumnName("operational_status").IsRequired().HasMaxLength(20);
            b.Property(x => x.CreatedAt).HasColumnName("created_at");
            b.Property(x => x.ApprovedAt).HasColumnName("approved_at");
            b.Property(x => x.ApprovedByAdminId).HasColumnName("approved_by_admin_id");
            b.Property(x => x.RejectReason).HasColumnName("reject_reason").HasMaxLength(500);
            b.HasIndex(x => x.RegistrationNumber).IsUnique();
            b.HasIndex(x => new { x.OperatorUserId, x.ApprovalStatus });
            b.HasOne(x => x.Operator)
                .WithMany()
                .HasForeignKey(x => x.OperatorUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SeatDefinition>(b =>
        {
            b.ToTable("seat_definitions");
            b.HasKey(s => s.Id);
            b.Property(s => s.Id).HasColumnName("id");
            b.Property(s => s.BusId).HasColumnName("bus_id");
            b.Property(s => s.SeatNumber).HasColumnName("seat_number").IsRequired().HasMaxLength(8);
            b.Property(s => s.RowIndex).HasColumnName("row_index");
            b.Property(s => s.ColumnIndex).HasColumnName("column_index");
            b.Property(s => s.SeatCategory).HasColumnName("seat_category").IsRequired().HasMaxLength(16);
            b.HasIndex(s => new { s.BusId, s.SeatNumber }).IsUnique();
            b.HasOne(s => s.Bus)
                .WithMany(x => x.Seats)
                .HasForeignKey(s => s.BusId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditLogEntry>(b =>
        {
            b.ToTable("audit_log");
            b.HasKey(a => a.Id);
            b.Property(a => a.Id).HasColumnName("id");
            b.Property(a => a.ActorUserId).HasColumnName("actor_user_id");
            b.Property(a => a.Action).HasColumnName("action").IsRequired().HasMaxLength(64);
            b.Property(a => a.TargetType).HasColumnName("target_type").IsRequired().HasMaxLength(64);
            b.Property(a => a.TargetId).HasColumnName("target_id");
            b.Property(a => a.MetadataJson).HasColumnName("metadata").HasColumnType("jsonb");
            b.Property(a => a.CreatedAt).HasColumnName("created_at");
            b.HasIndex(a => new { a.TargetType, a.TargetId });
        });
```

- [ ] **Step 13: Build**

```bash
cd backend/BusBooking.Api && dotnet build
```

Expected: `Build succeeded.` No warnings.

- [ ] **Step 14: Commit**

```bash
git add backend/BusBooking.Api/Models/ backend/BusBooking.Api/Infrastructure/AppDbContext.cs
git commit -m "feat(backend): add operator-domain entities (requests, offices, buses, seats, audit)"
```

---

## Task 2: EF migration for operator domain

**Files:**
- Create: `backend/BusBooking.Api/Migrations/<timestamp>_AddOperatorDomain.cs` (generated)
- Create: `backend/BusBooking.Api/Migrations/<timestamp>_AddOperatorDomain.Designer.cs` (generated)
- Modify: `backend/BusBooking.Api/Migrations/AppDbContextModelSnapshot.cs` (generated)

- [ ] **Step 1: Generate the migration**

```bash
cd backend/BusBooking.Api
DOTNET_ROLL_FORWARD=Major dotnet ef migrations add AddOperatorDomain
```

Expected: Two new files under `Migrations/` and `AppDbContextModelSnapshot.cs` updated.

- [ ] **Step 2: Inspect the generated migration**

Open the new `Migrations/<ts>_AddOperatorDomain.cs`. Confirm it creates five tables: `operator_requests`, `operator_offices`, `buses`, `seat_definitions`, `audit_log`. Confirm `buses.registration_number` is `citext`. Confirm the unique index `IX_operator_offices_operator_user_id_city_id` is present and unique. If any table is missing or mis-typed, delete the two new migration files, fix the `AppDbContext` mapping, and regenerate.

- [ ] **Step 3: Apply migration to dev DB**

```bash
cd backend/BusBooking.Api
DOTNET_ROLL_FORWARD=Major dotnet ef database update
```

Expected: `Done.` Verify with:

```bash
psql -d bus_booking -c "\dt" | grep -E "operator_requests|operator_offices|buses|seat_definitions|audit_log"
```

Expected: five matching lines.

- [ ] **Step 4: Apply migration to test DB**

```bash
cd backend/BusBooking.Api
DOTNET_ROLL_FORWARD=Major dotnet ef database update \
  --connection "Host=localhost;Database=bus_booking_test;Username=postgres;Password=postgres"
```

(Replace the credentials if different on your machine.)

- [ ] **Step 5: Commit**

```bash
git add backend/BusBooking.Api/Migrations/
git commit -m "feat(backend): migration for operator domain tables"
```

---

## Task 3: Extend `IntegrationFixture.ResetAsync` for new tables

**Files:**
- Modify: `backend/BusBooking.Api.Tests/Support/IntegrationFixture.cs`

- [ ] **Step 1: Replace the TRUNCATE body in `ResetAsync`**

Find the `ResetAsync` method in `IntegrationFixture.cs`. Its current body truncates `user_roles, users`. Replace it with:

```csharp
    public async Task ResetAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE audit_log, seat_definitions, buses, operator_offices, operator_requests, "
            + "platform_fee_config, routes, cities, user_roles, users "
            + "RESTART IDENTITY CASCADE");
    }
```

(Order matters only for readability — `CASCADE` handles FK ordering.)

- [ ] **Step 2: Build the test project**

```bash
cd backend/BusBooking.Api.Tests && dotnet build
```

Expected: `Build succeeded.`

- [ ] **Step 3: Re-run existing M1/M2 integration tests to confirm no regression**

```bash
cd backend/BusBooking.Api.Tests
DOTNET_ROLL_FORWARD=Major dotnet test --filter "FullyQualifiedName~Integration"
```

Expected: all passing. If any test fails with `relation "..." does not exist`, the test DB is missing the M3 migration — re-run Task 2 Step 4.

- [ ] **Step 4: Commit**

```bash
git add backend/BusBooking.Api.Tests/Support/IntegrationFixture.cs
git commit -m "test(backend): extend ResetAsync to truncate operator-domain tables"
```

---

## Task 4: `CurrentUserAccessor`, `IAuditLogWriter`, `INotificationSender` + DI

**Files:**
- Create: `backend/BusBooking.Api/Infrastructure/Auth/CurrentUserAccessor.cs`
- Create: `backend/BusBooking.Api/Services/IAuditLogWriter.cs`
- Create: `backend/BusBooking.Api/Services/AuditLogWriter.cs`
- Create: `backend/BusBooking.Api/Services/INotificationSender.cs`
- Create: `backend/BusBooking.Api/Services/LoggingNotificationSender.cs`
- Modify: `backend/BusBooking.Api/Program.cs`

- [ ] **Step 1: Create `CurrentUserAccessor.cs`**

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BusBooking.Api.Infrastructure.Errors;
using Microsoft.AspNetCore.Http;

namespace BusBooking.Api.Infrastructure.Auth;

public interface ICurrentUserAccessor
{
    Guid UserId { get; }
    bool TryGetUserId(out Guid userId);
}

public class CurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _http;

    public CurrentUserAccessor(IHttpContextAccessor http)
    {
        _http = http;
    }

    public Guid UserId
    {
        get
        {
            if (!TryGetUserId(out var id))
                throw new UnauthorizedException("UNAUTHORIZED", "Missing or invalid subject claim");
            return id;
        }
    }

    public bool TryGetUserId(out Guid userId)
    {
        userId = Guid.Empty;
        var user = _http.HttpContext?.User;
        if (user is null) return false;
        var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out userId);
    }
}
```

- [ ] **Step 2: Create `IAuditLogWriter.cs`**

```csharp
namespace BusBooking.Api.Services;

public interface IAuditLogWriter
{
    Task WriteAsync(
        Guid actorUserId,
        string action,
        string targetType,
        Guid targetId,
        object? metadata = null,
        CancellationToken ct = default);
}
```

- [ ] **Step 3: Create `AuditLogWriter.cs`**

```csharp
using System.Text.Json;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Models;

namespace BusBooking.Api.Services;

public class AuditLogWriter : IAuditLogWriter
{
    private readonly AppDbContext _db;

    public AuditLogWriter(AppDbContext db)
    {
        _db = db;
    }

    public async Task WriteAsync(
        Guid actorUserId,
        string action,
        string targetType,
        Guid targetId,
        object? metadata = null,
        CancellationToken ct = default)
    {
        var entry = new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            MetadataJson = metadata is null ? null : JsonSerializer.Serialize(metadata),
            CreatedAt = DateTime.UtcNow
        };
        _db.AuditLog.Add(entry);
        await _db.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 4: Create `INotificationSender.cs`**

```csharp
using BusBooking.Api.Models;

namespace BusBooking.Api.Services;

public interface INotificationSender
{
    Task SendOperatorApprovedAsync(User user, CancellationToken ct = default);
    Task SendOperatorRejectedAsync(User user, string reason, CancellationToken ct = default);
    Task SendBusApprovedAsync(User operatorUser, Bus bus, CancellationToken ct = default);
    Task SendBusRejectedAsync(User operatorUser, Bus bus, string reason, CancellationToken ct = default);
}
```

- [ ] **Step 5: Create `LoggingNotificationSender.cs`**

```csharp
using BusBooking.Api.Models;
using Microsoft.Extensions.Logging;

namespace BusBooking.Api.Services;

// M3 stub. M5 replaces with Resend-backed implementation.
public class LoggingNotificationSender : INotificationSender
{
    private readonly ILogger<LoggingNotificationSender> _log;

    public LoggingNotificationSender(ILogger<LoggingNotificationSender> log)
    {
        _log = log;
    }

    public Task SendOperatorApprovedAsync(User user, CancellationToken ct = default)
    {
        _log.LogInformation("NOTIFY operator-approved to={Email} name={Name}", user.Email, user.Name);
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
}
```

- [ ] **Step 6: Register the services in `Program.cs`**

In `backend/BusBooking.Api/Program.cs`, find the block that ends with `builder.Services.AddScoped<IPlatformFeeSeeder, PlatformFeeSeeder>();`. Immediately after that block, add:

```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<BusBooking.Api.Infrastructure.Auth.ICurrentUserAccessor,
                           BusBooking.Api.Infrastructure.Auth.CurrentUserAccessor>();
builder.Services.AddScoped<IAuditLogWriter, AuditLogWriter>();
builder.Services.AddScoped<INotificationSender, LoggingNotificationSender>();
```

- [ ] **Step 7: Build**

```bash
cd backend/BusBooking.Api && dotnet build
```

Expected: `Build succeeded.`

- [ ] **Step 8: Commit**

```bash
git add backend/BusBooking.Api/Infrastructure/Auth/CurrentUserAccessor.cs \
        backend/BusBooking.Api/Services/IAuditLogWriter.cs \
        backend/BusBooking.Api/Services/AuditLogWriter.cs \
        backend/BusBooking.Api/Services/INotificationSender.cs \
        backend/BusBooking.Api/Services/LoggingNotificationSender.cs \
        backend/BusBooking.Api/Program.cs
git commit -m "feat(backend): CurrentUserAccessor, audit writer, notification stub + DI"
```

---

## Task 5: `OperatorTokenFactory` test helper (generalises `AdminTokenFactory`)

**Files:**
- Create: `backend/BusBooking.Api.Tests/Support/OperatorTokenFactory.cs`

We keep `AdminTokenFactory` as-is so existing tests do not need edits. `OperatorTokenFactory` supports any role combination and returns the created `User` plus a JWT, so that BecomeOperator / operator-buses / admin-buses tests can all share one helper.

- [ ] **Step 1: Create `OperatorTokenFactory.cs`**

```csharp
using System.Net.Http.Headers;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Infrastructure.Auth;
using BusBooking.Api.Models;
using Microsoft.Extensions.DependencyInjection;

namespace BusBooking.Api.Tests.Support;

public static class OperatorTokenFactory
{
    public static async Task<(User user, string token)> CreateAsync(
        IntegrationFixture fx,
        string[] roles,
        string? email = null,
        string? name = null,
        CancellationToken ct = default)
    {
        using var scope = fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var tokens = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        var id = Guid.NewGuid();
        var user = new User
        {
            Id = id,
            Name = name ?? $"User-{id:N}".Substring(0, 12),
            Email = email ?? $"u-{id:N}@busbooking.local".Substring(0, 40),
            PasswordHash = hasher.Hash("x-not-used"),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        foreach (var r in roles)
            user.Roles.Add(new UserRole { UserId = id, Role = r });

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        var token = tokens.Generate(user, roles);
        return (user, token.Token);
    }

    public static void AttachBearer(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
```

- [ ] **Step 2: Build the test project**

```bash
cd backend/BusBooking.Api.Tests && dotnet build
```

Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add backend/BusBooking.Api.Tests/Support/OperatorTokenFactory.cs
git commit -m "test(backend): add OperatorTokenFactory helper for multi-role fixtures"
```

---

## Task 6: `IOperatorRequestService` + customer `become-operator` endpoint

**Files:**
- Create: `backend/BusBooking.Api/Dtos/BecomeOperatorRequest.cs`
- Create: `backend/BusBooking.Api/Dtos/OperatorRequestDto.cs`
- Create: `backend/BusBooking.Api/Dtos/RejectOperatorRequest.cs`
- Create: `backend/BusBooking.Api/Validators/BecomeOperatorRequestValidator.cs`
- Create: `backend/BusBooking.Api/Validators/RejectOperatorRequestValidator.cs`
- Create: `backend/BusBooking.Api/Services/IOperatorRequestService.cs`
- Create: `backend/BusBooking.Api/Services/OperatorRequestService.cs`
- Create: `backend/BusBooking.Api/Controllers/BecomeOperatorController.cs`
- Create: `backend/BusBooking.Api/Controllers/AdminOperatorRequestsController.cs`
- Create: `backend/BusBooking.Api.Tests/Integration/BecomeOperatorTests.cs`
- Modify: `backend/BusBooking.Api/Program.cs`

- [ ] **Step 1: Create `BecomeOperatorRequest.cs`**

```csharp
namespace BusBooking.Api.Dtos;

public class BecomeOperatorRequest
{
    public required string CompanyName { get; set; }
}
```

- [ ] **Step 2: Create `OperatorRequestDto.cs`**

```csharp
namespace BusBooking.Api.Dtos;

public record OperatorRequestDto(
    Guid Id,
    Guid UserId,
    string UserEmail,
    string UserName,
    string CompanyName,
    string Status,
    DateTime RequestedAt,
    DateTime? ReviewedAt,
    Guid? ReviewedByAdminId,
    string? RejectReason);
```

- [ ] **Step 3: Create `RejectOperatorRequest.cs`**

```csharp
namespace BusBooking.Api.Dtos;

public class RejectOperatorRequest
{
    public required string Reason { get; set; }
}
```

- [ ] **Step 4: Create `BecomeOperatorRequestValidator.cs`**

```csharp
using BusBooking.Api.Dtos;
using FluentValidation;

namespace BusBooking.Api.Validators;

public class BecomeOperatorRequestValidator : AbstractValidator<BecomeOperatorRequest>
{
    public BecomeOperatorRequestValidator()
    {
        RuleFor(r => r.CompanyName)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(160);
    }
}
```

- [ ] **Step 5: Create `RejectOperatorRequestValidator.cs`**

```csharp
using BusBooking.Api.Dtos;
using FluentValidation;

namespace BusBooking.Api.Validators;

public class RejectOperatorRequestValidator : AbstractValidator<RejectOperatorRequest>
{
    public RejectOperatorRequestValidator()
    {
        RuleFor(r => r.Reason)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(500);
    }
}
```

- [ ] **Step 6: Create `IOperatorRequestService.cs`**

```csharp
using BusBooking.Api.Dtos;

namespace BusBooking.Api.Services;

public interface IOperatorRequestService
{
    Task<OperatorRequestDto> SubmitAsync(Guid userId, BecomeOperatorRequest body, CancellationToken ct);
    Task<IReadOnlyList<OperatorRequestDto>> ListAsync(string? status, CancellationToken ct);
    Task<OperatorRequestDto> ApproveAsync(Guid adminId, Guid requestId, CancellationToken ct);
    Task<OperatorRequestDto> RejectAsync(Guid adminId, Guid requestId, string reason, CancellationToken ct);
}
```

- [ ] **Step 7: Create `OperatorRequestService.cs`**

```csharp
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Infrastructure.Errors;
using BusBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Api.Services;

public class OperatorRequestService : IOperatorRequestService
{
    private readonly AppDbContext _db;
    private readonly IAuditLogWriter _audit;
    private readonly INotificationSender _notifier;

    public OperatorRequestService(AppDbContext db, IAuditLogWriter audit, INotificationSender notifier)
    {
        _db = db;
        _audit = audit;
        _notifier = notifier;
    }

    public async Task<OperatorRequestDto> SubmitAsync(Guid userId, BecomeOperatorRequest body, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new UnauthorizedException("UNAUTHORIZED", "User no longer exists");

        if (user.Roles.Any(r => r.Role == Roles.Operator))
            throw new BusinessRuleException("ALREADY_OPERATOR", "User already has the operator role");

        var pending = await _db.OperatorRequests
            .AnyAsync(r => r.UserId == userId && r.Status == OperatorRequestStatus.Pending, ct);
        if (pending)
            throw new BusinessRuleException("REQUEST_ALREADY_PENDING", "You already have a pending operator request");

        var req = new OperatorRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = OperatorRequestStatus.Pending,
            CompanyName = body.CompanyName.Trim(),
            RequestedAt = DateTime.UtcNow
        };
        _db.OperatorRequests.Add(req);
        await _db.SaveChangesAsync(ct);

        return ToDto(req, user);
    }

    public async Task<IReadOnlyList<OperatorRequestDto>> ListAsync(string? status, CancellationToken ct)
    {
        var query = _db.OperatorRequests
            .AsNoTracking()
            .Include(r => r.User)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            if (!OperatorRequestStatus.All.Contains(status))
                throw new BusinessRuleException("INVALID_STATUS", "Unknown status filter");
            query = query.Where(r => r.Status == status);
        }

        var rows = await query
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync(ct);

        return rows.Select(r => ToDto(r, r.User!)).ToList();
    }

    public async Task<OperatorRequestDto> ApproveAsync(Guid adminId, Guid requestId, CancellationToken ct)
    {
        var req = await _db.OperatorRequests
            .Include(r => r.User).ThenInclude(u => u!.Roles)
            .FirstOrDefaultAsync(r => r.Id == requestId, ct)
            ?? throw new NotFoundException("Operator request not found");

        if (req.Status != OperatorRequestStatus.Pending)
            throw new BusinessRuleException("REQUEST_NOT_PENDING", "Request is not pending");

        req.Status = OperatorRequestStatus.Approved;
        req.ReviewedAt = DateTime.UtcNow;
        req.ReviewedByAdminId = adminId;

        var user = req.User!;
        if (!user.Roles.Any(r => r.Role == Roles.Operator))
            user.Roles.Add(new UserRole { UserId = user.Id, Role = Roles.Operator });

        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(adminId, AuditAction.OperatorRequestApproved,
            "operator_request", req.Id, new { req.CompanyName }, ct);
        await _notifier.SendOperatorApprovedAsync(user, ct);

        return ToDto(req, user);
    }

    public async Task<OperatorRequestDto> RejectAsync(Guid adminId, Guid requestId, string reason, CancellationToken ct)
    {
        var req = await _db.OperatorRequests
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == requestId, ct)
            ?? throw new NotFoundException("Operator request not found");

        if (req.Status != OperatorRequestStatus.Pending)
            throw new BusinessRuleException("REQUEST_NOT_PENDING", "Request is not pending");

        req.Status = OperatorRequestStatus.Rejected;
        req.ReviewedAt = DateTime.UtcNow;
        req.ReviewedByAdminId = adminId;
        req.RejectReason = reason.Trim();

        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(adminId, AuditAction.OperatorRequestRejected,
            "operator_request", req.Id, new { reason = req.RejectReason }, ct);
        await _notifier.SendOperatorRejectedAsync(req.User!, req.RejectReason, ct);

        return ToDto(req, req.User!);
    }

    private static OperatorRequestDto ToDto(OperatorRequest r, User u) => new(
        r.Id, r.UserId, u.Email, u.Name, r.CompanyName, r.Status,
        r.RequestedAt, r.ReviewedAt, r.ReviewedByAdminId, r.RejectReason);
}
```

- [ ] **Step 8: Create `BecomeOperatorController.cs`**

```csharp
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure.Auth;
using BusBooking.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.Api.Controllers;

[ApiController]
[Route("api/v1/me/become-operator")]
[Authorize(Roles = "customer")]
public class BecomeOperatorController : ControllerBase
{
    private readonly IOperatorRequestService _requests;
    private readonly ICurrentUserAccessor _me;

    public BecomeOperatorController(IOperatorRequestService requests, ICurrentUserAccessor me)
    {
        _requests = requests;
        _me = me;
    }

    [HttpPost]
    public async Task<ActionResult<OperatorRequestDto>> Submit(
        [FromBody] BecomeOperatorRequest body,
        [FromServices] IValidator<BecomeOperatorRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(body, ct);
        var dto = await _requests.SubmitAsync(_me.UserId, body, ct);
        return StatusCode(StatusCodes.Status201Created, dto);
    }
}
```

- [ ] **Step 9: Create `AdminOperatorRequestsController.cs`**

```csharp
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure.Auth;
using BusBooking.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.Api.Controllers;

[ApiController]
[Route("api/v1/admin/operator-requests")]
[Authorize(Roles = "admin")]
public class AdminOperatorRequestsController : ControllerBase
{
    private readonly IOperatorRequestService _requests;
    private readonly ICurrentUserAccessor _me;

    public AdminOperatorRequestsController(IOperatorRequestService requests, ICurrentUserAccessor me)
    {
        _requests = requests;
        _me = me;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OperatorRequestDto>>> List(
        [FromQuery] string? status, CancellationToken ct)
        => Ok(await _requests.ListAsync(status, ct));

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<OperatorRequestDto>> Approve(Guid id, CancellationToken ct)
        => Ok(await _requests.ApproveAsync(_me.UserId, id, ct));

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<OperatorRequestDto>> Reject(
        Guid id,
        [FromBody] RejectOperatorRequest body,
        [FromServices] IValidator<RejectOperatorRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(body, ct);
        return Ok(await _requests.RejectAsync(_me.UserId, id, body.Reason, ct));
    }
}
```

- [ ] **Step 10: Register the service in `Program.cs`**

Under the block of `AddScoped` service registrations (next to `ICityService`), add:

```csharp
builder.Services.AddScoped<IOperatorRequestService, OperatorRequestService>();
```

- [ ] **Step 11: Build**

```bash
cd backend/BusBooking.Api && dotnet build
```

Expected: `Build succeeded.`

- [ ] **Step 12: Commit**

```bash
git add backend/BusBooking.Api/Dtos/BecomeOperatorRequest.cs \
        backend/BusBooking.Api/Dtos/OperatorRequestDto.cs \
        backend/BusBooking.Api/Dtos/RejectOperatorRequest.cs \
        backend/BusBooking.Api/Validators/BecomeOperatorRequestValidator.cs \
        backend/BusBooking.Api/Validators/RejectOperatorRequestValidator.cs \
        backend/BusBooking.Api/Services/IOperatorRequestService.cs \
        backend/BusBooking.Api/Services/OperatorRequestService.cs \
        backend/BusBooking.Api/Controllers/BecomeOperatorController.cs \
        backend/BusBooking.Api/Controllers/AdminOperatorRequestsController.cs \
        backend/BusBooking.Api/Program.cs
git commit -m "feat(backend): operator-request submit + admin approve/reject"
```

---

## Task 7: Integration tests for operator-request flow

**Files:**
- Create: `backend/BusBooking.Api.Tests/Integration/BecomeOperatorTests.cs`
- Create: `backend/BusBooking.Api.Tests/Integration/AdminOperatorRequestsTests.cs`

- [ ] **Step 1: Create `BecomeOperatorTests.cs`**

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

namespace BusBooking.Api.Tests.Integration;

public class BecomeOperatorTests : IClassFixture<IntegrationFixture>, IAsyncLifetime
{
    private readonly IntegrationFixture _fx;

    public BecomeOperatorTests(IntegrationFixture fx) => _fx = fx;

    public async Task InitializeAsync() => await _fx.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Customer_can_submit_operator_request()
    {
        var (_, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        var client = _fx.CreateClient();
        client.AttachBearer(token);

        var resp = await client.PostAsJsonAsync(
            "/api/v1/me/become-operator",
            new BecomeOperatorRequest { CompanyName = "Shakti Travels" });

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await resp.Content.ReadFromJsonAsync<OperatorRequestDto>();
        dto!.Status.Should().Be(OperatorRequestStatus.Pending);
        dto.CompanyName.Should().Be("Shakti Travels");
    }

    [Fact]
    public async Task Second_pending_request_returns_422()
    {
        var (_, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        var client = _fx.CreateClient();
        client.AttachBearer(token);

        var first = await client.PostAsJsonAsync("/api/v1/me/become-operator",
            new BecomeOperatorRequest { CompanyName = "First Co" });
        first.EnsureSuccessStatusCode();

        var second = await client.PostAsJsonAsync("/api/v1/me/become-operator",
            new BecomeOperatorRequest { CompanyName = "Second Co" });

        second.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await second.Content.ReadAsStringAsync();
        body.Should().Contain("REQUEST_ALREADY_PENDING");
    }

    [Fact]
    public async Task Already_operator_returns_422()
    {
        var (_, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer, Roles.Operator]);
        var client = _fx.CreateClient();
        client.AttachBearer(token);

        var resp = await client.PostAsJsonAsync("/api/v1/me/become-operator",
            new BecomeOperatorRequest { CompanyName = "Doesn't matter" });

        resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await resp.Content.ReadAsStringAsync()).Should().Contain("ALREADY_OPERATOR");
    }

    [Fact]
    public async Task Anonymous_request_is_401()
    {
        var client = _fx.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/v1/me/become-operator",
            new BecomeOperatorRequest { CompanyName = "Anon" });
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Admin_only_token_is_403()
    {
        var (_, token) = await AdminTokenFactory.CreateAdminAsync(_fx, email: "x-admin@t.local");
        var client = _fx.CreateClient();
        client.AttachAdminBearer(token);

        var resp = await client.PostAsJsonAsync("/api/v1/me/become-operator",
            new BecomeOperatorRequest { CompanyName = "No" });

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
```

- [ ] **Step 2: Create `AdminOperatorRequestsTests.cs`**

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

namespace BusBooking.Api.Tests.Integration;

public class AdminOperatorRequestsTests : IClassFixture<IntegrationFixture>, IAsyncLifetime
{
    private readonly IntegrationFixture _fx;

    public AdminOperatorRequestsTests(IntegrationFixture fx) => _fx = fx;

    public async Task InitializeAsync() => await _fx.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<Guid> SeedPendingRequestAsync(string email = "cust@t.local")
    {
        var (customer, _) = await OperatorTokenFactory.CreateAsync(
            _fx, [Roles.Customer], email: email);

        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var req = new OperatorRequest
        {
            Id = Guid.NewGuid(),
            UserId = customer.Id,
            Status = OperatorRequestStatus.Pending,
            CompanyName = "Test Co",
            RequestedAt = DateTime.UtcNow
        };
        db.OperatorRequests.Add(req);
        await db.SaveChangesAsync();
        return req.Id;
    }

    [Fact]
    public async Task List_returns_all_by_default()
    {
        await SeedPendingRequestAsync("a@t.local");
        await SeedPendingRequestAsync("b@t.local");

        var (_, token) = await AdminTokenFactory.CreateAdminAsync(_fx, email: "admin-list@t.local");
        var client = _fx.CreateClient();
        client.AttachAdminBearer(token);

        var resp = await client.GetAsync("/api/v1/admin/operator-requests");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await resp.Content.ReadFromJsonAsync<List<OperatorRequestDto>>();
        list!.Count.Should().Be(2);
    }

    [Fact]
    public async Task List_filters_by_status()
    {
        var reqId = await SeedPendingRequestAsync();
        var (_, token) = await AdminTokenFactory.CreateAdminAsync(_fx, email: "admin-f@t.local");
        var client = _fx.CreateClient();
        client.AttachAdminBearer(token);

        var resp = await client.GetAsync("/api/v1/admin/operator-requests?status=pending");
        var list = await resp.Content.ReadFromJsonAsync<List<OperatorRequestDto>>();
        list!.Should().ContainSingle(x => x.Id == reqId);

        var none = await client.GetAsync("/api/v1/admin/operator-requests?status=approved");
        (await none.Content.ReadFromJsonAsync<List<OperatorRequestDto>>())!.Should().BeEmpty();
    }

    [Fact]
    public async Task Approve_grants_operator_role_and_writes_audit()
    {
        var reqId = await SeedPendingRequestAsync("approved@t.local");

        var (admin, token) = await AdminTokenFactory.CreateAdminAsync(_fx, email: "admin-app@t.local");
        var client = _fx.CreateClient();
        client.AttachAdminBearer(token);

        var resp = await client.PostAsync($"/api/v1/admin/operator-requests/{reqId}/approve", null);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updated = await db.OperatorRequests.FirstAsync(r => r.Id == reqId);
        updated.Status.Should().Be(OperatorRequestStatus.Approved);
        updated.ReviewedByAdminId.Should().Be(admin.Id);

        var roles = await db.UserRoles.Where(r => r.UserId == updated.UserId).Select(r => r.Role).ToListAsync();
        roles.Should().Contain(Roles.Operator);

        var audit = await db.AuditLog.FirstOrDefaultAsync(a =>
            a.Action == AuditAction.OperatorRequestApproved && a.TargetId == reqId);
        audit.Should().NotBeNull();
    }

    [Fact]
    public async Task Reject_stores_reason_and_does_not_grant_role()
    {
        var reqId = await SeedPendingRequestAsync("rejected@t.local");

        var (_, token) = await AdminTokenFactory.CreateAdminAsync(_fx, email: "admin-r@t.local");
        var client = _fx.CreateClient();
        client.AttachAdminBearer(token);

        var resp = await client.PostAsJsonAsync(
            $"/api/v1/admin/operator-requests/{reqId}/reject",
            new RejectOperatorRequest { Reason = "Missing documents" });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updated = await db.OperatorRequests.FirstAsync(r => r.Id == reqId);
        updated.Status.Should().Be(OperatorRequestStatus.Rejected);
        updated.RejectReason.Should().Be("Missing documents");

        var roles = await db.UserRoles.Where(r => r.UserId == updated.UserId).Select(r => r.Role).ToListAsync();
        roles.Should().NotContain(Roles.Operator);
    }

    [Fact]
    public async Task Non_admin_token_is_403()
    {
        var reqId = await SeedPendingRequestAsync();
        var (_, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        var client = _fx.CreateClient();
        client.AttachBearer(token);

        var resp = await client.PostAsync($"/api/v1/admin/operator-requests/{reqId}/approve", null);
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
```

- [ ] **Step 3: Run the tests**

```bash
cd backend/BusBooking.Api.Tests
DOTNET_ROLL_FORWARD=Major dotnet test --filter "FullyQualifiedName~BecomeOperator|FullyQualifiedName~AdminOperatorRequests"
```

Expected: all pass.

- [ ] **Step 4: Commit**

```bash
git add backend/BusBooking.Api.Tests/Integration/BecomeOperatorTests.cs \
        backend/BusBooking.Api.Tests/Integration/AdminOperatorRequestsTests.cs
git commit -m "test(backend): integration tests for operator-request flow"
```

---

## Task 8: Operator-offices service + controller + tests

**Files:**
- Create: `backend/BusBooking.Api/Dtos/OperatorOfficeDto.cs`
- Create: `backend/BusBooking.Api/Dtos/CreateOperatorOfficeRequest.cs`
- Create: `backend/BusBooking.Api/Validators/CreateOperatorOfficeRequestValidator.cs`
- Create: `backend/BusBooking.Api/Services/IOperatorOfficeService.cs`
- Create: `backend/BusBooking.Api/Services/OperatorOfficeService.cs`
- Create: `backend/BusBooking.Api/Controllers/OperatorOfficesController.cs`
- Create: `backend/BusBooking.Api.Tests/Integration/OperatorOfficesTests.cs`
- Modify: `backend/BusBooking.Api/Program.cs`

- [ ] **Step 1: Create `OperatorOfficeDto.cs`**

```csharp
namespace BusBooking.Api.Dtos;

public record OperatorOfficeDto(
    Guid Id,
    Guid CityId,
    string CityName,
    string AddressLine,
    string Phone,
    bool IsActive);
```

- [ ] **Step 2: Create `CreateOperatorOfficeRequest.cs`**

```csharp
namespace BusBooking.Api.Dtos;

public class CreateOperatorOfficeRequest
{
    public required Guid CityId { get; set; }
    public required string AddressLine { get; set; }
    public required string Phone { get; set; }
}
```

- [ ] **Step 3: Create `CreateOperatorOfficeRequestValidator.cs`**

```csharp
using BusBooking.Api.Dtos;
using FluentValidation;

namespace BusBooking.Api.Validators;

public class CreateOperatorOfficeRequestValidator : AbstractValidator<CreateOperatorOfficeRequest>
{
    public CreateOperatorOfficeRequestValidator()
    {
        RuleFor(r => r.CityId).NotEmpty();
        RuleFor(r => r.AddressLine).NotEmpty().MinimumLength(5).MaximumLength(300);
        RuleFor(r => r.Phone).NotEmpty().MinimumLength(6).MaximumLength(32);
    }
}
```

- [ ] **Step 4: Create `IOperatorOfficeService.cs`**

```csharp
using BusBooking.Api.Dtos;

namespace BusBooking.Api.Services;

public interface IOperatorOfficeService
{
    Task<IReadOnlyList<OperatorOfficeDto>> ListAsync(Guid operatorUserId, CancellationToken ct);
    Task<OperatorOfficeDto> CreateAsync(Guid operatorUserId, CreateOperatorOfficeRequest body, CancellationToken ct);
    Task DeleteAsync(Guid operatorUserId, Guid officeId, CancellationToken ct);
}
```

- [ ] **Step 5: Create `OperatorOfficeService.cs`**

```csharp
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Infrastructure.Errors;
using BusBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Api.Services;

public class OperatorOfficeService : IOperatorOfficeService
{
    private readonly AppDbContext _db;
    private readonly IAuditLogWriter _audit;

    public OperatorOfficeService(AppDbContext db, IAuditLogWriter audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<IReadOnlyList<OperatorOfficeDto>> ListAsync(Guid operatorUserId, CancellationToken ct)
    {
        return await _db.OperatorOffices
            .AsNoTracking()
            .Where(o => o.OperatorUserId == operatorUserId && o.IsActive)
            .Include(o => o.City)
            .OrderBy(o => o.City!.Name)
            .Select(o => new OperatorOfficeDto(
                o.Id, o.CityId, o.City!.Name, o.AddressLine, o.Phone, o.IsActive))
            .ToListAsync(ct);
    }

    public async Task<OperatorOfficeDto> CreateAsync(
        Guid operatorUserId, CreateOperatorOfficeRequest body, CancellationToken ct)
    {
        var city = await _db.Cities.FirstOrDefaultAsync(c => c.Id == body.CityId, ct)
            ?? throw new NotFoundException("City not found");
        if (!city.IsActive)
            throw new BusinessRuleException("CITY_INACTIVE", "City is not active");

        var existing = await _db.OperatorOffices
            .FirstOrDefaultAsync(o => o.OperatorUserId == operatorUserId && o.CityId == body.CityId, ct);
        if (existing != null)
        {
            if (existing.IsActive)
                throw new ConflictException("OFFICE_ALREADY_EXISTS",
                    "An office for this city already exists");
            existing.IsActive = true;
            existing.AddressLine = body.AddressLine.Trim();
            existing.Phone = body.Phone.Trim();
            await _db.SaveChangesAsync(ct);
            await _audit.WriteAsync(operatorUserId, AuditAction.OperatorOfficeCreated,
                "operator_office", existing.Id, new { cityId = city.Id, reactivated = true }, ct);
            return new OperatorOfficeDto(existing.Id, city.Id, city.Name,
                existing.AddressLine, existing.Phone, existing.IsActive);
        }

        var office = new OperatorOffice
        {
            Id = Guid.NewGuid(),
            OperatorUserId = operatorUserId,
            CityId = body.CityId,
            AddressLine = body.AddressLine.Trim(),
            Phone = body.Phone.Trim(),
            IsActive = true
        };
        _db.OperatorOffices.Add(office);
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(operatorUserId, AuditAction.OperatorOfficeCreated,
            "operator_office", office.Id, new { cityId = city.Id }, ct);

        return new OperatorOfficeDto(office.Id, city.Id, city.Name,
            office.AddressLine, office.Phone, office.IsActive);
    }

    public async Task DeleteAsync(Guid operatorUserId, Guid officeId, CancellationToken ct)
    {
        var office = await _db.OperatorOffices.FirstOrDefaultAsync(o => o.Id == officeId, ct)
            ?? throw new NotFoundException("Office not found");
        if (office.OperatorUserId != operatorUserId)
            throw new ForbiddenException("Cannot delete another operator's office");

        office.IsActive = false;
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(operatorUserId, AuditAction.OperatorOfficeDeleted,
            "operator_office", office.Id, null, ct);
    }
}
```

- [ ] **Step 6: Create `OperatorOfficesController.cs`**

```csharp
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure.Auth;
using BusBooking.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.Api.Controllers;

[ApiController]
[Route("api/v1/operator/offices")]
[Authorize(Roles = "operator")]
public class OperatorOfficesController : ControllerBase
{
    private readonly IOperatorOfficeService _offices;
    private readonly ICurrentUserAccessor _me;

    public OperatorOfficesController(IOperatorOfficeService offices, ICurrentUserAccessor me)
    {
        _offices = offices;
        _me = me;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OperatorOfficeDto>>> List(CancellationToken ct)
        => Ok(await _offices.ListAsync(_me.UserId, ct));

    [HttpPost]
    public async Task<ActionResult<OperatorOfficeDto>> Create(
        [FromBody] CreateOperatorOfficeRequest body,
        [FromServices] IValidator<CreateOperatorOfficeRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(body, ct);
        var dto = await _offices.CreateAsync(_me.UserId, body, ct);
        return StatusCode(StatusCodes.Status201Created, dto);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _offices.DeleteAsync(_me.UserId, id, ct);
        return NoContent();
    }
}
```

- [ ] **Step 7: Register in `Program.cs`**

Add next to the `OperatorRequestService` registration:

```csharp
builder.Services.AddScoped<IOperatorOfficeService, OperatorOfficeService>();
```

- [ ] **Step 8: Create `OperatorOfficesTests.cs`**

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

namespace BusBooking.Api.Tests.Integration;

public class OperatorOfficesTests : IClassFixture<IntegrationFixture>, IAsyncLifetime
{
    private readonly IntegrationFixture _fx;

    public OperatorOfficesTests(IntegrationFixture fx) => _fx = fx;

    public async Task InitializeAsync() => await _fx.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<Guid> SeedCityAsync(string name = "Coimbatore")
    {
        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var c = new City { Id = Guid.NewGuid(), Name = name, State = "Tamil Nadu", IsActive = true };
        db.Cities.Add(c);
        await db.SaveChangesAsync();
        return c.Id;
    }

    [Fact]
    public async Task Operator_can_create_list_delete_office()
    {
        var cityId = await SeedCityAsync();
        var (_, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Operator]);
        var client = _fx.CreateClient();
        client.AttachBearer(token);

        var create = await client.PostAsJsonAsync("/api/v1/operator/offices",
            new CreateOperatorOfficeRequest
            {
                CityId = cityId,
                AddressLine = "12 MG Road",
                Phone = "+91-98000-11111"
            });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await create.Content.ReadFromJsonAsync<OperatorOfficeDto>();
        dto!.CityId.Should().Be(cityId);
        dto.IsActive.Should().BeTrue();

        var list = await client.GetAsync("/api/v1/operator/offices");
        (await list.Content.ReadFromJsonAsync<List<OperatorOfficeDto>>())!.Should().HaveCount(1);

        var del = await client.DeleteAsync($"/api/v1/operator/offices/{dto.Id}");
        del.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var list2 = await client.GetAsync("/api/v1/operator/offices");
        (await list2.Content.ReadFromJsonAsync<List<OperatorOfficeDto>>())!.Should().BeEmpty();
    }

    [Fact]
    public async Task Duplicate_city_for_same_operator_returns_409()
    {
        var cityId = await SeedCityAsync();
        var (_, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Operator]);
        var client = _fx.CreateClient();
        client.AttachBearer(token);

        var body = new CreateOperatorOfficeRequest
        {
            CityId = cityId,
            AddressLine = "12 MG Road",
            Phone = "+91-98000-11111"
        };
        (await client.PostAsJsonAsync("/api/v1/operator/offices", body)).EnsureSuccessStatusCode();

        var dup = await client.PostAsJsonAsync("/api/v1/operator/offices", body);
        dup.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await dup.Content.ReadAsStringAsync()).Should().Contain("OFFICE_ALREADY_EXISTS");
    }

    [Fact]
    public async Task Unknown_city_returns_404()
    {
        var (_, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Operator]);
        var client = _fx.CreateClient();
        client.AttachBearer(token);

        var resp = await client.PostAsJsonAsync("/api/v1/operator/offices",
            new CreateOperatorOfficeRequest
            {
                CityId = Guid.NewGuid(),
                AddressLine = "Nowhere",
                Phone = "+91-0000000000"
            });
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Customer_only_token_is_403()
    {
        var cityId = await SeedCityAsync();
        var (_, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        var client = _fx.CreateClient();
        client.AttachBearer(token);

        var resp = await client.PostAsJsonAsync("/api/v1/operator/offices",
            new CreateOperatorOfficeRequest
            {
                CityId = cityId,
                AddressLine = "12 MG Road",
                Phone = "+91-98000-11111"
            });
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Deleting_another_operators_office_is_403()
    {
        var cityId = await SeedCityAsync();
        var (_, tokenA) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Operator], email: "a@t.local");
        var (_, tokenB) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Operator], email: "b@t.local");

        var clientA = _fx.CreateClient(); clientA.AttachBearer(tokenA);
        var create = await clientA.PostAsJsonAsync("/api/v1/operator/offices",
            new CreateOperatorOfficeRequest
            {
                CityId = cityId,
                AddressLine = "12 MG Road",
                Phone = "+91-98000-11111"
            });
        create.EnsureSuccessStatusCode();
        var dto = await create.Content.ReadFromJsonAsync<OperatorOfficeDto>();

        var clientB = _fx.CreateClient(); clientB.AttachBearer(tokenB);
        var resp = await clientB.DeleteAsync($"/api/v1/operator/offices/{dto!.Id}");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
```

- [ ] **Step 9: Build + run**

```bash
cd backend/BusBooking.Api && dotnet build
cd ../BusBooking.Api.Tests
DOTNET_ROLL_FORWARD=Major dotnet test --filter "FullyQualifiedName~OperatorOffices"
```

Expected: all pass.

- [ ] **Step 10: Commit**

```bash
git add backend/BusBooking.Api/Dtos/OperatorOfficeDto.cs \
        backend/BusBooking.Api/Dtos/CreateOperatorOfficeRequest.cs \
        backend/BusBooking.Api/Validators/CreateOperatorOfficeRequestValidator.cs \
        backend/BusBooking.Api/Services/IOperatorOfficeService.cs \
        backend/BusBooking.Api/Services/OperatorOfficeService.cs \
        backend/BusBooking.Api/Controllers/OperatorOfficesController.cs \
        backend/BusBooking.Api/Program.cs \
        backend/BusBooking.Api.Tests/Integration/OperatorOfficesTests.cs
git commit -m "feat(backend): operator offices CRUD + tests"
```

---

## Task 9: Seat-number generator (pure function) + unit tests

Seat generation is the one piece of non-trivial logic in M3, so we extract it and unit-test it first. Convention: rows are letters (`A`, `B`, …), columns are 1-indexed integers, so a 3 × 4 bus produces `A1 A2 A3 A4 B1 B2 B3 B4 C1 C2 C3 C4`.

**Files:**
- Create: `backend/BusBooking.Api/Services/SeatLayoutGenerator.cs`
- Create: `backend/BusBooking.Api.Tests/Unit/SeatLayoutGeneratorTests.cs`

- [ ] **Step 1: Create `SeatLayoutGenerator.cs`**

```csharp
using BusBooking.Api.Models;

namespace BusBooking.Api.Services;

public static class SeatLayoutGenerator
{
    public const int MaxRows = 26;     // A..Z
    public const int MaxColumns = 12;

    public static IReadOnlyList<SeatDefinition> Generate(Guid busId, int rows, int columns)
    {
        if (rows < 1 || rows > MaxRows)
            throw new ArgumentOutOfRangeException(nameof(rows),
                $"Rows must be between 1 and {MaxRows}");
        if (columns < 1 || columns > MaxColumns)
            throw new ArgumentOutOfRangeException(nameof(columns),
                $"Columns must be between 1 and {MaxColumns}");

        var seats = new List<SeatDefinition>(rows * columns);
        for (var r = 0; r < rows; r++)
        {
            var rowLetter = (char)('A' + r);
            for (var c = 0; c < columns; c++)
            {
                seats.Add(new SeatDefinition
                {
                    Id = Guid.NewGuid(),
                    BusId = busId,
                    SeatNumber = $"{rowLetter}{c + 1}",
                    RowIndex = r,
                    ColumnIndex = c,
                    SeatCategory = SeatCategory.Regular
                });
            }
        }
        return seats;
    }
}
```

- [ ] **Step 2: Create `SeatLayoutGeneratorTests.cs`**

```csharp
using BusBooking.Api.Services;
using FluentAssertions;

namespace BusBooking.Api.Tests.Unit;

public class SeatLayoutGeneratorTests
{
    [Fact]
    public void Three_by_four_produces_12_seats_with_expected_labels()
    {
        var busId = Guid.NewGuid();
        var seats = SeatLayoutGenerator.Generate(busId, rows: 3, columns: 4);

        seats.Should().HaveCount(12);
        seats.Select(s => s.SeatNumber).Should().ContainInOrder(
            "A1", "A2", "A3", "A4",
            "B1", "B2", "B3", "B4",
            "C1", "C2", "C3", "C4");
        seats.Should().OnlyContain(s => s.BusId == busId);
    }

    [Theory]
    [InlineData(0, 4)]
    [InlineData(1, 0)]
    [InlineData(27, 2)]
    [InlineData(2, 13)]
    public void Out_of_range_dimensions_throw(int rows, int columns)
    {
        Action act = () => SeatLayoutGenerator.Generate(Guid.NewGuid(), rows, columns);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Seat_categories_default_to_regular()
    {
        var seats = SeatLayoutGenerator.Generate(Guid.NewGuid(), 1, 2);
        seats.Should().OnlyContain(s => s.SeatCategory == "regular");
    }
}
```

- [ ] **Step 3: Run the unit tests**

```bash
cd backend/BusBooking.Api.Tests
DOTNET_ROLL_FORWARD=Major dotnet test --filter "FullyQualifiedName~SeatLayoutGenerator"
```

Expected: 5 tests, all pass.

- [ ] **Step 4: Commit**

```bash
git add backend/BusBooking.Api/Services/SeatLayoutGenerator.cs \
        backend/BusBooking.Api.Tests/Unit/SeatLayoutGeneratorTests.cs
git commit -m "feat(backend): seat-layout generator + unit tests"
```

---

## Task 10: `IBusService` + `/operator/buses` endpoints + tests

**Files:**
- Create: `backend/BusBooking.Api/Dtos/BusDto.cs`
- Create: `backend/BusBooking.Api/Dtos/CreateBusRequest.cs`
- Create: `backend/BusBooking.Api/Dtos/UpdateBusStatusRequest.cs`
- Create: `backend/BusBooking.Api/Validators/CreateBusRequestValidator.cs`
- Create: `backend/BusBooking.Api/Validators/UpdateBusStatusRequestValidator.cs`
- Create: `backend/BusBooking.Api/Services/IBusService.cs`
- Create: `backend/BusBooking.Api/Services/BusService.cs`
- Create: `backend/BusBooking.Api/Controllers/OperatorBusesController.cs`
- Create: `backend/BusBooking.Api.Tests/Integration/OperatorBusesTests.cs`
- Modify: `backend/BusBooking.Api/Program.cs`

- [ ] **Step 1: Create `BusDto.cs`**

```csharp
namespace BusBooking.Api.Dtos;

public record BusDto(
    Guid Id,
    Guid OperatorUserId,
    string RegistrationNumber,
    string BusName,
    string BusType,
    int Capacity,
    string ApprovalStatus,
    string OperationalStatus,
    DateTime CreatedAt,
    DateTime? ApprovedAt,
    string? RejectReason);
```

- [ ] **Step 2: Create `CreateBusRequest.cs`**

```csharp
namespace BusBooking.Api.Dtos;

public class CreateBusRequest
{
    public required string RegistrationNumber { get; set; }
    public required string BusName { get; set; }
    public required string BusType { get; set; }
    public int Rows { get; set; }
    public int Columns { get; set; }
}
```

- [ ] **Step 3: Create `UpdateBusStatusRequest.cs`**

```csharp
namespace BusBooking.Api.Dtos;

public class UpdateBusStatusRequest
{
    public required string OperationalStatus { get; set; }
}
```

- [ ] **Step 4: Create `CreateBusRequestValidator.cs`**

```csharp
using BusBooking.Api.Dtos;
using BusBooking.Api.Models;
using BusBooking.Api.Services;
using FluentValidation;

namespace BusBooking.Api.Validators;

public class CreateBusRequestValidator : AbstractValidator<CreateBusRequest>
{
    public CreateBusRequestValidator()
    {
        RuleFor(r => r.RegistrationNumber).NotEmpty().MinimumLength(4).MaximumLength(32);
        RuleFor(r => r.BusName).NotEmpty().MinimumLength(2).MaximumLength(120);
        RuleFor(r => r.BusType).NotEmpty().Must(t => BusType.All.Contains(t))
            .WithMessage($"BusType must be one of: {string.Join(", ", BusType.All)}");
        RuleFor(r => r.Rows).InclusiveBetween(1, SeatLayoutGenerator.MaxRows);
        RuleFor(r => r.Columns).InclusiveBetween(1, SeatLayoutGenerator.MaxColumns);
    }
}
```

- [ ] **Step 5: Create `UpdateBusStatusRequestValidator.cs`**

```csharp
using BusBooking.Api.Dtos;
using BusBooking.Api.Models;
using FluentValidation;

namespace BusBooking.Api.Validators;

public class UpdateBusStatusRequestValidator : AbstractValidator<UpdateBusStatusRequest>
{
    public UpdateBusStatusRequestValidator()
    {
        RuleFor(r => r.OperationalStatus)
            .NotEmpty()
            .Must(s => s == BusOperationalStatus.Active || s == BusOperationalStatus.UnderMaintenance)
            .WithMessage("OperationalStatus must be 'active' or 'under_maintenance'");
    }
}
```

- [ ] **Step 6: Create `IBusService.cs`**

```csharp
using BusBooking.Api.Dtos;

namespace BusBooking.Api.Services;

public interface IBusService
{
    Task<IReadOnlyList<BusDto>> ListForOperatorAsync(Guid operatorUserId, CancellationToken ct);
    Task<BusDto> CreateAsync(Guid operatorUserId, CreateBusRequest body, CancellationToken ct);
    Task<BusDto> UpdateOperationalStatusAsync(Guid operatorUserId, Guid busId, string newStatus, CancellationToken ct);
    Task<BusDto> RetireAsync(Guid operatorUserId, Guid busId, CancellationToken ct);

    Task<IReadOnlyList<BusDto>> ListByApprovalStatusAsync(string? status, CancellationToken ct);
    Task<BusDto> ApproveAsync(Guid adminId, Guid busId, CancellationToken ct);
    Task<BusDto> RejectAsync(Guid adminId, Guid busId, string reason, CancellationToken ct);
}
```

- [ ] **Step 7: Create `BusService.cs`**

```csharp
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Infrastructure.Errors;
using BusBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Api.Services;

public class BusService : IBusService
{
    private readonly AppDbContext _db;
    private readonly IAuditLogWriter _audit;
    private readonly INotificationSender _notifier;

    public BusService(AppDbContext db, IAuditLogWriter audit, INotificationSender notifier)
    {
        _db = db;
        _audit = audit;
        _notifier = notifier;
    }

    public async Task<IReadOnlyList<BusDto>> ListForOperatorAsync(Guid operatorUserId, CancellationToken ct)
    {
        return await _db.Buses.AsNoTracking()
            .Where(b => b.OperatorUserId == operatorUserId)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => ToDto(b))
            .ToListAsync(ct);
    }

    public async Task<BusDto> CreateAsync(Guid operatorUserId, CreateBusRequest body, CancellationToken ct)
    {
        var reg = body.RegistrationNumber.Trim();
        if (await _db.Buses.AnyAsync(b => b.RegistrationNumber == reg, ct))
            throw new ConflictException("REGISTRATION_TAKEN",
                "A bus with that registration number already exists");

        var bus = new Bus
        {
            Id = Guid.NewGuid(),
            OperatorUserId = operatorUserId,
            RegistrationNumber = reg,
            BusName = body.BusName.Trim(),
            BusType = body.BusType,
            Capacity = body.Rows * body.Columns,
            ApprovalStatus = BusApprovalStatus.Pending,
            OperationalStatus = BusOperationalStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        var seats = SeatLayoutGenerator.Generate(bus.Id, body.Rows, body.Columns);
        foreach (var s in seats) bus.Seats.Add(s);

        _db.Buses.Add(bus);
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(operatorUserId, AuditAction.BusCreated,
            "bus", bus.Id,
            new { bus.RegistrationNumber, bus.BusName, bus.BusType, bus.Capacity }, ct);

        return ToDto(bus);
    }

    public async Task<BusDto> UpdateOperationalStatusAsync(
        Guid operatorUserId, Guid busId, string newStatus, CancellationToken ct)
    {
        var bus = await _db.Buses.FirstOrDefaultAsync(b => b.Id == busId, ct)
            ?? throw new NotFoundException("Bus not found");
        if (bus.OperatorUserId != operatorUserId)
            throw new ForbiddenException("Cannot modify another operator's bus");
        if (bus.OperationalStatus == BusOperationalStatus.Retired)
            throw new BusinessRuleException("BUS_RETIRED", "Retired buses cannot change status");

        bus.OperationalStatus = newStatus;
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(operatorUserId, AuditAction.BusStatusChanged,
            "bus", bus.Id, new { newStatus }, ct);

        return ToDto(bus);
    }

    public async Task<BusDto> RetireAsync(Guid operatorUserId, Guid busId, CancellationToken ct)
    {
        var bus = await _db.Buses.FirstOrDefaultAsync(b => b.Id == busId, ct)
            ?? throw new NotFoundException("Bus not found");
        if (bus.OperatorUserId != operatorUserId)
            throw new ForbiddenException("Cannot retire another operator's bus");

        // Spec §5.3: "Blocked if future bookings exist." Bookings do not exist until M5;
        // once they do, this is the place to add the guard (query bookings → 422 BUS_HAS_BOOKINGS).

        bus.OperationalStatus = BusOperationalStatus.Retired;
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(operatorUserId, AuditAction.BusStatusChanged,
            "bus", bus.Id, new { newStatus = BusOperationalStatus.Retired }, ct);

        return ToDto(bus);
    }

    public async Task<IReadOnlyList<BusDto>> ListByApprovalStatusAsync(string? status, CancellationToken ct)
    {
        var query = _db.Buses.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(status))
        {
            if (!BusApprovalStatus.All.Contains(status))
                throw new BusinessRuleException("INVALID_STATUS", "Unknown approval status filter");
            query = query.Where(b => b.ApprovalStatus == status);
        }
        return await query
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => ToDto(b))
            .ToListAsync(ct);
    }

    public async Task<BusDto> ApproveAsync(Guid adminId, Guid busId, CancellationToken ct)
    {
        var bus = await _db.Buses.Include(b => b.Operator).FirstOrDefaultAsync(b => b.Id == busId, ct)
            ?? throw new NotFoundException("Bus not found");
        if (bus.ApprovalStatus != BusApprovalStatus.Pending)
            throw new BusinessRuleException("BUS_NOT_PENDING", "Bus is not pending approval");

        bus.ApprovalStatus = BusApprovalStatus.Approved;
        bus.ApprovedAt = DateTime.UtcNow;
        bus.ApprovedByAdminId = adminId;
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(adminId, AuditAction.BusApproved,
            "bus", bus.Id, new { bus.RegistrationNumber }, ct);
        if (bus.Operator is not null)
            await _notifier.SendBusApprovedAsync(bus.Operator, bus, ct);

        return ToDto(bus);
    }

    public async Task<BusDto> RejectAsync(Guid adminId, Guid busId, string reason, CancellationToken ct)
    {
        var bus = await _db.Buses.Include(b => b.Operator).FirstOrDefaultAsync(b => b.Id == busId, ct)
            ?? throw new NotFoundException("Bus not found");
        if (bus.ApprovalStatus != BusApprovalStatus.Pending)
            throw new BusinessRuleException("BUS_NOT_PENDING", "Bus is not pending approval");

        bus.ApprovalStatus = BusApprovalStatus.Rejected;
        bus.RejectReason = reason.Trim();
        bus.ApprovedAt = DateTime.UtcNow;
        bus.ApprovedByAdminId = adminId;
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(adminId, AuditAction.BusRejected,
            "bus", bus.Id, new { reason = bus.RejectReason }, ct);
        if (bus.Operator is not null)
            await _notifier.SendBusRejectedAsync(bus.Operator, bus, bus.RejectReason, ct);

        return ToDto(bus);
    }

    private static BusDto ToDto(Bus b) => new(
        b.Id, b.OperatorUserId, b.RegistrationNumber, b.BusName, b.BusType,
        b.Capacity, b.ApprovalStatus, b.OperationalStatus,
        b.CreatedAt, b.ApprovedAt, b.RejectReason);
}
```

- [ ] **Step 8: Create `OperatorBusesController.cs`**

```csharp
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure.Auth;
using BusBooking.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.Api.Controllers;

[ApiController]
[Route("api/v1/operator/buses")]
[Authorize(Roles = "operator")]
public class OperatorBusesController : ControllerBase
{
    private readonly IBusService _buses;
    private readonly ICurrentUserAccessor _me;

    public OperatorBusesController(IBusService buses, ICurrentUserAccessor me)
    {
        _buses = buses;
        _me = me;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BusDto>>> List(CancellationToken ct)
        => Ok(await _buses.ListForOperatorAsync(_me.UserId, ct));

    [HttpPost]
    public async Task<ActionResult<BusDto>> Create(
        [FromBody] CreateBusRequest body,
        [FromServices] IValidator<CreateBusRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(body, ct);
        var dto = await _buses.CreateAsync(_me.UserId, body, ct);
        return StatusCode(StatusCodes.Status201Created, dto);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<BusDto>> UpdateStatus(
        Guid id,
        [FromBody] UpdateBusStatusRequest body,
        [FromServices] IValidator<UpdateBusStatusRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(body, ct);
        return Ok(await _buses.UpdateOperationalStatusAsync(_me.UserId, id, body.OperationalStatus, ct));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<BusDto>> Retire(Guid id, CancellationToken ct)
        => Ok(await _buses.RetireAsync(_me.UserId, id, ct));
}
```

- [ ] **Step 9: Register in `Program.cs`**

```csharp
builder.Services.AddScoped<IBusService, BusService>();
```

- [ ] **Step 10: Create `OperatorBusesTests.cs`**

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

namespace BusBooking.Api.Tests.Integration;

public class OperatorBusesTests : IClassFixture<IntegrationFixture>, IAsyncLifetime
{
    private readonly IntegrationFixture _fx;

    public OperatorBusesTests(IntegrationFixture fx) => _fx = fx;

    public async Task InitializeAsync() => await _fx.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private static CreateBusRequest SampleBus(string reg = "TN-37-AB-1234") => new()
    {
        RegistrationNumber = reg,
        BusName = "Shakti Express",
        BusType = BusType.Seater,
        Rows = 3,
        Columns = 4
    };

    [Fact]
    public async Task Create_generates_seats_and_marks_pending()
    {
        var (op, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Operator]);
        var client = _fx.CreateClient(); client.AttachBearer(token);

        var resp = await client.PostAsJsonAsync("/api/v1/operator/buses", SampleBus());
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await resp.Content.ReadFromJsonAsync<BusDto>();
        dto!.ApprovalStatus.Should().Be(BusApprovalStatus.Pending);
        dto.OperationalStatus.Should().Be(BusOperationalStatus.Active);
        dto.Capacity.Should().Be(12);

        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seats = await db.SeatDefinitions.Where(s => s.BusId == dto.Id).ToListAsync();
        seats.Should().HaveCount(12);
        seats.Select(s => s.SeatNumber).Should().Contain(new[] { "A1", "C4" });
    }

    [Fact]
    public async Task Duplicate_registration_returns_409()
    {
        var (_, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Operator]);
        var client = _fx.CreateClient(); client.AttachBearer(token);

        (await client.PostAsJsonAsync("/api/v1/operator/buses", SampleBus())).EnsureSuccessStatusCode();
        var dup = await client.PostAsJsonAsync("/api/v1/operator/buses", SampleBus());
        dup.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await dup.Content.ReadAsStringAsync()).Should().Contain("REGISTRATION_TAKEN");
    }

    [Fact]
    public async Task List_scopes_to_current_operator()
    {
        var (_, tokenA) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Operator], email: "a@t.local");
        var (_, tokenB) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Operator], email: "b@t.local");

        var clientA = _fx.CreateClient(); clientA.AttachBearer(tokenA);
        var clientB = _fx.CreateClient(); clientB.AttachBearer(tokenB);

        (await clientA.PostAsJsonAsync("/api/v1/operator/buses", SampleBus("OP-A-01"))).EnsureSuccessStatusCode();
        (await clientB.PostAsJsonAsync("/api/v1/operator/buses", SampleBus("OP-B-01"))).EnsureSuccessStatusCode();

        var listA = await (await clientA.GetAsync("/api/v1/operator/buses"))
            .Content.ReadFromJsonAsync<List<BusDto>>();
        listA!.Should().HaveCount(1);
        listA[0].RegistrationNumber.Should().Be("OP-A-01");
    }

    [Fact]
    public async Task Patch_status_toggles_maintenance()
    {
        var (_, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Operator]);
        var client = _fx.CreateClient(); client.AttachBearer(token);

        var created = await (await client.PostAsJsonAsync("/api/v1/operator/buses", SampleBus()))
            .Content.ReadFromJsonAsync<BusDto>();

        var resp = await client.PatchAsJsonAsync($"/api/v1/operator/buses/{created!.Id}/status",
            new UpdateBusStatusRequest { OperationalStatus = BusOperationalStatus.UnderMaintenance });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await resp.Content.ReadFromJsonAsync<BusDto>();
        dto!.OperationalStatus.Should().Be(BusOperationalStatus.UnderMaintenance);
    }

    [Fact]
    public async Task Delete_soft_retires_bus()
    {
        var (_, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Operator]);
        var client = _fx.CreateClient(); client.AttachBearer(token);

        var created = await (await client.PostAsJsonAsync("/api/v1/operator/buses", SampleBus()))
            .Content.ReadFromJsonAsync<BusDto>();

        var resp = await client.DeleteAsync($"/api/v1/operator/buses/{created!.Id}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await resp.Content.ReadFromJsonAsync<BusDto>();
        dto!.OperationalStatus.Should().Be(BusOperationalStatus.Retired);

        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Buses.FirstAsync(b => b.Id == created.Id)).OperationalStatus
            .Should().Be(BusOperationalStatus.Retired);
    }

    [Fact]
    public async Task Customer_only_token_is_403()
    {
        var (_, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        var client = _fx.CreateClient(); client.AttachBearer(token);

        var resp = await client.PostAsJsonAsync("/api/v1/operator/buses", SampleBus());
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
```

- [ ] **Step 11: Build + run**

```bash
cd backend/BusBooking.Api && dotnet build
cd ../BusBooking.Api.Tests
DOTNET_ROLL_FORWARD=Major dotnet test --filter "FullyQualifiedName~OperatorBuses"
```

Expected: all pass.

- [ ] **Step 12: Commit**

```bash
git add backend/BusBooking.Api/Dtos/BusDto.cs \
        backend/BusBooking.Api/Dtos/CreateBusRequest.cs \
        backend/BusBooking.Api/Dtos/UpdateBusStatusRequest.cs \
        backend/BusBooking.Api/Validators/CreateBusRequestValidator.cs \
        backend/BusBooking.Api/Validators/UpdateBusStatusRequestValidator.cs \
        backend/BusBooking.Api/Services/IBusService.cs \
        backend/BusBooking.Api/Services/BusService.cs \
        backend/BusBooking.Api/Controllers/OperatorBusesController.cs \
        backend/BusBooking.Api/Program.cs \
        backend/BusBooking.Api.Tests/Integration/OperatorBusesTests.cs
git commit -m "feat(backend): operator buses CRUD + auto seat generation + tests"
```

---

## Task 11: Admin bus-approval controller + tests

**Files:**
- Create: `backend/BusBooking.Api/Dtos/RejectBusRequest.cs`
- Create: `backend/BusBooking.Api/Validators/RejectBusRequestValidator.cs`
- Create: `backend/BusBooking.Api/Controllers/AdminBusesController.cs`
- Create: `backend/BusBooking.Api.Tests/Integration/AdminBusesTests.cs`

- [ ] **Step 1: Create `RejectBusRequest.cs`**

```csharp
namespace BusBooking.Api.Dtos;

public class RejectBusRequest
{
    public required string Reason { get; set; }
}
```

- [ ] **Step 2: Create `RejectBusRequestValidator.cs`**

```csharp
using BusBooking.Api.Dtos;
using FluentValidation;

namespace BusBooking.Api.Validators;

public class RejectBusRequestValidator : AbstractValidator<RejectBusRequest>
{
    public RejectBusRequestValidator()
    {
        RuleFor(r => r.Reason).NotEmpty().MinimumLength(3).MaximumLength(500);
    }
}
```

- [ ] **Step 3: Create `AdminBusesController.cs`**

```csharp
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure.Auth;
using BusBooking.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.Api.Controllers;

[ApiController]
[Route("api/v1/admin/buses")]
[Authorize(Roles = "admin")]
public class AdminBusesController : ControllerBase
{
    private readonly IBusService _buses;
    private readonly ICurrentUserAccessor _me;

    public AdminBusesController(IBusService buses, ICurrentUserAccessor me)
    {
        _buses = buses;
        _me = me;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BusDto>>> List(
        [FromQuery] string? status, CancellationToken ct)
        => Ok(await _buses.ListByApprovalStatusAsync(status, ct));

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<BusDto>> Approve(Guid id, CancellationToken ct)
        => Ok(await _buses.ApproveAsync(_me.UserId, id, ct));

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<BusDto>> Reject(
        Guid id,
        [FromBody] RejectBusRequest body,
        [FromServices] IValidator<RejectBusRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(body, ct);
        return Ok(await _buses.RejectAsync(_me.UserId, id, body.Reason, ct));
    }
}
```

- [ ] **Step 4: Create `AdminBusesTests.cs`**

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

namespace BusBooking.Api.Tests.Integration;

public class AdminBusesTests : IClassFixture<IntegrationFixture>, IAsyncLifetime
{
    private readonly IntegrationFixture _fx;

    public AdminBusesTests(IntegrationFixture fx) => _fx = fx;

    public async Task InitializeAsync() => await _fx.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<Guid> SeedPendingBusAsync(string reg = "TN-99-ZZ-0001")
    {
        var (op, opToken) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Operator]);
        var client = _fx.CreateClient(); client.AttachBearer(opToken);
        var created = await (await client.PostAsJsonAsync("/api/v1/operator/buses",
            new CreateBusRequest
            {
                RegistrationNumber = reg,
                BusName = "Test Bus",
                BusType = BusType.Seater,
                Rows = 2, Columns = 2
            })).Content.ReadFromJsonAsync<BusDto>();
        return created!.Id;
    }

    [Fact]
    public async Task List_filters_by_pending()
    {
        await SeedPendingBusAsync("PEND-1");
        await SeedPendingBusAsync("PEND-2");

        var (_, token) = await AdminTokenFactory.CreateAdminAsync(_fx, email: "admin-list-b@t.local");
        var client = _fx.CreateClient(); client.AttachAdminBearer(token);

        var resp = await client.GetAsync("/api/v1/admin/buses?status=pending");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await resp.Content.ReadFromJsonAsync<List<BusDto>>();
        list!.Should().HaveCount(2);
        list.Should().OnlyContain(b => b.ApprovalStatus == BusApprovalStatus.Pending);
    }

    [Fact]
    public async Task Approve_flips_status_and_writes_audit()
    {
        var busId = await SeedPendingBusAsync("APP-1");
        var (admin, token) = await AdminTokenFactory.CreateAdminAsync(_fx, email: "admin-app-b@t.local");
        var client = _fx.CreateClient(); client.AttachAdminBearer(token);

        var resp = await client.PostAsync($"/api/v1/admin/buses/{busId}/approve", null);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await resp.Content.ReadFromJsonAsync<BusDto>();
        dto!.ApprovalStatus.Should().Be(BusApprovalStatus.Approved);

        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var audit = await db.AuditLog.FirstOrDefaultAsync(a =>
            a.Action == AuditAction.BusApproved && a.TargetId == busId);
        audit.Should().NotBeNull();
        audit!.ActorUserId.Should().Be(admin.Id);
    }

    [Fact]
    public async Task Reject_stores_reason()
    {
        var busId = await SeedPendingBusAsync("REJ-1");
        var (_, token) = await AdminTokenFactory.CreateAdminAsync(_fx, email: "admin-r-b@t.local");
        var client = _fx.CreateClient(); client.AttachAdminBearer(token);

        var resp = await client.PostAsJsonAsync(
            $"/api/v1/admin/buses/{busId}/reject",
            new RejectBusRequest { Reason = "Insurance expired" });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bus = await db.Buses.FirstAsync(b => b.Id == busId);
        bus.ApprovalStatus.Should().Be(BusApprovalStatus.Rejected);
        bus.RejectReason.Should().Be("Insurance expired");
    }

    [Fact]
    public async Task Approve_already_approved_returns_422()
    {
        var busId = await SeedPendingBusAsync("DBL-1");
        var (_, token) = await AdminTokenFactory.CreateAdminAsync(_fx, email: "admin-dbl@t.local");
        var client = _fx.CreateClient(); client.AttachAdminBearer(token);

        (await client.PostAsync($"/api/v1/admin/buses/{busId}/approve", null)).EnsureSuccessStatusCode();
        var second = await client.PostAsync($"/api/v1/admin/buses/{busId}/approve", null);
        second.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await second.Content.ReadAsStringAsync()).Should().Contain("BUS_NOT_PENDING");
    }

    [Fact]
    public async Task Non_admin_token_is_403()
    {
        var busId = await SeedPendingBusAsync("FORB-1");
        var (_, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Operator], email: "op-forb@t.local");
        var client = _fx.CreateClient(); client.AttachBearer(token);

        var resp = await client.PostAsync($"/api/v1/admin/buses/{busId}/approve", null);
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
```

- [ ] **Step 5: Build + run**

```bash
cd backend/BusBooking.Api && dotnet build
cd ../BusBooking.Api.Tests
DOTNET_ROLL_FORWARD=Major dotnet test --filter "FullyQualifiedName~AdminBuses"
```

Expected: all pass.

- [ ] **Step 6: Commit**

```bash
git add backend/BusBooking.Api/Dtos/RejectBusRequest.cs \
        backend/BusBooking.Api/Validators/RejectBusRequestValidator.cs \
        backend/BusBooking.Api/Controllers/AdminBusesController.cs \
        backend/BusBooking.Api.Tests/Integration/AdminBusesTests.cs
git commit -m "feat(backend): admin bus-approval endpoints + tests"
```

---

## Task 12: Frontend API services for M3

**Files:**
- Create: `frontend/bus-booking-web/src/app/core/api/operator-requests.api.ts`
- Create: `frontend/bus-booking-web/src/app/core/api/operator-offices.api.ts`
- Create: `frontend/bus-booking-web/src/app/core/api/operator-buses.api.ts`
- Create: `frontend/bus-booking-web/src/app/core/api/admin-buses.api.ts`

- [ ] **Step 1: Create `operator-requests.api.ts`**

```typescript
import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface OperatorRequestDto {
  id: string;
  userId: string;
  userEmail: string;
  userName: string;
  companyName: string;
  status: 'pending' | 'approved' | 'rejected';
  requestedAt: string;
  reviewedAt: string | null;
  reviewedByAdminId: string | null;
  rejectReason: string | null;
}

export interface BecomeOperatorRequest {
  companyName: string;
}

export interface RejectOperatorRequest {
  reason: string;
}

@Injectable({ providedIn: 'root' })
export class OperatorRequestsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  submit(body: BecomeOperatorRequest): Observable<OperatorRequestDto> {
    return this.http.post<OperatorRequestDto>(`${this.base}/me/become-operator`, body);
  }

  list(status?: 'pending' | 'approved' | 'rejected'): Observable<OperatorRequestDto[]> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    return this.http.get<OperatorRequestDto[]>(`${this.base}/admin/operator-requests`, { params });
  }

  approve(id: string): Observable<OperatorRequestDto> {
    return this.http.post<OperatorRequestDto>(
      `${this.base}/admin/operator-requests/${id}/approve`, {});
  }

  reject(id: string, body: RejectOperatorRequest): Observable<OperatorRequestDto> {
    return this.http.post<OperatorRequestDto>(
      `${this.base}/admin/operator-requests/${id}/reject`, body);
  }
}
```

- [ ] **Step 2: Create `operator-offices.api.ts`**

```typescript
import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface OperatorOfficeDto {
  id: string;
  cityId: string;
  cityName: string;
  addressLine: string;
  phone: string;
  isActive: boolean;
}

export interface CreateOperatorOfficeRequest {
  cityId: string;
  addressLine: string;
  phone: string;
}

@Injectable({ providedIn: 'root' })
export class OperatorOfficesApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/operator/offices`;

  list(): Observable<OperatorOfficeDto[]> {
    return this.http.get<OperatorOfficeDto[]>(this.base);
  }

  create(body: CreateOperatorOfficeRequest): Observable<OperatorOfficeDto> {
    return this.http.post<OperatorOfficeDto>(this.base, body);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
```

- [ ] **Step 3: Create `operator-buses.api.ts`**

```typescript
import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export type BusType = 'seater' | 'sleeper' | 'semi_sleeper';
export type BusApprovalStatus = 'pending' | 'approved' | 'rejected';
export type BusOperationalStatus = 'active' | 'under_maintenance' | 'retired';

export interface BusDto {
  id: string;
  operatorUserId: string;
  registrationNumber: string;
  busName: string;
  busType: BusType;
  capacity: number;
  approvalStatus: BusApprovalStatus;
  operationalStatus: BusOperationalStatus;
  createdAt: string;
  approvedAt: string | null;
  rejectReason: string | null;
}

export interface CreateBusRequest {
  registrationNumber: string;
  busName: string;
  busType: BusType;
  rows: number;
  columns: number;
}

export interface UpdateBusStatusRequest {
  operationalStatus: 'active' | 'under_maintenance';
}

@Injectable({ providedIn: 'root' })
export class OperatorBusesApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/operator/buses`;

  list(): Observable<BusDto[]> { return this.http.get<BusDto[]>(this.base); }
  create(body: CreateBusRequest): Observable<BusDto> { return this.http.post<BusDto>(this.base, body); }
  updateStatus(id: string, body: UpdateBusStatusRequest): Observable<BusDto> {
    return this.http.patch<BusDto>(`${this.base}/${id}/status`, body);
  }
  retire(id: string): Observable<BusDto> { return this.http.delete<BusDto>(`${this.base}/${id}`); }
}
```

- [ ] **Step 4: Create `admin-buses.api.ts`**

```typescript
import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { BusApprovalStatus, BusDto } from './operator-buses.api';

export interface RejectBusRequest { reason: string; }

@Injectable({ providedIn: 'root' })
export class AdminBusesApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/admin/buses`;

  list(status?: BusApprovalStatus): Observable<BusDto[]> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    return this.http.get<BusDto[]>(this.base, { params });
  }

  approve(id: string): Observable<BusDto> {
    return this.http.post<BusDto>(`${this.base}/${id}/approve`, {});
  }

  reject(id: string, body: RejectBusRequest): Observable<BusDto> {
    return this.http.post<BusDto>(`${this.base}/${id}/reject`, body);
  }
}
```

- [ ] **Step 5: Build**

```bash
cd frontend/bus-booking-web && npm run build
```

Expected: build succeeds.

- [ ] **Step 6: Commit**

```bash
git add frontend/bus-booking-web/src/app/core/api/operator-requests.api.ts \
        frontend/bus-booking-web/src/app/core/api/operator-offices.api.ts \
        frontend/bus-booking-web/src/app/core/api/operator-buses.api.ts \
        frontend/bus-booking-web/src/app/core/api/admin-buses.api.ts
git commit -m "feat(frontend): API services for M3 (operator requests, offices, buses, admin buses)"
```

---

## Task 13: Customer `become-operator` page

**Files:**
- Create: `frontend/bus-booking-web/src/app/features/customer/become-operator/become-operator-page.component.ts`
- Create: `frontend/bus-booking-web/src/app/features/customer/become-operator/become-operator-page.component.html`
- Create: `frontend/bus-booking-web/src/app/features/customer/become-operator/become-operator-page.component.scss`

- [ ] **Step 1: Create the component TypeScript**

```typescript
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { OperatorRequestsApiService } from '../../../core/api/operator-requests.api';

@Component({
  selector: 'app-become-operator-page',
  standalone: true,
  imports: [
    ReactiveFormsModule, MatCardModule, MatFormFieldModule,
    MatInputModule, MatButtonModule
  ],
  templateUrl: './become-operator-page.component.html',
  styleUrl: './become-operator-page.component.scss'
})
export class BecomeOperatorPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(OperatorRequestsApiService);
  private readonly router = inject(Router);

  readonly submitting = signal(false);
  readonly submitted = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    companyName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(160)]]
  });

  submit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.submitting.set(true);
    this.errorMessage.set(null);
    this.api.submit(this.form.getRawValue()).subscribe({
      next: () => { this.submitting.set(false); this.submitted.set(true); },
      error: (err: HttpErrorResponse) => {
        this.submitting.set(false);
        const code = err.error?.error?.code;
        if (code === 'REQUEST_ALREADY_PENDING')
          this.errorMessage.set('You already have a pending operator request.');
        else if (code === 'ALREADY_OPERATOR')
          this.errorMessage.set('You are already an operator.');
        else
          this.errorMessage.set(err.error?.error?.message ?? 'Something went wrong. Try again.');
      }
    });
  }

  goHome(): void { this.router.navigate(['/']); }
}
```

- [ ] **Step 2: Create the template**

```html
<div class="p-6 max-w-xl mx-auto">
  <mat-card>
    <mat-card-header>
      <mat-card-title>Become a bus operator</mat-card-title>
      <mat-card-subtitle>Submit a request — an admin will review and reply.</mat-card-subtitle>
    </mat-card-header>

    <mat-card-content>
      @if (submitted()) {
        <div class="py-6 text-center">
          <p class="text-lg font-medium">Request submitted.</p>
          <p class="text-sm opacity-80 mt-2">We'll notify you once an admin reviews it. Log in again to access the operator console after approval.</p>
          <button mat-stroked-button class="mt-4" (click)="goHome()">Back to home</button>
        </div>
      } @else {
        <form [formGroup]="form" (ngSubmit)="submit()" class="flex flex-col gap-4 pt-4">
          <mat-form-field appearance="outline">
            <mat-label>Company name</mat-label>
            <input matInput formControlName="companyName" maxlength="160" />
            @if (form.controls.companyName.hasError('required') && form.controls.companyName.touched) {
              <mat-error>Company name is required</mat-error>
            }
            @if (form.controls.companyName.hasError('minlength')) {
              <mat-error>Too short</mat-error>
            }
          </mat-form-field>

          @if (errorMessage()) {
            <div class="rounded-md bg-red-50 text-red-800 px-3 py-2 text-sm">
              {{ errorMessage() }}
            </div>
          }

          <button mat-flat-button color="primary" type="submit" [disabled]="submitting()">
            {{ submitting() ? 'Submitting…' : 'Submit request' }}
          </button>
        </form>
      }
    </mat-card-content>
  </mat-card>
</div>
```

- [ ] **Step 3: Create the empty SCSS stub**

```scss
:host { display: block; }
```

- [ ] **Step 4: Build**

```bash
cd frontend/bus-booking-web && npm run build
```

Expected: build succeeds.

- [ ] **Step 5: Commit**

```bash
git add frontend/bus-booking-web/src/app/features/customer/become-operator/
git commit -m "feat(frontend): customer become-operator page"
```

---

## Task 14: Operator shell + dashboard pages

**Files:**
- Create: `frontend/bus-booking-web/src/app/features/operator/operator-shell/operator-shell.component.ts`
- Create: `frontend/bus-booking-web/src/app/features/operator/operator-shell/operator-shell.component.html`
- Create: `frontend/bus-booking-web/src/app/features/operator/operator-shell/operator-shell.component.scss`
- Create: `frontend/bus-booking-web/src/app/features/operator/dashboard/operator-dashboard.component.ts`
- Create: `frontend/bus-booking-web/src/app/features/operator/dashboard/operator-dashboard.component.html`
- Create: `frontend/bus-booking-web/src/app/features/operator/dashboard/operator-dashboard.component.scss`

- [ ] **Step 1: Create `operator-shell.component.ts`**

```typescript
import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatSidenavModule } from '@angular/material/sidenav';

@Component({
  selector: 'app-operator-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, MatIconModule, MatListModule, MatSidenavModule],
  templateUrl: './operator-shell.component.html',
  styleUrl: './operator-shell.component.scss'
})
export class OperatorShellComponent { }
```

- [ ] **Step 2: Create `operator-shell.component.html`**

```html
<mat-sidenav-container class="h-[calc(100vh-64px)]">
  <mat-sidenav mode="side" opened class="w-56 border-r">
    <mat-nav-list>
      <a mat-list-item routerLink="." routerLinkActive="!bg-slate-100" [routerLinkActiveOptions]="{ exact: true }">
        <mat-icon>dashboard</mat-icon> Dashboard
      </a>
      <a mat-list-item routerLink="offices" routerLinkActive="!bg-slate-100">
        <mat-icon>store</mat-icon> Offices
      </a>
      <a mat-list-item routerLink="buses" routerLinkActive="!bg-slate-100">
        <mat-icon>directions_bus</mat-icon> Buses
      </a>
    </mat-nav-list>
  </mat-sidenav>

  <mat-sidenav-content>
    <div class="p-6">
      <router-outlet />
    </div>
  </mat-sidenav-content>
</mat-sidenav-container>
```

- [ ] **Step 3: Create `operator-shell.component.scss`**

```scss
:host { display: block; }
```

- [ ] **Step 4: Create `operator-dashboard.component.ts`**

```typescript
import { Component, inject, signal } from '@angular/core';
import { forkJoin } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { OperatorBusesApiService, BusDto } from '../../../core/api/operator-buses.api';
import { OperatorOfficesApiService, OperatorOfficeDto } from '../../../core/api/operator-offices.api';

@Component({
  selector: 'app-operator-dashboard',
  standalone: true,
  imports: [MatCardModule, MatIconModule],
  templateUrl: './operator-dashboard.component.html',
  styleUrl: './operator-dashboard.component.scss'
})
export class OperatorDashboardComponent {
  private readonly busesApi = inject(OperatorBusesApiService);
  private readonly officesApi = inject(OperatorOfficesApiService);

  readonly officesCount = signal(0);
  readonly busesCount = signal(0);
  readonly pendingBusesCount = signal(0);
  readonly loading = signal(true);

  constructor() {
    forkJoin({
      buses: this.busesApi.list(),
      offices: this.officesApi.list()
    }).subscribe({
      next: ({ buses, offices }: { buses: BusDto[]; offices: OperatorOfficeDto[] }) => {
        this.officesCount.set(offices.length);
        this.busesCount.set(buses.length);
        this.pendingBusesCount.set(buses.filter(b => b.approvalStatus === 'pending').length);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
```

- [ ] **Step 5: Create `operator-dashboard.component.html`**

```html
<h1 class="text-2xl font-semibold mb-6">Operator Dashboard</h1>

@if (loading()) {
  <p class="opacity-70">Loading…</p>
} @else {
  <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
    <mat-card>
      <mat-card-content class="flex items-center gap-3">
        <mat-icon class="text-slate-500">store</mat-icon>
        <div>
          <div class="text-3xl font-semibold">{{ officesCount() }}</div>
          <div class="text-sm opacity-70">Offices</div>
        </div>
      </mat-card-content>
    </mat-card>

    <mat-card>
      <mat-card-content class="flex items-center gap-3">
        <mat-icon class="text-slate-500">directions_bus</mat-icon>
        <div>
          <div class="text-3xl font-semibold">{{ busesCount() }}</div>
          <div class="text-sm opacity-70">Buses</div>
        </div>
      </mat-card-content>
    </mat-card>

    <mat-card>
      <mat-card-content class="flex items-center gap-3">
        <mat-icon class="text-amber-600">hourglass_empty</mat-icon>
        <div>
          <div class="text-3xl font-semibold">{{ pendingBusesCount() }}</div>
          <div class="text-sm opacity-70">Pending approval</div>
        </div>
      </mat-card-content>
    </mat-card>
  </div>
}
```

- [ ] **Step 6: Create `operator-dashboard.component.scss`**

```scss
:host { display: block; }
```

- [ ] **Step 7: Build**

```bash
cd frontend/bus-booking-web && npm run build
```

- [ ] **Step 8: Commit**

```bash
git add frontend/bus-booking-web/src/app/features/operator/operator-shell/ \
        frontend/bus-booking-web/src/app/features/operator/dashboard/
git commit -m "feat(frontend): operator shell + dashboard"
```

---

## Task 15: Operator offices page (list + add-office dialog + delete)

**Files:**
- Create: `frontend/bus-booking-web/src/app/features/operator/offices/operator-offices-page.component.ts`
- Create: `frontend/bus-booking-web/src/app/features/operator/offices/operator-offices-page.component.html`
- Create: `frontend/bus-booking-web/src/app/features/operator/offices/operator-offices-page.component.scss`
- Create: `frontend/bus-booking-web/src/app/features/operator/offices/add-office-dialog.component.ts`
- Create: `frontend/bus-booking-web/src/app/features/operator/offices/add-office-dialog.component.html`

The add-office dialog reuses the `CityAutocompleteComponent` created in M2 (`shared/components/city-autocomplete`). If that component does not exist or does not publish a `(citySelected)` output, fall back to a plain `mat-select` populated by `CitiesApiService.search`.

- [ ] **Step 1: Create `add-office-dialog.component.ts`**

```typescript
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { CityAutocompleteComponent } from '../../../shared/components/city-autocomplete/city-autocomplete.component';
import { CityDto } from '../../../core/api/cities.api';

@Component({
  selector: 'app-add-office-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, CityAutocompleteComponent
  ],
  templateUrl: './add-office-dialog.component.html'
})
export class AddOfficeDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly dialog = inject(MatDialogRef<AddOfficeDialogComponent>);

  readonly city = signal<CityDto | null>(null);
  readonly form = this.fb.nonNullable.group({
    addressLine: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(300)]],
    phone: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(32)]]
  });

  onCitySelected(c: CityDto | null): void { this.city.set(c); }

  submit(): void {
    if (this.form.invalid || !this.city()) {
      this.form.markAllAsTouched();
      return;
    }
    this.dialog.close({
      cityId: this.city()!.id,
      addressLine: this.form.controls.addressLine.value.trim(),
      phone: this.form.controls.phone.value.trim()
    });
  }

  cancel(): void { this.dialog.close(null); }
}
```

- [ ] **Step 2: Create `add-office-dialog.component.html`**

```html
<h2 mat-dialog-title>Add office</h2>
<mat-dialog-content>
  <div class="flex flex-col gap-4 pt-2 min-w-[320px]">
    <app-city-autocomplete (citySelected)="onCitySelected($event)" label="City" />

    <mat-form-field appearance="outline">
      <mat-label>Address</mat-label>
      <input matInput [formControl]="form.controls.addressLine" maxlength="300" />
    </mat-form-field>

    <mat-form-field appearance="outline">
      <mat-label>Phone</mat-label>
      <input matInput [formControl]="form.controls.phone" maxlength="32" />
    </mat-form-field>
  </div>
</mat-dialog-content>
<mat-dialog-actions align="end">
  <button mat-button (click)="cancel()">Cancel</button>
  <button mat-flat-button color="primary" (click)="submit()"
          [disabled]="form.invalid || !city()">Save</button>
</mat-dialog-actions>
```

- [ ] **Step 3: Create `operator-offices-page.component.ts`**

```typescript
import { Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import {
  CreateOperatorOfficeRequest, OperatorOfficeDto, OperatorOfficesApiService
} from '../../../core/api/operator-offices.api';
import { AddOfficeDialogComponent } from './add-office-dialog.component';

@Component({
  selector: 'app-operator-offices-page',
  standalone: true,
  imports: [
    MatCardModule, MatTableModule, MatButtonModule, MatIconModule,
    MatDialogModule, MatProgressSpinnerModule
  ],
  templateUrl: './operator-offices-page.component.html',
  styleUrl: './operator-offices-page.component.scss'
})
export class OperatorOfficesPageComponent {
  private readonly api = inject(OperatorOfficesApiService);
  private readonly dialog = inject(MatDialog);
  private readonly snack = inject(MatSnackBar);

  readonly offices = signal<OperatorOfficeDto[]>([]);
  readonly loading = signal(true);
  readonly columns = ['cityName', 'addressLine', 'phone', 'actions'];

  constructor() { this.reload(); }

  reload(): void {
    this.loading.set(true);
    this.api.list().subscribe({
      next: rows => { this.offices.set(rows); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  add(): void {
    const ref = this.dialog.open<AddOfficeDialogComponent, unknown,
      CreateOperatorOfficeRequest | null>(AddOfficeDialogComponent);
    ref.afterClosed().subscribe(body => {
      if (!body) return;
      this.api.create(body).subscribe({
        next: () => { this.snack.open('Office added', 'OK', { duration: 2000 }); this.reload(); },
        error: (err: HttpErrorResponse) => {
          const msg = err.error?.error?.code === 'OFFICE_ALREADY_EXISTS'
            ? 'You already have an office in that city.'
            : err.error?.error?.message ?? 'Could not save office.';
          this.snack.open(msg, 'OK', { duration: 3000 });
        }
      });
    });
  }

  remove(office: OperatorOfficeDto): void {
    if (!confirm(`Delete office in ${office.cityName}?`)) return;
    this.api.delete(office.id).subscribe({
      next: () => { this.snack.open('Office removed', 'OK', { duration: 2000 }); this.reload(); }
    });
  }
}
```

- [ ] **Step 4: Create `operator-offices-page.component.html`**

```html
<div class="flex items-center justify-between mb-4">
  <h1 class="text-2xl font-semibold">Offices</h1>
  <button mat-flat-button color="primary" (click)="add()">
    <mat-icon>add</mat-icon> Add office
  </button>
</div>

@if (loading()) {
  <mat-progress-spinner mode="indeterminate" diameter="28" />
} @else if (offices().length === 0) {
  <mat-card>
    <mat-card-content class="py-10 text-center opacity-70">
      You haven't added any offices yet. You need an office in a route's source and destination city before you can run a schedule there.
    </mat-card-content>
  </mat-card>
} @else {
  <mat-card>
    <table mat-table [dataSource]="offices()" class="w-full">
      <ng-container matColumnDef="cityName">
        <th mat-header-cell *matHeaderCellDef>City</th>
        <td mat-cell *matCellDef="let o">{{ o.cityName }}</td>
      </ng-container>
      <ng-container matColumnDef="addressLine">
        <th mat-header-cell *matHeaderCellDef>Address</th>
        <td mat-cell *matCellDef="let o">{{ o.addressLine }}</td>
      </ng-container>
      <ng-container matColumnDef="phone">
        <th mat-header-cell *matHeaderCellDef>Phone</th>
        <td mat-cell *matCellDef="let o">{{ o.phone }}</td>
      </ng-container>
      <ng-container matColumnDef="actions">
        <th mat-header-cell *matHeaderCellDef></th>
        <td mat-cell *matCellDef="let o">
          <button mat-icon-button (click)="remove(o)" aria-label="Remove">
            <mat-icon>delete</mat-icon>
          </button>
        </td>
      </ng-container>
      <tr mat-header-row *matHeaderRowDef="columns"></tr>
      <tr mat-row *matRowDef="let row; columns: columns;"></tr>
    </table>
  </mat-card>
}
```

- [ ] **Step 5: Create the SCSS stub**

```scss
:host { display: block; }
```

- [ ] **Step 6: Build**

```bash
cd frontend/bus-booking-web && npm run build
```

If the build fails because `CityAutocompleteComponent` does not exist yet or does not emit `(citySelected)`, either (a) complete M2 Task 10 first, or (b) replace the `<app-city-autocomplete>` element with a `mat-select` populated by `CitiesApiService.list()` — keep the same `onCitySelected` handler.

- [ ] **Step 7: Commit**

```bash
git add frontend/bus-booking-web/src/app/features/operator/offices/
git commit -m "feat(frontend): operator offices page with add/delete"
```

---

## Task 16: Operator buses pages (list + new-bus form)

**Files:**
- Create: `frontend/bus-booking-web/src/app/features/operator/buses/operator-buses-list.component.ts`
- Create: `frontend/bus-booking-web/src/app/features/operator/buses/operator-buses-list.component.html`
- Create: `frontend/bus-booking-web/src/app/features/operator/buses/operator-buses-list.component.scss`
- Create: `frontend/bus-booking-web/src/app/features/operator/buses/operator-bus-form.component.ts`
- Create: `frontend/bus-booking-web/src/app/features/operator/buses/operator-bus-form.component.html`
- Create: `frontend/bus-booking-web/src/app/features/operator/buses/operator-bus-form.component.scss`

- [ ] **Step 1: Create `operator-buses-list.component.ts`**

```typescript
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { BusDto, OperatorBusesApiService } from '../../../core/api/operator-buses.api';

@Component({
  selector: 'app-operator-buses-list',
  standalone: true,
  imports: [
    RouterLink, MatCardModule, MatTableModule, MatChipsModule, MatButtonModule,
    MatIconModule, MatMenuModule, MatProgressSpinnerModule
  ],
  templateUrl: './operator-buses-list.component.html',
  styleUrl: './operator-buses-list.component.scss'
})
export class OperatorBusesListComponent {
  private readonly api = inject(OperatorBusesApiService);
  private readonly snack = inject(MatSnackBar);

  readonly buses = signal<BusDto[]>([]);
  readonly loading = signal(true);
  readonly columns = ['registrationNumber', 'busName', 'busType', 'approvalStatus', 'operationalStatus', 'actions'];

  constructor() { this.reload(); }

  reload(): void {
    this.loading.set(true);
    this.api.list().subscribe({
      next: rows => { this.buses.set(rows); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  approvalChipColor(status: string): 'primary' | 'warn' | undefined {
    if (status === 'approved') return 'primary';
    if (status === 'rejected') return 'warn';
    return undefined;
  }

  setActive(bus: BusDto): void {
    this.api.updateStatus(bus.id, { operationalStatus: 'active' }).subscribe({
      next: () => { this.snack.open('Bus marked active', 'OK', { duration: 2000 }); this.reload(); }
    });
  }

  setMaintenance(bus: BusDto): void {
    this.api.updateStatus(bus.id, { operationalStatus: 'under_maintenance' }).subscribe({
      next: () => { this.snack.open('Bus under maintenance', 'OK', { duration: 2000 }); this.reload(); }
    });
  }

  retire(bus: BusDto): void {
    if (!confirm(`Retire ${bus.registrationNumber}? This cannot be undone.`)) return;
    this.api.retire(bus.id).subscribe({
      next: () => { this.snack.open('Bus retired', 'OK', { duration: 2000 }); this.reload(); }
    });
  }
}
```

- [ ] **Step 2: Create `operator-buses-list.component.html`**

```html
<div class="flex items-center justify-between mb-4">
  <h1 class="text-2xl font-semibold">Buses</h1>
  <a mat-flat-button color="primary" routerLink="new">
    <mat-icon>add</mat-icon> Add bus
  </a>
</div>

@if (loading()) {
  <mat-progress-spinner mode="indeterminate" diameter="28" />
} @else if (buses().length === 0) {
  <mat-card>
    <mat-card-content class="py-10 text-center opacity-70">
      You haven't added any buses yet.
    </mat-card-content>
  </mat-card>
} @else {
  <mat-card>
    <table mat-table [dataSource]="buses()" class="w-full">
      <ng-container matColumnDef="registrationNumber">
        <th mat-header-cell *matHeaderCellDef>Registration</th>
        <td mat-cell *matCellDef="let b">{{ b.registrationNumber }}</td>
      </ng-container>
      <ng-container matColumnDef="busName">
        <th mat-header-cell *matHeaderCellDef>Name</th>
        <td mat-cell *matCellDef="let b">{{ b.busName }}</td>
      </ng-container>
      <ng-container matColumnDef="busType">
        <th mat-header-cell *matHeaderCellDef>Type</th>
        <td mat-cell *matCellDef="let b">{{ b.busType }}</td>
      </ng-container>
      <ng-container matColumnDef="approvalStatus">
        <th mat-header-cell *matHeaderCellDef>Approval</th>
        <td mat-cell *matCellDef="let b">
          <mat-chip [color]="approvalChipColor(b.approvalStatus)" highlighted>
            {{ b.approvalStatus }}
          </mat-chip>
        </td>
      </ng-container>
      <ng-container matColumnDef="operationalStatus">
        <th mat-header-cell *matHeaderCellDef>Operational</th>
        <td mat-cell *matCellDef="let b">{{ b.operationalStatus }}</td>
      </ng-container>
      <ng-container matColumnDef="actions">
        <th mat-header-cell *matHeaderCellDef></th>
        <td mat-cell *matCellDef="let b">
          <button mat-icon-button [matMenuTriggerFor]="menu"
                  [disabled]="b.operationalStatus === 'retired'">
            <mat-icon>more_vert</mat-icon>
          </button>
          <mat-menu #menu="matMenu">
            @if (b.operationalStatus !== 'active') {
              <button mat-menu-item (click)="setActive(b)">Mark active</button>
            }
            @if (b.operationalStatus !== 'under_maintenance') {
              <button mat-menu-item (click)="setMaintenance(b)">Mark maintenance</button>
            }
            <button mat-menu-item (click)="retire(b)">Retire</button>
          </mat-menu>
        </td>
      </ng-container>
      <tr mat-header-row *matHeaderRowDef="columns"></tr>
      <tr mat-row *matRowDef="let row; columns: columns;"></tr>
    </table>
  </mat-card>
}
```

- [ ] **Step 3: Create `operator-buses-list.component.scss`**

```scss
:host { display: block; }
```

- [ ] **Step 4: Create `operator-bus-form.component.ts`**

```typescript
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { CreateBusRequest, OperatorBusesApiService } from '../../../core/api/operator-buses.api';

@Component({
  selector: 'app-operator-bus-form',
  standalone: true,
  imports: [
    ReactiveFormsModule, MatCardModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatButtonModule
  ],
  templateUrl: './operator-bus-form.component.html',
  styleUrl: './operator-bus-form.component.scss'
})
export class OperatorBusFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(OperatorBusesApiService);
  private readonly router = inject(Router);
  private readonly snack = inject(MatSnackBar);

  readonly submitting = signal(false);

  readonly form = this.fb.nonNullable.group({
    registrationNumber: ['', [Validators.required, Validators.minLength(4), Validators.maxLength(32)]],
    busName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(120)]],
    busType: ['seater' as 'seater' | 'sleeper' | 'semi_sleeper', Validators.required],
    rows: [3, [Validators.required, Validators.min(1), Validators.max(26)]],
    columns: [4, [Validators.required, Validators.min(1), Validators.max(12)]]
  });

  readonly capacityPreview = computed(() => this.form.value.rows! * this.form.value.columns!);

  submit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.submitting.set(true);
    const body: CreateBusRequest = this.form.getRawValue();
    this.api.create(body).subscribe({
      next: () => {
        this.snack.open('Bus submitted for approval', 'OK', { duration: 2500 });
        this.router.navigate(['/operator/buses']);
      },
      error: (err: HttpErrorResponse) => {
        this.submitting.set(false);
        const msg = err.error?.error?.code === 'REGISTRATION_TAKEN'
          ? 'That registration number is already in use.'
          : err.error?.error?.message ?? 'Could not create bus.';
        this.snack.open(msg, 'OK', { duration: 4000 });
      }
    });
  }

  cancel(): void { this.router.navigate(['/operator/buses']); }
}
```

- [ ] **Step 5: Create `operator-bus-form.component.html`**

```html
<div class="max-w-xl">
  <h1 class="text-2xl font-semibold mb-4">Add a bus</h1>
  <mat-card>
    <form [formGroup]="form" (ngSubmit)="submit()" class="flex flex-col gap-4 p-4">
      <mat-form-field appearance="outline">
        <mat-label>Registration number</mat-label>
        <input matInput formControlName="registrationNumber" />
      </mat-form-field>

      <mat-form-field appearance="outline">
        <mat-label>Bus name</mat-label>
        <input matInput formControlName="busName" />
      </mat-form-field>

      <mat-form-field appearance="outline">
        <mat-label>Type</mat-label>
        <mat-select formControlName="busType">
          <mat-option value="seater">Seater</mat-option>
          <mat-option value="sleeper">Sleeper</mat-option>
          <mat-option value="semi_sleeper">Semi-sleeper</mat-option>
        </mat-select>
      </mat-form-field>

      <div class="flex gap-3">
        <mat-form-field appearance="outline" class="flex-1">
          <mat-label>Rows</mat-label>
          <input matInput type="number" formControlName="rows" min="1" max="26" />
        </mat-form-field>
        <mat-form-field appearance="outline" class="flex-1">
          <mat-label>Columns</mat-label>
          <input matInput type="number" formControlName="columns" min="1" max="12" />
        </mat-form-field>
      </div>

      <p class="text-sm opacity-70">Capacity: {{ capacityPreview() }} seats (rows × columns)</p>

      <div class="flex gap-3 justify-end">
        <button mat-button type="button" (click)="cancel()">Cancel</button>
        <button mat-flat-button color="primary" type="submit" [disabled]="submitting()">
          Submit for approval
        </button>
      </div>
    </form>
  </mat-card>
</div>
```

- [ ] **Step 6: Create the SCSS stub**

```scss
:host { display: block; }
```

- [ ] **Step 7: Build**

```bash
cd frontend/bus-booking-web && npm run build
```

- [ ] **Step 8: Commit**

```bash
git add frontend/bus-booking-web/src/app/features/operator/buses/
git commit -m "feat(frontend): operator buses list + new-bus form"
```

---

## Task 17: Admin operator-requests page

**Files:**
- Create: `frontend/bus-booking-web/src/app/features/admin/operator-requests/admin-operator-requests-page.component.ts`
- Create: `frontend/bus-booking-web/src/app/features/admin/operator-requests/admin-operator-requests-page.component.html`
- Create: `frontend/bus-booking-web/src/app/features/admin/operator-requests/admin-operator-requests-page.component.scss`
- Create: `frontend/bus-booking-web/src/app/features/admin/operator-requests/reject-dialog.component.ts`
- Create: `frontend/bus-booking-web/src/app/features/admin/operator-requests/reject-dialog.component.html`

- [ ] **Step 1: Create `reject-dialog.component.ts`**

```typescript
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

@Component({
  selector: 'app-reject-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  templateUrl: './reject-dialog.component.html'
})
export class RejectDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly dialog = inject(MatDialogRef<RejectDialogComponent>);

  readonly form = this.fb.nonNullable.group({
    reason: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(500)]]
  });

  submit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.dialog.close(this.form.controls.reason.value.trim());
  }
  cancel(): void { this.dialog.close(null); }
}
```

- [ ] **Step 2: Create `reject-dialog.component.html`**

```html
<h2 mat-dialog-title>Reason for rejection</h2>
<mat-dialog-content>
  <mat-form-field appearance="outline" class="w-full mt-2">
    <mat-label>Reason</mat-label>
    <textarea matInput rows="4" [formControl]="form.controls.reason" maxlength="500"></textarea>
  </mat-form-field>
</mat-dialog-content>
<mat-dialog-actions align="end">
  <button mat-button (click)="cancel()">Cancel</button>
  <button mat-flat-button color="warn" (click)="submit()" [disabled]="form.invalid">Reject</button>
</mat-dialog-actions>
```

- [ ] **Step 3: Create `admin-operator-requests-page.component.ts`**

```typescript
import { Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { OperatorRequestDto, OperatorRequestsApiService } from '../../../core/api/operator-requests.api';
import { RejectDialogComponent } from './reject-dialog.component';

@Component({
  selector: 'app-admin-operator-requests-page',
  standalone: true,
  imports: [
    MatCardModule, MatTableModule, MatButtonModule, MatButtonToggleModule,
    MatChipsModule, MatDialogModule, MatProgressSpinnerModule
  ],
  templateUrl: './admin-operator-requests-page.component.html',
  styleUrl: './admin-operator-requests-page.component.scss'
})
export class AdminOperatorRequestsPageComponent {
  private readonly api = inject(OperatorRequestsApiService);
  private readonly dialog = inject(MatDialog);
  private readonly snack = inject(MatSnackBar);

  readonly filter = signal<'pending' | 'approved' | 'rejected'>('pending');
  readonly rows = signal<OperatorRequestDto[]>([]);
  readonly loading = signal(true);
  readonly columns = ['userName', 'userEmail', 'companyName', 'requestedAt', 'status', 'actions'];

  constructor() { this.reload(); }

  setFilter(v: 'pending' | 'approved' | 'rejected'): void {
    this.filter.set(v);
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.api.list(this.filter()).subscribe({
      next: rows => { this.rows.set(rows); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  approve(req: OperatorRequestDto): void {
    this.api.approve(req.id).subscribe({
      next: () => { this.snack.open('Approved', 'OK', { duration: 2000 }); this.reload(); }
    });
  }

  reject(req: OperatorRequestDto): void {
    const ref = this.dialog.open<RejectDialogComponent, unknown, string | null>(RejectDialogComponent);
    ref.afterClosed().subscribe(reason => {
      if (!reason) return;
      this.api.reject(req.id, { reason }).subscribe({
        next: () => { this.snack.open('Rejected', 'OK', { duration: 2000 }); this.reload(); }
      });
    });
  }
}
```

- [ ] **Step 4: Create `admin-operator-requests-page.component.html`**

```html
<div class="p-6">
  <h1 class="text-2xl font-semibold mb-4">Operator requests</h1>

  <mat-button-toggle-group [value]="filter()" (change)="setFilter($event.value)" class="mb-4">
    <mat-button-toggle value="pending">Pending</mat-button-toggle>
    <mat-button-toggle value="approved">Approved</mat-button-toggle>
    <mat-button-toggle value="rejected">Rejected</mat-button-toggle>
  </mat-button-toggle-group>

  @if (loading()) {
    <mat-progress-spinner mode="indeterminate" diameter="28" />
  } @else if (rows().length === 0) {
    <mat-card><mat-card-content class="py-10 text-center opacity-70">No requests in this state.</mat-card-content></mat-card>
  } @else {
    <mat-card>
      <table mat-table [dataSource]="rows()" class="w-full">
        <ng-container matColumnDef="userName">
          <th mat-header-cell *matHeaderCellDef>Name</th>
          <td mat-cell *matCellDef="let r">{{ r.userName }}</td>
        </ng-container>
        <ng-container matColumnDef="userEmail">
          <th mat-header-cell *matHeaderCellDef>Email</th>
          <td mat-cell *matCellDef="let r">{{ r.userEmail }}</td>
        </ng-container>
        <ng-container matColumnDef="companyName">
          <th mat-header-cell *matHeaderCellDef>Company</th>
          <td mat-cell *matCellDef="let r">{{ r.companyName }}</td>
        </ng-container>
        <ng-container matColumnDef="requestedAt">
          <th mat-header-cell *matHeaderCellDef>Requested</th>
          <td mat-cell *matCellDef="let r">{{ r.requestedAt | date:'short' }}</td>
        </ng-container>
        <ng-container matColumnDef="status">
          <th mat-header-cell *matHeaderCellDef>Status</th>
          <td mat-cell *matCellDef="let r">
            <mat-chip highlighted>{{ r.status }}</mat-chip>
            @if (r.status === 'rejected' && r.rejectReason) {
              <div class="text-xs opacity-70 mt-1">{{ r.rejectReason }}</div>
            }
          </td>
        </ng-container>
        <ng-container matColumnDef="actions">
          <th mat-header-cell *matHeaderCellDef></th>
          <td mat-cell *matCellDef="let r">
            @if (r.status === 'pending') {
              <button mat-flat-button color="primary" class="mr-2" (click)="approve(r)">Approve</button>
              <button mat-stroked-button color="warn" (click)="reject(r)">Reject</button>
            }
          </td>
        </ng-container>
        <tr mat-header-row *matHeaderRowDef="columns"></tr>
        <tr mat-row *matRowDef="let row; columns: columns;"></tr>
      </table>
    </mat-card>
  }
</div>
```

- [ ] **Step 5: Create the SCSS stub**

```scss
:host { display: block; }
```

- [ ] **Step 6: Build**

```bash
cd frontend/bus-booking-web && npm run build
```

- [ ] **Step 7: Commit**

```bash
git add frontend/bus-booking-web/src/app/features/admin/operator-requests/
git commit -m "feat(frontend): admin operator-requests approval page"
```

---

## Task 18: Admin bus-approvals page

**Files:**
- Create: `frontend/bus-booking-web/src/app/features/admin/bus-approvals/admin-bus-approvals-page.component.ts`
- Create: `frontend/bus-booking-web/src/app/features/admin/bus-approvals/admin-bus-approvals-page.component.html`
- Create: `frontend/bus-booking-web/src/app/features/admin/bus-approvals/admin-bus-approvals-page.component.scss`

- [ ] **Step 1: Create `admin-bus-approvals-page.component.ts`**

```typescript
import { Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { AdminBusesApiService } from '../../../core/api/admin-buses.api';
import { BusApprovalStatus, BusDto } from '../../../core/api/operator-buses.api';
import { RejectDialogComponent } from '../operator-requests/reject-dialog.component';

@Component({
  selector: 'app-admin-bus-approvals-page',
  standalone: true,
  imports: [
    MatCardModule, MatTableModule, MatButtonModule, MatButtonToggleModule,
    MatChipsModule, MatDialogModule, MatProgressSpinnerModule
  ],
  templateUrl: './admin-bus-approvals-page.component.html',
  styleUrl: './admin-bus-approvals-page.component.scss'
})
export class AdminBusApprovalsPageComponent {
  private readonly api = inject(AdminBusesApiService);
  private readonly dialog = inject(MatDialog);
  private readonly snack = inject(MatSnackBar);

  readonly filter = signal<BusApprovalStatus>('pending');
  readonly rows = signal<BusDto[]>([]);
  readonly loading = signal(true);
  readonly columns = ['registrationNumber', 'busName', 'busType', 'capacity', 'createdAt', 'status', 'actions'];

  constructor() { this.reload(); }

  setFilter(v: BusApprovalStatus): void { this.filter.set(v); this.reload(); }

  reload(): void {
    this.loading.set(true);
    this.api.list(this.filter()).subscribe({
      next: rows => { this.rows.set(rows); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  approve(bus: BusDto): void {
    this.api.approve(bus.id).subscribe({
      next: () => { this.snack.open('Bus approved', 'OK', { duration: 2000 }); this.reload(); }
    });
  }

  reject(bus: BusDto): void {
    const ref = this.dialog.open<RejectDialogComponent, unknown, string | null>(RejectDialogComponent);
    ref.afterClosed().subscribe(reason => {
      if (!reason) return;
      this.api.reject(bus.id, { reason }).subscribe({
        next: () => { this.snack.open('Bus rejected', 'OK', { duration: 2000 }); this.reload(); }
      });
    });
  }
}
```

- [ ] **Step 2: Create `admin-bus-approvals-page.component.html`**

```html
<div class="p-6">
  <h1 class="text-2xl font-semibold mb-4">Bus approvals</h1>

  <mat-button-toggle-group [value]="filter()" (change)="setFilter($event.value)" class="mb-4">
    <mat-button-toggle value="pending">Pending</mat-button-toggle>
    <mat-button-toggle value="approved">Approved</mat-button-toggle>
    <mat-button-toggle value="rejected">Rejected</mat-button-toggle>
  </mat-button-toggle-group>

  @if (loading()) {
    <mat-progress-spinner mode="indeterminate" diameter="28" />
  } @else if (rows().length === 0) {
    <mat-card><mat-card-content class="py-10 text-center opacity-70">No buses in this state.</mat-card-content></mat-card>
  } @else {
    <mat-card>
      <table mat-table [dataSource]="rows()" class="w-full">
        <ng-container matColumnDef="registrationNumber">
          <th mat-header-cell *matHeaderCellDef>Registration</th>
          <td mat-cell *matCellDef="let b">{{ b.registrationNumber }}</td>
        </ng-container>
        <ng-container matColumnDef="busName">
          <th mat-header-cell *matHeaderCellDef>Name</th>
          <td mat-cell *matCellDef="let b">{{ b.busName }}</td>
        </ng-container>
        <ng-container matColumnDef="busType">
          <th mat-header-cell *matHeaderCellDef>Type</th>
          <td mat-cell *matCellDef="let b">{{ b.busType }}</td>
        </ng-container>
        <ng-container matColumnDef="capacity">
          <th mat-header-cell *matHeaderCellDef>Capacity</th>
          <td mat-cell *matCellDef="let b">{{ b.capacity }}</td>
        </ng-container>
        <ng-container matColumnDef="createdAt">
          <th mat-header-cell *matHeaderCellDef>Submitted</th>
          <td mat-cell *matCellDef="let b">{{ b.createdAt | date:'short' }}</td>
        </ng-container>
        <ng-container matColumnDef="status">
          <th mat-header-cell *matHeaderCellDef>Status</th>
          <td mat-cell *matCellDef="let b">
            <mat-chip highlighted>{{ b.approvalStatus }}</mat-chip>
            @if (b.approvalStatus === 'rejected' && b.rejectReason) {
              <div class="text-xs opacity-70 mt-1">{{ b.rejectReason }}</div>
            }
          </td>
        </ng-container>
        <ng-container matColumnDef="actions">
          <th mat-header-cell *matHeaderCellDef></th>
          <td mat-cell *matCellDef="let b">
            @if (b.approvalStatus === 'pending') {
              <button mat-flat-button color="primary" class="mr-2" (click)="approve(b)">Approve</button>
              <button mat-stroked-button color="warn" (click)="reject(b)">Reject</button>
            }
          </td>
        </ng-container>
        <tr mat-header-row *matHeaderRowDef="columns"></tr>
        <tr mat-row *matRowDef="let row; columns: columns;"></tr>
      </table>
    </mat-card>
  }
</div>
```

- [ ] **Step 3: Create the SCSS stub**

```scss
:host { display: block; }
```

- [ ] **Step 4: Build**

```bash
cd frontend/bus-booking-web && npm run build
```

- [ ] **Step 5: Commit**

```bash
git add frontend/bus-booking-web/src/app/features/admin/bus-approvals/
git commit -m "feat(frontend): admin bus-approvals page"
```

---

## Task 19: Wire routes, navbar, admin dashboard links

**Files:**
- Modify: `frontend/bus-booking-web/src/app/app.routes.ts`
- Modify: `frontend/bus-booking-web/src/app/features/admin/admin-dashboard/admin-dashboard.component.html`
- Modify (or create): navbar — see Step 3

- [ ] **Step 1: Replace `app.routes.ts`**

Overwrite `frontend/bus-booking-web/src/app/app.routes.ts` with:

```typescript
import { Routes } from '@angular/router';
import { roleGuard } from './core/auth/role.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/public/home/home.component').then(m => m.HomeComponent)
  },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/register/register.component').then(m => m.RegisterComponent)
  },
  {
    path: 'become-operator',
    canMatch: [roleGuard(['customer'])],
    loadComponent: () =>
      import('./features/customer/become-operator/become-operator-page.component')
        .then(m => m.BecomeOperatorPageComponent)
  },
  {
    path: 'operator',
    canMatch: [roleGuard(['operator'])],
    loadComponent: () =>
      import('./features/operator/operator-shell/operator-shell.component')
        .then(m => m.OperatorShellComponent),
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./features/operator/dashboard/operator-dashboard.component')
            .then(m => m.OperatorDashboardComponent)
      },
      {
        path: 'offices',
        loadComponent: () =>
          import('./features/operator/offices/operator-offices-page.component')
            .then(m => m.OperatorOfficesPageComponent)
      },
      {
        path: 'buses',
        loadComponent: () =>
          import('./features/operator/buses/operator-buses-list.component')
            .then(m => m.OperatorBusesListComponent)
      },
      {
        path: 'buses/new',
        loadComponent: () =>
          import('./features/operator/buses/operator-bus-form.component')
            .then(m => m.OperatorBusFormComponent)
      }
    ]
  },
  {
    path: 'admin',
    canMatch: [roleGuard(['admin'])],
    loadComponent: () =>
      import('./features/admin/admin-dashboard/admin-dashboard.component')
        .then(m => m.AdminDashboardComponent)
  },
  {
    path: 'admin/operator-requests',
    canMatch: [roleGuard(['admin'])],
    loadComponent: () =>
      import('./features/admin/operator-requests/admin-operator-requests-page.component')
        .then(m => m.AdminOperatorRequestsPageComponent)
  },
  {
    path: 'admin/bus-approvals',
    canMatch: [roleGuard(['admin'])],
    loadComponent: () =>
      import('./features/admin/bus-approvals/admin-bus-approvals-page.component')
        .then(m => m.AdminBusApprovalsPageComponent)
  },
  { path: '**', redirectTo: '' }
];
```

- [ ] **Step 2: Add admin-dashboard quick links**

Open `frontend/bus-booking-web/src/app/features/admin/admin-dashboard/admin-dashboard.component.html`. Append this block (adjust to the file's existing layout; if it uses Material cards already, add two more cards with `routerLink`):

```html
<section class="mt-6 grid grid-cols-1 sm:grid-cols-2 gap-4">
  <a routerLink="/admin/operator-requests" class="block">
    <mat-card class="hover:shadow-md transition-shadow">
      <mat-card-content class="flex items-center gap-3 py-4">
        <mat-icon class="text-slate-500">person_add</mat-icon>
        <div>
          <div class="font-medium">Operator requests</div>
          <div class="text-sm opacity-70">Review and approve customer operator applications.</div>
        </div>
      </mat-card-content>
    </mat-card>
  </a>

  <a routerLink="/admin/bus-approvals" class="block">
    <mat-card class="hover:shadow-md transition-shadow">
      <mat-card-content class="flex items-center gap-3 py-4">
        <mat-icon class="text-slate-500">directions_bus</mat-icon>
        <div>
          <div class="font-medium">Bus approvals</div>
          <div class="text-sm opacity-70">Approve or reject buses submitted by operators.</div>
        </div>
      </mat-card-content>
    </mat-card>
  </a>
</section>
```

Then ensure the component imports `RouterLink`:

Open `admin-dashboard.component.ts` and add `import { RouterLink } from '@angular/router';` at the top, then add `RouterLink` to the `imports:` array.

- [ ] **Step 3: Update the navbar avatar menu**

Find the navbar. It may live in `src/app/app.html` (if the app shell renders it directly) or in `src/app/shared/components/navbar/*`. In either file, the logged-in avatar menu gets three new items (guarded by role):

```html
<!-- inside the avatar mat-menu, after existing items -->
@if (auth.hasRole('customer') && !auth.hasRole('operator')) {
  <a mat-menu-item routerLink="/become-operator">
    <mat-icon>upgrade</mat-icon>
    <span>Become an operator</span>
  </a>
}
@if (auth.hasRole('operator')) {
  <a mat-menu-item routerLink="/operator">
    <mat-icon>swap_horiz</mat-icon>
    <span>Switch to Operator Console</span>
  </a>
}
@if (auth.hasRole('admin')) {
  <a mat-menu-item routerLink="/admin">
    <mat-icon>admin_panel_settings</mat-icon>
    <span>Admin Console</span>
  </a>
}
```

In the component class, make sure `AuthStore` is injected as `readonly auth = inject(AuthStore)` and that `RouterLink`, `MatMenuModule`, `MatIconModule` are in `imports`.

If there is no navbar yet, extract the current avatar / login controls from `app.html` into a new `src/app/shared/components/navbar/navbar.component.{ts,html,scss}` in this step — keep the existing behavior intact.

- [ ] **Step 4: Build + lint**

```bash
cd frontend/bus-booking-web && npm run build
```

Expected: build succeeds with no TypeScript errors.

- [ ] **Step 5: Manually smoke-test the flow**

```bash
# Terminal 1 — backend
cd backend/BusBooking.Api
DOTNET_ROLL_FORWARD=Major dotnet run

# Terminal 2 — frontend
cd frontend/bus-booking-web
npm start
```

In the browser:
1. Register a new customer → log in.
2. Avatar menu shows "Become an operator" → click, fill the form, submit.
3. Log out. Log in as seeded admin (`admin@busbooking.local`).
4. Admin dashboard → "Operator requests" → approve the pending request.
5. Log out. Log in as the customer again — JWT now carries the operator role.
6. Avatar menu shows "Switch to Operator Console" → click.
7. Offices → add office with a seeded M2 city.
8. Buses → Add bus (rows 3, cols 4) → submitted for approval.
9. Log out. Log in as admin. "Bus approvals" → approve the bus.
10. Log in as operator. Buses list shows status "approved".

- [ ] **Step 6: Commit**

```bash
git add frontend/bus-booking-web/src/app/app.routes.ts \
        frontend/bus-booking-web/src/app/features/admin/admin-dashboard/ \
        frontend/bus-booking-web/src/app/app.html \
        frontend/bus-booking-web/src/app/shared/components/navbar/ 2>/dev/null || true
git commit -m "feat(frontend): wire M3 routes, navbar menu, admin dashboard links"
```

(The `2>/dev/null || true` handles the case where the navbar is still inline in `app.html` — adjust the `git add` list to the files you actually touched.)

---

## Task 20: Full-suite verification

**Files:** None — verification only.

- [ ] **Step 1: Run the full backend test suite**

```bash
cd backend/BusBooking.Api.Tests
DOTNET_ROLL_FORWARD=Major dotnet test
```

Expected: every M1 + M2 + M3 test passes. Take a screenshot or copy the summary line (e.g. `Passed! - Failed: 0, Passed: 42`) for the commit message.

- [ ] **Step 2: Run the full frontend build + unit tests**

```bash
cd frontend/bus-booking-web
npm run build
npm test -- --watch=false --browsers=ChromeHeadless
```

Expected: build succeeds; unit tests pass.

- [ ] **Step 3: Manual verification against the spec demoable outcome**

Re-read the M3 row in the spec's "Delivery milestones" table:

> M3 | Operator onboarding: offices + buses + approvals | Customer requests operator; admin approves; operator adds offices + bus; admin approves bus.

Confirm each of the four bolded verbs was exercised during the Task 19 Step 5 smoke test.

- [ ] **Step 4: Audit-log spot check**

```bash
psql -d bus_booking -c "SELECT action, target_type, count(*) FROM audit_log GROUP BY 1, 2 ORDER BY 1;"
```

Expected at least: `OPERATOR_REQUEST_APPROVED`, `OFFICE_CREATED`, `BUS_CREATED`, `BUS_APPROVED` rows.

- [ ] **Step 5: Final commit (if anything was fixed during verification)**

Otherwise, no commit needed.

---

## Acceptance criteria

- [ ] Customer with only the `customer` role can `POST /me/become-operator` once while no request is pending.
- [ ] Repeat submission while a request is pending returns 422 `REQUEST_ALREADY_PENDING`.
- [ ] A customer who already has the operator role gets 422 `ALREADY_OPERATOR`.
- [ ] Admin can list operator requests filtered by `status` and approve or reject.
- [ ] Approval inserts `user_roles('operator')`, stamps `reviewed_at`/`reviewed_by_admin_id`, writes an `OPERATOR_REQUEST_APPROVED` audit row, and calls `INotificationSender.SendOperatorApprovedAsync`.
- [ ] Rejection stores the reason, writes an `OPERATOR_REQUEST_REJECTED` audit row, and calls `SendOperatorRejectedAsync`. The operator role is not granted.
- [ ] An operator can CRUD offices (city + address + phone); duplicate `(operator, city)` returns 409; unknown city returns 404; deleting another operator's office returns 403.
- [ ] An operator can create a bus by registration + name + type + rows + columns; seat definitions are auto-generated with labels `A1..<row>{<col>}`; capacity = rows × columns; `approval_status = 'pending'`, `operational_status = 'active'`.
- [ ] Operator can PATCH a bus's operational status between `active` and `under_maintenance`; retired buses reject further status changes.
- [ ] Operator DELETE soft-retires the bus (status = `retired`).
- [ ] Admin GET `/admin/buses?status=pending` returns only pending buses; approve flips to `approved` + audit row + notification; reject stores reason.
- [ ] Bearer-less requests → 401; wrong-role tokens → 403 on every M3 endpoint.
- [ ] Frontend: customer sees "Become an operator" in avatar menu; operator sees "Switch to Operator Console"; admin sees the two new quick-links on dashboard.
- [ ] Full backend test suite passes (including M1/M2 regressions).
- [ ] Angular build succeeds; existing unit tests pass.

---

## Risks and open questions

- **Role updates require re-login.** Per spec §11, approval only takes effect on the user's next login because roles are embedded in the JWT at issue time. Acceptable for M3; call it out in the README demo script.
- **Email stubbing.** `LoggingNotificationSender` logs but does not send. M5 swaps in a Resend-backed implementation. If the demo viewer expects real emails before then, either finish M5 first or add a temporary `Resend` implementation gated by a feature flag.
- **Future-bookings guard on `DELETE /operator/buses/{id}`.** Bookings do not exist until M5, so the guard is deferred; a comment in `BusService.RetireAsync` marks the exact insertion point.
- **City autocomplete dependency in office dialog.** If M2 Task 10 (`CityAutocompleteComponent`) has not shipped, the add-office dialog falls back to a `mat-select` — see Task 15 Step 6 note.
- **Test-DB cascade ordering.** `ResetAsync` now truncates 10 tables. If future milestones add FK relationships pointing into M3 tables, those truncate calls need updating too; this plan leaves that for later milestones to handle.
- **Audit log retention.** No retention/rotation policy is set. Out of scope for M3; revisit in M9 (polish).

