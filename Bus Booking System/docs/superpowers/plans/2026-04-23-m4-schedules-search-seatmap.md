# M4 — Schedules, Search & Seat Map (View-Only) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. **Work directly on `main` — do NOT create a feature branch.** Commit messages MUST NOT include a `Co-Authored-By: Claude` trailer.

**Goal:** Deliver the M4 demoable outcome: an anonymous user searches for buses between two cities on a date, sees a list of trip results, clicks through to a trip detail page, and views the seat map in read-only mode. Operators can create and manage schedules from the operator console.

**Architecture:** Two new EF Core 9 entities (`bus_schedules`, `bus_trips`) added to `AppDbContext`. `ScheduleService` handles operator schedule CRUD with business-rule guards (`BUS_NOT_APPROVED`, `NO_OFFICE_AT_CITY`). `TripService` handles anonymous search (materialising `bus_trips` rows on first access), trip detail, and seat layout. Three new controllers: `OperatorSchedulesController` (operator-auth), `SearchController` (anonymous), `TripsController` (anonymous). Angular adds operator schedule list + form pages, a shared read-only `SeatMapComponent`, and two public pages (`SearchResultsComponent`, `TripDetailComponent`). `HomeComponent` gains a `MatDatepicker` and search button.

**Tech Stack:** .NET 9 · EF Core 9 · Npgsql · `DateOnly`/`TimeOnly` · FluentValidation · xUnit · FluentAssertions · `Microsoft.AspNetCore.Mvc.Testing` · Angular 20 (standalone + Signals) · Angular Material (`MatDatepicker`, `MatTable`, `MatCard`) · Tailwind v3 · PostgreSQL.

---

## File map

### New backend files

| Path | Responsibility |
|---|---|
| `backend/BusBooking.Api/Models/BusSchedule.cs` | `bus_schedules` entity |
| `backend/BusBooking.Api/Models/BusTrip.cs` | `bus_trips` entity |
| `backend/BusBooking.Api/Models/TripStatus.cs` | `scheduled` / `cancelled` / `completed` string constants |
| `backend/BusBooking.Api/Dtos/BusScheduleDto.cs` | Schedule response shape |
| `backend/BusBooking.Api/Dtos/RouteOptionDto.cs` | Route dropdown item (id + city names) |
| `backend/BusBooking.Api/Dtos/CreateBusScheduleRequest.cs` | Operator create payload |
| `backend/BusBooking.Api/Dtos/UpdateBusScheduleRequest.cs` | Operator PATCH payload |
| `backend/BusBooking.Api/Dtos/SearchResultDto.cs` | Anonymous search result shape |
| `backend/BusBooking.Api/Dtos/TripDetailDto.cs` | Trip detail response shape |
| `backend/BusBooking.Api/Dtos/SeatLayoutDto.cs` | Rows × cols + per-seat statuses |
| `backend/BusBooking.Api/Validators/CreateBusScheduleRequestValidator.cs` | FluentValidation |
| `backend/BusBooking.Api/Validators/UpdateBusScheduleRequestValidator.cs` | FluentValidation |
| `backend/BusBooking.Api/Services/IScheduleService.cs` | Schedule CRUD + active routes contract |
| `backend/BusBooking.Api/Services/ScheduleService.cs` | Schedule CRUD + business-rule guards |
| `backend/BusBooking.Api/Services/ITripService.cs` | Search / materialize / detail / seats contract |
| `backend/BusBooking.Api/Services/TripService.cs` | Trip materialization + seat layout assembly |
| `backend/BusBooking.Api/Controllers/OperatorSchedulesController.cs` | `GET/POST/PATCH/DELETE /operator/schedules` + `GET /operator/routes` |
| `backend/BusBooking.Api/Controllers/SearchController.cs` | `GET /search` (anonymous) |
| `backend/BusBooking.Api/Controllers/TripsController.cs` | `GET /trips/{id}` + `GET /trips/{id}/seats` (anonymous) |

### Modified backend files

- `backend/BusBooking.Api/Infrastructure/AppDbContext.cs` — add `DbSet<BusSchedule>`, `DbSet<BusTrip>` + EF mappings
- `backend/BusBooking.Api/Program.cs` — DI: `IScheduleService → ScheduleService`, `ITripService → TripService`

### New test files

| Path | Responsibility |
|---|---|
| `backend/BusBooking.Api.Tests/Unit/ScheduleDayOfWeekTests.cs` | Unit: `ScheduleRunsOnDate` bitmask helper |
| `backend/BusBooking.Api.Tests/Integration/OperatorSchedulesTests.cs` | CRUD + `BUS_NOT_APPROVED` + `NO_OFFICE_AT_CITY` |
| `backend/BusBooking.Api.Tests/Integration/SearchTests.cs` | Search materializes trips; trip detail; seat layout |

### New frontend files

| Path | Responsibility |
|---|---|
| `frontend/bus-booking-web/src/app/core/api/schedules.api.ts` | `SchedulesApiService`: schedule CRUD + active routes list |
| `frontend/bus-booking-web/src/app/core/api/search.api.ts` | `SearchApiService`: search, trip detail, seat layout |
| `frontend/bus-booking-web/src/app/shared/components/seat-map/seat-map.component.ts` | Read-only color-coded seat grid |
| `frontend/bus-booking-web/src/app/shared/components/seat-map/seat-map.component.html` | Seat grid template |
| `frontend/bus-booking-web/src/app/features/operator/schedules/operator-schedules-list.component.ts` | Schedule list + create/edit/delete actions |
| `frontend/bus-booking-web/src/app/features/operator/schedules/operator-schedules-list.component.html` | mat-table template |
| `frontend/bus-booking-web/src/app/features/operator/schedules/operator-schedule-form.component.ts` | Create/edit schedule reactive form |
| `frontend/bus-booking-web/src/app/features/operator/schedules/operator-schedule-form.component.html` | Form template |
| `frontend/bus-booking-web/src/app/features/public/search-results/search-results.component.ts` | Trip list from search |
| `frontend/bus-booking-web/src/app/features/public/search-results/search-results.component.html` | Trip cards template |
| `frontend/bus-booking-web/src/app/features/public/trip-detail/trip-detail.component.ts` | Trip detail + seat map |
| `frontend/bus-booking-web/src/app/features/public/trip-detail/trip-detail.component.html` | Trip detail template |

### Modified frontend files

- `frontend/bus-booking-web/src/app/features/public/home/home.component.ts` — add `travelDate` signal, date picker, search button + `Router` navigation
- `frontend/bus-booking-web/src/app/features/public/home/home.component.html` — add `MatDatepicker` + search `<button>`
- `frontend/bus-booking-web/src/app/app.routes.ts` — add `search-results`, `trips/:id`, `operator/schedules` routes
- `frontend/bus-booking-web/src/app/features/operator/operator-shell/operator-shell.component.html` — add Schedules nav link

---

## Prerequisites

- M3 complete: `AppDbContext` has `Buses`, `SeatDefinitions`, `OperatorOffices`, `Routes`, `Cities`.
- An operator user with at least one approved bus and offices at both cities of a route must be seed-able via tests.

---

## Task 1: BusSchedule + BusTrip entities + EF mapping + migration

**Files:**
- Create: `backend/BusBooking.Api/Models/BusSchedule.cs`
- Create: `backend/BusBooking.Api/Models/BusTrip.cs`
- Create: `backend/BusBooking.Api/Models/TripStatus.cs`
- Modify: `backend/BusBooking.Api/Infrastructure/AppDbContext.cs`

- [ ] **Step 1: Create TripStatus constants**

```csharp
// backend/BusBooking.Api/Models/TripStatus.cs
namespace BusBooking.Api.Models;

public static class TripStatus
{
    public const string Scheduled = "scheduled";
    public const string Cancelled = "cancelled";
    public const string Completed = "completed";
}
```

- [ ] **Step 2: Create BusSchedule entity**

```csharp
// backend/BusBooking.Api/Models/BusSchedule.cs
namespace BusBooking.Api.Models;

public class BusSchedule
{
    public Guid Id { get; set; }
    public Guid BusId { get; set; }
    public Guid RouteId { get; set; }
    public TimeOnly DepartureTime { get; set; }
    public TimeOnly ArrivalTime { get; set; }
    public decimal FarePerSeat { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly ValidTo { get; set; }
    /// <summary>Bitmask: Mon=1,Tue=2,Wed=4,Thu=8,Fri=16,Sat=32,Sun=64</summary>
    public int DaysOfWeek { get; set; }
    public bool IsActive { get; set; } = true;

    public Bus? Bus { get; set; }
    public Route? Route { get; set; }
}
```

- [ ] **Step 3: Create BusTrip entity**

```csharp
// backend/BusBooking.Api/Models/BusTrip.cs
namespace BusBooking.Api.Models;

public class BusTrip
{
    public Guid Id { get; set; }
    public Guid ScheduleId { get; set; }
    public DateOnly TripDate { get; set; }
    public string Status { get; set; } = TripStatus.Scheduled;
    public string? CancelReason { get; set; }

    public BusSchedule? Schedule { get; set; }
}
```

- [ ] **Step 4: Add DbSets and EF mappings in AppDbContext**

Add to the `DbSet` properties block (after `AuditLog`):
```csharp
public DbSet<BusSchedule> BusSchedules => Set<BusSchedule>();
public DbSet<BusTrip> BusTrips => Set<BusTrip>();
```

Add to `OnModelCreating` (after the `AuditLogEntry` block):
```csharp
modelBuilder.Entity<BusSchedule>(b =>
{
    b.ToTable("bus_schedules");
    b.HasKey(s => s.Id);
    b.Property(s => s.Id).HasColumnName("id");
    b.Property(s => s.BusId).HasColumnName("bus_id");
    b.Property(s => s.RouteId).HasColumnName("route_id");
    b.Property(s => s.DepartureTime).HasColumnName("departure_time").HasColumnType("time");
    b.Property(s => s.ArrivalTime).HasColumnName("arrival_time").HasColumnType("time");
    b.Property(s => s.FarePerSeat).HasColumnName("fare_per_seat").HasColumnType("decimal(10,2)");
    b.Property(s => s.ValidFrom).HasColumnName("valid_from").HasColumnType("date");
    b.Property(s => s.ValidTo).HasColumnName("valid_to").HasColumnType("date");
    b.Property(s => s.DaysOfWeek).HasColumnName("days_of_week");
    b.Property(s => s.IsActive).HasColumnName("is_active");
    b.HasIndex(s => new { s.RouteId, s.IsActive });
    b.HasOne(s => s.Bus).WithMany().HasForeignKey(s => s.BusId).OnDelete(DeleteBehavior.Cascade);
    b.HasOne(s => s.Route).WithMany().HasForeignKey(s => s.RouteId).OnDelete(DeleteBehavior.Restrict);
});

modelBuilder.Entity<BusTrip>(b =>
{
    b.ToTable("bus_trips");
    b.HasKey(t => t.Id);
    b.Property(t => t.Id).HasColumnName("id");
    b.Property(t => t.ScheduleId).HasColumnName("schedule_id");
    b.Property(t => t.TripDate).HasColumnName("trip_date").HasColumnType("date");
    b.Property(t => t.Status).HasColumnName("status").IsRequired().HasMaxLength(16);
    b.Property(t => t.CancelReason).HasColumnName("cancel_reason").HasMaxLength(500);
    b.HasIndex(t => new { t.ScheduleId, t.TripDate }).IsUnique();
    b.HasOne(t => t.Schedule).WithMany().HasForeignKey(t => t.ScheduleId).OnDelete(DeleteBehavior.Cascade);
});
```

- [ ] **Step 5: Generate and apply EF migration**

```bash
cd backend/BusBooking.Api
dotnet ef migrations add AddSchedulesDomain
dotnet ef database update
```

Expected: migration file created in `Migrations/`, database updated with `bus_schedules` and `bus_trips` tables.

- [ ] **Step 6: Verify build passes**

```bash
cd backend
dotnet build BusBookingSystem.sln
```

Expected: `Build succeeded.`

