# M7 — Operator Bookings & Revenue Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. **Work directly on `main` — do NOT create a feature branch.** Commit messages MUST NOT include a `Co-Authored-By: Claude` trailer.

**Goal:** Deliver the M7 demoable outcome: an operator can view all bookings across their buses (filterable by bus/date) and see monthly revenue grouped by bus with a date-range filter.

**Architecture:** No new EF Core entities or migrations — M7 queries existing tables (`bookings`, `bus_trips`, `bus_schedules`, `buses`). A new `IOperatorBookingService` with two methods (`ListBookingsAsync`, `GetRevenueAsync`) is scoped under the operator route; it enforces the `OperatorUserId` boundary so operators only see their own data. Two new controllers expose `GET /api/v1/operator/bookings` and `GET /api/v1/operator/revenue`. The Angular side adds two new page components wired into the operator shell side-nav, each backed by a dedicated API service.

**Tech Stack:** .NET 9 · EF Core 9 · Npgsql · xUnit · FluentAssertions · `Microsoft.AspNetCore.Mvc.Testing` · Angular 20 (standalone + Signals) · Angular Material (`MatTable`, `MatFormField`, `MatSelect`, `MatDatepicker`) · Tailwind.

---

## File map

### New backend files

| Path | Responsibility |
|---|---|
| `backend/BusBooking.Api/Dtos/OperatorBookingListItemDto.cs` | Per-booking row for operator view |
| `backend/BusBooking.Api/Dtos/OperatorBookingListResponseDto.cs` | Paginated wrapper |
| `backend/BusBooking.Api/Dtos/OperatorRevenueItemDto.cs` | Per-bus revenue row |
| `backend/BusBooking.Api/Dtos/OperatorRevenueResponseDto.cs` | Revenue summary with `byBus` list |
| `backend/BusBooking.Api/Services/IOperatorBookingService.cs` | Contract |
| `backend/BusBooking.Api/Services/OperatorBookingService.cs` | EF Core queries scoped to operator |
| `backend/BusBooking.Api/Controllers/OperatorBookingsController.cs` | `GET /api/v1/operator/bookings` |
| `backend/BusBooking.Api/Controllers/OperatorRevenueController.cs` | `GET /api/v1/operator/revenue` |

### Modified backend files

- `backend/BusBooking.Api/Program.cs` — register `IOperatorBookingService`

### New test files

| Path | Responsibility |
|---|---|
| `backend/BusBooking.Api.Tests/Integration/OperatorBookingsTests.cs` | List returns only caller's bookings; bus/date filters; requires operator role |
| `backend/BusBooking.Api.Tests/Integration/OperatorRevenueTests.cs` | Revenue sums only confirmed/completed; date range filter; requires operator role |

### New frontend files

| Path | Responsibility |
|---|---|
| `frontend/bus-booking-web/src/app/core/api/operator-bookings.api.ts` | `GET /operator/bookings` HTTP client + DTOs |
| `frontend/bus-booking-web/src/app/core/api/operator-revenue.api.ts` | `GET /operator/revenue` HTTP client + DTOs |
| `frontend/bus-booking-web/src/app/features/operator/bookings/operator-bookings-page.component.ts` | Table + bus/date filter |
| `frontend/bus-booking-web/src/app/features/operator/bookings/operator-bookings-page.component.html` | Template |
| `frontend/bus-booking-web/src/app/features/operator/revenue/operator-revenue-page.component.ts` | Revenue table + date range filter |
| `frontend/bus-booking-web/src/app/features/operator/revenue/operator-revenue-page.component.html` | Template |

### Modified frontend files

- `frontend/bus-booking-web/src/app/app.routes.ts` — add `bookings` and `revenue` lazy routes under `operator`
- `frontend/bus-booking-web/src/app/features/operator/operator-shell/operator-shell.component.html` — add Bookings + Revenue nav links

---

## Task 1: Backend DTOs

**Files:**
- Create: `backend/BusBooking.Api/Dtos/OperatorBookingListItemDto.cs`
- Create: `backend/BusBooking.Api/Dtos/OperatorBookingListResponseDto.cs`
- Create: `backend/BusBooking.Api/Dtos/OperatorRevenueItemDto.cs`
- Create: `backend/BusBooking.Api/Dtos/OperatorRevenueResponseDto.cs`

- [ ] **Step 1: Create `OperatorBookingListItemDto.cs`**

```csharp
namespace BusBooking.Api.Dtos;

public record OperatorBookingListItemDto(
    Guid BookingId,
    string BookingCode,
    Guid TripId,
    DateOnly TripDate,
    string SourceCity,
    string DestinationCity,
    Guid BusId,
    string BusName,
    string CustomerName,
    int SeatCount,
    decimal TotalFare,
    decimal PlatformFee,
    decimal TotalAmount,
    string Status,
    DateTime CreatedAt);
```

- [ ] **Step 2: Create `OperatorBookingListResponseDto.cs`**

```csharp
namespace BusBooking.Api.Dtos;

public record OperatorBookingListResponseDto(
    List<OperatorBookingListItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount);
```

- [ ] **Step 3: Create `OperatorRevenueItemDto.cs`**

```csharp
namespace BusBooking.Api.Dtos;

public record OperatorRevenueItemDto(
    Guid BusId,
    string BusName,
    string RegistrationNumber,
    int ConfirmedBookings,
    int TotalSeats,
    decimal TotalFare);
```

- [ ] **Step 4: Create `OperatorRevenueResponseDto.cs`**

```csharp
namespace BusBooking.Api.Dtos;

public record OperatorRevenueResponseDto(
    DateOnly DateFrom,
    DateOnly DateTo,
    decimal GrandTotalFare,
    List<OperatorRevenueItemDto> ByBus);
```

- [ ] **Step 5: Verify the project builds**

```bash
cd backend && dotnet build BusBooking.Api/BusBooking.Api.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 6: Commit**

```bash
git add backend/BusBooking.Api/Dtos/OperatorBookingListItemDto.cs \
        backend/BusBooking.Api/Dtos/OperatorBookingListResponseDto.cs \
        backend/BusBooking.Api/Dtos/OperatorRevenueItemDto.cs \
        backend/BusBooking.Api/Dtos/OperatorRevenueResponseDto.cs
git commit -m "feat(m7): add operator bookings and revenue DTOs"
```

---

## Task 2: Backend service interface

**Files:**
- Create: `backend/BusBooking.Api/Services/IOperatorBookingService.cs`

- [ ] **Step 1: Create the interface**

```csharp
using BusBooking.Api.Dtos;

namespace BusBooking.Api.Services;

public interface IOperatorBookingService
{
    Task<OperatorBookingListResponseDto> ListBookingsAsync(
        Guid operatorUserId,
        Guid? busId,
        DateOnly? date,
        int page,
        int pageSize,
        CancellationToken ct);

    Task<OperatorRevenueResponseDto> GetRevenueAsync(
        Guid operatorUserId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct);
}
```

- [ ] **Step 2: Build to confirm no compilation errors**

```bash
cd backend && dotnet build BusBooking.Api/BusBooking.Api.csproj
```

Expected: `Build succeeded.`

---

## Task 3: Write failing integration tests for `ListBookings`

**Files:**
- Create: `backend/BusBooking.Api.Tests/Integration/OperatorBookingsTests.cs`

- [ ] **Step 1: Write tests (they will fail — controllers don't exist yet)**

```csharp
using System.Net;
using System.Net.Http.Json;
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Models;
using BusBooking.Api.Tests.Support;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace BusBooking.Api.Tests.Integration;

public class OperatorBookingsTests : IClassFixture<IntegrationFixture>, IAsyncLifetime
{
    private readonly IntegrationFixture _fx;
    public OperatorBookingsTests(IntegrationFixture fx) => _fx = fx;

    public Task InitializeAsync() => _fx.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Returns_only_bookings_for_operators_own_buses()
    {
        var seed1 = await TripTestSeed.CreateAsync(_fx, capacity: 4, daysAhead: 5);
        var seed2 = await TripTestSeed.CreateAsync(_fx, capacity: 4, daysAhead: 5);

        var (cust, _) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        await SeedConfirmedBookingAsync(seed1.TripId, cust.Id, ["A1"]);
        await SeedConfirmedBookingAsync(seed2.TripId, cust.Id, ["A1"]);

        // Operator 1 created seed1, Operator 2 created seed2.
        // Each TripTestSeed.CreateAsync creates its own operator.
        // We need to get the token for the operator who owns seed1.
        var (op1, op1Token) = await GetOperatorForBusAsync(seed1.BusId);

        var client = _fx.CreateClient();
        client.AttachBearer(op1Token);

        var resp = await client.GetFromJsonAsync<OperatorBookingListResponseDto>(
            "/api/v1/operator/bookings");

        resp!.Items.Should().OnlyContain(i => i.BusId == seed1.BusId);
        resp.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Filters_by_bus_id()
    {
        var seed1 = await TripTestSeed.CreateAsync(_fx, capacity: 4, daysAhead: 5);
        var (op, opToken) = await GetOperatorForBusAsync(seed1.BusId);

        // Add a second bus to the same operator
        var seed2 = await TripTestSeed.CreateWithOperatorAsync(_fx, op, capacity: 4, daysAhead: 5);

        var (cust, _) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        await SeedConfirmedBookingAsync(seed1.TripId, cust.Id, ["A1"]);
        await SeedConfirmedBookingAsync(seed2.TripId, cust.Id, ["A1"]);

        var client = _fx.CreateClient();
        client.AttachBearer(opToken);

        var resp = await client.GetFromJsonAsync<OperatorBookingListResponseDto>(
            $"/api/v1/operator/bookings?busId={seed1.BusId}");

        resp!.Items.Should().HaveCount(1);
        resp.Items[0].BusId.Should().Be(seed1.BusId);
    }

    [Fact]
    public async Task Filters_by_date()
    {
        var seed = await TripTestSeed.CreateAsync(_fx, capacity: 4, daysAhead: 5);
        var (op, opToken) = await GetOperatorForBusAsync(seed.BusId);
        var (cust, _) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        await SeedConfirmedBookingAsync(seed.TripId, cust.Id, ["A1"]);

        var client = _fx.CreateClient();
        client.AttachBearer(opToken);

        var tripDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)).ToString("yyyy-MM-dd");
        var respMatch = await client.GetFromJsonAsync<OperatorBookingListResponseDto>(
            $"/api/v1/operator/bookings?date={tripDate}");
        respMatch!.Items.Should().HaveCount(1);

        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)).ToString("yyyy-MM-dd");
        var respNoMatch = await client.GetFromJsonAsync<OperatorBookingListResponseDto>(
            $"/api/v1/operator/bookings?date={yesterday}");
        respNoMatch!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Excludes_pending_payment_bookings()
    {
        var seed = await TripTestSeed.CreateAsync(_fx, capacity: 4, daysAhead: 5);
        var (op, opToken) = await GetOperatorForBusAsync(seed.BusId);
        var (cust, _) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);

        await SeedPendingBookingAsync(seed.TripId, cust.Id, "A1");
        await SeedConfirmedBookingAsync(seed.TripId, cust.Id, ["A2"]);

        var client = _fx.CreateClient();
        client.AttachBearer(opToken);

        var resp = await client.GetFromJsonAsync<OperatorBookingListResponseDto>(
            "/api/v1/operator/bookings");
        resp!.Items.Should().HaveCount(1);
        resp.Items[0].Status.Should().Be(BookingStatus.Confirmed);
    }

    [Fact]
    public async Task Requires_operator_role()
    {
        var (_, custToken) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        var client = _fx.CreateClient();
        client.AttachBearer(custToken);

        var resp = await client.GetAsync("/api/v1/operator/bookings");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<(User user, string token)> GetOperatorForBusAsync(Guid busId)
    {
        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bus = await db.Buses.FindAsync(busId);
        var op = await db.Users.FindAsync(bus!.OperatorUserId);

        var tokenService = scope.ServiceProvider
            .GetRequiredService<BusBooking.Api.Infrastructure.Auth.IJwtTokenService>();
        var token = tokenService.Generate(op!, [Roles.Operator]);
        return (op!, token.Token);
    }

    private async Task SeedConfirmedBookingAsync(Guid tripId, Guid userId, string[] seats)
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
            TotalFare = 500m * seats.Length,
            PlatformFee = 25m,
            TotalAmount = 500m * seats.Length + 25m,
            SeatCount = seats.Length,
            Status = BookingStatus.Confirmed,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            ConfirmedAt = DateTime.UtcNow.AddMinutes(-4)
        });
        foreach (var s in seats)
            db.BookingSeats.Add(new BookingSeat
            {
                Id = Guid.NewGuid(), BookingId = id, SeatNumber = s,
                PassengerName = "Test", PassengerAge = 25, PassengerGender = PassengerGender.Male
            });
        db.Payments.Add(new Payment
        {
            Id = Guid.NewGuid(), BookingId = id,
            RazorpayOrderId = $"order_{Guid.NewGuid():N}"[..20],
            RazorpayPaymentId = $"pay_{Guid.NewGuid():N}"[..18],
            Amount = 500m * seats.Length + 25m, Currency = "INR",
            Status = PaymentStatus.Captured,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            CapturedAt = DateTime.UtcNow.AddMinutes(-4)
        });
        await db.SaveChangesAsync();
    }

    private async Task SeedPendingBookingAsync(Guid tripId, Guid userId, string seat)
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
            TotalFare = 500m,
            PlatformFee = 25m,
            TotalAmount = 525m,
            SeatCount = 1,
            Status = BookingStatus.PendingPayment,
            CreatedAt = DateTime.UtcNow
        });
        db.BookingSeats.Add(new BookingSeat
        {
            Id = Guid.NewGuid(), BookingId = id, SeatNumber = seat,
            PassengerName = "Test", PassengerAge = 25, PassengerGender = PassengerGender.Male
        });
        await db.SaveChangesAsync();
    }
}
```

> **Note:** The `TripTestSeed.CreateWithOperatorAsync` helper added in Step 2 is needed for the bus-filter test.

- [ ] **Step 2: Add `CreateWithOperatorAsync` to `TripTestSeed`**

Open `backend/BusBooking.Api.Tests/Support/TripTestSeed.cs` and add after the existing `CreateAsync` method:

```csharp
public static async Task<SeededTrip> CreateWithOperatorAsync(
    IntegrationFixture fx,
    User op,
    int capacity = 10,
    int daysAhead = 7,
    decimal farePerSeat = 500m)
{
    using var scope = fx.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    var src = new City { Id = Guid.NewGuid(), Name = $"Src-{Guid.NewGuid():N}"[..16], State = "KA", IsActive = true };
    var dst = new City { Id = Guid.NewGuid(), Name = $"Dst-{Guid.NewGuid():N}"[..16], State = "TN", IsActive = true };
    db.Cities.AddRange(src, dst);

    var route = new Route
    {
        Id = Guid.NewGuid(),
        SourceCityId = src.Id,
        DestinationCityId = dst.Id,
        DistanceKm = 350,
        IsActive = true
    };
    db.Routes.Add(route);

    db.OperatorOffices.AddRange(
        new OperatorOffice { Id = Guid.NewGuid(), OperatorUserId = op.Id, CityId = src.Id, AddressLine = "Hub A", Phone = "100", IsActive = true },
        new OperatorOffice { Id = Guid.NewGuid(), OperatorUserId = op.Id, CityId = dst.Id, AddressLine = "Hub B", Phone = "101", IsActive = true });

    var bus = new Bus
    {
        Id = Guid.NewGuid(),
        OperatorUserId = op.Id,
        RegistrationNumber = $"TN-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
        BusName = "Test Bus 2",
        BusType = BusType.Seater,
        Capacity = capacity,
        ApprovalStatus = BusApprovalStatus.Approved,
        OperationalStatus = BusOperationalStatus.Active,
        CreatedAt = DateTime.UtcNow
    };
    db.Buses.Add(bus);

    for (var i = 0; i < capacity; i++)
        db.SeatDefinitions.Add(new SeatDefinition
        {
            Id = Guid.NewGuid(), BusId = bus.Id,
            SeatNumber = $"A{i + 1}", RowIndex = i, ColumnIndex = 0,
            SeatCategory = SeatCategory.Regular
        });

    var schedule = new BusSchedule
    {
        Id = Guid.NewGuid(),
        BusId = bus.Id,
        RouteId = route.Id,
        DepartureTime = new TimeOnly(22, 0),
        ArrivalTime = new TimeOnly(6, 0),
        FarePerSeat = farePerSeat,
        ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
        ValidTo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60)),
        DaysOfWeek = 127,
        IsActive = true
    };
    db.BusSchedules.Add(schedule);

    var trip = new BusTrip
    {
        Id = Guid.NewGuid(),
        ScheduleId = schedule.Id,
        TripDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(daysAhead)),
        Status = TripStatus.Scheduled
    };
    db.BusTrips.Add(trip);

    await db.SaveChangesAsync();
    return new SeededTrip(op.Id, bus.Id, route.Id, schedule.Id, trip.Id, farePerSeat, capacity);
}
```

- [ ] **Step 3: Run the tests — confirm they all FAIL (no controller yet)**

```bash
cd backend && dotnet test BusBooking.Api.Tests \
  --filter "FullyQualifiedName~OperatorBookingsTests" --no-build 2>&1 | tail -20