- [ ] **Step 7: Commit**

```bash
git add backend/BusBooking.Api/Models/BusSchedule.cs \
        backend/BusBooking.Api/Models/BusTrip.cs \
        backend/BusBooking.Api/Models/TripStatus.cs \
        backend/BusBooking.Api/Infrastructure/AppDbContext.cs \
        backend/BusBooking.Api/Migrations/
git commit -m "feat: add BusSchedule and BusTrip entities with EF migration (M4)"
```

---

## Task 2: Schedule DTOs + validators

**Files:**
- Create: `backend/BusBooking.Api/Dtos/BusScheduleDto.cs`
- Create: `backend/BusBooking.Api/Dtos/RouteOptionDto.cs`
- Create: `backend/BusBooking.Api/Dtos/CreateBusScheduleRequest.cs`
- Create: `backend/BusBooking.Api/Dtos/UpdateBusScheduleRequest.cs`
- Create: `backend/BusBooking.Api/Dtos/SearchResultDto.cs`
- Create: `backend/BusBooking.Api/Dtos/TripDetailDto.cs`
- Create: `backend/BusBooking.Api/Dtos/SeatLayoutDto.cs`
- Create: `backend/BusBooking.Api/Validators/CreateBusScheduleRequestValidator.cs`
- Create: `backend/BusBooking.Api/Validators/UpdateBusScheduleRequestValidator.cs`

- [ ] **Step 1: Create all DTOs**

```csharp
// backend/BusBooking.Api/Dtos/BusScheduleDto.cs
namespace BusBooking.Api.Dtos;

public record BusScheduleDto(
    Guid Id,
    Guid BusId,
    string BusName,
    Guid RouteId,
    string SourceCityName,
    string DestinationCityName,
    TimeOnly DepartureTime,
    TimeOnly ArrivalTime,
    decimal FarePerSeat,
    DateOnly ValidFrom,
    DateOnly ValidTo,
    int DaysOfWeek,
    bool IsActive
);
```

```csharp
// backend/BusBooking.Api/Dtos/RouteOptionDto.cs
namespace BusBooking.Api.Dtos;

public record RouteOptionDto(
    Guid Id,
    string SourceCityName,
    string DestinationCityName,
    decimal? DistanceKm
);
```

```csharp
// backend/BusBooking.Api/Dtos/CreateBusScheduleRequest.cs
namespace BusBooking.Api.Dtos;

public record CreateBusScheduleRequest(
    Guid BusId,
    Guid RouteId,
    TimeOnly DepartureTime,
    TimeOnly ArrivalTime,
    decimal FarePerSeat,
    DateOnly ValidFrom,
    DateOnly ValidTo,
    int DaysOfWeek
);
```

```csharp
// backend/BusBooking.Api/Dtos/UpdateBusScheduleRequest.cs
namespace BusBooking.Api.Dtos;

public record UpdateBusScheduleRequest(
    TimeOnly? DepartureTime,
    TimeOnly? ArrivalTime,
    decimal? FarePerSeat,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    int? DaysOfWeek,
    bool? IsActive
);
```

```csharp
// backend/BusBooking.Api/Dtos/SearchResultDto.cs
namespace BusBooking.Api.Dtos;

public record SearchResultDto(
    Guid TripId,
    string BusName,
    string BusType,
    string OperatorName,
    TimeOnly DepartureTime,
    TimeOnly ArrivalTime,
    decimal FarePerSeat,
    int SeatsLeft,
    string PickupAddress,
    string DropAddress
);
```

```csharp
// backend/BusBooking.Api/Dtos/TripDetailDto.cs
namespace BusBooking.Api.Dtos;

public record TripDetailDto(
    Guid TripId,
    Guid BusId,
    string BusName,
    string BusType,
    string OperatorName,
    DateOnly TripDate,
    TimeOnly DepartureTime,
    TimeOnly ArrivalTime,
    decimal FarePerSeat,
    int SeatsLeft,
    string SourceCityName,
    string DestinationCityName,
    string? PickupAddress,
    string? DropAddress,
    SeatLayoutDto SeatLayout
);
```

```csharp
// backend/BusBooking.Api/Dtos/SeatLayoutDto.cs
namespace BusBooking.Api.Dtos;

public record SeatLayoutDto(
    int Rows,
    int Columns,
    IReadOnlyList<SeatStatusDto> Seats
);

public record SeatStatusDto(
    string SeatNumber,
    int RowIndex,
    int ColumnIndex,
    string Status   // "available" | "locked" | "booked"
);
```

- [ ] **Step 2: Create validators**

```csharp
// backend/BusBooking.Api/Validators/CreateBusScheduleRequestValidator.cs
using BusBooking.Api.Dtos;
using FluentValidation;

namespace BusBooking.Api.Validators;

public class CreateBusScheduleRequestValidator : AbstractValidator<CreateBusScheduleRequest>
{
    public CreateBusScheduleRequestValidator()
    {
        RuleFor(x => x.BusId).NotEmpty();
        RuleFor(x => x.RouteId).NotEmpty();
        RuleFor(x => x.FarePerSeat).GreaterThan(0);
        RuleFor(x => x.ValidFrom).NotEmpty();
        RuleFor(x => x.ValidTo).GreaterThanOrEqualTo(x => x.ValidFrom)
            .WithMessage("ValidTo must be on or after ValidFrom");
        RuleFor(x => x.DaysOfWeek).InclusiveBetween(1, 127)
            .WithMessage("DaysOfWeek must be a bitmask between 1 and 127 (at least one day selected)");
    }
}
```

```csharp
// backend/BusBooking.Api/Validators/UpdateBusScheduleRequestValidator.cs
using BusBooking.Api.Dtos;
using FluentValidation;

namespace BusBooking.Api.Validators;

public class UpdateBusScheduleRequestValidator : AbstractValidator<UpdateBusScheduleRequest>
{
    public UpdateBusScheduleRequestValidator()
    {
        When(x => x.FarePerSeat.HasValue, () =>
            RuleFor(x => x.FarePerSeat!.Value).GreaterThan(0));
        When(x => x.DaysOfWeek.HasValue, () =>
            RuleFor(x => x.DaysOfWeek!.Value).InclusiveBetween(1, 127)
                .WithMessage("DaysOfWeek must be a bitmask between 1 and 127"));
        When(x => x.ValidFrom.HasValue && x.ValidTo.HasValue, () =>
            RuleFor(x => x.ValidTo!.Value).GreaterThanOrEqualTo(x => x.ValidFrom!.Value)
                .WithMessage("ValidTo must be on or after ValidFrom"));
    }
}
```

- [ ] **Step 3: Verify build**

```bash
cd backend && dotnet build BusBookingSystem.sln
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add backend/BusBooking.Api/Dtos/ backend/BusBooking.Api/Validators/CreateBusScheduleRequestValidator.cs \
        backend/BusBooking.Api/Validators/UpdateBusScheduleRequestValidator.cs
git commit -m "feat: add M4 schedule and search DTOs with validators"
```

---

## Task 3: Unit test for days-of-week bitmask + IScheduleService + ScheduleService

**Files:**
- Create: `backend/BusBooking.Api.Tests/Unit/ScheduleDayOfWeekTests.cs`
- Create: `backend/BusBooking.Api/Services/IScheduleService.cs`
- Create: `backend/BusBooking.Api/Services/ScheduleService.cs`

- [ ] **Step 1: Write the failing unit test**

```csharp
// backend/BusBooking.Api.Tests/Unit/ScheduleDayOfWeekTests.cs
using BusBooking.Api.Services;
using FluentAssertions;

namespace BusBooking.Api.Tests.Unit;

public class ScheduleDayOfWeekTests
{
    // 2026-04-27 is a Monday
    [Theory]
    [InlineData("2026-04-27", 1,   true)]   // Monday, Mon flag set
    [InlineData("2026-04-27", 2,   false)]  // Monday, only Tue flag set
    [InlineData("2026-04-27", 127, true)]   // Monday, all days set
    [InlineData("2026-04-26", 64,  true)]   // Sunday = bit6 = 64
    [InlineData("2026-04-25", 32,  true)]   // Saturday = bit5 = 32
    [InlineData("2026-04-24", 16,  true)]   // Friday = bit4 = 16
    [InlineData("2026-04-23", 8,   true)]   // Thursday = bit3 = 8
    [InlineData("2026-04-22", 4,   true)]   // Wednesday = bit2 = 4
    [InlineData("2026-04-28", 2,   true)]   // Tuesday = bit1 = 2
    public void GetDayBit_returns_correct_flag(string dateStr, int mask, bool expected)
    {
        var date = DateOnly.Parse(dateStr);
        ScheduleService.GetDayBit(date.DayOfWeek).Should().Match<int>(bit => ((mask & bit) != 0) == expected);
    }
}
```

- [ ] **Step 2: Run test to confirm it fails (class not defined)**

```bash
cd backend && dotnet test BusBooking.Api.Tests/BusBooking.Api.Tests.csproj \
    --filter "ScheduleDayOfWeekTests" --no-build 2>&1 | tail -5
```

Expected: Build error or test failure — `ScheduleService` does not exist yet.

- [ ] **Step 3: Create IScheduleService**

```csharp
// backend/BusBooking.Api/Services/IScheduleService.cs
using BusBooking.Api.Dtos;

namespace BusBooking.Api.Services;

public interface IScheduleService
{
    Task<IReadOnlyList<BusScheduleDto>> ListAsync(Guid operatorUserId, Guid? busId, CancellationToken ct);
    Task<BusScheduleDto> CreateAsync(Guid operatorUserId, CreateBusScheduleRequest req, CancellationToken ct);
    Task<BusScheduleDto> UpdateAsync(Guid operatorUserId, Guid scheduleId, UpdateBusScheduleRequest req, CancellationToken ct);
    Task DeleteAsync(Guid operatorUserId, Guid scheduleId, CancellationToken ct);
    Task<IReadOnlyList<RouteOptionDto>> ListActiveRoutesAsync(CancellationToken ct);
}
```

- [ ] **Step 4: Create ScheduleService with public GetDayBit helper**