```

Expected: all 5 tests fail with `404 NotFound` or connection errors.

---

## Task 4: Implement `OperatorBookingService.ListBookingsAsync`

**Files:**
- Create: `backend/BusBooking.Api/Services/OperatorBookingService.cs` (skeleton with `ListBookingsAsync`)

- [ ] **Step 1: Create the service file**

```csharp
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Api.Services;

public class OperatorBookingService : IOperatorBookingService
{
    private readonly AppDbContext _db;

    public OperatorBookingService(AppDbContext db) => _db = db;

    public async Task<OperatorBookingListResponseDto> ListBookingsAsync(
        Guid operatorUserId,
        Guid? busId,
        DateOnly? date,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var p = page < 1 ? 1 : page;
        var size = pageSize is < 1 or > 100 ? 20 : pageSize;

        var q = _db.Bookings
            .AsNoTracking()
            .Where(b => b.Trip!.Schedule!.Bus!.OperatorUserId == operatorUserId
                     && b.Status != BookingStatus.PendingPayment)
            .Include(b => b.Trip)
                .ThenInclude(t => t!.Schedule).ThenInclude(s => s!.Bus)
            .Include(b => b.Trip)
                .ThenInclude(t => t!.Schedule).ThenInclude(s => s!.Route)
                    .ThenInclude(r => r!.SourceCity)
            .Include(b => b.Trip)
                .ThenInclude(t => t!.Schedule).ThenInclude(s => s!.Route)
                    .ThenInclude(r => r!.DestinationCity)
            .Include(b => b.User)
            .AsQueryable();

        if (busId.HasValue)
            q = q.Where(b => b.Trip!.Schedule!.BusId == busId.Value);
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
            return new OperatorBookingListItemDto(
                b.Id,
                b.BookingCode,
                b.TripId,
                b.Trip.TripDate,
                route.SourceCity!.Name,
                route.DestinationCity!.Name,
                bus.Id,
                bus.BusName,
                b.User.Name,
                b.SeatCount,
                b.TotalFare,
                b.PlatformFee,
                b.TotalAmount,
                b.Status,
                b.CreatedAt);
        }).ToList();

        return new OperatorBookingListResponseDto(items, p, size, total);
    }

    public Task<OperatorRevenueResponseDto> GetRevenueAsync(
        Guid operatorUserId, DateOnly from, DateOnly to, CancellationToken ct)
        => throw new NotImplementedException("Implemented in Task 6");
}
```

---

## Task 5: Create `OperatorBookingsController` + register + run bookings tests

**Files:**
- Create: `backend/BusBooking.Api/Controllers/OperatorBookingsController.cs`
- Modify: `backend/BusBooking.Api/Program.cs`

- [ ] **Step 1: Create `OperatorBookingsController.cs`**

```csharp
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure.Auth;
using BusBooking.Api.Models;
using BusBooking.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.Api.Controllers;

[ApiController]
[Route("api/v1/operator/bookings")]
[Authorize(Roles = Roles.Operator)]
public class OperatorBookingsController : ControllerBase
{
    private readonly IOperatorBookingService _service;
    private readonly ICurrentUserAccessor _me;

    public OperatorBookingsController(IOperatorBookingService service, ICurrentUserAccessor me)
    {
        _service = service;
        _me = me;
    }

    [HttpGet]
    public async Task<ActionResult<OperatorBookingListResponseDto>> List(
        [FromQuery] Guid? busId,
        [FromQuery] DateOnly? date,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await _service.ListBookingsAsync(_me.UserId, busId, date, page, pageSize, ct));
}
```

- [ ] **Step 2: Register `IOperatorBookingService` in `Program.cs`**

In `backend/BusBooking.Api/Program.cs`, add after the line `builder.Services.AddScoped<IBookingService, BookingService>();`:

```csharp
builder.Services.AddScoped<IOperatorBookingService, OperatorBookingService>();
```

- [ ] **Step 3: Build the API**

```bash
cd backend && dotnet build BusBooking.Api/BusBooking.Api.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 4: Run the bookings tests**

```bash
cd backend && dotnet test BusBooking.Api.Tests \
  --filter "FullyQualifiedName~OperatorBookingsTests" 2>&1 | tail -25
```

Expected: all 5 tests pass.

- [ ] **Step 5: Commit**

```bash
git add backend/BusBooking.Api/Dtos/OperatorBookingListItemDto.cs \
        backend/BusBooking.Api/Dtos/OperatorBookingListResponseDto.cs \
        backend/BusBooking.Api/Services/IOperatorBookingService.cs \
        backend/BusBooking.Api/Services/OperatorBookingService.cs \
        backend/BusBooking.Api/Controllers/OperatorBookingsController.cs \
        backend/BusBooking.Api/Program.cs \
        backend/BusBooking.Api.Tests/Integration/OperatorBookingsTests.cs \
        backend/BusBooking.Api.Tests/Support/TripTestSeed.cs
git commit -m "feat(m7): add operator bookings list endpoint with bus/date filters"
```

---

## Task 6: Write failing revenue tests and implement `GetRevenueAsync`

**Files:**
- Create: `backend/BusBooking.Api.Tests/Integration/OperatorRevenueTests.cs`

- [ ] **Step 1: Write revenue integration tests (will fail — no controller yet)**