```csharp
// backend/BusBooking.Api/Services/ScheduleService.cs
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Infrastructure.Errors;
using BusBooking.Api.Models;
using Microsoft.EntityFrameworkCore;
using Route = BusBooking.Api.Models.Route;

namespace BusBooking.Api.Services;

public class ScheduleService : IScheduleService
{
    private readonly AppDbContext _db;

    public ScheduleService(AppDbContext db) => _db = db;

    public static int GetDayBit(DayOfWeek dow) => dow switch
    {
        DayOfWeek.Monday    => 1,
        DayOfWeek.Tuesday   => 2,
        DayOfWeek.Wednesday => 4,
        DayOfWeek.Thursday  => 8,
        DayOfWeek.Friday    => 16,
        DayOfWeek.Saturday  => 32,
        DayOfWeek.Sunday    => 64,
        _                   => 0
    };

    public async Task<IReadOnlyList<BusScheduleDto>> ListAsync(
        Guid operatorUserId, Guid? busId, CancellationToken ct)
    {
        var query = _db.BusSchedules
            .AsNoTracking()
            .Include(s => s.Bus)
            .Include(s => s.Route).ThenInclude(r => r!.SourceCity)
            .Include(s => s.Route).ThenInclude(r => r!.DestinationCity)
            .Where(s => s.Bus!.OperatorUserId == operatorUserId);

        if (busId.HasValue)
            query = query.Where(s => s.BusId == busId.Value);

        return await query
            .OrderBy(s => s.Route!.SourceCity!.Name)
            .ThenBy(s => s.DepartureTime)
            .Select(s => ToDto(s))
            .ToListAsync(ct);
    }

    public async Task<BusScheduleDto> CreateAsync(
        Guid operatorUserId, CreateBusScheduleRequest req, CancellationToken ct)
    {
        var bus = await _db.Buses
            .FirstOrDefaultAsync(b => b.Id == req.BusId && b.OperatorUserId == operatorUserId, ct)
            ?? throw new NotFoundException("Bus not found");

        if (bus.ApprovalStatus != BusApprovalStatus.Approved)
            throw new BusinessRuleException("BUS_NOT_APPROVED", "Bus must be approved before creating schedules");

        var route = await _db.Routes
            .Include(r => r.SourceCity)
            .Include(r => r.DestinationCity)
            .FirstOrDefaultAsync(r => r.Id == req.RouteId && r.IsActive, ct)
            ?? throw new NotFoundException("Route not found");

        await RequireOfficeAsync(operatorUserId, route.SourceCityId, ct);
        await RequireOfficeAsync(operatorUserId, route.DestinationCityId, ct);

        var schedule = new BusSchedule
        {
            Id             = Guid.NewGuid(),
            BusId          = req.BusId,
            RouteId        = req.RouteId,
            DepartureTime  = req.DepartureTime,
            ArrivalTime    = req.ArrivalTime,
            FarePerSeat    = req.FarePerSeat,
            ValidFrom      = req.ValidFrom,
            ValidTo        = req.ValidTo,
            DaysOfWeek     = req.DaysOfWeek,
            IsActive       = true
        };
        _db.BusSchedules.Add(schedule);
        await _db.SaveChangesAsync(ct);

        schedule.Bus   = bus;
        schedule.Route = route;
        return ToDto(schedule);
    }

    public async Task<BusScheduleDto> UpdateAsync(
        Guid operatorUserId, Guid scheduleId, UpdateBusScheduleRequest req, CancellationToken ct)
    {
        var schedule = await _db.BusSchedules
            .Include(s => s.Bus)
            .Include(s => s.Route).ThenInclude(r => r!.SourceCity)
            .Include(s => s.Route).ThenInclude(r => r!.DestinationCity)
            .FirstOrDefaultAsync(s => s.Id == scheduleId && s.Bus!.OperatorUserId == operatorUserId, ct)
            ?? throw new NotFoundException("Schedule not found");

        if (req.DepartureTime.HasValue) schedule.DepartureTime = req.DepartureTime.Value;
        if (req.ArrivalTime.HasValue)   schedule.ArrivalTime   = req.ArrivalTime.Value;
        if (req.FarePerSeat.HasValue)   schedule.FarePerSeat   = req.FarePerSeat.Value;
        if (req.ValidFrom.HasValue)     schedule.ValidFrom     = req.ValidFrom.Value;
        if (req.ValidTo.HasValue)       schedule.ValidTo       = req.ValidTo.Value;
        if (req.DaysOfWeek.HasValue)    schedule.DaysOfWeek    = req.DaysOfWeek.Value;
        if (req.IsActive.HasValue)      schedule.IsActive      = req.IsActive.Value;

        await _db.SaveChangesAsync(ct);
        return ToDto(schedule);
    }

    public async Task DeleteAsync(Guid operatorUserId, Guid scheduleId, CancellationToken ct)
    {
        var schedule = await _db.BusSchedules
            .Include(s => s.Bus)
            .FirstOrDefaultAsync(s => s.Id == scheduleId && s.Bus!.OperatorUserId == operatorUserId, ct)
            ?? throw new NotFoundException("Schedule not found");

        _db.BusSchedules.Remove(schedule);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<RouteOptionDto>> ListActiveRoutesAsync(CancellationToken ct)
        => await _db.Routes
            .AsNoTracking()
            .Include(r => r.SourceCity)
            .Include(r => r.DestinationCity)
            .Where(r => r.IsActive)
            .OrderBy(r => r.SourceCity!.Name)
            .ThenBy(r => r.DestinationCity!.Name)
            .Select(r => new RouteOptionDto(
                r.Id,
                r.SourceCity!.Name,
                r.DestinationCity!.Name,
                r.DistanceKm))
            .ToListAsync(ct);

    private async Task RequireOfficeAsync(Guid operatorUserId, Guid cityId, CancellationToken ct)
    {
        var hasOffice = await _db.OperatorOffices.AnyAsync(
            o => o.OperatorUserId == operatorUserId && o.CityId == cityId && o.IsActive, ct);
        if (!hasOffice)
            throw new BusinessRuleException("NO_OFFICE_AT_CITY",
                "Operator must have an active office in every city on the route");
    }

    private static BusScheduleDto ToDto(BusSchedule s) => new(
        s.Id,
        s.BusId,
        s.Bus!.BusName,
        s.RouteId,
        s.Route!.SourceCity!.Name,
        s.Route.DestinationCity!.Name,
        s.DepartureTime,
        s.ArrivalTime,
        s.FarePerSeat,
        s.ValidFrom,
        s.ValidTo,
        s.DaysOfWeek,
        s.IsActive
    );
}
```

- [ ] **Step 5: Run unit tests — should pass now**

```bash
cd backend && dotnet test BusBooking.Api.Tests/BusBooking.Api.Tests.csproj \
    --filter "ScheduleDayOfWeekTests" -v n
```

Expected: `9 passed, 0 failed`

- [ ] **Step 6: Commit**

```bash
git add backend/BusBooking.Api/Services/IScheduleService.cs \
        backend/BusBooking.Api/Services/ScheduleService.cs \
        backend/BusBooking.Api.Tests/Unit/ScheduleDayOfWeekTests.cs
git commit -m "feat: add ScheduleService with days-of-week bitmask helper (M4)"
```

---

## Task 4: OperatorSchedulesController + integration tests

**Files:**
- Create: `backend/BusBooking.Api/Controllers/OperatorSchedulesController.cs`
- Create: `backend/BusBooking.Api.Tests/Integration/OperatorSchedulesTests.cs`
- Modify: `backend/BusBooking.Api/Program.cs`

- [ ] **Step 1: Write the failing integration tests**

```csharp
// backend/BusBooking.Api.Tests/Integration/OperatorSchedulesTests.cs
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
public class OperatorSchedulesTests : IAsyncLifetime
{
    private readonly IntegrationFixture _fx;
    public OperatorSchedulesTests(IntegrationFixture fx) => _fx = fx;
    public async Task InitializeAsync() => await _fx.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // Seeds: operator user + approved bus + two cities + route + offices at both cities
    private async Task<(User op, string token, Bus bus, Models.Route route)> SeedOperatorWithApprovedBus()
    {
        var (op, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Operator]);

        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var srcCity = new City { Id = Guid.NewGuid(), Name = "Chennai",   State = "TN", IsActive = true };
        var dstCity = new City { Id = Guid.NewGuid(), Name = "Bangalore", State = "KA", IsActive = true };
        db.Cities.AddRange(srcCity, dstCity);

        var route = new Models.Route
        {
            Id = Guid.NewGuid(), SourceCityId = srcCity.Id, DestinationCityId = dstCity.Id, IsActive = true
        };
        db.Routes.Add(route);

        db.OperatorOffices.AddRange(
            new OperatorOffice { Id = Guid.NewGuid(), OperatorUserId = op.Id, CityId = srcCity.Id, AddressLine = "1 Main St", Phone = "9999999999", IsActive = true },
            new OperatorOffice { Id = Guid.NewGuid(), OperatorUserId = op.Id, CityId = dstCity.Id, AddressLine = "2 MG Road",  Phone = "8888888888", IsActive = true }
        );

        var bus = new Bus
        {
            Id = Guid.NewGuid(), OperatorUserId = op.Id,
            RegistrationNumber = "TN-01-AA-0001", BusName = "Express One",
            BusType = BusType.Seater, Capacity = 40,
            ApprovalStatus = BusApprovalStatus.Approved,
            OperationalStatus = BusOperationalStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        db.Buses.Add(bus);
        await db.SaveChangesAsync();

        return (op, token, bus, route);
    }

    private static CreateBusScheduleRequest SampleSchedule(Guid busId, Guid routeId) => new(
        busId, routeId,
        new TimeOnly(8, 0), new TimeOnly(14, 0),
        350m,
        DateOnly.FromDateTime(DateTime.UtcNow),
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
        127   // all days
    );

    [Fact]
    public async Task Create_valid_schedule_returns_201()
    {
        var (_, token, bus, route) = await SeedOperatorWithApprovedBus();
        var client = _fx.CreateClient(); client.AttachBearer(token);

        var resp = await client.PostAsJsonAsync("/api/v1/operator/schedules", SampleSchedule(bus.Id, route.Id));
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await resp.Content.ReadFromJsonAsync<BusScheduleDto>();
        dto!.BusId.Should().Be(bus.Id);
        dto.DaysOfWeek.Should().Be(127);
        dto.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Create_with_unapproved_bus_returns_422()
    {
        var (op, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Operator], email: "unapproved@t.local");
        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var srcCity = new City { Id = Guid.NewGuid(), Name = "CityA", State = "XX", IsActive = true };
        var dstCity = new City { Id = Guid.NewGuid(), Name = "CityB", State = "XX", IsActive = true };
        db.Cities.AddRange(srcCity, dstCity);
        var route = new Models.Route { Id = Guid.NewGuid(), SourceCityId = srcCity.Id, DestinationCityId = dstCity.Id, IsActive = true };
        db.Routes.Add(route);
        var bus = new Bus
        {
            Id = Guid.NewGuid(), OperatorUserId = op.Id,
            RegistrationNumber = "TN-PENDING", BusName = "Pending Bus",
            BusType = BusType.Seater, Capacity = 20,
            ApprovalStatus = BusApprovalStatus.Pending,
            OperationalStatus = BusOperationalStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        db.Buses.Add(bus);
        await db.SaveChangesAsync();

        var client = _fx.CreateClient(); client.AttachBearer(token);
        var resp = await client.PostAsJsonAsync("/api/v1/operator/schedules", SampleSchedule(bus.Id, route.Id));
        resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await resp.Content.ReadAsStringAsync()).Should().Contain("BUS_NOT_APPROVED");
    }

    [Fact]
    public async Task Create_without_office_at_source_city_returns_422()
    {
        var (op, token) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Operator], email: "nooffice@t.local");
        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var srcCity = new City { Id = Guid.NewGuid(), Name = "CityC", State = "XX", IsActive = true };
        var dstCity = new City { Id = Guid.NewGuid(), Name = "CityD", State = "XX", IsActive = true };
        db.Cities.AddRange(srcCity, dstCity);
        var route = new Models.Route { Id = Guid.NewGuid(), SourceCityId = srcCity.Id, DestinationCityId = dstCity.Id, IsActive = true };
        db.Routes.Add(route);
        // Only office at dstCity — missing srcCity
        db.OperatorOffices.Add(new OperatorOffice { Id = Guid.NewGuid(), OperatorUserId = op.Id, CityId = dstCity.Id, AddressLine = "Addr", Phone = "0000", IsActive = true });
        var bus = new Bus
        {
            Id = Guid.NewGuid(), OperatorUserId = op.Id,
            RegistrationNumber = "TN-NOOFFICE", BusName = "No Office Bus",
            BusType = BusType.Seater, Capacity = 20,
            ApprovalStatus = BusApprovalStatus.Approved,
            OperationalStatus = BusOperationalStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        db.Buses.Add(bus);
        await db.SaveChangesAsync();

        var client = _fx.CreateClient(); client.AttachBearer(token);
        var resp = await client.PostAsJsonAsync("/api/v1/operator/schedules", SampleSchedule(bus.Id, route.Id));
        resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await resp.Content.ReadAsStringAsync()).Should().Contain("NO_OFFICE_AT_CITY");
    }

    [Fact]
    public async Task List_scopes_to_operator()
    {
        var (_, token, bus, route) = await SeedOperatorWithApprovedBus();
        var client = _fx.CreateClient(); client.AttachBearer(token);
        await client.PostAsJsonAsync("/api/v1/operator/schedules", SampleSchedule(bus.Id, route.Id));
        await client.PostAsJsonAsync("/api/v1/operator/schedules", SampleSchedule(bus.Id, route.Id) with { DepartureTime = new TimeOnly(18, 0) });

        var list = await (await client.GetAsync("/api/v1/operator/schedules"))
            .Content.ReadFromJsonAsync<List<BusScheduleDto>>();
        list!.Should().HaveCount(2);
        list.Should().OnlyContain(s => s.BusId == bus.Id);
    }

    [Fact]
    public async Task Update_fare_returns_200_with_new_fare()
    {
        var (_, token, bus, route) = await SeedOperatorWithApprovedBus();
        var client = _fx.CreateClient(); client.AttachBearer(token);
        var created = await (await client.PostAsJsonAsync("/api/v1/operator/schedules", SampleSchedule(bus.Id, route.Id)))
            .Content.ReadFromJsonAsync<BusScheduleDto>();

        var resp = await client.PatchAsJsonAsync($"/api/v1/operator/schedules/{created!.Id}",
            new UpdateBusScheduleRequest(null, null, 500m, null, null, null, null));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await resp.Content.ReadFromJsonAsync<BusScheduleDto>();
        updated!.FarePerSeat.Should().Be(500m);
    }

    [Fact]
    public async Task Delete_removes_schedule()
    {
        var (_, token, bus, route) = await SeedOperatorWithApprovedBus();
        var client = _fx.CreateClient(); client.AttachBearer(token);
        var created = await (await client.PostAsJsonAsync("/api/v1/operator/schedules", SampleSchedule(bus.Id, route.Id)))
            .Content.ReadFromJsonAsync<BusScheduleDto>();

        var del = await client.DeleteAsync($"/api/v1/operator/schedules/{created!.Id}");
        del.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var list = await (await client.GetAsync("/api/v1/operator/schedules"))
            .Content.ReadFromJsonAsync<List<BusScheduleDto>>();
        list!.Should().BeEmpty();
    }

    [Fact]
    public async Task ListRoutes_returns_active_routes()
    {
        var (_, token, _, route) = await SeedOperatorWithApprovedBus();
        var client = _fx.CreateClient(); client.AttachBearer(token);
        var routes = await (await client.GetAsync("/api/v1/operator/routes"))
            .Content.ReadFromJsonAsync<List<RouteOptionDto>>();
        routes!.Should().ContainSingle(r => r.Id == route.Id);
    }
}
```