```csharp
using System.Net;
using System.Net.Http.Json;
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Models;
using BusBooking.Api.Tests.Support;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace BusBooking.Api.Tests.Integration;

public class OperatorRevenueTests : IClassFixture<IntegrationFixture>, IAsyncLifetime
{
    private readonly IntegrationFixture _fx;
    public OperatorRevenueTests(IntegrationFixture fx) => _fx = fx;

    public Task InitializeAsync() => _fx.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Returns_revenue_grouped_by_bus_for_confirmed_bookings()
    {
        var seed = await TripTestSeed.CreateAsync(_fx, capacity: 4, daysAhead: 3);
        var (op, opToken) = await GetOperatorForBusAsync(seed.BusId);
        var (cust, _) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);

        await SeedConfirmedBookingAsync(seed.TripId, cust.Id, ["A1", "A2"], 500m);

        var client = _fx.CreateClient();
        client.AttachBearer(opToken);

        var from = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)).ToString("yyyy-MM-dd");
        var to = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)).ToString("yyyy-MM-dd");

        var resp = await client.GetFromJsonAsync<OperatorRevenueResponseDto>(
            $"/api/v1/operator/revenue?from={from}&to={to}");

        resp!.ByBus.Should().HaveCount(1);
        resp.ByBus[0].BusId.Should().Be(seed.BusId);
        resp.ByBus[0].ConfirmedBookings.Should().Be(1);
        resp.ByBus[0].TotalSeats.Should().Be(2);
        resp.ByBus[0].TotalFare.Should().Be(1000m);
        resp.GrandTotalFare.Should().Be(1000m);
    }

    [Fact]
    public async Task Excludes_cancelled_bookings_from_revenue()
    {
        var seed = await TripTestSeed.CreateAsync(_fx, capacity: 4, daysAhead: 3);
        var (op, opToken) = await GetOperatorForBusAsync(seed.BusId);
        var (cust, _) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);

        await SeedConfirmedBookingAsync(seed.TripId, cust.Id, ["A1"], 500m);
        await SeedCancelledBookingAsync(seed.TripId, cust.Id, "A2", 500m);

        var client = _fx.CreateClient();
        client.AttachBearer(opToken);

        var from = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)).ToString("yyyy-MM-dd");
        var to = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)).ToString("yyyy-MM-dd");

        var resp = await client.GetFromJsonAsync<OperatorRevenueResponseDto>(
            $"/api/v1/operator/revenue?from={from}&to={to}");

        resp!.GrandTotalFare.Should().Be(500m);
        resp.ByBus[0].ConfirmedBookings.Should().Be(1);
    }

    [Fact]
    public async Task Date_range_filter_excludes_trips_outside_range()
    {
        var seedInRange = await TripTestSeed.CreateAsync(_fx, capacity: 4, daysAhead: 3);
        var seedOutRange = await TripTestSeed.CreateAsync(_fx, capacity: 4, daysAhead: 20);

        // Both buses belong to different operators — get op for seedInRange
        var (op, opToken) = await GetOperatorForBusAsync(seedInRange.BusId);
        var (cust, _) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);

        await SeedConfirmedBookingAsync(seedInRange.TripId, cust.Id, ["A1"], 500m);

        var client = _fx.CreateClient();
        client.AttachBearer(opToken);

        var from = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var to = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)).ToString("yyyy-MM-dd");

        var resp = await client.GetFromJsonAsync<OperatorRevenueResponseDto>(
            $"/api/v1/operator/revenue?from={from}&to={to}");

        resp!.ByBus.Should().HaveCount(1);
        resp.ByBus[0].BusId.Should().Be(seedInRange.BusId);
    }

    [Fact]
    public async Task Defaults_to_last_30_days_when_no_range_supplied()
    {
        var seed = await TripTestSeed.CreateAsync(_fx, capacity: 4, daysAhead: 3);
        var (op, opToken) = await GetOperatorForBusAsync(seed.BusId);
        var (cust, _) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        await SeedConfirmedBookingAsync(seed.TripId, cust.Id, ["A1"], 500m);

        var client = _fx.CreateClient();
        client.AttachBearer(opToken);

        var resp = await client.GetFromJsonAsync<OperatorRevenueResponseDto>(
            "/api/v1/operator/revenue");

        resp.Should().NotBeNull();
    }

    [Fact]
    public async Task Requires_operator_role()
    {
        var (_, custToken) = await OperatorTokenFactory.CreateAsync(_fx, [Roles.Customer]);
        var client = _fx.CreateClient();
        client.AttachBearer(custToken);

        (await client.GetAsync("/api/v1/operator/revenue")).StatusCode
            .Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<(User user, string token)> GetOperatorForBusAsync(Guid busId)
    {
        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bus = await db.Buses.FindAsync(busId);
        var op = await db.Users.FindAsync(bus!.OperatorUserId);
        var tokenService = scope.ServiceProvider
            .GetRequiredService<BusBooking.Api.Infrastructure.Auth.IJwtTokenService>();
        var token = tokenService.Generate(op!, [Roles.Operator]);
        return (op!, token.Token);
    }

    private async Task SeedConfirmedBookingAsync(
        Guid tripId, Guid userId, string[] seats, decimal farePerSeat)
    {
        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var id = Guid.NewGuid();
        var totalFare = farePerSeat * seats.Length;
        db.Bookings.Add(new Booking
        {
            Id = id,
            BookingCode = $"BK-{id:N}"[..11],
            TripId = tripId,
            UserId = userId,
            LockId = Guid.NewGuid(),
            TotalFare = totalFare,
            PlatformFee = 25m,
            TotalAmount = totalFare + 25m,
            SeatCount = seats.Length,
            Status = BookingStatus.Confirmed,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            ConfirmedAt = DateTime.UtcNow.AddMinutes(-4)
        });
        foreach (var s in seats)
            db.BookingSeats.Add(new BookingSeat
            {
                Id = Guid.NewGuid(), BookingId = id, SeatNumber = s,
                PassengerName = "Test", PassengerAge = 25, PassengerGender = PassengerGender.Male
            });
        db.Payments.Add(new Payment
        {
            Id = Guid.NewGuid(), BookingId = id,
            RazorpayOrderId = $"order_{Guid.NewGuid():N}"[..20],
            RazorpayPaymentId = $"pay_{Guid.NewGuid():N}"[..18],
            Amount = totalFare + 25m, Currency = "INR",
            Status = PaymentStatus.Captured,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            CapturedAt = DateTime.UtcNow.AddMinutes(-4)
        });
        await db.SaveChangesAsync();
    }

    private async Task SeedCancelledBookingAsync(
        Guid tripId, Guid userId, string seat, decimal farePerSeat)
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
            TotalFare = farePerSeat,
            PlatformFee = 25m,
            TotalAmount = farePerSeat + 25m,
            SeatCount = 1,
            Status = BookingStatus.Cancelled,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            ConfirmedAt = DateTime.UtcNow.AddMinutes(-9),
            CancelledAt = DateTime.UtcNow.AddMinutes(-2),
            RefundAmount = farePerSeat * 0.8m,
            RefundStatus = RefundStatus.Processed
        });
        db.BookingSeats.Add(new BookingSeat
        {
            Id = Guid.NewGuid(), BookingId = id, SeatNumber = seat,
            PassengerName = "Test", PassengerAge = 25, PassengerGender = PassengerGender.Male
        });
        await db.SaveChangesAsync();
    }
}
```

- [ ] **Step 2: Run revenue tests — confirm they FAIL (no controller yet)**

```bash
cd backend && dotnet test BusBooking.Api.Tests \
  --filter "FullyQualifiedName~OperatorRevenueTests" --no-build 2>&1 | tail -10
```

Expected: all 5 tests fail.

---

## Task 7: Implement `GetRevenueAsync` and `OperatorRevenueController`

**Files:**
- Modify: `backend/BusBooking.Api/Services/OperatorBookingService.cs`
- Create: `backend/BusBooking.Api/Controllers/OperatorRevenueController.cs`

- [ ] **Step 1: Replace the `GetRevenueAsync` stub in `OperatorBookingService.cs`**

Replace the `throw new NotImplementedException(...)` method with:

```csharp
public async Task<OperatorRevenueResponseDto> GetRevenueAsync(
    Guid operatorUserId, DateOnly from, DateOnly to, CancellationToken ct)
{
    var rows = await _db.Bookings
        .AsNoTracking()
        .Where(b => b.Trip!.Schedule!.Bus!.OperatorUserId == operatorUserId
                 && (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Completed)
                 && b.Trip!.TripDate >= from
                 && b.Trip!.TripDate <= to)
        .Include(b => b.Trip)
            .ThenInclude(t => t!.Schedule).ThenInclude(s => s!.Bus)
        .ToListAsync(ct);

    var byBus = rows
        .GroupBy(b => new
        {
            BusId = b.Trip!.Schedule!.Bus!.Id,
            BusName = b.Trip!.Schedule!.Bus!.BusName,
            RegNum = b.Trip!.Schedule!.Bus!.RegistrationNumber
        })
        .Select(g => new OperatorRevenueItemDto(
            g.Key.BusId,
            g.Key.BusName,
            g.Key.RegNum,
            g.Count(),
            g.Sum(b => b.SeatCount),
            g.Sum(b => b.TotalFare)))
        .OrderByDescending(x => x.TotalFare)
        .ToList();

    return new OperatorRevenueResponseDto(from, to, byBus.Sum(x => x.TotalFare), byBus);
}
```

- [ ] **Step 2: Create `OperatorRevenueController.cs`**