- [ ] **Step 2: Run tests — confirm they fail (controller missing)**

```bash
cd backend && dotnet test BusBooking.Api.Tests/BusBooking.Api.Tests.csproj \
    --filter "OperatorSchedulesTests" -v n 2>&1 | tail -10
```

Expected: build error or 404 failures.

- [ ] **Step 3: Create OperatorSchedulesController**

```csharp
// backend/BusBooking.Api/Controllers/OperatorSchedulesController.cs
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure.Auth;
using BusBooking.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.Api.Controllers;

[ApiController]
[Authorize(Roles = "operator")]
public class OperatorSchedulesController : ControllerBase
{
    private readonly IScheduleService _schedules;
    private readonly ICurrentUserAccessor _me;

    public OperatorSchedulesController(IScheduleService schedules, ICurrentUserAccessor me)
    {
        _schedules = schedules;
        _me = me;
    }

    [HttpGet("api/v1/operator/schedules")]
    public async Task<ActionResult<IReadOnlyList<BusScheduleDto>>> List(
        [FromQuery] Guid? busId, CancellationToken ct)
        => Ok(await _schedules.ListAsync(_me.UserId, busId, ct));

    [HttpPost("api/v1/operator/schedules")]
    public async Task<ActionResult<BusScheduleDto>> Create(
        [FromBody] CreateBusScheduleRequest body,
        [FromServices] IValidator<CreateBusScheduleRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(body, ct);
        var dto = await _schedules.CreateAsync(_me.UserId, body, ct);
        return StatusCode(StatusCodes.Status201Created, dto);
    }

    [HttpPatch("api/v1/operator/schedules/{id:guid}")]
    public async Task<ActionResult<BusScheduleDto>> Update(
        Guid id,
        [FromBody] UpdateBusScheduleRequest body,
        [FromServices] IValidator<UpdateBusScheduleRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(body, ct);
        return Ok(await _schedules.UpdateAsync(_me.UserId, id, body, ct));
    }

    [HttpDelete("api/v1/operator/schedules/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _schedules.DeleteAsync(_me.UserId, id, ct);
        return NoContent();
    }

    [HttpGet("api/v1/operator/routes")]
    public async Task<ActionResult<IReadOnlyList<RouteOptionDto>>> ListRoutes(CancellationToken ct)
        => Ok(await _schedules.ListActiveRoutesAsync(ct));
}
```

- [ ] **Step 4: Register services in Program.cs**

Add these two lines to `Program.cs` in the services block (near the other scoped services):
```csharp
builder.Services.AddScoped<IScheduleService, ScheduleService>();
// ITripService registration comes in Task 6
```

Also register the new validators (add to the `AddValidatorsFromAssemblyContaining` block — this should be automatic if using `AddValidatorsFromAssemblyContaining<Program>()`; confirm that pattern is already in use).

Check existing Program.cs registration pattern:
```bash
grep "AddValidators\|AddScoped" backend/BusBooking.Api/Program.cs | head -10
```

If using `AddValidatorsFromAssemblyContaining<Program>()`, no change needed for validators. Only add:
```csharp
builder.Services.AddScoped<IScheduleService, ScheduleService>();
```

- [ ] **Step 5: Run integration tests — should pass**

```bash
cd backend && dotnet test BusBooking.Api.Tests/BusBooking.Api.Tests.csproj \
    --filter "OperatorSchedulesTests" -v n
```

Expected: `7 passed, 0 failed`

- [ ] **Step 6: Run full test suite to check for regressions**

```bash
cd backend && dotnet test BusBooking.Api.Tests/BusBooking.Api.Tests.csproj -v n
```

Expected: all prior tests pass.

- [ ] **Step 7: Commit**

```bash
git add backend/BusBooking.Api/Controllers/OperatorSchedulesController.cs \
        backend/BusBooking.Api.Tests/Integration/OperatorSchedulesTests.cs \
        backend/BusBooking.Api/Program.cs
git commit -m "feat: add OperatorSchedulesController with CRUD and business rules (M4)"
```

---

## Task 5: ITripService + TripService + unit tests

**Files:**
- Create: `backend/BusBooking.Api/Services/ITripService.cs`
- Create: `backend/BusBooking.Api/Services/TripService.cs`
- Create: `backend/BusBooking.Api.Tests/Unit/ScheduleDayOfWeekTests.cs` (extend with schedule-run check)

- [ ] **Step 1: Create ITripService**

```csharp
// backend/BusBooking.Api/Services/ITripService.cs
using BusBooking.Api.Dtos;

namespace BusBooking.Api.Services;

public interface ITripService
{
    Task<IReadOnlyList<SearchResultDto>> SearchAsync(Guid srcCityId, Guid dstCityId, DateOnly date, CancellationToken ct);
    Task<TripDetailDto> GetDetailAsync(Guid tripId, CancellationToken ct);
    Task<SeatLayoutDto> GetSeatLayoutAsync(Guid tripId, CancellationToken ct);
}
```

- [ ] **Step 2: Add ScheduleRunsOnDate helper test to ScheduleDayOfWeekTests**

Add to the existing `ScheduleDayOfWeekTests` class:
```csharp
[Fact]
public void ScheduleRunsOnDate_true_when_day_and_range_match()
{
    // Monday 2026-04-27, mask Mon=1, valid range includes that date
    TripService.ScheduleRunsOnDate(1, DateOnly.Parse("2026-04-27"),
        DateOnly.Parse("2026-04-01"), DateOnly.Parse("2026-04-30"))
        .Should().BeTrue();
}

[Fact]
public void ScheduleRunsOnDate_false_when_day_outside_range()
{
    TripService.ScheduleRunsOnDate(1, DateOnly.Parse("2026-04-27"),
        DateOnly.Parse("2026-05-01"), DateOnly.Parse("2026-05-31"))
        .Should().BeFalse();
}

[Fact]
public void ScheduleRunsOnDate_false_when_day_bit_not_set()
{
    // Monday, mask=2 (Tuesday only)
    TripService.ScheduleRunsOnDate(2, DateOnly.Parse("2026-04-27"),
        DateOnly.Parse("2026-04-01"), DateOnly.Parse("2026-04-30"))
        .Should().BeFalse();
}
```

- [ ] **Step 3: Run tests — confirm new tests fail (TripService not defined)**

```bash
cd backend && dotnet test BusBooking.Api.Tests/BusBooking.Api.Tests.csproj \
    --filter "ScheduleDayOfWeekTests" -v n 2>&1 | tail -5
```

Expected: build error.

- [ ] **Step 4: Create TripService**

```csharp
// backend/BusBooking.Api/Services/TripService.cs
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Infrastructure.Errors;
using BusBooking.Api.Models;
using Microsoft.EntityFrameworkCore;
using Route = BusBooking.Api.Models.Route;

namespace BusBooking.Api.Services;

public class TripService : ITripService
{
    private readonly AppDbContext _db;
    public TripService(AppDbContext db) => _db = db;

    public static bool ScheduleRunsOnDate(int daysOfWeek, DateOnly date, DateOnly validFrom, DateOnly validTo)
    {
        if (date < validFrom || date > validTo) return false;
        return (daysOfWeek & ScheduleService.GetDayBit(date.DayOfWeek)) != 0;
    }

    public async Task<IReadOnlyList<SearchResultDto>> SearchAsync(
        Guid srcCityId, Guid dstCityId, DateOnly date, CancellationToken ct)
    {
        var route = await _db.Routes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.SourceCityId == srcCityId
                && r.DestinationCityId == dstCityId && r.IsActive, ct);
        if (route == null) return [];

        var schedules = await _db.BusSchedules
            .Include(s => s.Bus).ThenInclude(b => b!.Operator)
            .Include(s => s.Route).ThenInclude(r => r!.SourceCity)
            .Include(s => s.Route).ThenInclude(r => r!.DestinationCity)
            .Where(s => s.RouteId == route.Id
                && s.IsActive
                && s.Bus!.ApprovalStatus == BusApprovalStatus.Approved
                && s.Bus.OperationalStatus == BusOperationalStatus.Active)
            .ToListAsync(ct);

        var results = new List<SearchResultDto>();
        foreach (var schedule in schedules.Where(s =>
            ScheduleRunsOnDate(s.DaysOfWeek, date, s.ValidFrom, s.ValidTo)))
        {
            var trip = await MaterializeTripAsync(schedule.Id, date, ct);
            if (trip.Status == TripStatus.Cancelled) continue;

            var pickup = await _db.OperatorOffices.AsNoTracking()
                .FirstOrDefaultAsync(o => o.OperatorUserId == schedule.Bus!.OperatorUserId
                    && o.CityId == route.SourceCityId && o.IsActive, ct);
            var drop = await _db.OperatorOffices.AsNoTracking()
                .FirstOrDefaultAsync(o => o.OperatorUserId == schedule.Bus!.OperatorUserId
                    && o.CityId == route.DestinationCityId && o.IsActive, ct);

            int seatsLeft = schedule.Bus!.Capacity; // M5 will subtract locked/booked

            results.Add(new SearchResultDto(
                trip.Id,
                schedule.Bus.BusName,
                schedule.Bus.BusType,
                schedule.Bus.Operator!.Name,
                schedule.DepartureTime,
                schedule.ArrivalTime,
                schedule.FarePerSeat,
                seatsLeft,
                pickup?.AddressLine ?? "",
                drop?.AddressLine ?? ""
            ));
        }
        return results;
    }

    public async Task<TripDetailDto> GetDetailAsync(Guid tripId, CancellationToken ct)
    {
        var trip = await _db.BusTrips
            .AsNoTracking()
            .Include(t => t.Schedule).ThenInclude(s => s!.Bus).ThenInclude(b => b!.Operator)
            .Include(t => t.Schedule).ThenInclude(s => s!.Route).ThenInclude(r => r!.SourceCity)
            .Include(t => t.Schedule).ThenInclude(s => s!.Route).ThenInclude(r => r!.DestinationCity)
            .FirstOrDefaultAsync(t => t.Id == tripId, ct)
            ?? throw new NotFoundException("Trip not found");

        var schedule = trip.Schedule!;
        var bus      = schedule.Bus!;
        var route    = schedule.Route!;

        var pickup = await _db.OperatorOffices.AsNoTracking()
            .FirstOrDefaultAsync(o => o.OperatorUserId == bus.OperatorUserId
                && o.CityId == route.SourceCityId && o.IsActive, ct);
        var drop = await _db.OperatorOffices.AsNoTracking()
            .FirstOrDefaultAsync(o => o.OperatorUserId == bus.OperatorUserId
                && o.CityId == route.DestinationCityId && o.IsActive, ct);

        int seatsLeft = bus.Capacity; // M5 will subtract locked/booked
        var layout    = await BuildSeatLayoutAsync(bus.Id, tripId, ct);

        return new TripDetailDto(
            trip.Id, bus.Id, bus.BusName, bus.BusType, bus.Operator!.Name,
            trip.TripDate, schedule.DepartureTime, schedule.ArrivalTime, schedule.FarePerSeat,
            seatsLeft, route.SourceCity!.Name, route.DestinationCity!.Name,
            pickup?.AddressLine, drop?.AddressLine, layout
        );
    }

    public async Task<SeatLayoutDto> GetSeatLayoutAsync(Guid tripId, CancellationToken ct)
    {
        var trip = await _db.BusTrips
            .AsNoTracking()
            .Include(t => t.Schedule).ThenInclude(s => s!.Bus)
            .FirstOrDefaultAsync(t => t.Id == tripId, ct)
            ?? throw new NotFoundException("Trip not found");

        return await BuildSeatLayoutAsync(trip.Schedule!.BusId, tripId, ct);
    }

    private async Task<BusTrip> MaterializeTripAsync(Guid scheduleId, DateOnly date, CancellationToken ct)
    {
        var existing = await _db.BusTrips
            .FirstOrDefaultAsync(t => t.ScheduleId == scheduleId && t.TripDate == date, ct);
        if (existing != null) return existing;

        var trip = new BusTrip
        {
            Id         = Guid.NewGuid(),
            ScheduleId = scheduleId,
            TripDate   = date,
            Status     = TripStatus.Scheduled
        };
        _db.BusTrips.Add(trip);
        await _db.SaveChangesAsync(ct);
        return trip;
    }

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

        // M4: all seats are available — M5 will check seat_locks and booking_seats
        var statusList = seats.Select(s => new SeatStatusDto(
            s.SeatNumber, s.RowIndex, s.ColumnIndex, "available"
        )).ToList();

        return new SeatLayoutDto(rows, cols, statusList);
    }
}
```