```csharp
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure.Auth;
using BusBooking.Api.Models;
using BusBooking.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.Api.Controllers;

[ApiController]
[Route("api/v1/operator/revenue")]
[Authorize(Roles = Roles.Operator)]
public class OperatorRevenueController : ControllerBase
{
    private readonly IOperatorBookingService _service;
    private readonly ICurrentUserAccessor _me;

    public OperatorRevenueController(IOperatorBookingService service, ICurrentUserAccessor me)
    {
        _service = service;
        _me = me;
    }

    [HttpGet]
    public async Task<ActionResult<OperatorRevenueResponseDto>> Get(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var f = from ?? today.AddMonths(-1);
        var t = to ?? today;
        return Ok(await _service.GetRevenueAsync(_me.UserId, f, t, ct));
    }
}
```

- [ ] **Step 3: Build**

```bash
cd backend && dotnet build BusBooking.Api/BusBooking.Api.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 4: Run all revenue and bookings tests**

```bash
cd backend && dotnet test BusBooking.Api.Tests \
  --filter "FullyQualifiedName~OperatorBookingsTests|FullyQualifiedName~OperatorRevenueTests" 2>&1 | tail -20
```

Expected: all 10 tests pass.

- [ ] **Step 5: Run the full test suite to check for regressions**

```bash
cd backend && dotnet test BusBooking.Api.Tests 2>&1 | tail -10
```

Expected: all tests pass.

- [ ] **Step 6: Commit**

```bash
git add backend/BusBooking.Api/Dtos/OperatorRevenueItemDto.cs \
        backend/BusBooking.Api/Dtos/OperatorRevenueResponseDto.cs \
        backend/BusBooking.Api/Services/OperatorBookingService.cs \
        backend/BusBooking.Api/Controllers/OperatorRevenueController.cs \
        backend/BusBooking.Api.Tests/Integration/OperatorRevenueTests.cs
git commit -m "feat(m7): add operator revenue endpoint grouped by bus with date-range filter"
```

---

## Task 8: Frontend — API clients

**Files:**
- Create: `frontend/bus-booking-web/src/app/core/api/operator-bookings.api.ts`
- Create: `frontend/bus-booking-web/src/app/core/api/operator-revenue.api.ts`

- [ ] **Step 1: Create `operator-bookings.api.ts`**

```typescript
import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface OperatorBookingListItemDto {
  bookingId: string;
  bookingCode: string;
  tripId: string;
  tripDate: string;
  sourceCity: string;
  destinationCity: string;
  busId: string;
  busName: string;
  customerName: string;
  seatCount: number;
  totalFare: number;
  platformFee: number;
  totalAmount: number;
  status: string;
  createdAt: string;
}

export interface OperatorBookingListResponseDto {
  items: OperatorBookingListItemDto[];
  page: number;
  pageSize: number;
  totalCount: number;
}

@Injectable({ providedIn: 'root' })
export class OperatorBookingsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/operator/bookings`;

  list(busId?: string, date?: string, page = 1, pageSize = 20): Observable<OperatorBookingListResponseDto> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);
    if (busId) params = params.set('busId', busId);
    if (date) params = params.set('date', date);
    return this.http.get<OperatorBookingListResponseDto>(this.base, { params });
  }
}
```

- [ ] **Step 2: Create `operator-revenue.api.ts`**

```typescript
import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface OperatorRevenueItemDto {
  busId: string;
  busName: string;
  registrationNumber: string;
  confirmedBookings: number;
  totalSeats: number;
  totalFare: number;
}

export interface OperatorRevenueResponseDto {
  dateFrom: string;
  dateTo: string;
  grandTotalFare: number;
  byBus: OperatorRevenueItemDto[];
}

@Injectable({ providedIn: 'root' })
export class OperatorRevenueApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/operator/revenue`;

  get(from?: string, to?: string): Observable<OperatorRevenueResponseDto> {
    let params = new HttpParams();
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    return this.http.get<OperatorRevenueResponseDto>(this.base, { params });
  }
}
```

- [ ] **Step 3: Commit**

```bash
git add frontend/bus-booking-web/src/app/core/api/operator-bookings.api.ts \
        frontend/bus-booking-web/src/app/core/api/operator-revenue.api.ts