- [ ] **Step 5: Run unit tests — should pass**

```bash
cd backend && dotnet test BusBooking.Api.Tests/BusBooking.Api.Tests.csproj \
    --filter "ScheduleDayOfWeekTests" -v n
```

Expected: all tests pass.

- [ ] **Step 6: Commit**

```bash
git add backend/BusBooking.Api/Services/ITripService.cs \
        backend/BusBooking.Api/Services/TripService.cs \
        backend/BusBooking.Api.Tests/Unit/ScheduleDayOfWeekTests.cs
git commit -m "feat: add TripService with search materialization and seat layout (M4)"
```

---

## Task 6: SearchController + TripsController + integration tests

**Files:**
- Create: `backend/BusBooking.Api/Controllers/SearchController.cs`
- Create: `backend/BusBooking.Api/Controllers/TripsController.cs`
- Create: `backend/BusBooking.Api.Tests/Integration/SearchTests.cs`
- Modify: `backend/BusBooking.Api/Program.cs`

- [ ] **Step 1: Write failing integration tests**

```csharp
// backend/BusBooking.Api.Tests/Integration/SearchTests.cs
using System.Net;
using System.Net.Http.Json;
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Models;
using BusBooking.Api.Tests.Support;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace BusBooking.Api.Tests.Integration;

[Collection("Integration")]
public class SearchTests : IAsyncLifetime
{
    private readonly IntegrationFixture _fx;
    public SearchTests(IntegrationFixture fx) => _fx = fx;
    public async Task InitializeAsync() => await _fx.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // Seeds a full chain: operator + approved bus + offices + route + active schedule for every day
    private async Task<(City src, City dst, BusSchedule schedule)> SeedSearchFixture()
    {
        var (op, _) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Operator], email: "search-op@t.local");
        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var src = new City { Id = Guid.NewGuid(), Name = "Hyderabad", State = "TS", IsActive = true };
        var dst = new City { Id = Guid.NewGuid(), Name = "Pune",      State = "MH", IsActive = true };
        db.Cities.AddRange(src, dst);

        var route = new Models.Route { Id = Guid.NewGuid(), SourceCityId = src.Id, DestinationCityId = dst.Id, IsActive = true };
        db.Routes.Add(route);

        db.OperatorOffices.AddRange(
            new OperatorOffice { Id = Guid.NewGuid(), OperatorUserId = op.Id, CityId = src.Id, AddressLine = "Addr1", Phone = "111", IsActive = true },
            new OperatorOffice { Id = Guid.NewGuid(), OperatorUserId = op.Id, CityId = dst.Id, AddressLine = "Addr2", Phone = "222", IsActive = true }
        );

        var bus = new Bus
        {
            Id = Guid.NewGuid(), OperatorUserId = op.Id,
            RegistrationNumber = "TS-SEARCH-01", BusName = "Search Express",
            BusType = BusType.Seater, Capacity = 12,
            ApprovalStatus = BusApprovalStatus.Approved,
            OperationalStatus = BusOperationalStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        db.Buses.Add(bus);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var schedule = new BusSchedule
        {
            Id = Guid.NewGuid(), BusId = bus.Id, RouteId = route.Id,
            DepartureTime = new TimeOnly(9, 0), ArrivalTime = new TimeOnly(18, 0),
            FarePerSeat = 400m,
            ValidFrom = today, ValidTo = today.AddDays(30),
            DaysOfWeek = 127, // every day
            IsActive = true
        };
        db.BusSchedules.Add(schedule);

        // Generate seat definitions (3 rows × 4 cols)
        for (var r = 0; r < 3; r++)
            for (var c = 0; c < 4; c++)
                db.SeatDefinitions.Add(new SeatDefinition
                {
                    Id = Guid.NewGuid(), BusId = bus.Id,
                    SeatNumber = $"{(char)('A' + r)}{c + 1}",
                    RowIndex = r, ColumnIndex = c, SeatCategory = SeatCategory.Regular
                });

        await db.SaveChangesAsync();
        return (src, dst, schedule);
    }

    [Fact]
    public async Task Search_returns_trip_on_valid_route_and_date()
    {
        var (src, dst, _) = await SeedSearchFixture();
        var client = _fx.CreateClient();
        var date = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");

        var results = await (await client.GetAsync(
            $"/api/v1/search?src={src.Id}&dst={dst.Id}&date={date}"))
            .Content.ReadFromJsonAsync<List<SearchResultDto>>();

        results!.Should().HaveCount(1);
        results[0].BusName.Should().Be("Search Express");
        results[0].SeatsLeft.Should().Be(12);
    }

    [Fact]
    public async Task Search_materializes_trip_idempotently()
    {
        var (src, dst, _) = await SeedSearchFixture();
        var client = _fx.CreateClient();
        var date = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var url = $"/api/v1/search?src={src.Id}&dst={dst.Id}&date={date}";

        var r1 = await (await client.GetAsync(url)).Content.ReadFromJsonAsync<List<SearchResultDto>>();
        var r2 = await (await client.GetAsync(url)).Content.ReadFromJsonAsync<List<SearchResultDto>>();
        r1![0].TripId.Should().Be(r2![0].TripId);
    }

    [Fact]
    public async Task Search_unknown_route_returns_empty_array()
    {
        var client = _fx.CreateClient();
        var date = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var results = await (await client.GetAsync(
            $"/api/v1/search?src={Guid.NewGuid()}&dst={Guid.NewGuid()}&date={date}"))
            .Content.ReadFromJsonAsync<List<SearchResultDto>>();
        results!.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTripDetail_returns_seat_layout()
    {
        var (src, dst, _) = await SeedSearchFixture();
        var client = _fx.CreateClient();
        var date = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var tripId = (await (await client.GetAsync(
            $"/api/v1/search?src={src.Id}&dst={dst.Id}&date={date}"))
            .Content.ReadFromJsonAsync<List<SearchResultDto>>())![0].TripId;

        var detail = await (await client.GetAsync($"/api/v1/trips/{tripId}"))
            .Content.ReadFromJsonAsync<TripDetailDto>();

        detail!.BusName.Should().Be("Search Express");
        detail.SeatLayout.Rows.Should().Be(3);
        detail.SeatLayout.Columns.Should().Be(4);
        detail.SeatLayout.Seats.Should().HaveCount(12);
        detail.SeatLayout.Seats.Should().OnlyContain(s => s.Status == "available");
    }

    [Fact]
    public async Task GetSeatLayout_returns_all_available_seats()
    {
        var (src, dst, _) = await SeedSearchFixture();
        var client = _fx.CreateClient();
        var date = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var tripId = (await (await client.GetAsync(
            $"/api/v1/search?src={src.Id}&dst={dst.Id}&date={date}"))
            .Content.ReadFromJsonAsync<List<SearchResultDto>>())![0].TripId;

        var layout = await (await client.GetAsync($"/api/v1/trips/{tripId}/seats"))
            .Content.ReadFromJsonAsync<SeatLayoutDto>();

        layout!.Seats.Should().HaveCount(12)
            .And.OnlyContain(s => s.Status == "available");
    }
}
```

- [ ] **Step 2: Run tests — confirm they fail**

```bash
cd backend && dotnet test BusBooking.Api.Tests/BusBooking.Api.Tests.csproj \
    --filter "SearchTests" -v n 2>&1 | tail -5
```

Expected: build error or 404 failures.

- [ ] **Step 3: Create SearchController**

```csharp
// backend/BusBooking.Api/Controllers/SearchController.cs
using BusBooking.Api.Dtos;
using BusBooking.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.Api.Controllers;

[ApiController]
[Route("api/v1/search")]
[AllowAnonymous]
public class SearchController : ControllerBase
{
    private readonly ITripService _trips;
    public SearchController(ITripService trips) => _trips = trips;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SearchResultDto>>> Search(
        [FromQuery] Guid src,
        [FromQuery] Guid dst,
        [FromQuery] DateOnly date,
        CancellationToken ct)
        => Ok(await _trips.SearchAsync(src, dst, date, ct));
}
```

- [ ] **Step 4: Create TripsController**

```csharp
// backend/BusBooking.Api/Controllers/TripsController.cs
using BusBooking.Api.Dtos;
using BusBooking.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.Api.Controllers;

[ApiController]
[Route("api/v1/trips")]
[AllowAnonymous]
public class TripsController : ControllerBase
{
    private readonly ITripService _trips;
    public TripsController(ITripService trips) => _trips = trips;

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TripDetailDto>> GetDetail(Guid id, CancellationToken ct)
        => Ok(await _trips.GetDetailAsync(id, ct));

    [HttpGet("{id:guid}/seats")]
    public async Task<ActionResult<SeatLayoutDto>> GetSeats(Guid id, CancellationToken ct)
        => Ok(await _trips.GetSeatLayoutAsync(id, ct));
}
```

- [ ] **Step 5: Register ITripService in Program.cs**

Add to `Program.cs` services block:
```csharp
builder.Services.AddScoped<ITripService, TripService>();
```

- [ ] **Step 6: Run all integration tests**

```bash
cd backend && dotnet test BusBooking.Api.Tests/BusBooking.Api.Tests.csproj -v n
```

Expected: all tests pass including the 5 new `SearchTests`.

- [ ] **Step 7: Commit**

```bash
git add backend/BusBooking.Api/Controllers/SearchController.cs \
        backend/BusBooking.Api/Controllers/TripsController.cs \
        backend/BusBooking.Api.Tests/Integration/SearchTests.cs \
        backend/BusBooking.Api/Program.cs
git commit -m "feat: add SearchController and TripsController with trip materialization (M4)"
```

---

## Task 7: schedules.api.ts + search.api.ts (Angular API services)

**Files:**
- Create: `frontend/bus-booking-web/src/app/core/api/schedules.api.ts`
- Create: `frontend/bus-booking-web/src/app/core/api/search.api.ts`

- [ ] **Step 1: Create schedules.api.ts**

```typescript
// frontend/bus-booking-web/src/app/core/api/schedules.api.ts
import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface RouteOptionDto {
  id: string;
  sourceCityName: string;
  destinationCityName: string;
  distanceKm: number | null;
}

export interface BusScheduleDto {
  id: string;
  busId: string;
  busName: string;
  routeId: string;
  sourceCityName: string;
  destinationCityName: string;
  departureTime: string;   // "HH:mm:ss"
  arrivalTime: string;
  farePerSeat: number;
  validFrom: string;       // "yyyy-MM-dd"
  validTo: string;
  daysOfWeek: number;
  isActive: boolean;
}

export interface CreateBusScheduleRequest {
  busId: string;
  routeId: string;
  departureTime: string;
  arrivalTime: string;
  farePerSeat: number;
  validFrom: string;
  validTo: string;
  daysOfWeek: number;
}

export interface UpdateBusScheduleRequest {
  departureTime?: string;
  arrivalTime?: string;
  farePerSeat?: number;
  validFrom?: string;
  validTo?: string;
  daysOfWeek?: number;
  isActive?: boolean;
}

@Injectable({ providedIn: 'root' })
export class SchedulesApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/operator/schedules`;

  listRoutes(): Observable<RouteOptionDto[]> {
    return this.http.get<RouteOptionDto[]>(`${environment.apiBaseUrl}/operator/routes`);
  }

  list(busId?: string): Observable<BusScheduleDto[]> {
    const params = busId ? { busId } : {};
    return this.http.get<BusScheduleDto[]>(this.base, { params });
  }

  create(body: CreateBusScheduleRequest): Observable<BusScheduleDto> {
    return this.http.post<BusScheduleDto>(this.base, body);
  }

  update(id: string, body: UpdateBusScheduleRequest): Observable<BusScheduleDto> {
    return this.http.patch<BusScheduleDto>(`${this.base}/${id}`, body);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
```

- [ ] **Step 2: Create search.api.ts**

```typescript
// frontend/bus-booking-web/src/app/core/api/search.api.ts
import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface SearchResultDto {
  tripId: string;
  busName: string;
  busType: string;
  operatorName: string;
  departureTime: string;
  arrivalTime: string;
  farePerSeat: number;
  seatsLeft: number;
  pickupAddress: string;
  dropAddress: string;
}

export interface SeatStatusDto {
  seatNumber: string;
  rowIndex: number;
  columnIndex: number;
  status: 'available' | 'locked' | 'booked';
}

export interface SeatLayoutDto {
  rows: number;
  columns: number;
  seats: SeatStatusDto[];
}

export interface TripDetailDto {
  tripId: string;
  busId: string;
  busName: string;
  busType: string;
  operatorName: string;
  tripDate: string;
  departureTime: string;
  arrivalTime: string;
  farePerSeat: number;
  seatsLeft: number;
  sourceCityName: string;
  destinationCityName: string;
  pickupAddress: string | null;
  dropAddress: string | null;
  seatLayout: SeatLayoutDto;
}

@Injectable({ providedIn: 'root' })
export class SearchApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  search(srcId: string, dstId: string, date: string): Observable<SearchResultDto[]> {
    return this.http.get<SearchResultDto[]>(`${this.base}/search`, {
      params: { src: srcId, dst: dstId, date }
    });
  }

  getTripDetail(tripId: string): Observable<TripDetailDto> {
    return this.http.get<TripDetailDto>(`${this.base}/trips/${tripId}`);
  }

  getSeatLayout(tripId: string): Observable<SeatLayoutDto> {
    return this.http.get<SeatLayoutDto>(`${this.base}/trips/${tripId}/seats`);
  }
}
```

- [ ] **Step 3: Build check**

```bash
cd frontend/bus-booking-web && ng build --configuration development 2>&1 | tail -5
```

Expected: `Build at: ... - Hash: ...`

- [ ] **Step 4: Commit**

```bash
git add frontend/bus-booking-web/src/app/core/api/schedules.api.ts \
        frontend/bus-booking-web/src/app/core/api/search.api.ts
git commit -m "feat: add Angular API services for schedules and search (M4)"
```

---

## Task 8: SeatMapComponent (shared, view-only)

**Files:**
- Create: `frontend/bus-booking-web/src/app/shared/components/seat-map/seat-map.component.ts`
- Create: `frontend/bus-booking-web/src/app/shared/components/seat-map/seat-map.component.html`

- [ ] **Step 1: Create SeatMapComponent**

```typescript
// frontend/bus-booking-web/src/app/shared/components/seat-map/seat-map.component.ts
import { ChangeDetectionStrategy, Component, Input, computed, signal } from '@angular/core';
import { SeatLayoutDto, SeatStatusDto } from '../../../core/api/search.api';

@Component({
  selector: 'app-seat-map',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './seat-map.component.html'
})
export class SeatMapComponent {
  @Input({ required: true }) layout!: SeatLayoutDto;

  get grid(): SeatStatusDto[][] {
    const rows: SeatStatusDto[][] = Array.from({ length: this.layout.rows }, () => []);
    for (const seat of this.layout.seats) {
      rows[seat.rowIndex][seat.columnIndex] = seat;
    }
    return rows;
  }

  seatClass(seat: SeatStatusDto): string {
    const base = 'w-10 h-10 rounded text-xs font-medium flex items-center justify-center border ';
    return base + {
      available: 'bg-emerald-100 border-emerald-400 text-emerald-800',
      locked:    'bg-amber-100  border-amber-400  text-amber-800',
      booked:    'bg-rose-100   border-rose-400   text-rose-800'
    }[seat.status];
  }
}
```

- [ ] **Step 2: Create seat map template**

```html
<!-- frontend/bus-booking-web/src/app/shared/components/seat-map/seat-map.component.html -->
<div class="space-y-2">
  <div class="flex gap-3 text-xs mb-3">
    <span class="flex items-center gap-1">
      <span class="w-4 h-4 rounded bg-emerald-100 border border-emerald-400 inline-block"></span> Available
    </span>
    <span class="flex items-center gap-1">
      <span class="w-4 h-4 rounded bg-amber-100 border border-amber-400 inline-block"></span> Locked
    </span>
    <span class="flex items-center gap-1">
      <span class="w-4 h-4 rounded bg-rose-100 border border-rose-400 inline-block"></span> Booked
    </span>
  </div>
  @for (row of grid; track $index) {
    <div class="flex gap-2">
      @for (seat of row; track seat.seatNumber) {
        <button type="button" [class]="seatClass(seat)" [title]="seat.seatNumber" disabled>
          {{ seat.seatNumber }}
        </button>
      }
    </div>
  }
</div>
```

- [ ] **Step 3: Build check**

```bash
cd frontend/bus-booking-web && ng build --configuration development 2>&1 | tail -5
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add frontend/bus-booking-web/src/app/shared/components/seat-map/
git commit -m "feat: add shared SeatMapComponent (view-only, M4)"
```

---

## Task 9: Operator schedule list + form components

**Files:**
- Create: `frontend/bus-booking-web/src/app/features/operator/schedules/operator-schedules-list.component.ts`
- Create: `frontend/bus-booking-web/src/app/features/operator/schedules/operator-schedules-list.component.html`
- Create: `frontend/bus-booking-web/src/app/features/operator/schedules/operator-schedule-form.component.ts`
- Create: `frontend/bus-booking-web/src/app/features/operator/schedules/operator-schedule-form.component.html`

- [ ] **Step 1: Create OperatorSchedulesListComponent**

```typescript
// frontend/bus-booking-web/src/app/features/operator/schedules/operator-schedules-list.component.ts
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { BusScheduleDto, SchedulesApiService } from '../../../core/api/schedules.api';
import { OperatorScheduleFormComponent } from './operator-schedule-form.component';

@Component({
  selector: 'app-operator-schedules-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatTableModule, MatButtonModule, MatIconModule],
  templateUrl: './operator-schedules-list.component.html'
})
export class OperatorSchedulesListComponent implements OnInit {
  private readonly api    = inject(SchedulesApiService);
  private readonly dialog = inject(MatDialog);
  private readonly snack  = inject(MatSnackBar);

  readonly schedules  = signal<BusScheduleDto[]>([]);
  readonly displayedColumns = ['route', 'bus', 'departure', 'arrival', 'fare', 'days', 'active', 'actions'];

  ngOnInit() { this.load(); }

  load() {
    this.api.list().subscribe(s => this.schedules.set(s));
  }

  openForm(schedule?: BusScheduleDto) {
    this.dialog.open(OperatorScheduleFormComponent, { data: schedule, width: '560px' })
      .afterClosed().subscribe(saved => { if (saved) this.load(); });
  }

  delete(id: string) {
    if (!confirm('Delete this schedule?')) return;
    this.api.delete(id).subscribe({
      next: () => { this.load(); this.snack.open('Deleted', 'OK', { duration: 2000 }); },
      error: () => this.snack.open('Delete failed', 'OK', { duration: 3000 })
    });
  }

  daysLabel(mask: number): string {
    const names = ['Mon','Tue','Wed','Thu','Fri','Sat','Sun'];
    return names.filter((_, i) => mask & (1 << i)).join(', ');
  }
}
```

- [ ] **Step 2: Create schedules list template**

```html
<!-- frontend/bus-booking-web/src/app/features/operator/schedules/operator-schedules-list.component.html -->
<div class="flex justify-between items-center mb-4">
  <h2 class="text-xl font-medium">Schedules</h2>
  <button mat-flat-button color="primary" (click)="openForm()">
    <mat-icon>add</mat-icon> New Schedule
  </button>
</div>

@if (schedules().length === 0) {
  <p class="text-slate-500">No schedules yet. Create one to make your bus searchable.</p>
} @else {
  <mat-table [dataSource]="schedules()" class="w-full shadow-sm">
    <ng-container matColumnDef="route">
      <mat-header-cell *matHeaderCellDef>Route</mat-header-cell>
      <mat-cell *matCellDef="let s">{{ s.sourceCityName }} → {{ s.destinationCityName }}</mat-cell>
    </ng-container>
    <ng-container matColumnDef="bus">
      <mat-header-cell *matHeaderCellDef>Bus</mat-header-cell>
      <mat-cell *matCellDef="let s">{{ s.busName }}</mat-cell>
    </ng-container>
    <ng-container matColumnDef="departure">
      <mat-header-cell *matHeaderCellDef>Departs</mat-header-cell>
      <mat-cell *matCellDef="let s">{{ s.departureTime.slice(0,5) }}</mat-cell>
    </ng-container>
    <ng-container matColumnDef="arrival">
      <mat-header-cell *matHeaderCellDef>Arrives</mat-header-cell>
      <mat-cell *matCellDef="let s">{{ s.arrivalTime.slice(0,5) }}</mat-cell>
    </ng-container>
    <ng-container matColumnDef="fare">
      <mat-header-cell *matHeaderCellDef>Fare (₹)</mat-header-cell>
      <mat-cell *matCellDef="let s">{{ s.farePerSeat | number:'1.0-0' }}</mat-cell>
    </ng-container>
    <ng-container matColumnDef="days">
      <mat-header-cell *matHeaderCellDef>Days</mat-header-cell>
      <mat-cell *matCellDef="let s">{{ daysLabel(s.daysOfWeek) }}</mat-cell>
    </ng-container>
    <ng-container matColumnDef="active">
      <mat-header-cell *matHeaderCellDef>Active</mat-header-cell>
      <mat-cell *matCellDef="let s">{{ s.isActive ? 'Yes' : 'No' }}</mat-cell>
    </ng-container>
    <ng-container matColumnDef="actions">
      <mat-header-cell *matHeaderCellDef></mat-header-cell>
      <mat-cell *matCellDef="let s">
        <button mat-icon-button (click)="openForm(s)"><mat-icon>edit</mat-icon></button>
        <button mat-icon-button color="warn" (click)="delete(s.id)"><mat-icon>delete</mat-icon></button>
      </mat-cell>
    </ng-container>
    <mat-header-row *matHeaderRowDef="displayedColumns"></mat-header-row>
    <mat-row *matRowDef="let row; columns: displayedColumns;"></mat-row>
  </mat-table>
}
```

- [ ] **Step 3: Create OperatorScheduleFormComponent**

```typescript
// frontend/bus-booking-web/src/app/features/operator/schedules/operator-schedule-form.component.ts
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatSnackBar } from '@angular/material/snack-bar';
import { BusScheduleDto, RouteOptionDto, SchedulesApiService } from '../../../core/api/schedules.api';
import { OperatorBusesApiService, BusDto } from '../../../core/api/operator-buses.api';