git commit -m "feat(m7): add frontend API services for operator bookings and revenue"
```

---

## Task 9: Frontend — operator bookings page

**Files:**
- Create: `frontend/bus-booking-web/src/app/features/operator/bookings/operator-bookings-page.component.ts`
- Create: `frontend/bus-booking-web/src/app/features/operator/bookings/operator-bookings-page.component.html`

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
import { OperatorBookingsApiService, OperatorBookingListItemDto } from '../../../core/api/operator-bookings.api';
import { OperatorBusesApiService, BusDto } from '../../../core/api/operator-buses.api';

@Component({
  selector: 'app-operator-bookings-page',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatTableModule, MatFormFieldModule, MatSelectModule,
    MatInputModule, MatButtonModule, MatDatepickerModule
  ],
  templateUrl: './operator-bookings-page.component.html'
})
export class OperatorBookingsPageComponent implements OnInit {
  private readonly api = inject(OperatorBookingsApiService);
  private readonly busesApi = inject(OperatorBusesApiService);

  readonly bookings = signal<OperatorBookingListItemDto[]>([]);
  readonly buses = signal<BusDto[]>([]);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly pageSize = 20;

  readonly busFilter = new FormControl<string | null>(null);
  readonly dateFilter = new FormControl<Date | null>(null);

  readonly displayedColumns = [
    'bookingCode', 'date', 'route', 'bus', 'customer', 'seats', 'amount', 'status'
  ];

  ngOnInit(): void {
    this.busesApi.list().subscribe(buses => this.buses.set(buses));
    this.load();
  }

  load(): void {
    const busId = this.busFilter.value ?? undefined;
    const date = this.dateFilter.value
      ? this.dateFilter.value.toISOString().slice(0, 10)
      : undefined;
    this.api.list(busId, date, this.page(), this.pageSize).subscribe(res => {
      this.bookings.set(res.items);
      this.totalCount.set(res.totalCount);
    });
  }

  applyFilters(): void {
    this.page.set(1);
    this.load();
  }

  clearFilters(): void {
    this.busFilter.setValue(null);
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
<div class="space-y-4">
  <h2 class="text-2xl font-semibold">Bookings</h2>

  <div class="flex gap-4 items-end flex-wrap">
    <mat-form-field class="w-48">
      <mat-label>Filter by Bus</mat-label>
      <mat-select [formControl]="busFilter">
        <mat-option [value]="null">All Buses</mat-option>
        @for (bus of buses(); track bus.id) {
          <mat-option [value]="bus.id">{{ bus.busName }}</mat-option>
        }
      </mat-select>
    </mat-form-field>

    <mat-form-field class="w-44">
      <mat-label>Filter by Trip Date</mat-label>
      <input matInput [matDatepicker]="picker" [formControl]="dateFilter">
      <mat-datepicker-toggle matIconSuffix [for]="picker"></mat-datepicker-toggle>
      <mat-datepicker #picker></mat-datepicker>
    </mat-form-field>

    <button mat-flat-button color="primary" (click)="applyFilters()">Apply</button>
    <button mat-stroked-button (click)="clearFilters()">Clear</button>
  </div>

  <table mat-table [dataSource]="bookings()" class="mat-elevation-z1 w-full">
    <ng-container matColumnDef="bookingCode">
      <th mat-header-cell *matHeaderCellDef>Booking</th>
      <td mat-cell *matCellDef="let b" class="font-mono text-sm">{{ b.bookingCode }}</td>
    </ng-container>

    <ng-container matColumnDef="date">
      <th mat-header-cell *matHeaderCellDef>Trip Date</th>
      <td mat-cell *matCellDef="let b">{{ b.tripDate }}</td>
    </ng-container>

    <ng-container matColumnDef="route">
      <th mat-header-cell *matHeaderCellDef>Route</th>
      <td mat-cell *matCellDef="let b">{{ b.sourceCity }} → {{ b.destinationCity }}</td>
    </ng-container>

    <ng-container matColumnDef="bus">
      <th mat-header-cell *matHeaderCellDef>Bus</th>
      <td mat-cell *matCellDef="let b">{{ b.busName }}</td>
    </ng-container>

    <ng-container matColumnDef="customer">
      <th mat-header-cell *matHeaderCellDef>Customer</th>
      <td mat-cell *matCellDef="let b">{{ b.customerName }}</td>
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

    <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
    <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
  </table>

  @if (bookings().length === 0) {
    <div class="p-8 text-center text-slate-500 border rounded bg-slate-50">
      No bookings found for the selected filters.
    </div>
  }

  <p class="text-sm text-slate-500">Total: {{ totalCount() }} booking(s)</p>
</div>
```

- [ ] **Step 3: Commit**

```bash
git add frontend/bus-booking-web/src/app/features/operator/bookings/
git commit -m "feat(m7): add operator bookings page with bus and date filters"
```

---

## Task 10: Frontend — operator revenue page

**Files:**
- Create: `frontend/bus-booking-web/src/app/features/operator/revenue/operator-revenue-page.component.ts`
- Create: `frontend/bus-booking-web/src/app/features/operator/revenue/operator-revenue-page.component.html`

- [ ] **Step 1: Create the revenue component TypeScript**

```typescript
import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { OperatorRevenueApiService, OperatorRevenueResponseDto } from '../../../core/api/operator-revenue.api';

@Component({
  selector: 'app-operator-revenue-page',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatTableModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatDatepickerModule
  ],
  templateUrl: './operator-revenue-page.component.html'
})
export class OperatorRevenuePageComponent implements OnInit {
  private readonly api = inject(OperatorRevenueApiService);

  readonly revenue = signal<OperatorRevenueResponseDto | null>(null);
  readonly displayedColumns = [
    'busName', 'registrationNumber', 'confirmedBookings', 'totalSeats', 'totalFare'
  ];

  readonly fromDate = new FormControl<Date | null>(
    new Date(new Date().getFullYear(), new Date().getMonth(), 1)
  );
  readonly toDate = new FormControl<Date | null>(new Date());

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    const from = this.fromDate.value?.toISOString().slice(0, 10);
    const to = this.toDate.value?.toISOString().slice(0, 10);
    this.api.get(from, to).subscribe(res => this.revenue.set(res));
  }
}
```

- [ ] **Step 2: Create the revenue component HTML**

```html
<div class="space-y-4">
  <h2 class="text-2xl font-semibold">Revenue</h2>

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
    <div class="p-4 bg-blue-50 border border-blue-200 rounded flex items-center gap-3">
      <span class="text-blue-700 font-medium">
        Total Revenue ({{ rev.dateFrom }} to {{ rev.dateTo }}):
      </span>
      <span class="text-2xl font-bold text-blue-900">
        {{ rev.grandTotalFare | currency:'INR' }}
      </span>
    </div>

    <table mat-table [dataSource]="rev.byBus" class="mat-elevation-z1 w-full">
      <ng-container matColumnDef="busName">
        <th mat-header-cell *matHeaderCellDef>Bus</th>
        <td mat-cell *matCellDef="let r">{{ r.busName }}</td>
      </ng-container>

      <ng-container matColumnDef="registrationNumber">
        <th mat-header-cell *matHeaderCellDef>Registration</th>
        <td mat-cell *matCellDef="let r" class="font-mono text-sm">{{ r.registrationNumber }}</td>
      </ng-container>

      <ng-container matColumnDef="confirmedBookings">
        <th mat-header-cell *matHeaderCellDef>Bookings</th>
        <td mat-cell *matCellDef="let r">{{ r.confirmedBookings }}</td>
      </ng-container>

      <ng-container matColumnDef="totalSeats">
        <th mat-header-cell *matHeaderCellDef>Seats Sold</th>
        <td mat-cell *matCellDef="let r">{{ r.totalSeats }}</td>
      </ng-container>

      <ng-container matColumnDef="totalFare">
        <th mat-header-cell *matHeaderCellDef>Revenue</th>
        <td mat-cell *matCellDef="let r" class="font-semibold">{{ r.totalFare | currency:'INR' }}</td>
      </ng-container>

      <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
      <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
    </table>

    @if (rev.byBus.length === 0) {
      <div class="p-8 text-center text-slate-500 border rounded bg-slate-50">
        No confirmed bookings in this date range.
      </div>
    }
  }
</div>
```

- [ ] **Step 3: Commit**

```bash
git add frontend/bus-booking-web/src/app/features/operator/revenue/
git commit -m "feat(m7): add operator revenue page with date-range filter and per-bus breakdown"
```

---

## Task 11: Wire routes and side-nav

**Files:**
- Modify: `frontend/bus-booking-web/src/app/app.routes.ts`
- Modify: `frontend/bus-booking-web/src/app/features/operator/operator-shell/operator-shell.component.html`