const DAY_BITS = [
  { label: 'Mon', bit: 1 },  { label: 'Tue', bit: 2 },  { label: 'Wed', bit: 4 },
  { label: 'Thu', bit: 8 },  { label: 'Fri', bit: 16 }, { label: 'Sat', bit: 32 },
  { label: 'Sun', bit: 64 }
];

@Component({
  selector: 'app-operator-schedule-form',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatButtonModule, MatCheckboxModule
  ],
  templateUrl: './operator-schedule-form.component.html'
})
export class OperatorScheduleFormComponent implements OnInit {
  private readonly fb     = inject(FormBuilder);
  private readonly api    = inject(SchedulesApiService);
  private readonly busApi = inject(OperatorBusesApiService);
  private readonly ref    = inject(MatDialogRef<OperatorScheduleFormComponent>);
  private readonly snack  = inject(MatSnackBar);
  readonly existing       = inject<BusScheduleDto | undefined>(MAT_DIALOG_DATA);

  readonly routes  = signal<RouteOptionDto[]>([]);
  readonly buses   = signal<BusDto[]>([]);
  readonly days    = DAY_BITS;
  readonly saving  = signal(false);

  form = this.fb.group({
    busId:         [this.existing?.busId   ?? '', Validators.required],
    routeId:       [this.existing?.routeId ?? '', Validators.required],
    departureTime: [this.existing?.departureTime?.slice(0,5) ?? '', Validators.required],
    arrivalTime:   [this.existing?.arrivalTime?.slice(0,5)   ?? '', Validators.required],
    farePerSeat:   [this.existing?.farePerSeat ?? null, [Validators.required, Validators.min(1)]],
    validFrom:     [this.existing?.validFrom ?? '', Validators.required],
    validTo:       [this.existing?.validTo   ?? '', Validators.required],
    daysOfWeek:    [this.existing?.daysOfWeek ?? 31]  // Mon-Fri default
  });

  ngOnInit() {
    this.api.listRoutes().subscribe(r => this.routes.set(r));
    this.busApi.list().subscribe(b => this.buses.set(b.filter(x => x.approvalStatus === 'approved')));
  }

  isDaySet(bit: number): boolean {
    return (this.form.value.daysOfWeek! & bit) !== 0;
  }

  toggleDay(bit: number) {
    const cur = this.form.value.daysOfWeek ?? 0;
    this.form.patchValue({ daysOfWeek: cur ^ bit });
  }

  save() {
    if (this.form.invalid) return;
    this.saving.set(true);
    const v = this.form.value;

    const toTimeStr = (t: string) => t.length === 5 ? t + ':00' : t;
    const body = {
      busId:         v.busId!,
      routeId:       v.routeId!,
      departureTime: toTimeStr(v.departureTime!),
      arrivalTime:   toTimeStr(v.arrivalTime!),
      farePerSeat:   v.farePerSeat!,
      validFrom:     v.validFrom!,
      validTo:       v.validTo!,
      daysOfWeek:    v.daysOfWeek!
    };

    const req$ = this.existing
      ? this.api.update(this.existing.id, { farePerSeat: body.farePerSeat, departureTime: body.departureTime, arrivalTime: body.arrivalTime, validFrom: body.validFrom, validTo: body.validTo, daysOfWeek: body.daysOfWeek })
      : this.api.create(body);

    req$.subscribe({
      next: dto => { this.saving.set(false); this.ref.close(dto); },
      error: () => { this.saving.set(false); this.snack.open('Save failed', 'OK', { duration: 3000 }); }
    });
  }
}
```

- [ ] **Step 4: Create schedule form template**

```html
<!-- frontend/bus-booking-web/src/app/features/operator/schedules/operator-schedule-form.component.html -->
<h2 mat-dialog-title>{{ existing ? 'Edit Schedule' : 'New Schedule' }}</h2>
<mat-dialog-content [formGroup]="form" class="space-y-3 pt-2">

  <mat-form-field class="w-full">
    <mat-label>Bus</mat-label>
    <mat-select formControlName="busId">
      @for (b of buses(); track b.id) {
        <mat-option [value]="b.id">{{ b.busName }} ({{ b.registrationNumber }})</mat-option>
      }
    </mat-select>
  </mat-form-field>

  <mat-form-field class="w-full">
    <mat-label>Route</mat-label>
    <mat-select formControlName="routeId">
      @for (r of routes(); track r.id) {
        <mat-option [value]="r.id">{{ r.sourceCityName }} → {{ r.destinationCityName }}</mat-option>
      }
    </mat-select>
  </mat-form-field>

  <div class="grid grid-cols-2 gap-3">
    <mat-form-field>
      <mat-label>Departure (HH:mm)</mat-label>
      <input matInput type="time" formControlName="departureTime" />
    </mat-form-field>
    <mat-form-field>
      <mat-label>Arrival (HH:mm)</mat-label>
      <input matInput type="time" formControlName="arrivalTime" />
    </mat-form-field>
  </div>

  <mat-form-field class="w-full">
    <mat-label>Fare per seat (₹)</mat-label>
    <input matInput type="number" formControlName="farePerSeat" min="1" />
  </mat-form-field>

  <div class="grid grid-cols-2 gap-3">
    <mat-form-field>
      <mat-label>Valid from</mat-label>
      <input matInput type="date" formControlName="validFrom" />
    </mat-form-field>
    <mat-form-field>
      <mat-label>Valid to</mat-label>
      <input matInput type="date" formControlName="validTo" />
    </mat-form-field>
  </div>

  <div>
    <p class="text-sm font-medium mb-1">Days of week</p>
    <div class="flex gap-2 flex-wrap">
      @for (d of days; track d.bit) {
        <mat-checkbox [checked]="isDaySet(d.bit)" (change)="toggleDay(d.bit)">{{ d.label }}</mat-checkbox>
      }
    </div>
  </div>

</mat-dialog-content>
<mat-dialog-actions align="end">
  <button mat-button mat-dialog-close>Cancel</button>
  <button mat-flat-button color="primary" [disabled]="form.invalid || saving()" (click)="save()">
    {{ saving() ? 'Saving…' : 'Save' }}
  </button>
</mat-dialog-actions>
```

- [ ] **Step 5: Build check**

```bash
cd frontend/bus-booking-web && ng build --configuration development 2>&1 | tail -5
```

Expected: `Build succeeded.`

- [ ] **Step 6: Commit**

```bash
git add frontend/bus-booking-web/src/app/features/operator/schedules/
git commit -m "feat: add operator schedules list and form components (M4)"
```

---

## Task 10: SearchResultsComponent

**Files:**
- Create: `frontend/bus-booking-web/src/app/features/public/search-results/search-results.component.ts`
- Create: `frontend/bus-booking-web/src/app/features/public/search-results/search-results.component.html`

- [ ] **Step 1: Create SearchResultsComponent**

```typescript
// frontend/bus-booking-web/src/app/features/public/search-results/search-results.component.ts
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { SearchApiService, SearchResultDto } from '../../../core/api/search.api';

@Component({
  selector: 'app-search-results',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatCardModule, MatButtonModule, MatProgressSpinnerModule, RouterLink],
  templateUrl: './search-results.component.html'
})
export class SearchResultsComponent implements OnInit {
  private readonly route  = inject(ActivatedRoute);
  private readonly api    = inject(SearchApiService);

  readonly loading = signal(true);
  readonly results = signal<SearchResultDto[]>([]);
  readonly error   = signal<string | null>(null);

  src  = '';
  dst  = '';
  date = '';

  ngOnInit() {
    const p = this.route.snapshot.queryParams;
    this.src  = p['src']  ?? '';
    this.dst  = p['dst']  ?? '';
    this.date = p['date'] ?? '';

    this.api.search(this.src, this.dst, this.date).subscribe({
      next:  r => { this.results.set(r); this.loading.set(false); },
      error: () => { this.error.set('Search failed. Please try again.'); this.loading.set(false); }
    });
  }
}
```

- [ ] **Step 2: Create search results template**

```html
<!-- frontend/bus-booking-web/src/app/features/public/search-results/search-results.component.html -->
<div class="max-w-3xl mx-auto p-6">
  <h2 class="text-xl font-medium mb-4">Available Buses</h2>

  @if (loading()) {
    <div class="flex justify-center py-12">
      <mat-spinner diameter="40" />
    </div>
  } @else if (error()) {
    <p class="text-rose-600">{{ error() }}</p>
  } @else if (results().length === 0) {
    <p class="text-slate-500">No buses found for this route and date.</p>
  } @else {
    <div class="space-y-4">
      @for (trip of results(); track trip.tripId) {
        <mat-card class="p-4">
          <div class="flex justify-between items-start">
            <div>
              <p class="font-semibold text-lg">{{ trip.busName }}</p>
              <p class="text-sm text-slate-500">{{ trip.busType }} · {{ trip.operatorName }}</p>
              <p class="mt-2 text-sm">
                <span class="font-medium">{{ trip.departureTime.slice(0,5) }}</span>
                → <span class="font-medium">{{ trip.arrivalTime.slice(0,5) }}</span>
              </p>
              <p class="text-xs text-slate-500 mt-1">
                Pickup: {{ trip.pickupAddress || '—' }}<br/>
                Drop: {{ trip.dropAddress || '—' }}
              </p>
            </div>
            <div class="text-right">
              <p class="text-2xl font-bold text-primary-600">₹{{ trip.farePerSeat }}</p>
              <p class="text-sm text-slate-500">{{ trip.seatsLeft }} seats left</p>
              <a mat-flat-button color="primary" class="mt-2" [routerLink]="['/trips', trip.tripId]">
                View Seats
              </a>
            </div>
          </div>
        </mat-card>
      }
    </div>
  }
</div>
```

- [ ] **Step 3: Build check**

```bash
cd frontend/bus-booking-web && ng build --configuration development 2>&1 | tail -5
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add frontend/bus-booking-web/src/app/features/public/search-results/
git commit -m "feat: add SearchResultsComponent (M4)"
```

---

## Task 11: TripDetailComponent

**Files:**
- Create: `frontend/bus-booking-web/src/app/features/public/trip-detail/trip-detail.component.ts`
- Create: `frontend/bus-booking-web/src/app/features/public/trip-detail/trip-detail.component.html`

- [ ] **Step 1: Create TripDetailComponent**

```typescript
// frontend/bus-booking-web/src/app/features/public/trip-detail/trip-detail.component.ts
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { SearchApiService, TripDetailDto } from '../../../core/api/search.api';
import { SeatMapComponent } from '../../../shared/components/seat-map/seat-map.component';

@Component({
  selector: 'app-trip-detail',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatCardModule, MatButtonModule, MatProgressSpinnerModule, RouterLink, SeatMapComponent],
  templateUrl: './trip-detail.component.html'
})
export class TripDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api   = inject(SearchApiService);

  readonly loading = signal(true);
  readonly trip    = signal<TripDetailDto | null>(null);
  readonly error   = signal<string | null>(null);

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.api.getTripDetail(id).subscribe({
      next:  t => { this.trip.set(t); this.loading.set(false); },
      error: () => { this.error.set('Trip not found.'); this.loading.set(false); }
    });
  }
}
```

- [ ] **Step 2: Create trip detail template**

```html
<!-- frontend/bus-booking-web/src/app/features/public/trip-detail/trip-detail.component.html -->
<div class="max-w-3xl mx-auto p-6">
  <a mat-button routerLink="/" class="mb-4 -ml-2">← Back to search</a>

  @if (loading()) {
    <div class="flex justify-center py-12"><mat-spinner diameter="40" /></div>
  } @else if (error()) {
    <p class="text-rose-600">{{ error() }}</p>
  } @else if (trip(); as t) {
    <mat-card class="p-6 space-y-4">
      <div>
        <h2 class="text-2xl font-semibold">{{ t.busName }}</h2>
        <p class="text-slate-500">{{ t.busType }} · {{ t.operatorName }}</p>
      </div>

      <div class="grid grid-cols-2 gap-4 text-sm">
        <div>
          <p class="text-slate-500">Departure</p>
          <p class="font-medium">{{ t.departureTime.slice(0,5) }} on {{ t.tripDate }}</p>
          <p class="text-slate-500 mt-1">Pickup: {{ t.pickupAddress || '—' }}</p>
        </div>
        <div>
          <p class="text-slate-500">Arrival</p>
          <p class="font-medium">{{ t.arrivalTime.slice(0,5) }}</p>
          <p class="text-slate-500 mt-1">Drop: {{ t.dropAddress || '—' }}</p>
        </div>
        <div>
          <p class="text-slate-500">Fare per seat</p>
          <p class="font-bold text-xl">₹{{ t.farePerSeat }}</p>
        </div>
        <div>
          <p class="text-slate-500">Seats available</p>
          <p class="font-medium">{{ t.seatsLeft }}</p>
        </div>
      </div>

      <div>
        <h3 class="font-medium mb-3">Seat Map</h3>
        <app-seat-map [layout]="t.seatLayout" />
      </div>
    </mat-card>
  }
</div>
```

- [ ] **Step 3: Build check**

```bash
cd frontend/bus-booking-web && ng build --configuration development 2>&1 | tail -5
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add frontend/bus-booking-web/src/app/features/public/trip-detail/
git commit -m "feat: add TripDetailComponent with SeatMapComponent (M4)"
```

---

## Task 12: HomeComponent search update

**Files:**
- Modify: `frontend/bus-booking-web/src/app/features/public/home/home.component.ts`
- Modify: `frontend/bus-booking-web/src/app/features/public/home/home.component.html`

- [ ] **Step 1: Update HomeComponent**

Replace `home.component.ts` with:

```typescript
// frontend/bus-booking-web/src/app/features/public/home/home.component.ts
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatNativeDateModule } from '@angular/material/core';
import { DatePipe } from '@angular/common';
import { HealthApiService, HealthResponse } from '../../../core/api/health.api';
import { CityAutocompleteComponent } from '../../../shared/components/city-autocomplete/city-autocomplete.component';
import { CityDto } from '../../../core/api/cities.api';

type Status = 'loading' | 'ok' | 'failed';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    MatButtonModule, MatCardModule, MatDatepickerModule,
    MatFormFieldModule, MatInputModule, MatNativeDateModule,
    DatePipe, CityAutocompleteComponent
  ],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent implements OnInit {
  private readonly healthApi = inject(HealthApiService);
  private readonly router    = inject(Router);

  readonly status    = signal<Status>('loading');
  readonly payload   = signal<HealthResponse | null>(null);
  readonly source    = signal<CityDto | null>(null);
  readonly destination = signal<CityDto | null>(null);
  readonly travelDate  = signal<Date | null>(null);

  readonly today  = new Date();
  readonly maxDate = new Date(Date.now() + 60 * 24 * 60 * 60 * 1000);

  readonly canSearch = computed(() =>
    !!this.source() && !!this.destination() && !!this.travelDate()
  );

  readonly statusLabel = computed(() => {
    const s = this.status();
    if (s === 'loading') return 'checking…';
    if (s === 'ok') return 'backend online';
    return 'backend unreachable';
  });

  ngOnInit() { this.ping(); }

  ping() {
    this.status.set('loading');
    this.healthApi.ping().subscribe({
      next:  r => { this.payload.set(r);   this.status.set('ok'); },
      error: () => { this.payload.set(null); this.status.set('failed'); }
    });
  }

  search() {
    if (!this.canSearch()) return;
    const d = this.travelDate()!;
    const dateStr = `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}`;
    this.router.navigate(['/search-results'], {
      queryParams: { src: this.source()!.id, dst: this.destination()!.id, date: dateStr }
    });
  }
}
```

- [ ] **Step 2: Update home template**

Replace `home.component.html` with:

```html
<!-- frontend/bus-booking-web/src/app/features/public/home/home.component.html -->
<section class="p-6 max-w-3xl mx-auto space-y-4">
  <h2 class="text-xl font-medium">Where to?</h2>
  <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
    <app-city-autocomplete label="From" (citySelected)="source.set($event)" />
    <app-city-autocomplete label="To"   (citySelected)="destination.set($event)" />
  </div>

  <mat-form-field class="w-full md:w-64">
    <mat-label>Travel date</mat-label>
    <input matInput [matDatepicker]="picker" [min]="today" [max]="maxDate"
           (dateChange)="travelDate.set($event.value)" placeholder="Pick a date" readonly />
    <mat-datepicker-toggle matSuffix [for]="picker" />
    <mat-datepicker #picker />
  </mat-form-field>

  <button mat-flat-button color="primary" [disabled]="!canSearch()" (click)="search()">
    Search Buses
  </button>

  @if (source() && destination()) {
    <p class="text-sm text-gray-600">
      <strong>{{ source()!.name }}</strong> → <strong>{{ destination()!.name }}</strong>
    </p>
  }
</section>

<div class="min-h-screen flex items-center justify-center bg-slate-50 p-6">
  <mat-card class="w-full max-w-md p-6">
    <h1 class="text-2xl font-semibold mb-2">Bus Booking System</h1>
    <p class="text-slate-600 mb-6">Foundation milestone — health check.</p>

    <div class="flex items-center gap-3 mb-4">
      @switch (status()) {
        @case ('loading') { <span class="inline-block w-3 h-3 rounded-full bg-amber-400 animate-pulse"></span> }
        @case ('ok')      { <span class="inline-block w-3 h-3 rounded-full bg-emerald-500"></span> }
        @case ('failed')  { <span class="inline-block w-3 h-3 rounded-full bg-rose-500"></span> }
      }
      <span data-testid="status-label">{{ statusLabel() }}</span>
    </div>

    @if (payload(); as p) {
      <dl class="text-sm text-slate-600 space-y-1">
        <div class="flex gap-2"><dt class="font-medium w-24">Service</dt><dd>{{ p.service }}</dd></div>
        <div class="flex gap-2"><dt class="font-medium w-24">Version</dt><dd>{{ p.version }}</dd></div>
        <div class="flex gap-2"><dt class="font-medium w-24">Time (UTC)</dt><dd>{{ p.timestampUtc | date:'medium':'UTC' }}</dd></div>
      </dl>
    }

    <button mat-flat-button color="primary" class="mt-6" (click)="ping()">Check again</button>
  </mat-card>
</div>
```

- [ ] **Step 3: Build check**

```bash
cd frontend/bus-booking-web && ng build --configuration development 2>&1 | tail -5
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add frontend/bus-booking-web/src/app/features/public/home/
git commit -m "feat: add date picker and search button to HomeComponent (M4)"
```

---

## Task 13: Route wiring + operator-shell nav

**Files:**
- Modify: `frontend/bus-booking-web/src/app/app.routes.ts`
- Modify: `frontend/bus-booking-web/src/app/features/operator/operator-shell/operator-shell.component.html`

- [ ] **Step 1: Add new routes to app.routes.ts**

In `app.routes.ts`, add these routes:

After the `''` (home) route, add:
```typescript
{
  path: 'search-results',
  loadComponent: () => import('./features/public/search-results/search-results.component')
    .then(m => m.SearchResultsComponent)
},
{
  path: 'trips/:id',
  loadComponent: () => import('./features/public/trip-detail/trip-detail.component')
    .then(m => m.TripDetailComponent)
},
```

Inside the `operator` children array (after `buses`), add:
```typescript
{
  path: 'schedules',
  loadComponent: () => import('./features/operator/schedules/operator-schedules-list.component')
    .then(m => m.OperatorSchedulesListComponent)
},
```

- [ ] **Step 2: Add Schedules nav item to operator-shell.component.html**

After the `buses` nav link, add:
```html
<a mat-list-item routerLink="schedules" routerLinkActive="bg-slate-100">
  <mat-icon matListItemIcon>schedule</mat-icon>
  <span matListItemTitle>Schedules</span>
</a>
```

- [ ] **Step 3: Also add MatDialogModule to app.config.ts if not present**

Check that `provideAnimations()` is in `app.config.ts`. MatDialog requires it. If missing:
```bash
grep "provideAnimations\|provideAnimationsAsync" frontend/bus-booking-web/src/app/app.config.ts
```

If not found, add `provideAnimationsAsync()` to the providers array in `app.config.ts`:
```typescript
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
// add to providers: provideAnimationsAsync()
```

- [ ] **Step 4: Final build check**

```bash
cd frontend/bus-booking-web && ng build --configuration development 2>&1 | tail -10
```

Expected: `Build succeeded.` with lazy chunks for `search-results`, `trip-detail`, `operator-schedules-list`.

- [ ] **Step 5: Run full backend test suite (regression check)**

```bash
cd backend && dotnet test BusBooking.Api.Tests/BusBooking.Api.Tests.csproj -v n 2>&1 | tail -5
```

Expected: all tests pass.

- [ ] **Step 6: Commit**

```bash
git add frontend/bus-booking-web/src/app/app.routes.ts \
        frontend/bus-booking-web/src/app/features/operator/operator-shell/operator-shell.component.html \
        frontend/bus-booking-web/src/app/app.config.ts
git commit -m "feat: wire M4 routes and add schedules nav to operator shell"
```

---

## Acceptance criteria

- [ ] `GET /api/v1/search?src={cityId}&dst={cityId}&date={date}` returns an array of `SearchResultDto`; calling twice returns the same trip IDs (idempotent materialisation).
- [ ] `GET /api/v1/trips/{id}` returns `TripDetailDto` with `seatLayout.seats` all `"available"`.
- [ ] `GET /api/v1/trips/{id}/seats` returns `SeatLayoutDto`.
- [ ] `POST /api/v1/operator/schedules` with unapproved bus → 422 `BUS_NOT_APPROVED`.
- [ ] `POST /api/v1/operator/schedules` without office at source or destination city → 422 `NO_OFFICE_AT_CITY`.
- [ ] Operator console shows a **Schedules** nav item; clicking it shows the schedule list page.
- [ ] Operator can create a schedule via the form dialog (bus dropdown shows only approved buses).
- [ ] Home page shows a date picker and **Search Buses** button (disabled until all three inputs are filled).
- [ ] Searching navigates to `/search-results?src=…&dst=…&date=…` and shows trip cards.
- [ ] Clicking **View Seats** navigates to `/trips/{id}` and renders the seat map grid.

---

## Risks

- **DateOnly/TimeOnly ASP.NET binding:** `DateOnly` query-parameter binding requires `.NET 7+`; confirm `DateOnlyTypeConverter` or just use `string` in `[FromQuery]` and parse manually if binding fails.
- **MatNativeDateModule vs MatMomentDateModule:** `MatNativeDateModule` uses JS `Date` which is already in `HomeComponent`; confirm it is provided in `app.config.ts` via `provideNativeDateAdapter()` or import `MatNativeDateModule` in the component.
- **M5 seat status:** `TripService.BuildSeatLayoutAsync` is marked with a comment that M5 will add lock/booking filtering — do not add that logic here.