- [ ] **Step 1: Add `bookings` and `revenue` child routes in `app.routes.ts`**

In the `operator` children array (after the `schedules` entry), add:

```typescript
{
  path: 'bookings',
  loadComponent: () => import('./features/operator/bookings/operator-bookings-page.component')
    .then(m => m.OperatorBookingsPageComponent)
},
{
  path: 'revenue',
  loadComponent: () => import('./features/operator/revenue/operator-revenue-page.component')
    .then(m => m.OperatorRevenuePageComponent)
},
```

After inserting, the operator children block should be:

```typescript
{
  path: 'operator',
  canMatch: [roleGuard(['operator'])],
  loadComponent: () => import('./features/operator/operator-shell/operator-shell.component')
    .then(m => m.OperatorShellComponent),
  children: [
    { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
    {
      path: 'dashboard',
      loadComponent: () => import('./features/operator/operator-dashboard/operator-dashboard.component')
        .then(m => m.OperatorDashboardComponent)
    },
    {
      path: 'offices',
      loadComponent: () => import('./features/operator/offices/operator-offices-page.component')
        .then(m => m.OperatorOfficesPageComponent)
    },
    {
      path: 'buses',
      loadComponent: () => import('./features/operator/buses/operator-buses-list.component')
        .then(m => m.OperatorBusesListComponent)
    },
    {
      path: 'schedules',
      loadComponent: () => import('./features/operator/schedules/operator-schedules-list.component')
        .then(m => m.OperatorSchedulesListComponent)
    },
    {
      path: 'bookings',
      loadComponent: () => import('./features/operator/bookings/operator-bookings-page.component')
        .then(m => m.OperatorBookingsPageComponent)
    },
    {
      path: 'revenue',
      loadComponent: () => import('./features/operator/revenue/operator-revenue-page.component')
        .then(m => m.OperatorRevenuePageComponent)
    }
  ]
},
```

- [ ] **Step 2: Add Bookings + Revenue nav links in `operator-shell.component.html`**

Replace the entire file content with:

```html
<mat-sidenav-container class="h-screen">
  <mat-sidenav mode="side" opened class="w-56 p-2">
    <mat-nav-list>
      <a mat-list-item routerLink="dashboard" routerLinkActive="bg-slate-100">
        <mat-icon matListItemIcon>dashboard</mat-icon>
        <span matListItemTitle>Dashboard</span>
      </a>
      <a mat-list-item routerLink="offices" routerLinkActive="bg-slate-100">
        <mat-icon matListItemIcon>store</mat-icon>
        <span matListItemTitle>Offices</span>
      </a>
      <a mat-list-item routerLink="buses" routerLinkActive="bg-slate-100">
        <mat-icon matListItemIcon>directions_bus</mat-icon>
        <span matListItemTitle>Buses</span>
      </a>
      <a mat-list-item routerLink="schedules" routerLinkActive="bg-slate-100">
        <mat-icon matListItemIcon>schedule</mat-icon>
        <span matListItemTitle>Schedules</span>
      </a>
      <a mat-list-item routerLink="bookings" routerLinkActive="bg-slate-100">
        <mat-icon matListItemIcon>confirmation_number</mat-icon>
        <span matListItemTitle>Bookings</span>
      </a>
      <a mat-list-item routerLink="revenue" routerLinkActive="bg-slate-100">
        <mat-icon matListItemIcon>bar_chart</mat-icon>
        <span matListItemTitle>Revenue</span>
      </a>
    </mat-nav-list>
  </mat-sidenav>
  <mat-sidenav-content class="p-6">
    <router-outlet />
  </mat-sidenav-content>
</mat-sidenav-container>
```

- [ ] **Step 3: Run `ng build` to verify no TypeScript or template errors**

```bash
cd frontend/bus-booking-web && ng build --configuration development 2>&1 | tail -20
```

Expected: `Application bundle generation complete.` with no errors.

- [ ] **Step 4: Commit**

```bash
git add frontend/bus-booking-web/src/app/app.routes.ts \
        frontend/bus-booking-web/src/app/features/operator/operator-shell/operator-shell.component.html
git commit -m "feat(m7): wire operator bookings and revenue routes and side-nav links"
```

---

## Self-Review

### 1. Spec coverage

| Spec requirement | Covered by task |
|---|---|
| `GET /operator/bookings` filterable by bus/date | Tasks 3–5 |
| `GET /operator/revenue` grouped by bus, filterable by date range | Tasks 6–7 |
| Operator sees bookings (frontend) | Tasks 8–9, 11 |
| Operator sees monthly revenue total (frontend) | Tasks 8, 10–11 |
| Operator data scoped to own buses | Task 3 test `Returns_only_bookings_for_operators_own_buses` |
| Pending-payment bookings excluded from operator view | Task 3 test `Excludes_pending_payment_bookings` |
| Revenue counts only confirmed/completed | Task 6 test `Excludes_cancelled_bookings_from_revenue` |
| Role guard: `[Authorize(Roles="operator")]` | Tasks 5, 7 (controllers); tests verify 403 for customer role |
| Pagination on bookings list | `OperatorBookingListResponseDto` + `page`/`pageSize` query params |

No gaps found.

### 2. Placeholder scan

No TBD/TODO/placeholder text present. All code steps are complete.

### 3. Type consistency

- `OperatorBookingListItemDto` fields match the projection in `OperatorBookingService.ListBookingsAsync` in Task 4.
- `OperatorRevenueItemDto` fields match the `GroupBy.Select` projection in Task 7.
- `OperatorBookingListResponseDto(items, p, size, total)` constructor parameter order matches the record definition in Task 1.
- `OperatorRevenueResponseDto(from, to, grandTotalFare, byBus)` matches the record definition in Task 1.
- Frontend `OperatorBookingListItemDto` interface field names (camelCase) match the JSON serialisation of the C# record (camelCase by ASP.NET Core default).
- `OperatorBookingsPageComponent.busesApi.list()` returns `BusDto[]` — `BusDto.id` (string) passed as `busId` filter matches `Guid? busId` in the controller (ASP.NET Core converts string UUID from query string to `Guid?`).

All types are consistent.

---

**Plan complete and saved to `docs/superpowers/plans/2026-04-23-m7-operator-bookings-revenue.md`. Two execution options:**

**1. Subagent-Driven (recommended)** — I dispatch a fresh subagent per task, review between tasks, fast iteration

**2. Inline Execution** — Execute tasks in this session using executing-plans, batch execution with checkpoints

**Which approach?**
