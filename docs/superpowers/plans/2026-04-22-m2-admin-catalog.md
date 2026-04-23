# M2 — Admin Catalog (Cities, Routes, Platform Fee) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. **Work directly on `main` — do NOT create a feature branch.** Commit messages MUST NOT include a `Co-Authored-By: Claude` trailer.

**Goal:** Deliver the M2 demoable outcome from the spec: admin can create/edit/deactivate cities, routes, and the platform fee config; the public home page exposes a fuzzy city autocomplete (pg_trgm) that matches what the admin seeded.

**Architecture:** Three additional EF Core 9 entities (`cities`, `routes`, `platform_fee_config`) mapped into the existing `AppDbContext`. Three admin-only REST controllers (`/admin/cities`, `/admin/routes`, `/admin/platform-fee`) built on the layered Controller → Service → DbContext pattern established in M1. One public `GET /cities?q=` endpoint using the `pg_trgm` GIN index for fuzzy autocomplete. Angular side adds a reusable `CityAutocompleteComponent` (Material `mat-autocomplete`, 200 ms debounce, min 2 chars), three admin list/form pages, and wires the autocomplete into the public home-page source/destination inputs.

**Tech Stack:** .NET 9 · EF Core 9 · Npgsql · FluentValidation · xUnit · FluentAssertions · `Microsoft.AspNetCore.Mvc.Testing` · Angular 20 (standalone + Signals) · Angular Material · Tailwind v3 · Jasmine/Karma · PostgreSQL (`citext`, `pg_trgm` — both already registered by M1 migration).

---

## File map

### New backend files

| Path | Responsibility |
|---|---|
| `backend/BusBooking.Api/Models/City.cs` | `cities` entity |
| `backend/BusBooking.Api/Models/Route.cs` | `routes` entity (named `Route` in C#, table `routes`) |
| `backend/BusBooking.Api/Models/PlatformFeeConfig.cs` | `platform_fee_config` entity |
| `backend/BusBooking.Api/Models/PlatformFeeType.cs` | `PlatformFeeType` constants (`fixed`, `percent`) |
| `backend/BusBooking.Api/Dtos/CityDto.cs` | Public shape for a city |
| `backend/BusBooking.Api/Dtos/CreateCityRequest.cs` | Admin create payload |
| `backend/BusBooking.Api/Dtos/UpdateCityRequest.cs` | Admin patch payload |
| `backend/BusBooking.Api/Dtos/RouteDto.cs` | Public shape for a route (includes nested source/destination CityDto) |
| `backend/BusBooking.Api/Dtos/CreateRouteRequest.cs` | Admin create payload |
| `backend/BusBooking.Api/Dtos/UpdateRouteRequest.cs` | Admin patch payload |
| `backend/BusBooking.Api/Dtos/PlatformFeeDto.cs` | `PlatformFeeDto(FeeType, Value, EffectiveFrom)` |
| `backend/BusBooking.Api/Dtos/UpdatePlatformFeeRequest.cs` | Admin PUT payload |
| `backend/BusBooking.Api/Validators/CreateCityRequestValidator.cs` | FluentValidation |
| `backend/BusBooking.Api/Validators/UpdateCityRequestValidator.cs` | FluentValidation |
| `backend/BusBooking.Api/Validators/CreateRouteRequestValidator.cs` | FluentValidation |
| `backend/BusBooking.Api/Validators/UpdateRouteRequestValidator.cs` | FluentValidation |
| `backend/BusBooking.Api/Validators/UpdatePlatformFeeRequestValidator.cs` | FluentValidation |
| `backend/BusBooking.Api/Services/ICityService.cs` | City contract (search, list, get, create, update) |
| `backend/BusBooking.Api/Services/CityService.cs` | City implementation |
| `backend/BusBooking.Api/Services/IRouteService.cs` | Route contract |
| `backend/BusBooking.Api/Services/RouteService.cs` | Route implementation |
| `backend/BusBooking.Api/Services/IPlatformFeeService.cs` | Platform-fee contract (`GetActiveAsync`, `UpdateAsync`) |
| `backend/BusBooking.Api/Services/PlatformFeeService.cs` | Platform-fee implementation |
| `backend/BusBooking.Api/Controllers/CitiesController.cs` | Public `GET /cities?q=` |
| `backend/BusBooking.Api/Controllers/AdminCitiesController.cs` | Admin `/admin/cities` GET/POST/PATCH |
| `backend/BusBooking.Api/Controllers/AdminRoutesController.cs` | Admin `/admin/routes` GET/POST/PATCH |
| `backend/BusBooking.Api/Controllers/AdminPlatformFeeController.cs` | Admin `/admin/platform-fee` GET/PUT |
| `backend/BusBooking.Api/Migrations/<ts>_AddCitiesRoutesAndPlatformFee.cs` | EF migration |

### Modified backend files

- `backend/BusBooking.Api/Infrastructure/AppDbContext.cs` — add `DbSet<City>`, `DbSet<Route>`, `DbSet<PlatformFeeConfig>` plus their mappings. Add the `pg_trgm` GIN index on `cities.name` via raw SQL in the migration (EF Core cannot express `gin_trgm_ops` cleanly through the model builder).
- `backend/BusBooking.Api/Program.cs` — DI registrations for the three new services.

### New test files

| Path | Responsibility |
|---|---|
| `backend/BusBooking.Api.Tests/Support/IntegrationFixture.cs` | Created in M1. M2 extends its `ResetAsync` to also truncate the three new tables. **If the file was not created in M1, Task 4 below creates it.** |
| `backend/BusBooking.Api.Tests/Support/AdminTokenFactory.cs` | Helper: seeds an admin user and returns a valid JWT. Created here; reused by M3+. |
| `backend/BusBooking.Api.Tests/Unit/PlatformFeeServiceTests.cs` | Unit: history lookup picks most recent `EffectiveFrom <= now()` |
| `backend/BusBooking.Api.Tests/Integration/CitiesSearchTests.cs` | Public `GET /cities?q=` — fuzzy match + `q` too short + case-insensitive |
| `backend/BusBooking.Api.Tests/Integration/AdminCitiesTests.cs` | Admin CRUD + role gate + duplicate name + deactivate |
| `backend/BusBooking.Api.Tests/Integration/AdminRoutesTests.cs` | Admin CRUD + UNIQUE (source, destination) + unknown city id + role gate |
| `backend/BusBooking.Api.Tests/Integration/AdminPlatformFeeTests.cs` | GET active when none exists (seeded default) + PUT inserts new row + next GET returns the new row |

### New frontend files

| Path | Responsibility |
|---|---|
| `frontend/bus-booking-web/src/app/core/api/cities.api.ts` | `CitiesApiService.search(q)` |
| `frontend/bus-booking-web/src/app/core/api/admin-cities.api.ts` | `AdminCitiesApiService.list / get / create / update` |
| `frontend/bus-booking-web/src/app/core/api/admin-routes.api.ts` | `AdminRoutesApiService.list / get / create / update` |
| `frontend/bus-booking-web/src/app/core/api/admin-platform-fee.api.ts` | `AdminPlatformFeeApiService.get / update` |
| `frontend/bus-booking-web/src/app/shared/components/city-autocomplete/city-autocomplete.component.ts` | Reusable Material autocomplete |
| `frontend/bus-booking-web/src/app/shared/components/city-autocomplete/city-autocomplete.component.html` | Template |
| `frontend/bus-booking-web/src/app/shared/components/city-autocomplete/city-autocomplete.component.scss` | Styles (mostly empty; Tailwind in template) |
| `frontend/bus-booking-web/src/app/shared/components/city-autocomplete/city-autocomplete.component.spec.ts` | Unit test |
| `frontend/bus-booking-web/src/app/features/admin/cities/admin-cities-page.component.{ts,html,scss}` | List + inline create/edit dialog |
| `frontend/bus-booking-web/src/app/features/admin/routes/admin-routes-page.component.{ts,html,scss}` | List + inline create/edit dialog |
| `frontend/bus-booking-web/src/app/features/admin/platform-fee/admin-platform-fee-page.component.{ts,html,scss}` | Read active; PUT new row |

### Modified frontend files

- `frontend/bus-booking-web/src/app/app.routes.ts` — add three admin routes gated by `roleGuard(['admin'])`
- `frontend/bus-booking-web/src/app/features/admin/admin-dashboard/admin-dashboard.component.{ts,html}` — replace the M1 stub with three `mat-card` tiles linking to the new admin pages
- `frontend/bus-booking-web/src/app/features/public/home/home.component.{ts,html}` — add two `CityAutocompleteComponent` inputs (source, destination) above the existing content

---

## Prerequisites

Run once before starting (user action — credentials live in the developer's shell):

```bash
# Both extensions already exist if M1 migration ran; re-running is idempotent.
psql -d bus_booking      -c "CREATE EXTENSION IF NOT EXISTS citext;"
psql -d bus_booking      -c "CREATE EXTENSION IF NOT EXISTS pg_trgm;"
psql -d bus_booking_test -c "CREATE EXTENSION IF NOT EXISTS citext;"
psql -d bus_booking_test -c "CREATE EXTENSION IF NOT EXISTS pg_trgm;"
```

`bus_booking_test` should already exist from M1 Task 4. If not, the `IntegrationFixture` will try to `createdb` it; if that also fails, run `createdb bus_booking_test` once.

Every task below that runs `dotnet` commands does so from `backend/BusBooking.Api` (or `backend/BusBooking.Api.Tests` for test runs). Every task that runs `ng` / `npm` commands does so from `frontend/bus-booking-web`.

---

## Task 1: City, Route, PlatformFeeConfig entities + DbContext mappings

**Files:**
- Create: `backend/BusBooking.Api/Models/City.cs`
- Create: `backend/BusBooking.Api/Models/Route.cs`
- Create: `backend/BusBooking.Api/Models/PlatformFeeConfig.cs`
- Create: `backend/BusBooking.Api/Models/PlatformFeeType.cs`
- Modify: `backend/BusBooking.Api/Infrastructure/AppDbContext.cs`

- [ ] **Step 1: Create `City.cs`**

```csharp
namespace BusBooking.Api.Models;

public class City
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string State { get; set; }
    public bool IsActive { get; set; } = true;
}
```

- [ ] **Step 2: Create `Route.cs`**

```csharp
namespace BusBooking.Api.Models;

public class Route
{
    public Guid Id { get; set; }
    public Guid SourceCityId { get; set; }
    public Guid DestinationCityId { get; set; }
    public int? DistanceKm { get; set; }
    public bool IsActive { get; set; } = true;

    public City? SourceCity { get; set; }
    public City? DestinationCity { get; set; }
}
```

Note: the C# type is called `Route` even though `Microsoft.AspNetCore.Routing.Route` exists. We live in the `BusBooking.Api.Models` namespace, and controllers/services import explicitly, so there is no collision. If any file ever pulls in `Microsoft.AspNetCore.Routing` directly, disambiguate with `using RouteModel = BusBooking.Api.Models.Route;`.

- [ ] **Step 3: Create `PlatformFeeType.cs`**

```csharp
namespace BusBooking.Api.Models;

public static class PlatformFeeType
{
    public const string Fixed = "fixed";
    public const string Percent = "percent";

    public static readonly string[] All = [Fixed, Percent];
}
```

- [ ] **Step 4: Create `PlatformFeeConfig.cs`**

```csharp
namespace BusBooking.Api.Models;

public class PlatformFeeConfig
{
    public Guid Id { get; set; }
    public required string FeeType { get; set; } // PlatformFeeType.Fixed | PlatformFeeType.Percent
    public decimal Value { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public Guid CreatedByAdminId { get; set; }
}
```

- [ ] **Step 5: Extend `AppDbContext.cs`**

Open `backend/BusBooking.Api/Infrastructure/AppDbContext.cs` and apply the following edits. The existing `OnModelCreating` keeps its current body; you are adding three new `DbSet` properties and three new entity blocks inside `OnModelCreating`.

Add the new `using` line at the top (directly after the existing `using BusBooking.Api.Models;`):

No new using is needed — `City`, `Route`, `PlatformFeeConfig` all live in `BusBooking.Api.Models`, which is already imported.

After the `public DbSet<UserRole> UserRoles => Set<UserRole>();` line, add:

```csharp
    public DbSet<City> Cities => Set<City>();
    public DbSet<Route> Routes => Set<Route>();
    public DbSet<PlatformFeeConfig> PlatformFeeConfigs => Set<PlatformFeeConfig>();
```

Inside `OnModelCreating`, after the existing `UserRole` block and before the closing `}`, add:

```csharp
        modelBuilder.Entity<City>(b =>
        {
            b.ToTable("cities");
            b.HasKey(c => c.Id);
            b.Property(c => c.Id).HasColumnName("id");
            b.Property(c => c.Name).HasColumnName("name").HasColumnType("citext").IsRequired().HasMaxLength(120);
            b.Property(c => c.State).HasColumnName("state").IsRequired().HasMaxLength(120);
            b.Property(c => c.IsActive).HasColumnName("is_active");
            b.HasIndex(c => c.Name).IsUnique();
            // The gin_trgm_ops GIN index is added by raw SQL in the migration (see Task 2).
        });

        modelBuilder.Entity<Route>(b =>
        {
            b.ToTable("routes");
            b.HasKey(r => r.Id);
            b.Property(r => r.Id).HasColumnName("id");
            b.Property(r => r.SourceCityId).HasColumnName("source_city_id");
            b.Property(r => r.DestinationCityId).HasColumnName("destination_city_id");
            b.Property(r => r.DistanceKm).HasColumnName("distance_km");
            b.Property(r => r.IsActive).HasColumnName("is_active");
            b.HasIndex(r => new { r.SourceCityId, r.DestinationCityId }).IsUnique();
            b.HasOne(r => r.SourceCity)
                .WithMany()
                .HasForeignKey(r => r.SourceCityId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(r => r.DestinationCity)
                .WithMany()
                .HasForeignKey(r => r.DestinationCityId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PlatformFeeConfig>(b =>
        {
            b.ToTable("platform_fee_config");
            b.HasKey(p => p.Id);
            b.Property(p => p.Id).HasColumnName("id");
            b.Property(p => p.FeeType).HasColumnName("fee_type").IsRequired().HasMaxLength(16);
            b.Property(p => p.Value).HasColumnName("value").HasColumnType("decimal(10,2)");
            b.Property(p => p.EffectiveFrom).HasColumnName("effective_from");
            b.Property(p => p.CreatedByAdminId).HasColumnName("created_by_admin_id");
            b.HasIndex(p => p.EffectiveFrom);
        });
```

- [ ] **Step 6: Build**

```bash
cd backend/BusBooking.Api && dotnet build
```

Expected: `Build succeeded.` No warnings about ambiguous `Route`.

- [ ] **Step 7: Commit**

```bash
git add backend/BusBooking.Api/Models/City.cs backend/BusBooking.Api/Models/Route.cs \
        backend/BusBooking.Api/Models/PlatformFeeConfig.cs backend/BusBooking.Api/Models/PlatformFeeType.cs \
        backend/BusBooking.Api/Infrastructure/AppDbContext.cs
git commit -m "feat(backend): add City, Route, PlatformFeeConfig entities"
```

---

## Task 2: EF migration for cities, routes, platform_fee_config

**Files:**
- Create: `backend/BusBooking.Api/Migrations/<timestamp>_AddCitiesRoutesAndPlatformFee.cs` (generated)
- Create: `backend/BusBooking.Api/Migrations/<timestamp>_AddCitiesRoutesAndPlatformFee.Designer.cs` (generated)
- Modify: `backend/BusBooking.Api/Migrations/AppDbContextModelSnapshot.cs` (generated)

- [ ] **Step 1: Generate the migration**

```bash
cd backend/BusBooking.Api && dotnet ef migrations add AddCitiesRoutesAndPlatformFee
```

Expected: creates two new files in `Migrations/` and updates `AppDbContextModelSnapshot.cs`.

- [ ] **Step 2: Inspect the generated `Up()`**

Open the new `<timestamp>_AddCitiesRoutesAndPlatformFee.cs`. Verify `Up(MigrationBuilder migrationBuilder)` creates:

- Table `cities` with columns `id` (uuid pk), `name` (citext, unique), `state`, `is_active` — and a **unique** index on `name`.
- Table `routes` with `id`, `source_city_id`, `destination_city_id`, `distance_km` (nullable), `is_active`, plus FKs to `cities.id` and a **unique** composite index on `(source_city_id, destination_city_id)`.
- Table `platform_fee_config` with `id`, `fee_type`, `value` (decimal(10,2)), `effective_from`, `created_by_admin_id`, plus an index on `effective_from`.

If any column name is PascalCase instead of snake_case, stop and fix the `HasColumnName` bindings in `AppDbContext.cs`, then regenerate (`dotnet ef migrations remove` → `dotnet ef migrations add AddCitiesRoutesAndPlatformFee`).

- [ ] **Step 3: Append the `pg_trgm` GIN index via raw SQL**

EF Core's Npgsql provider cannot describe a GIN index with `gin_trgm_ops` through the model builder in a way that survives all provider versions. We add and drop it explicitly in the migration. At the **end** of the generated `Up()` method (inside the `Up` body but after the last `migrationBuilder.CreateIndex(...)` call), append:

```csharp
            migrationBuilder.Sql(
                "CREATE INDEX ix_cities_name_trgm ON cities USING gin (name gin_trgm_ops);");
```

At the **beginning** of the generated `Down()` method, prepend:

```csharp
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_cities_name_trgm;");
```

- [ ] **Step 4: Apply to dev DB**

```bash
cd backend/BusBooking.Api && dotnet ef database update
```

Expected: migration applied. No error.

- [ ] **Step 5: Verify the tables and the GIN index exist**

```bash
psql -d bus_booking -c "\d cities"
psql -d bus_booking -c "\d routes"
psql -d bus_booking -c "\d platform_fee_config"
psql -d bus_booking -c "\di ix_cities_name_trgm"
```

Expected: all three tables appear with the expected columns; `ix_cities_name_trgm` appears as a `gin` index on table `cities`.

- [ ] **Step 6: Commit**

```bash
git add backend/BusBooking.Api/Migrations/
git commit -m "feat(backend): add EF migration for cities, routes, platform_fee_config"
```

---

## Task 3: Default platform fee seeder

**Why:** M5 (booking) snapshots `platform_fee` into every booking. If no row exists, that lookup returns null and the booking service cannot compute totals. Seed a sensible default (`fixed`, `25.00` INR) on first boot. Admins can override via the PUT endpoint in Task 8.

**Files:**
- Create: `backend/BusBooking.Api/Infrastructure/Seeding/IPlatformFeeSeeder.cs`
- Create: `backend/BusBooking.Api/Infrastructure/Seeding/PlatformFeeSeeder.cs`
- Modify: `backend/BusBooking.Api/Program.cs`

- [ ] **Step 1: Create the contract**

```csharp
namespace BusBooking.Api.Infrastructure.Seeding;

public interface IPlatformFeeSeeder
{
    Task SeedAsync(CancellationToken ct);
}
```

- [ ] **Step 2: Create the implementation**

```csharp
using BusBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Api.Infrastructure.Seeding;

public class PlatformFeeSeeder : IPlatformFeeSeeder
{
    private readonly AppDbContext _db;
    private readonly ILogger<PlatformFeeSeeder> _log;

    public PlatformFeeSeeder(AppDbContext db, ILogger<PlatformFeeSeeder> log)
    {
        _db = db;
        _log = log;
    }

    public async Task SeedAsync(CancellationToken ct)
    {
        if (await _db.PlatformFeeConfigs.AnyAsync(ct))
        {
            _log.LogInformation("Platform fee config already present; skipping seed");
            return;
        }

        _db.PlatformFeeConfigs.Add(new PlatformFeeConfig
        {
            Id = Guid.NewGuid(),
            FeeType = PlatformFeeType.Fixed,
            Value = 25.00m,
            EffectiveFrom = DateTime.UtcNow,
            CreatedByAdminId = Guid.Empty // seeded by the system, not by a specific admin
        });
        await _db.SaveChangesAsync(ct);
        _log.LogInformation("Seeded default platform fee: fixed ₹25.00");
    }
}
```

- [ ] **Step 3: Register and call it from `Program.cs`**

In `Program.cs`, add the service registration next to the existing `AdminSeeder` registration:

```csharp
builder.Services.AddScoped<IPlatformFeeSeeder, PlatformFeeSeeder>();
```

Inside the existing startup scope (the `using (var scope = app.Services.CreateScope()) { ... }` block), after the admin seeder line, add:

```csharp
    var feeSeeder = scope.ServiceProvider.GetRequiredService<IPlatformFeeSeeder>();
    await feeSeeder.SeedAsync(CancellationToken.None);
```

- [ ] **Step 4: Build + run once to prove it seeds**

```bash
cd backend/BusBooking.Api && dotnet build && dotnet run --no-build
```

Expected: console prints `Seeded default platform fee: fixed ₹25.00`. Ctrl+C the server.

Run the server once more:

```bash
cd backend/BusBooking.Api && dotnet run --no-build
```

Expected: this time it prints `Platform fee config already present; skipping seed`.

- [ ] **Step 5: Commit**

```bash
git add backend/BusBooking.Api/Infrastructure/Seeding/IPlatformFeeSeeder.cs \
        backend/BusBooking.Api/Infrastructure/Seeding/PlatformFeeSeeder.cs \
        backend/BusBooking.Api/Program.cs
git commit -m "feat(backend): seed default platform fee on startup"
```

---

## Task 4: Admin JWT test helper

**Why:** Every integration test in this milestone hits an `[Authorize(Roles="admin")]` endpoint. DRY that setup into one helper.

**Files:**
- Create: `backend/BusBooking.Api.Tests/Support/AdminTokenFactory.cs`

> **If `backend/BusBooking.Api.Tests/Support/IntegrationFixture.cs` does not yet exist in your tree, stop and complete M1 Task 4 first.** The fixture exposes an `AppDbContext` and an `HttpClient`; this helper depends on both.

- [ ] **Step 1: Create the helper**

```csharp
using System.Net.Http.Headers;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Infrastructure.Auth;
using BusBooking.Api.Models;
using Microsoft.Extensions.DependencyInjection;

namespace BusBooking.Api.Tests.Support;

public static class AdminTokenFactory
{
    public static async Task<(User user, string token)> CreateAdminAsync(
        IntegrationFixture fx,
        string email = "admin-test@busbooking.local",
        string name = "Admin Test",
        CancellationToken ct = default)
    {
        using var scope = fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var tokens = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        var admin = new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            PasswordHash = hasher.Hash("x-not-used"),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        admin.Roles.Add(new UserRole { UserId = admin.Id, Role = Roles.Admin });
        db.Users.Add(admin);
        await db.SaveChangesAsync(ct);

        var token = tokens.Generate(admin, [Roles.Admin]);
        return (admin, token.Token);
    }

    public static void AttachAdminBearer(this HttpClient client, string token)
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
git add backend/BusBooking.Api.Tests/Support/AdminTokenFactory.cs
git commit -m "test(backend): add AdminTokenFactory helper"
```

---

## Task 5: CityService + admin CRUD + public fuzzy search

**Files:**
- Create: `backend/BusBooking.Api/Dtos/CityDto.cs`
- Create: `backend/BusBooking.Api/Dtos/CreateCityRequest.cs`
- Create: `backend/BusBooking.Api/Dtos/UpdateCityRequest.cs`
- Create: `backend/BusBooking.Api/Validators/CreateCityRequestValidator.cs`
- Create: `backend/BusBooking.Api/Validators/UpdateCityRequestValidator.cs`
- Create: `backend/BusBooking.Api/Services/ICityService.cs`
- Create: `backend/BusBooking.Api/Services/CityService.cs`
- Create: `backend/BusBooking.Api/Controllers/CitiesController.cs`
- Create: `backend/BusBooking.Api/Controllers/AdminCitiesController.cs`
- Modify: `backend/BusBooking.Api/Program.cs`

- [ ] **Step 1: DTOs**

`Dtos/CityDto.cs`:

```csharp
namespace BusBooking.Api.Dtos;

public record CityDto(Guid Id, string Name, string State, bool IsActive);
```

`Dtos/CreateCityRequest.cs`:

```csharp
namespace BusBooking.Api.Dtos;

public class CreateCityRequest
{
    public required string Name { get; set; }
    public required string State { get; set; }
}
```

`Dtos/UpdateCityRequest.cs`:

```csharp
namespace BusBooking.Api.Dtos;

public class UpdateCityRequest
{
    public string? Name { get; set; }
    public string? State { get; set; }
    public bool? IsActive { get; set; }
}
```

- [ ] **Step 2: Validators**

`Validators/CreateCityRequestValidator.cs`:

```csharp
using BusBooking.Api.Dtos;
using FluentValidation;

namespace BusBooking.Api.Validators;

public class CreateCityRequestValidator : AbstractValidator<CreateCityRequest>
{
    public CreateCityRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.State).NotEmpty().MaximumLength(120);
    }
}
```

`Validators/UpdateCityRequestValidator.cs`:

```csharp
using BusBooking.Api.Dtos;
using FluentValidation;

namespace BusBooking.Api.Validators;

public class UpdateCityRequestValidator : AbstractValidator<UpdateCityRequest>
{
    public UpdateCityRequestValidator()
    {
        RuleFor(x => x.Name!).NotEmpty().MaximumLength(120).When(x => x.Name is not null);
        RuleFor(x => x.State!).NotEmpty().MaximumLength(120).When(x => x.State is not null);
    }
}
```

- [ ] **Step 3: Service contract**

`Services/ICityService.cs`:

```csharp
using BusBooking.Api.Dtos;

namespace BusBooking.Api.Services;

public interface ICityService
{
    Task<IReadOnlyList<CityDto>> SearchActiveAsync(string query, int limit, CancellationToken ct);
    Task<IReadOnlyList<CityDto>> ListAllAsync(CancellationToken ct);
    Task<CityDto> GetAsync(Guid id, CancellationToken ct);
    Task<CityDto> CreateAsync(CreateCityRequest request, CancellationToken ct);
    Task<CityDto> UpdateAsync(Guid id, UpdateCityRequest request, CancellationToken ct);
}
```

- [ ] **Step 4: Service implementation**

`Services/CityService.cs`:

```csharp
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Infrastructure.Errors;
using BusBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Api.Services;

public class CityService : ICityService
{
    private const int MinQueryLength = 2;
    private const int MaxSearchResults = 20;

    private readonly AppDbContext _db;

    public CityService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CityDto>> SearchActiveAsync(string query, int limit, CancellationToken ct)
    {
        var q = (query ?? string.Empty).Trim();
        if (q.Length < MinQueryLength)
            return Array.Empty<CityDto>();

        var take = Math.Clamp(limit <= 0 ? 10 : limit, 1, MaxSearchResults);
        var pattern = $"%{q}%";

        var results = await _db.Cities
            .AsNoTracking()
            .Where(c => c.IsActive && EF.Functions.ILike(c.Name, pattern))
            .OrderBy(c => c.Name)
            .Take(take)
            .Select(c => new CityDto(c.Id, c.Name, c.State, c.IsActive))
            .ToListAsync(ct);

        return results;
    }

    public async Task<IReadOnlyList<CityDto>> ListAllAsync(CancellationToken ct)
    {
        return await _db.Cities
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CityDto(c.Id, c.Name, c.State, c.IsActive))
            .ToListAsync(ct);
    }

    public async Task<CityDto> GetAsync(Guid id, CancellationToken ct)
    {
        var c = await _db.Cities.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("City not found");
        return new CityDto(c.Id, c.Name, c.State, c.IsActive);
    }

    public async Task<CityDto> CreateAsync(CreateCityRequest request, CancellationToken ct)
    {
        var name = request.Name.Trim();
        var state = request.State.Trim();

        if (await _db.Cities.AnyAsync(c => c.Name == name, ct))
            throw new ConflictException("CITY_NAME_TAKEN", "A city with that name already exists");

        var city = new City
        {
            Id = Guid.NewGuid(),
            Name = name,
            State = state,
            IsActive = true
        };
        _db.Cities.Add(city);
        await _db.SaveChangesAsync(ct);
        return new CityDto(city.Id, city.Name, city.State, city.IsActive);
    }

    public async Task<CityDto> UpdateAsync(Guid id, UpdateCityRequest request, CancellationToken ct)
    {
        var city = await _db.Cities.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException("City not found");

        if (request.Name is not null)
        {
            var newName = request.Name.Trim();
            if (!string.Equals(newName, city.Name, StringComparison.OrdinalIgnoreCase)
                && await _db.Cities.AnyAsync(c => c.Name == newName, ct))
            {
                throw new ConflictException("CITY_NAME_TAKEN", "A city with that name already exists");
            }
            city.Name = newName;
        }
        if (request.State is not null) city.State = request.State.Trim();
        if (request.IsActive is not null) city.IsActive = request.IsActive.Value;

        await _db.SaveChangesAsync(ct);
        return new CityDto(city.Id, city.Name, city.State, city.IsActive);
    }
}
```

Why `ILike` instead of raw `similarity(...)`: the `pg_trgm` GIN index accelerates both `ILIKE` and `%` similarity operators on a `citext` column. `ILIKE` covers the "I typed `ban` and I want `Bangalore`" case with zero extra plumbing, uses the index, and returns deterministically ordered results.

- [ ] **Step 5: Public controller**

`Controllers/CitiesController.cs`:

```csharp
using BusBooking.Api.Dtos;
using BusBooking.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.Api.Controllers;

[ApiController]
[Route("api/v1/cities")]
[AllowAnonymous]
public class CitiesController : ControllerBase
{
    private readonly ICityService _cities;

    public CitiesController(ICityService cities)
    {
        _cities = cities;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CityDto>>> Search(
        [FromQuery] string q = "",
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
    {
        var results = await _cities.SearchActiveAsync(q, limit, ct);
        return Ok(results);
    }
}
```

- [ ] **Step 6: Admin controller**

`Controllers/AdminCitiesController.cs`:

```csharp
using BusBooking.Api.Dtos;
using BusBooking.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.Api.Controllers;

[ApiController]
[Route("api/v1/admin/cities")]
[Authorize(Roles = "admin")]
public class AdminCitiesController : ControllerBase
{
    private readonly ICityService _cities;

    public AdminCitiesController(ICityService cities)
    {
        _cities = cities;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CityDto>>> List(CancellationToken ct)
        => Ok(await _cities.ListAllAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CityDto>> Get(Guid id, CancellationToken ct)
        => Ok(await _cities.GetAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<CityDto>> Create(
        [FromBody] CreateCityRequest body,
        [FromServices] IValidator<CreateCityRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(body, ct);
        var city = await _cities.CreateAsync(body, ct);
        return StatusCode(StatusCodes.Status201Created, city);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<CityDto>> Update(
        Guid id,
        [FromBody] UpdateCityRequest body,
        [FromServices] IValidator<UpdateCityRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(body, ct);
        var city = await _cities.UpdateAsync(id, body, ct);
        return Ok(city);
    }
}
```

- [ ] **Step 7: DI registration**

In `Program.cs`, near the other `AddScoped` service registrations, add:

```csharp
builder.Services.AddScoped<ICityService, CityService>();
```

- [ ] **Step 8: Build**

```bash
cd backend/BusBooking.Api && dotnet build
```

Expected: `Build succeeded.`

- [ ] **Step 9: Commit**

```bash
git add backend/BusBooking.Api/Dtos/CityDto.cs backend/BusBooking.Api/Dtos/CreateCityRequest.cs \
        backend/BusBooking.Api/Dtos/UpdateCityRequest.cs \
        backend/BusBooking.Api/Validators/CreateCityRequestValidator.cs \
        backend/BusBooking.Api/Validators/UpdateCityRequestValidator.cs \
        backend/BusBooking.Api/Services/ICityService.cs backend/BusBooking.Api/Services/CityService.cs \
        backend/BusBooking.Api/Controllers/CitiesController.cs \
        backend/BusBooking.Api/Controllers/AdminCitiesController.cs \
        backend/BusBooking.Api/Program.cs
git commit -m "feat(backend): admin CRUD and public fuzzy search for cities"
```

---

## Task 6: Integration tests for cities

**Files:**
- Create: `backend/BusBooking.Api.Tests/Integration/CitiesSearchTests.cs`
- Create: `backend/BusBooking.Api.Tests/Integration/AdminCitiesTests.cs`

- [ ] **Step 1: Public fuzzy-search test**

`Integration/CitiesSearchTests.cs`:

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

[Collection("Integration")]
public class CitiesSearchTests : IAsyncLifetime
{
    private readonly IntegrationFixture _fx;

    public CitiesSearchTests(IntegrationFixture fx)
    {
        _fx = fx;
    }

    public async Task InitializeAsync()
    {
        await _fx.ResetAsync();
        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Cities.AddRange(
            new City { Id = Guid.NewGuid(), Name = "Bangalore", State = "Karnataka", IsActive = true },
            new City { Id = Guid.NewGuid(), Name = "Bengaluru",  State = "Karnataka", IsActive = true },
            new City { Id = Guid.NewGuid(), Name = "Chennai",    State = "Tamil Nadu", IsActive = true },
            new City { Id = Guid.NewGuid(), Name = "Mumbai",     State = "Maharashtra", IsActive = false });
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Returns_empty_when_query_shorter_than_two_chars()
    {
        var resp = await _fx.Client.GetFromJsonAsync<List<CityDto>>("/api/v1/cities?q=b");
        resp.Should().NotBeNull();
        resp!.Should().BeEmpty();
    }

    [Fact]
    public async Task Fuzzy_matches_prefix_case_insensitively()
    {
        var resp = await _fx.Client.GetFromJsonAsync<List<CityDto>>("/api/v1/cities?q=ban");
        resp!.Should().ContainSingle(c => c.Name == "Bangalore");
    }

    [Fact]
    public async Task Excludes_inactive_cities()
    {
        var resp = await _fx.Client.GetFromJsonAsync<List<CityDto>>("/api/v1/cities?q=mum");
        resp!.Should().BeEmpty();
    }

    [Fact]
    public async Task Respects_limit_parameter()
    {
        var resp = await _fx.Client.GetAsync("/api/v1/cities?q=a&limit=1");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<List<CityDto>>();
        body!.Count.Should().BeLessOrEqualTo(1);
    }
}
```

- [ ] **Step 2: Admin CRUD test**

`Integration/AdminCitiesTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using BusBooking.Api.Dtos;
using BusBooking.Api.Tests.Support;
using FluentAssertions;

namespace BusBooking.Api.Tests.Integration;

[Collection("Integration")]
public class AdminCitiesTests : IAsyncLifetime
{
    private readonly IntegrationFixture _fx;

    public AdminCitiesTests(IntegrationFixture fx)
    {
        _fx = fx;
    }

    public async Task InitializeAsync() => await _fx.ResetAsync();

    public Task DisposeAsync()
    {
        _fx.Client.DefaultRequestHeaders.Authorization = null;
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Anonymous_list_returns_401()
    {
        var resp = await _fx.Client.GetAsync("/api/v1/admin/cities");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Admin_can_create_update_and_deactivate()
    {
        var (_, token) = await AdminTokenFactory.CreateAdminAsync(_fx);
        _fx.Client.AttachAdminBearer(token);

        var created = await _fx.Client.PostAsJsonAsync("/api/v1/admin/cities",
            new { name = "Hyderabad", state = "Telangana" });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdBody = await created.Content.ReadFromJsonAsync<CityDto>();
        createdBody!.Name.Should().Be("Hyderabad");
        createdBody.IsActive.Should().BeTrue();

        var patched = await _fx.Client.PatchAsJsonAsync(
            $"/api/v1/admin/cities/{createdBody.Id}",
            new { isActive = false });
        patched.StatusCode.Should().Be(HttpStatusCode.OK);
        var patchedBody = await patched.Content.ReadFromJsonAsync<CityDto>();
        patchedBody!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Duplicate_name_returns_409()
    {
        var (_, token) = await AdminTokenFactory.CreateAdminAsync(_fx);
        _fx.Client.AttachAdminBearer(token);

        (await _fx.Client.PostAsJsonAsync("/api/v1/admin/cities",
            new { name = "Pune", state = "Maharashtra" })).EnsureSuccessStatusCode();

        var dup = await _fx.Client.PostAsJsonAsync("/api/v1/admin/cities",
            new { name = "pune", state = "Maharashtra" });
        dup.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Customer_role_receives_403()
    {
        var (_, token) = await AdminTokenFactory.CreateAdminAsync(_fx);

        // Create a customer with the existing /auth/register flow
        var registered = await _fx.Client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            name = "Alice",
            email = $"alice-{Guid.NewGuid():N}@example.com",
            password = "Abcdef1!",
            phone = (string?)null
        });
        registered.EnsureSuccessStatusCode();

        var login = await _fx.Client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = (await registered.Content.ReadFromJsonAsync<UserDto>())!.Email,
            password = "Abcdef1!"
        });
        login.EnsureSuccessStatusCode();
        var loginBody = await login.Content.ReadFromJsonAsync<LoginResponse>();

        _fx.Client.AttachAdminBearer(loginBody!.Token);
        var forbidden = await _fx.Client.GetAsync("/api/v1/admin/cities");
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
```

- [ ] **Step 3: Run the new tests**

```bash
cd backend/BusBooking.Api.Tests && dotnet test --filter "FullyQualifiedName~Cities"
```

Expected: `Passed! - 8` (4 in `CitiesSearchTests` + 4 in `AdminCitiesTests`). If any fail, diagnose and fix before moving on.

- [ ] **Step 4: Commit**

```bash
git add backend/BusBooking.Api.Tests/Integration/CitiesSearchTests.cs \
        backend/BusBooking.Api.Tests/Integration/AdminCitiesTests.cs
git commit -m "test(backend): integration tests for cities search and admin CRUD"
```

---

## Task 7: RouteService + admin CRUD + tests

**Files:**
- Create: `backend/BusBooking.Api/Dtos/RouteDto.cs`
- Create: `backend/BusBooking.Api/Dtos/CreateRouteRequest.cs`
- Create: `backend/BusBooking.Api/Dtos/UpdateRouteRequest.cs`
- Create: `backend/BusBooking.Api/Validators/CreateRouteRequestValidator.cs`
- Create: `backend/BusBooking.Api/Validators/UpdateRouteRequestValidator.cs`
- Create: `backend/BusBooking.Api/Services/IRouteService.cs`
- Create: `backend/BusBooking.Api/Services/RouteService.cs`
- Create: `backend/BusBooking.Api/Controllers/AdminRoutesController.cs`
- Create: `backend/BusBooking.Api.Tests/Integration/AdminRoutesTests.cs`
- Modify: `backend/BusBooking.Api/Program.cs`

- [ ] **Step 1: DTOs**

`Dtos/RouteDto.cs`:

```csharp
namespace BusBooking.Api.Dtos;

public record RouteDto(
    Guid Id,
    CityDto Source,
    CityDto Destination,
    int? DistanceKm,
    bool IsActive);
```

`Dtos/CreateRouteRequest.cs`:

```csharp
namespace BusBooking.Api.Dtos;

public class CreateRouteRequest
{
    public Guid SourceCityId { get; set; }
    public Guid DestinationCityId { get; set; }
    public int? DistanceKm { get; set; }
}
```

`Dtos/UpdateRouteRequest.cs`:

```csharp
namespace BusBooking.Api.Dtos;

public class UpdateRouteRequest
{
    public int? DistanceKm { get; set; }
    public bool? IsActive { get; set; }
}
```

- [ ] **Step 2: Validators**

`Validators/CreateRouteRequestValidator.cs`:

```csharp
using BusBooking.Api.Dtos;
using FluentValidation;

namespace BusBooking.Api.Validators;

public class CreateRouteRequestValidator : AbstractValidator<CreateRouteRequest>
{
    public CreateRouteRequestValidator()
    {
        RuleFor(x => x.SourceCityId).NotEmpty();
        RuleFor(x => x.DestinationCityId).NotEmpty()
            .NotEqual(x => x.SourceCityId)
            .WithMessage("Destination city must differ from source city");
        RuleFor(x => x.DistanceKm!.Value).GreaterThan(0).LessThanOrEqualTo(5000)
            .When(x => x.DistanceKm is not null);
    }
}
```

`Validators/UpdateRouteRequestValidator.cs`:

```csharp
using BusBooking.Api.Dtos;
using FluentValidation;

namespace BusBooking.Api.Validators;

public class UpdateRouteRequestValidator : AbstractValidator<UpdateRouteRequest>
{
    public UpdateRouteRequestValidator()
    {
        RuleFor(x => x.DistanceKm!.Value).GreaterThan(0).LessThanOrEqualTo(5000)
            .When(x => x.DistanceKm is not null);
    }
}
```

- [ ] **Step 3: Service contract**

`Services/IRouteService.cs`:

```csharp
using BusBooking.Api.Dtos;

namespace BusBooking.Api.Services;

public interface IRouteService
{
    Task<IReadOnlyList<RouteDto>> ListAllAsync(CancellationToken ct);
    Task<RouteDto> GetAsync(Guid id, CancellationToken ct);
    Task<RouteDto> CreateAsync(CreateRouteRequest request, CancellationToken ct);
    Task<RouteDto> UpdateAsync(Guid id, UpdateRouteRequest request, CancellationToken ct);
}
```

- [ ] **Step 4: Service implementation**

`Services/RouteService.cs`:

```csharp
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Infrastructure.Errors;
using BusBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Api.Services;

public class RouteService : IRouteService
{
    private readonly AppDbContext _db;

    public RouteService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<RouteDto>> ListAllAsync(CancellationToken ct)
    {
        return await _db.Routes
            .AsNoTracking()
            .Include(r => r.SourceCity)
            .Include(r => r.DestinationCity)
            .OrderBy(r => r.SourceCity!.Name).ThenBy(r => r.DestinationCity!.Name)
            .Select(r => ToDto(r))
            .ToListAsync(ct);
    }

    public async Task<RouteDto> GetAsync(Guid id, CancellationToken ct)
    {
        var r = await _db.Routes.AsNoTracking()
            .Include(x => x.SourceCity).Include(x => x.DestinationCity)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Route not found");
        return ToDto(r);
    }

    public async Task<RouteDto> CreateAsync(CreateRouteRequest request, CancellationToken ct)
    {
        var source = await _db.Cities.FindAsync([request.SourceCityId], ct)
            ?? throw new NotFoundException("Source city not found");
        var destination = await _db.Cities.FindAsync([request.DestinationCityId], ct)
            ?? throw new NotFoundException("Destination city not found");

        var duplicate = await _db.Routes.AnyAsync(
            r => r.SourceCityId == request.SourceCityId
              && r.DestinationCityId == request.DestinationCityId, ct);
        if (duplicate)
            throw new ConflictException("ROUTE_EXISTS", "A route between these cities already exists");

        var route = new Route
        {
            Id = Guid.NewGuid(),
            SourceCityId = request.SourceCityId,
            DestinationCityId = request.DestinationCityId,
            DistanceKm = request.DistanceKm,
            IsActive = true,
            SourceCity = source,
            DestinationCity = destination
        };
        _db.Routes.Add(route);
        await _db.SaveChangesAsync(ct);
        return ToDto(route);
    }

    public async Task<RouteDto> UpdateAsync(Guid id, UpdateRouteRequest request, CancellationToken ct)
    {
        var route = await _db.Routes
            .Include(r => r.SourceCity).Include(r => r.DestinationCity)
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException("Route not found");

        if (request.DistanceKm is not null) route.DistanceKm = request.DistanceKm;
        if (request.IsActive is not null) route.IsActive = request.IsActive.Value;
        await _db.SaveChangesAsync(ct);
        return ToDto(route);
    }

    private static RouteDto ToDto(Route r) => new(
        r.Id,
        new CityDto(r.SourceCity!.Id, r.SourceCity.Name, r.SourceCity.State, r.SourceCity.IsActive),
        new CityDto(r.DestinationCity!.Id, r.DestinationCity.Name, r.DestinationCity.State, r.DestinationCity.IsActive),
        r.DistanceKm,
        r.IsActive);
}
```

- [ ] **Step 5: Admin controller**

`Controllers/AdminRoutesController.cs`:

```csharp
using BusBooking.Api.Dtos;
using BusBooking.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.Api.Controllers;

[ApiController]
[Route("api/v1/admin/routes")]
[Authorize(Roles = "admin")]
public class AdminRoutesController : ControllerBase
{
    private readonly IRouteService _routes;

    public AdminRoutesController(IRouteService routes)
    {
        _routes = routes;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RouteDto>>> List(CancellationToken ct)
        => Ok(await _routes.ListAllAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RouteDto>> Get(Guid id, CancellationToken ct)
        => Ok(await _routes.GetAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<RouteDto>> Create(
        [FromBody] CreateRouteRequest body,
        [FromServices] IValidator<CreateRouteRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(body, ct);
        var route = await _routes.CreateAsync(body, ct);
        return StatusCode(StatusCodes.Status201Created, route);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<RouteDto>> Update(
        Guid id,
        [FromBody] UpdateRouteRequest body,
        [FromServices] IValidator<UpdateRouteRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(body, ct);
        var route = await _routes.UpdateAsync(id, body, ct);
        return Ok(route);
    }
}
```

- [ ] **Step 6: DI registration**

In `Program.cs`, under the `AddScoped<ICityService, CityService>();` line, add:

```csharp
builder.Services.AddScoped<IRouteService, RouteService>();
```

- [ ] **Step 7: Integration test**

`Integration/AdminRoutesTests.cs`:

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

[Collection("Integration")]
public class AdminRoutesTests : IAsyncLifetime
{
    private readonly IntegrationFixture _fx;
    private Guid _bangalore, _chennai;

    public AdminRoutesTests(IntegrationFixture fx)
    {
        _fx = fx;
    }

    public async Task InitializeAsync()
    {
        await _fx.ResetAsync();
        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var b = new City { Id = Guid.NewGuid(), Name = "Bangalore", State = "Karnataka" };
        var c = new City { Id = Guid.NewGuid(), Name = "Chennai", State = "Tamil Nadu" };
        db.Cities.AddRange(b, c);
        await db.SaveChangesAsync();
        _bangalore = b.Id;
        _chennai = c.Id;
    }

    public Task DisposeAsync()
    {
        _fx.Client.DefaultRequestHeaders.Authorization = null;
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Admin_can_create_and_deactivate_route()
    {
        var (_, token) = await AdminTokenFactory.CreateAdminAsync(_fx);
        _fx.Client.AttachAdminBearer(token);

        var created = await _fx.Client.PostAsJsonAsync("/api/v1/admin/routes", new
        {
            sourceCityId = _bangalore,
            destinationCityId = _chennai,
            distanceKm = 350
        });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await created.Content.ReadFromJsonAsync<RouteDto>();
        dto!.Source.Name.Should().Be("Bangalore");
        dto.Destination.Name.Should().Be("Chennai");
        dto.DistanceKm.Should().Be(350);

        var patched = await _fx.Client.PatchAsJsonAsync(
            $"/api/v1/admin/routes/{dto.Id}", new { isActive = false });
        patched.StatusCode.Should().Be(HttpStatusCode.OK);
        (await patched.Content.ReadFromJsonAsync<RouteDto>())!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Duplicate_route_returns_409()
    {
        var (_, token) = await AdminTokenFactory.CreateAdminAsync(_fx);
        _fx.Client.AttachAdminBearer(token);

        var body = new { sourceCityId = _bangalore, destinationCityId = _chennai };
        (await _fx.Client.PostAsJsonAsync("/api/v1/admin/routes", body)).EnsureSuccessStatusCode();
        var dup = await _fx.Client.PostAsJsonAsync("/api/v1/admin/routes", body);
        dup.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Unknown_city_returns_404()
    {
        var (_, token) = await AdminTokenFactory.CreateAdminAsync(_fx);
        _fx.Client.AttachAdminBearer(token);

        var resp = await _fx.Client.PostAsJsonAsync("/api/v1/admin/routes", new
        {
            sourceCityId = Guid.NewGuid(),
            destinationCityId = _chennai
        });
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Source_equals_destination_returns_400()
    {
        var (_, token) = await AdminTokenFactory.CreateAdminAsync(_fx);
        _fx.Client.AttachAdminBearer(token);

        var resp = await _fx.Client.PostAsJsonAsync("/api/v1/admin/routes", new
        {
            sourceCityId = _bangalore,
            destinationCityId = _bangalore
        });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Anonymous_returns_401()
    {
        var resp = await _fx.Client.GetAsync("/api/v1/admin/routes");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

- [ ] **Step 8: Build and run tests**

```bash
cd backend/BusBooking.Api && dotnet build
cd ../BusBooking.Api.Tests && dotnet test --filter "FullyQualifiedName~Routes"
```

Expected: all 5 tests pass.

- [ ] **Step 9: Commit**

```bash
git add backend/BusBooking.Api/Dtos/RouteDto.cs backend/BusBooking.Api/Dtos/CreateRouteRequest.cs \
        backend/BusBooking.Api/Dtos/UpdateRouteRequest.cs \
        backend/BusBooking.Api/Validators/CreateRouteRequestValidator.cs \
        backend/BusBooking.Api/Validators/UpdateRouteRequestValidator.cs \
        backend/BusBooking.Api/Services/IRouteService.cs backend/BusBooking.Api/Services/RouteService.cs \
        backend/BusBooking.Api/Controllers/AdminRoutesController.cs \
        backend/BusBooking.Api/Program.cs \
        backend/BusBooking.Api.Tests/Integration/AdminRoutesTests.cs
git commit -m "feat(backend): admin CRUD for routes + integration tests"
```

---

## Task 8: PlatformFeeService + admin GET/PUT + tests

**Files:**
- Create: `backend/BusBooking.Api/Dtos/PlatformFeeDto.cs`
- Create: `backend/BusBooking.Api/Dtos/UpdatePlatformFeeRequest.cs`
- Create: `backend/BusBooking.Api/Validators/UpdatePlatformFeeRequestValidator.cs`
- Create: `backend/BusBooking.Api/Services/IPlatformFeeService.cs`
- Create: `backend/BusBooking.Api/Services/PlatformFeeService.cs`
- Create: `backend/BusBooking.Api/Controllers/AdminPlatformFeeController.cs`
- Create: `backend/BusBooking.Api.Tests/Unit/PlatformFeeServiceTests.cs`
- Create: `backend/BusBooking.Api.Tests/Integration/AdminPlatformFeeTests.cs`
- Modify: `backend/BusBooking.Api/Program.cs`

- [ ] **Step 1: DTOs**

`Dtos/PlatformFeeDto.cs`:

```csharp
namespace BusBooking.Api.Dtos;

public record PlatformFeeDto(string FeeType, decimal Value, DateTime EffectiveFrom);
```

`Dtos/UpdatePlatformFeeRequest.cs`:

```csharp
namespace BusBooking.Api.Dtos;

public class UpdatePlatformFeeRequest
{
    public required string FeeType { get; set; } // "fixed" or "percent"
    public decimal Value { get; set; }
}
```

- [ ] **Step 2: Validator**

`Validators/UpdatePlatformFeeRequestValidator.cs`:

```csharp
using BusBooking.Api.Dtos;
using BusBooking.Api.Models;
using FluentValidation;

namespace BusBooking.Api.Validators;

public class UpdatePlatformFeeRequestValidator : AbstractValidator<UpdatePlatformFeeRequest>
{
    public UpdatePlatformFeeRequestValidator()
    {
        RuleFor(x => x.FeeType).NotEmpty().Must(v => PlatformFeeType.All.Contains(v))
            .WithMessage("feeType must be 'fixed' or 'percent'");
        RuleFor(x => x.Value).GreaterThanOrEqualTo(0).LessThanOrEqualTo(10000);
        // percent cap of 100 is enforced in the service (after FeeType known, avoids double validation)
    }
}
```

- [ ] **Step 3: Service**

`Services/IPlatformFeeService.cs`:

```csharp
using BusBooking.Api.Dtos;

namespace BusBooking.Api.Services;

public interface IPlatformFeeService
{
    Task<PlatformFeeDto> GetActiveAsync(CancellationToken ct);
    Task<PlatformFeeDto> UpdateAsync(Guid adminUserId, UpdatePlatformFeeRequest request, CancellationToken ct);
}
```

`Services/PlatformFeeService.cs`:

```csharp
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Infrastructure.Errors;
using BusBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Api.Services;

public class PlatformFeeService : IPlatformFeeService
{
    private readonly AppDbContext _db;
    private readonly TimeProvider _time;

    public PlatformFeeService(AppDbContext db, TimeProvider time)
    {
        _db = db;
        _time = time;
    }

    public async Task<PlatformFeeDto> GetActiveAsync(CancellationToken ct)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var active = await _db.PlatformFeeConfigs
            .AsNoTracking()
            .Where(p => p.EffectiveFrom <= now)
            .OrderByDescending(p => p.EffectiveFrom)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("No active platform fee configured");
        return new PlatformFeeDto(active.FeeType, active.Value, active.EffectiveFrom);
    }

    public async Task<PlatformFeeDto> UpdateAsync(Guid adminUserId, UpdatePlatformFeeRequest request, CancellationToken ct)
    {
        if (request.FeeType == PlatformFeeType.Percent && request.Value > 100m)
            throw new BusinessRuleException("PLATFORM_FEE_OUT_OF_RANGE", "Percent fee cannot exceed 100");

        var row = new PlatformFeeConfig
        {
            Id = Guid.NewGuid(),
            FeeType = request.FeeType,
            Value = request.Value,
            EffectiveFrom = _time.GetUtcNow().UtcDateTime,
            CreatedByAdminId = adminUserId
        };
        _db.PlatformFeeConfigs.Add(row);
        await _db.SaveChangesAsync(ct);
        return new PlatformFeeDto(row.FeeType, row.Value, row.EffectiveFrom);
    }
}
```

We inject `TimeProvider` (built-in since .NET 8) so the unit test in Step 6 can freeze time. Register it in Step 7.

- [ ] **Step 4: Admin controller**

`Controllers/AdminPlatformFeeController.cs`:

```csharp
using System.Security.Claims;
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure.Errors;
using BusBooking.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.Api.Controllers;

[ApiController]
[Route("api/v1/admin/platform-fee")]
[Authorize(Roles = "admin")]
public class AdminPlatformFeeController : ControllerBase
{
    private readonly IPlatformFeeService _fees;

    public AdminPlatformFeeController(IPlatformFeeService fees)
    {
        _fees = fees;
    }

    [HttpGet]
    public async Task<ActionResult<PlatformFeeDto>> GetActive(CancellationToken ct)
        => Ok(await _fees.GetActiveAsync(ct));

    [HttpPut]
    public async Task<ActionResult<PlatformFeeDto>> Update(
        [FromBody] UpdatePlatformFeeRequest body,
        [FromServices] IValidator<UpdatePlatformFeeRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(body, ct);
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub")
               ?? throw new UnauthorizedException();
        var adminId = Guid.Parse(sub);
        var updated = await _fees.UpdateAsync(adminId, body, ct);
        return Ok(updated);
    }
}
```

- [ ] **Step 5: DI registrations**

In `Program.cs`, next to the other `AddScoped` lines, add:

```csharp
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IPlatformFeeService, PlatformFeeService>();
```

(`TimeProvider.System` is cheap and thread-safe; one instance for the whole app is fine.)

- [ ] **Step 6: Unit test**

`Unit/PlatformFeeServiceTests.cs`:

```csharp
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Models;
using BusBooking.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace BusBooking.Api.Tests.Unit;

public class PlatformFeeServiceTests
{
    private static AppDbContext NewInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"pf-{Guid.NewGuid():N}")
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Active_returns_most_recent_row_whose_EffectiveFrom_has_passed()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.Parse("2026-05-01T00:00:00Z"));
        await using var db = NewInMemoryDb();
        db.PlatformFeeConfigs.AddRange(
            new PlatformFeeConfig { Id = Guid.NewGuid(), FeeType = PlatformFeeType.Fixed,   Value =  20m,
                                    EffectiveFrom = DateTime.Parse("2026-01-01T00:00:00Z").ToUniversalTime() },
            new PlatformFeeConfig { Id = Guid.NewGuid(), FeeType = PlatformFeeType.Fixed,   Value =  25m,
                                    EffectiveFrom = DateTime.Parse("2026-03-01T00:00:00Z").ToUniversalTime() },
            new PlatformFeeConfig { Id = Guid.NewGuid(), FeeType = PlatformFeeType.Percent, Value =   5m,
                                    EffectiveFrom = DateTime.Parse("2026-06-01T00:00:00Z").ToUniversalTime() }); // future
        await db.SaveChangesAsync();

        var svc = new PlatformFeeService(db, clock);
        var active = await svc.GetActiveAsync(CancellationToken.None);

        active.FeeType.Should().Be(PlatformFeeType.Fixed);
        active.Value.Should().Be(25m);
    }

    [Fact]
    public async Task Update_inserts_new_row_and_becomes_active()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.Parse("2026-05-01T00:00:00Z"));
        await using var db = NewInMemoryDb();
        var svc = new PlatformFeeService(db, clock);

        var adminId = Guid.NewGuid();
        var created = await svc.UpdateAsync(adminId,
            new Dtos.UpdatePlatformFeeRequest { FeeType = PlatformFeeType.Fixed, Value = 30m },
            CancellationToken.None);

        created.Value.Should().Be(30m);
        (await db.PlatformFeeConfigs.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Percent_value_above_100_is_rejected()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.Parse("2026-05-01T00:00:00Z"));
        await using var db = NewInMemoryDb();
        var svc = new PlatformFeeService(db, clock);

        var act = async () => await svc.UpdateAsync(Guid.NewGuid(),
            new Dtos.UpdatePlatformFeeRequest { FeeType = PlatformFeeType.Percent, Value = 101m },
            CancellationToken.None);

        await act.Should().ThrowAsync<BusBooking.Api.Infrastructure.Errors.BusinessRuleException>();
    }
}
```

Two NuGet packages are required for this test:

```bash
cd backend/BusBooking.Api.Tests
dotnet add package Microsoft.EntityFrameworkCore.InMemory --version 9.0.0
dotnet add package Microsoft.Extensions.TimeProvider.Testing --version 9.0.0
```

- [ ] **Step 7: Integration test**

`Integration/AdminPlatformFeeTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using BusBooking.Api.Dtos;
using BusBooking.Api.Tests.Support;
using FluentAssertions;

namespace BusBooking.Api.Tests.Integration;

[Collection("Integration")]
public class AdminPlatformFeeTests : IAsyncLifetime
{
    private readonly IntegrationFixture _fx;

    public AdminPlatformFeeTests(IntegrationFixture fx)
    {
        _fx = fx;
    }

    public async Task InitializeAsync() => await _fx.ResetAsync();

    public Task DisposeAsync()
    {
        _fx.Client.DefaultRequestHeaders.Authorization = null;
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Get_returns_the_seeded_default_after_a_fresh_reset()
    {
        var (_, token) = await AdminTokenFactory.CreateAdminAsync(_fx);
        _fx.Client.AttachAdminBearer(token);

        var resp = await _fx.Client.GetAsync("/api/v1/admin/platform-fee");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<PlatformFeeDto>();
        body!.FeeType.Should().Be("fixed");
        body.Value.Should().Be(25m);
    }

    [Fact]
    public async Task Put_inserts_new_row_and_subsequent_Get_returns_it()
    {
        var (_, token) = await AdminTokenFactory.CreateAdminAsync(_fx);
        _fx.Client.AttachAdminBearer(token);

        var put = await _fx.Client.PutAsJsonAsync("/api/v1/admin/platform-fee",
            new { feeType = "percent", value = 4.5m });
        put.StatusCode.Should().Be(HttpStatusCode.OK);

        var get = await _fx.Client.GetFromJsonAsync<PlatformFeeDto>("/api/v1/admin/platform-fee");
        get!.FeeType.Should().Be("percent");
        get.Value.Should().Be(4.5m);
    }

    [Fact]
    public async Task Invalid_fee_type_returns_400()
    {
        var (_, token) = await AdminTokenFactory.CreateAdminAsync(_fx);
        _fx.Client.AttachAdminBearer(token);

        var resp = await _fx.Client.PutAsJsonAsync("/api/v1/admin/platform-fee",
            new { feeType = "banana", value = 3m });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
```

**Important:** the `IntegrationFixture.ResetAsync()` truncates tables between tests. The seeded default platform fee row is created during app startup, so once the fixture truncates `platform_fee_config`, subsequent tests have an empty table. To keep the "GET returns seeded default" test meaningful, `ResetAsync` must re-run the platform-fee seeder after truncation.

Add the following to `IntegrationFixture.ResetAsync` (right after the truncate of `platform_fee_config`, or at the end of the method):

```csharp
        using var scope = Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IPlatformFeeSeeder>();
        await seeder.SeedAsync(CancellationToken.None);
```

(Add `using BusBooking.Api.Infrastructure.Seeding;` at the top of the fixture if it isn't there.)

- [ ] **Step 8: Run the new tests**

```bash
cd backend/BusBooking.Api && dotnet build
cd ../BusBooking.Api.Tests && dotnet test --filter "FullyQualifiedName~PlatformFee"
```

Expected: 3 unit tests + 3 integration tests pass.

- [ ] **Step 9: Run the full backend suite**

```bash
cd backend/BusBooking.Api.Tests && dotnet test
```

Expected: every test across the project passes.

- [ ] **Step 10: Commit**

```bash
git add backend/BusBooking.Api/Dtos/PlatformFeeDto.cs backend/BusBooking.Api/Dtos/UpdatePlatformFeeRequest.cs \
        backend/BusBooking.Api/Validators/UpdatePlatformFeeRequestValidator.cs \
        backend/BusBooking.Api/Services/IPlatformFeeService.cs backend/BusBooking.Api/Services/PlatformFeeService.cs \
        backend/BusBooking.Api/Controllers/AdminPlatformFeeController.cs \
        backend/BusBooking.Api/Program.cs \
        backend/BusBooking.Api.Tests/BusBooking.Api.Tests.csproj \
        backend/BusBooking.Api.Tests/Support/IntegrationFixture.cs \
        backend/BusBooking.Api.Tests/Unit/PlatformFeeServiceTests.cs \
        backend/BusBooking.Api.Tests/Integration/AdminPlatformFeeTests.cs
git commit -m "feat(backend): admin platform fee endpoints with history + tests"
```

---

## Task 9: Frontend API services for M2

**Files:**
- Create: `frontend/bus-booking-web/src/app/core/api/cities.api.ts`
- Create: `frontend/bus-booking-web/src/app/core/api/admin-cities.api.ts`
- Create: `frontend/bus-booking-web/src/app/core/api/admin-routes.api.ts`
- Create: `frontend/bus-booking-web/src/app/core/api/admin-platform-fee.api.ts`

- [ ] **Step 1: Public cities API**

`core/api/cities.api.ts`:

```typescript
import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface CityDto {
  id: string;
  name: string;
  state: string;
  isActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class CitiesApiService {
  private readonly http = inject(HttpClient);

  search(query: string, limit = 10): Observable<CityDto[]> {
    const params = new HttpParams().set('q', query).set('limit', limit.toString());
    return this.http.get<CityDto[]>(`${environment.apiBaseUrl}/cities`, { params });
  }
}
```

- [ ] **Step 2: Admin cities API**

`core/api/admin-cities.api.ts`:

```typescript
import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CityDto } from './cities.api';

export interface CreateCityRequest { name: string; state: string; }
export interface UpdateCityRequest { name?: string; state?: string; isActive?: boolean; }

@Injectable({ providedIn: 'root' })
export class AdminCitiesApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/admin/cities`;

  list(): Observable<CityDto[]> { return this.http.get<CityDto[]>(this.base); }
  get(id: string): Observable<CityDto> { return this.http.get<CityDto>(`${this.base}/${id}`); }
  create(body: CreateCityRequest): Observable<CityDto> { return this.http.post<CityDto>(this.base, body); }
  update(id: string, body: UpdateCityRequest): Observable<CityDto> {
    return this.http.patch<CityDto>(`${this.base}/${id}`, body);
  }
}
```

- [ ] **Step 3: Admin routes API**

`core/api/admin-routes.api.ts`:

```typescript
import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CityDto } from './cities.api';

export interface RouteDto {
  id: string;
  source: CityDto;
  destination: CityDto;
  distanceKm: number | null;
  isActive: boolean;
}
export interface CreateRouteRequest {
  sourceCityId: string;
  destinationCityId: string;
  distanceKm?: number | null;
}
export interface UpdateRouteRequest { distanceKm?: number | null; isActive?: boolean; }

@Injectable({ providedIn: 'root' })
export class AdminRoutesApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/admin/routes`;

  list(): Observable<RouteDto[]> { return this.http.get<RouteDto[]>(this.base); }
  create(body: CreateRouteRequest): Observable<RouteDto> { return this.http.post<RouteDto>(this.base, body); }
  update(id: string, body: UpdateRouteRequest): Observable<RouteDto> {
    return this.http.patch<RouteDto>(`${this.base}/${id}`, body);
  }
}
```

- [ ] **Step 4: Admin platform fee API**

`core/api/admin-platform-fee.api.ts`:

```typescript
import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export type PlatformFeeType = 'fixed' | 'percent';

export interface PlatformFeeDto {
  feeType: PlatformFeeType;
  value: number;
  effectiveFrom: string; // ISO
}
export interface UpdatePlatformFeeRequest {
  feeType: PlatformFeeType;
  value: number;
}

@Injectable({ providedIn: 'root' })
export class AdminPlatformFeeApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/admin/platform-fee`;

  get(): Observable<PlatformFeeDto> { return this.http.get<PlatformFeeDto>(this.base); }
  update(body: UpdatePlatformFeeRequest): Observable<PlatformFeeDto> {
    return this.http.put<PlatformFeeDto>(this.base, body);
  }
}
```

- [ ] **Step 5: Lint + type-check**

```bash
cd frontend/bus-booking-web && npx ng build --configuration development
```

Expected: build succeeds. No TS errors.

- [ ] **Step 6: Commit**

```bash
git add frontend/bus-booking-web/src/app/core/api/cities.api.ts \
        frontend/bus-booking-web/src/app/core/api/admin-cities.api.ts \
        frontend/bus-booking-web/src/app/core/api/admin-routes.api.ts \
        frontend/bus-booking-web/src/app/core/api/admin-platform-fee.api.ts
git commit -m "feat(web): API services for cities, routes, platform fee"
```

---

## Task 10: Reusable `CityAutocompleteComponent`

**Files:**
- Create: `frontend/bus-booking-web/src/app/shared/components/city-autocomplete/city-autocomplete.component.ts`
- Create: `frontend/bus-booking-web/src/app/shared/components/city-autocomplete/city-autocomplete.component.html`
- Create: `frontend/bus-booking-web/src/app/shared/components/city-autocomplete/city-autocomplete.component.scss`
- Create: `frontend/bus-booking-web/src/app/shared/components/city-autocomplete/city-autocomplete.component.spec.ts`

- [ ] **Step 1: Write the failing spec first**

`city-autocomplete.component.spec.ts`:

```typescript
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { environment } from '../../../../environments/environment';
import { CityAutocompleteComponent } from './city-autocomplete.component';

describe('CityAutocompleteComponent', () => {
  let fixture: ComponentFixture<CityAutocompleteComponent>;
  let component: CityAutocompleteComponent;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CityAutocompleteComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideNoopAnimations()]
    }).compileComponents();

    fixture = TestBed.createComponent(CityAutocompleteComponent);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
    fixture.componentRef.setInput('label', 'From');
    fixture.detectChanges();
  });

  afterEach(() => http.verify());

  it('does not query when input is shorter than 2 chars', fakeAsync(() => {
    component.control.setValue('b');
    tick(250);
    http.expectNone(() => true);
  }));

  it('queries the API after the debounce window', fakeAsync(() => {
    component.control.setValue('ban');
    tick(250);
    const req = http.expectOne(r =>
      r.url === `${environment.apiBaseUrl}/cities` && r.params.get('q') === 'ban');
    req.flush([{ id: 'c1', name: 'Bangalore', state: 'Karnataka', isActive: true }]);
    expect(component.options()).toEqual([
      { id: 'c1', name: 'Bangalore', state: 'Karnataka', isActive: true }
    ]);
  }));

  it('emits the selected city through `citySelected`', fakeAsync(() => {
    let emitted: unknown = null;
    component.citySelected.subscribe(c => (emitted = c));
    component.control.setValue('che');
    tick(250);
    http.expectOne(() => true).flush([
      { id: 'c2', name: 'Chennai', state: 'Tamil Nadu', isActive: true }
    ]);
    component.onSelect({ id: 'c2', name: 'Chennai', state: 'Tamil Nadu', isActive: true });
    expect(emitted).toEqual({ id: 'c2', name: 'Chennai', state: 'Tamil Nadu', isActive: true });
  }));
});
```

- [ ] **Step 2: Run the spec — expect failure (component doesn't exist yet)**

```bash
cd frontend/bus-booking-web && npx ng test --watch=false --include='**/city-autocomplete.component.spec.ts'
```

Expected: test file errors out with `Cannot find module './city-autocomplete.component'`. This confirms the harness is wired.

- [ ] **Step 3: Implement the component**

`city-autocomplete.component.ts`:

```typescript
import {
  ChangeDetectionStrategy, Component, EventEmitter, Output,
  input, signal
} from '@angular/core';
import { ReactiveFormsModule, FormControl } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import {
  MatAutocompleteModule, MatAutocompleteSelectedEvent
} from '@angular/material/autocomplete';
import { debounceTime, distinctUntilChanged, filter, switchMap } from 'rxjs';
import { CitiesApiService, CityDto } from '../../../core/api/cities.api';

@Component({
  selector: 'app-city-autocomplete',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatFormFieldModule, MatInputModule, MatAutocompleteModule
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './city-autocomplete.component.html',
  styleUrl: './city-autocomplete.component.scss'
})
export class CityAutocompleteComponent {
  readonly label = input.required<string>();
  readonly placeholder = input<string>('Start typing a city…');

  @Output() readonly citySelected = new EventEmitter<CityDto>();

  readonly control = new FormControl<string>('', { nonNullable: true });
  readonly options = signal<CityDto[]>([]);
  readonly loading = signal(false);

  constructor(private readonly api: CitiesApiService) {
    this.control.valueChanges.pipe(
      debounceTime(200),
      distinctUntilChanged(),
      filter(v => (v ?? '').trim().length >= 2),
      switchMap(v => {
        this.loading.set(true);
        return this.api.search(v!.trim());
      })
    ).subscribe({
      next: list => { this.options.set(list); this.loading.set(false); },
      error: () => { this.options.set([]); this.loading.set(false); }
    });

    this.control.valueChanges.pipe(
      filter(v => (v ?? '').trim().length < 2)
    ).subscribe(() => this.options.set([]));
  }

  displayFn = (c: CityDto | string | null): string =>
    typeof c === 'string' || c === null ? (c ?? '') : c.name;

  onSelect(city: CityDto): void {
    this.citySelected.emit(city);
  }

  onOptionPicked(event: MatAutocompleteSelectedEvent): void {
    this.onSelect(event.option.value as CityDto);
  }
}
```

- [ ] **Step 4: Template**

`city-autocomplete.component.html`:

```html
<mat-form-field appearance="outline" class="w-full">
  <mat-label>{{ label() }}</mat-label>
  <input
    matInput
    type="text"
    [formControl]="control"
    [placeholder]="placeholder()"
    [matAutocomplete]="auto" />
  <mat-autocomplete
    #auto="matAutocomplete"
    [displayWith]="displayFn"
    (optionSelected)="onOptionPicked($event)">
    @for (c of options(); track c.id) {
      <mat-option [value]="c">{{ c.name }} <span class="text-gray-500 text-sm">· {{ c.state }}</span></mat-option>
    }
    @if (loading()) {
      <mat-option disabled>Searching…</mat-option>
    }
  </mat-autocomplete>
</mat-form-field>
```

- [ ] **Step 5: Styles**

`city-autocomplete.component.scss`:

```scss
:host { display: block; }
```

- [ ] **Step 6: Run the spec — expect pass**

```bash
cd frontend/bus-booking-web && npx ng test --watch=false --include='**/city-autocomplete.component.spec.ts'
```

Expected: 3 specs pass.

- [ ] **Step 7: Commit**

```bash
git add frontend/bus-booking-web/src/app/shared/components/city-autocomplete/
git commit -m "feat(web): reusable CityAutocomplete component"
```

---

## Task 11: Admin cities page

**Files:**
- Create: `frontend/bus-booking-web/src/app/features/admin/cities/admin-cities-page.component.ts`
- Create: `frontend/bus-booking-web/src/app/features/admin/cities/admin-cities-page.component.html`
- Create: `frontend/bus-booking-web/src/app/features/admin/cities/admin-cities-page.component.scss`

- [ ] **Step 1: Component**

`admin-cities-page.component.ts`:

```typescript
import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { AdminCitiesApiService } from '../../../core/api/admin-cities.api';
import { CityDto } from '../../../core/api/cities.api';

@Component({
  selector: 'app-admin-cities-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule,
    MatSlideToggleModule, MatTableModule
  ],
  templateUrl: './admin-cities-page.component.html',
  styleUrl: './admin-cities-page.component.scss'
})
export class AdminCitiesPageComponent {
  private readonly api = inject(AdminCitiesApiService);
  private readonly fb = inject(FormBuilder);
  private readonly snack = inject(MatSnackBar);

  readonly cities = signal<CityDto[]>([]);
  readonly saving = signal(false);
  readonly columns = ['name', 'state', 'active'];

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(120)]],
    state: ['', [Validators.required, Validators.maxLength(120)]]
  });

  constructor() {
    this.refresh();
  }

  refresh(): void {
    this.api.list().subscribe({
      next: list => this.cities.set(list),
      error: () => this.snack.open('Failed to load cities', 'Dismiss', { duration: 4000 })
    });
  }

  submit(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    this.api.create(this.form.getRawValue()).subscribe({
      next: created => {
        this.cities.set([...this.cities(), created].sort((a, b) => a.name.localeCompare(b.name)));
        this.form.reset();
        this.saving.set(false);
      },
      error: err => {
        this.saving.set(false);
        this.snack.open(err?.error?.error?.message ?? 'Create failed', 'Dismiss', { duration: 4000 });
      }
    });
  }

  toggleActive(c: CityDto): void {
    this.api.update(c.id, { isActive: !c.isActive }).subscribe({
      next: updated => {
        this.cities.set(this.cities().map(x => (x.id === updated.id ? updated : x)));
      },
      error: () => this.snack.open('Update failed', 'Dismiss', { duration: 4000 })
    });
  }
}
```

- [ ] **Step 2: Template**

`admin-cities-page.component.html`:

```html
<section class="p-6 max-w-4xl mx-auto space-y-6">
  <header>
    <h1 class="text-2xl font-semibold">Cities</h1>
    <p class="text-gray-600">Manage cities that appear in search and routes.</p>
  </header>

  <form [formGroup]="form" (ngSubmit)="submit()" class="flex flex-col md:flex-row gap-4 items-end">
    <mat-form-field appearance="outline" class="flex-1">
      <mat-label>Name</mat-label>
      <input matInput formControlName="name" />
    </mat-form-field>
    <mat-form-field appearance="outline" class="flex-1">
      <mat-label>State</mat-label>
      <input matInput formControlName="state" />
    </mat-form-field>
    <button mat-flat-button color="primary" type="submit"
            [disabled]="form.invalid || saving()">
      {{ saving() ? 'Adding…' : 'Add City' }}
    </button>
  </form>

  <table mat-table [dataSource]="cities()" class="w-full">
    <ng-container matColumnDef="name">
      <th mat-header-cell *matHeaderCellDef>Name</th>
      <td mat-cell *matCellDef="let c">{{ c.name }}</td>
    </ng-container>
    <ng-container matColumnDef="state">
      <th mat-header-cell *matHeaderCellDef>State</th>
      <td mat-cell *matCellDef="let c">{{ c.state }}</td>
    </ng-container>
    <ng-container matColumnDef="active">
      <th mat-header-cell *matHeaderCellDef>Active</th>
      <td mat-cell *matCellDef="let c">
        <mat-slide-toggle [checked]="c.isActive" (change)="toggleActive(c)"></mat-slide-toggle>
      </td>
    </ng-container>
    <tr mat-header-row *matHeaderRowDef="columns"></tr>
    <tr mat-row *matRowDef="let row; columns: columns"></tr>
  </table>

  @if (cities().length === 0) {
    <p class="text-gray-500 italic">No cities yet.</p>
  }
</section>
```

- [ ] **Step 3: Styles**

`admin-cities-page.component.scss`:

```scss
:host { display: block; }
```

- [ ] **Step 4: Build**

```bash
cd frontend/bus-booking-web && npx ng build --configuration development
```

Expected: build succeeds.

- [ ] **Step 5: Commit**

```bash
git add frontend/bus-booking-web/src/app/features/admin/cities/
git commit -m "feat(web): admin cities page"
```

---

## Task 12: Admin routes page

**Files:**
- Create: `frontend/bus-booking-web/src/app/features/admin/routes/admin-routes-page.component.ts`
- Create: `frontend/bus-booking-web/src/app/features/admin/routes/admin-routes-page.component.html`
- Create: `frontend/bus-booking-web/src/app/features/admin/routes/admin-routes-page.component.scss`

- [ ] **Step 1: Component**

`admin-routes-page.component.ts`:

```typescript
import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { AdminRoutesApiService, RouteDto } from '../../../core/api/admin-routes.api';
import { CityAutocompleteComponent } from '../../../shared/components/city-autocomplete/city-autocomplete.component';
import { CityDto } from '../../../core/api/cities.api';

@Component({
  selector: 'app-admin-routes-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatButtonModule, MatFormFieldModule, MatInputModule,
    MatSlideToggleModule, MatTableModule,
    CityAutocompleteComponent
  ],
  templateUrl: './admin-routes-page.component.html',
  styleUrl: './admin-routes-page.component.scss'
})
export class AdminRoutesPageComponent {
  private readonly api = inject(AdminRoutesApiService);
  private readonly fb = inject(FormBuilder);
  private readonly snack = inject(MatSnackBar);

  readonly routes = signal<RouteDto[]>([]);
  readonly source = signal<CityDto | null>(null);
  readonly destination = signal<CityDto | null>(null);
  readonly columns = ['source', 'destination', 'distance', 'active'];

  readonly form = this.fb.nonNullable.group({
    distanceKm: [null as number | null,
      [Validators.min(1), Validators.max(5000)]]
  });

  constructor() {
    this.refresh();
  }

  refresh(): void {
    this.api.list().subscribe({
      next: list => this.routes.set(list),
      error: () => this.snack.open('Failed to load routes', 'Dismiss', { duration: 4000 })
    });
  }

  canSubmit(): boolean {
    const s = this.source(); const d = this.destination();
    return !!s && !!d && s.id !== d.id && this.form.valid;
  }

  submit(): void {
    const s = this.source(); const d = this.destination();
    if (!s || !d) return;
    this.api.create({
      sourceCityId: s.id,
      destinationCityId: d.id,
      distanceKm: this.form.getRawValue().distanceKm
    }).subscribe({
      next: created => {
        this.routes.set([created, ...this.routes()]);
        this.source.set(null);
        this.destination.set(null);
        this.form.reset();
      },
      error: err => this.snack.open(
        err?.error?.error?.message ?? 'Create failed', 'Dismiss', { duration: 4000 })
    });
  }

  toggleActive(r: RouteDto): void {
    this.api.update(r.id, { isActive: !r.isActive }).subscribe({
      next: updated => this.routes.set(this.routes().map(x => x.id === updated.id ? updated : x)),
      error: () => this.snack.open('Update failed', 'Dismiss', { duration: 4000 })
    });
  }
}
```

- [ ] **Step 2: Template**

`admin-routes-page.component.html`:

```html
<section class="p-6 max-w-5xl mx-auto space-y-6">
  <header>
    <h1 class="text-2xl font-semibold">Routes</h1>
    <p class="text-gray-600">Source–destination pairs that operators can schedule buses on.</p>
  </header>

  <div class="grid grid-cols-1 md:grid-cols-4 gap-4 items-end">
    <app-city-autocomplete
      label="Source"
      (citySelected)="source.set($event)" />
    <app-city-autocomplete
      label="Destination"
      (citySelected)="destination.set($event)" />

    <form [formGroup]="form" class="contents">
      <mat-form-field appearance="outline">
        <mat-label>Distance (km)</mat-label>
        <input matInput type="number" formControlName="distanceKm" />
      </mat-form-field>
      <button mat-flat-button color="primary" type="button"
              [disabled]="!canSubmit()" (click)="submit()">
        Add Route
      </button>
    </form>
  </div>

  <table mat-table [dataSource]="routes()" class="w-full">
    <ng-container matColumnDef="source">
      <th mat-header-cell *matHeaderCellDef>From</th>
      <td mat-cell *matCellDef="let r">{{ r.source.name }}</td>
    </ng-container>
    <ng-container matColumnDef="destination">
      <th mat-header-cell *matHeaderCellDef>To</th>
      <td mat-cell *matCellDef="let r">{{ r.destination.name }}</td>
    </ng-container>
    <ng-container matColumnDef="distance">
      <th mat-header-cell *matHeaderCellDef>Distance (km)</th>
      <td mat-cell *matCellDef="let r">{{ r.distanceKm ?? '—' }}</td>
    </ng-container>
    <ng-container matColumnDef="active">
      <th mat-header-cell *matHeaderCellDef>Active</th>
      <td mat-cell *matCellDef="let r">
        <mat-slide-toggle [checked]="r.isActive" (change)="toggleActive(r)"></mat-slide-toggle>
      </td>
    </ng-container>
    <tr mat-header-row *matHeaderRowDef="columns"></tr>
    <tr mat-row *matRowDef="let row; columns: columns"></tr>
  </table>

  @if (routes().length === 0) {
    <p class="text-gray-500 italic">No routes yet.</p>
  }
</section>
```

- [ ] **Step 3: Styles**

`admin-routes-page.component.scss`:

```scss
:host { display: block; }
```

- [ ] **Step 4: Build**

```bash
cd frontend/bus-booking-web && npx ng build --configuration development
```

Expected: build succeeds.

- [ ] **Step 5: Commit**

```bash
git add frontend/bus-booking-web/src/app/features/admin/routes/
git commit -m "feat(web): admin routes page"
```

---

## Task 13: Admin platform-fee page

**Files:**
- Create: `frontend/bus-booking-web/src/app/features/admin/platform-fee/admin-platform-fee-page.component.ts`
- Create: `frontend/bus-booking-web/src/app/features/admin/platform-fee/admin-platform-fee-page.component.html`
- Create: `frontend/bus-booking-web/src/app/features/admin/platform-fee/admin-platform-fee-page.component.scss`

- [ ] **Step 1: Component**

`admin-platform-fee-page.component.ts`:

```typescript
import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatRadioModule } from '@angular/material/radio';
import { MatSnackBar } from '@angular/material/snack-bar';
import {
  AdminPlatformFeeApiService, PlatformFeeDto, PlatformFeeType
} from '../../../core/api/admin-platform-fee.api';

@Component({
  selector: 'app-admin-platform-fee-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, ReactiveFormsModule, DatePipe,
    MatButtonModule, MatCardModule, MatFormFieldModule,
    MatInputModule, MatRadioModule
  ],
  templateUrl: './admin-platform-fee-page.component.html',
  styleUrl: './admin-platform-fee-page.component.scss'
})
export class AdminPlatformFeePageComponent {
  private readonly api = inject(AdminPlatformFeeApiService);
  private readonly fb = inject(FormBuilder);
  private readonly snack = inject(MatSnackBar);

  readonly active = signal<PlatformFeeDto | null>(null);
  readonly saving = signal(false);

  readonly form = this.fb.nonNullable.group({
    feeType: ['fixed' as PlatformFeeType, [Validators.required]],
    value: [25, [Validators.required, Validators.min(0), Validators.max(10000)]]
  });

  constructor() {
    this.load();
  }

  load(): void {
    this.api.get().subscribe({
      next: dto => {
        this.active.set(dto);
        this.form.patchValue({ feeType: dto.feeType, value: dto.value });
      },
      error: () => this.snack.open('Failed to load platform fee', 'Dismiss', { duration: 4000 })
    });
  }

  submit(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    this.api.update(this.form.getRawValue()).subscribe({
      next: dto => {
        this.active.set(dto);
        this.saving.set(false);
        this.snack.open('Platform fee updated', 'Dismiss', { duration: 3000 });
      },
      error: err => {
        this.saving.set(false);
        this.snack.open(err?.error?.error?.message ?? 'Update failed', 'Dismiss', { duration: 4000 });
      }
    });
  }
}
```

- [ ] **Step 2: Template**

`admin-platform-fee-page.component.html`:

```html
<section class="p-6 max-w-xl mx-auto space-y-6">
  <header>
    <h1 class="text-2xl font-semibold">Platform fee</h1>
    <p class="text-gray-600">Saving inserts a new history row; bookings always use the most recent entry.</p>
  </header>

  @if (active(); as a) {
    <mat-card>
      <mat-card-content>
        <div class="text-sm text-gray-500">Currently active</div>
        <div class="text-xl font-medium">
          {{ a.feeType === 'fixed' ? '₹' + a.value : a.value + '%' }}
        </div>
        <div class="text-xs text-gray-500">since {{ a.effectiveFrom | date:'medium' }}</div>
      </mat-card-content>
    </mat-card>
  }

  <form [formGroup]="form" (ngSubmit)="submit()" class="space-y-4">
    <mat-radio-group formControlName="feeType" class="flex gap-6">
      <mat-radio-button value="fixed">Fixed (₹)</mat-radio-button>
      <mat-radio-button value="percent">Percent (%)</mat-radio-button>
    </mat-radio-group>

    <mat-form-field appearance="outline" class="w-full">
      <mat-label>Value</mat-label>
      <input matInput type="number" step="0.01" formControlName="value" />
    </mat-form-field>

    <div class="flex justify-end">
      <button mat-flat-button color="primary" type="submit"
              [disabled]="form.invalid || saving()">
        {{ saving() ? 'Saving…' : 'Save new fee' }}
      </button>
    </div>
  </form>
</section>
```

- [ ] **Step 3: Styles**

`admin-platform-fee-page.component.scss`:

```scss
:host { display: block; }
```

- [ ] **Step 4: Build**

```bash
cd frontend/bus-booking-web && npx ng build --configuration development
```

Expected: build succeeds.

- [ ] **Step 5: Commit**

```bash
git add frontend/bus-booking-web/src/app/features/admin/platform-fee/
git commit -m "feat(web): admin platform fee page"
```

---

## Task 14: Wire admin routes, update dashboard, add autocomplete to home

**Files:**
- Modify: `frontend/bus-booking-web/src/app/app.routes.ts`
- Modify: `frontend/bus-booking-web/src/app/features/admin/admin-dashboard/admin-dashboard.component.ts`
- Modify: `frontend/bus-booking-web/src/app/features/admin/admin-dashboard/admin-dashboard.component.html`
- Modify: `frontend/bus-booking-web/src/app/features/public/home/home.component.ts`
- Modify: `frontend/bus-booking-web/src/app/features/public/home/home.component.html`

- [ ] **Step 1: Routing**

Replace the entire contents of `app.routes.ts` with:

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
      }
    ]
  },
  { path: '**', redirectTo: '' }
];
```

- [ ] **Step 2: Admin dashboard tiles**

Replace the existing `admin-dashboard.component.ts` with:

```typescript
import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, MatCardModule, MatIconModule],
  templateUrl: './admin-dashboard.component.html'
})
export class AdminDashboardComponent {}
```

Replace `admin-dashboard.component.html`:

```html
<section class="p-6 max-w-5xl mx-auto space-y-6">
  <header>
    <h1 class="text-2xl font-semibold">Admin console</h1>
    <p class="text-gray-600">Manage the catalog and platform configuration.</p>
  </header>

  <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
    <mat-card class="hover:shadow-md transition" [routerLink]="['/admin/cities']">
      <mat-card-content class="flex items-start gap-3 cursor-pointer">
        <mat-icon>location_city</mat-icon>
        <div>
          <div class="font-medium">Cities</div>
          <div class="text-sm text-gray-600">Add, deactivate, rename</div>
        </div>
      </mat-card-content>
    </mat-card>

    <mat-card class="hover:shadow-md transition" [routerLink]="['/admin/routes']">
      <mat-card-content class="flex items-start gap-3 cursor-pointer">
        <mat-icon>alt_route</mat-icon>
        <div>
          <div class="font-medium">Routes</div>
          <div class="text-sm text-gray-600">Pair cities that operators can serve</div>
        </div>
      </mat-card-content>
    </mat-card>

    <mat-card class="hover:shadow-md transition" [routerLink]="['/admin/platform-fee']">
      <mat-card-content class="flex items-start gap-3 cursor-pointer">
        <mat-icon>payments</mat-icon>
        <div>
          <div class="font-medium">Platform fee</div>
          <div class="text-sm text-gray-600">Fixed ₹ or percent, with history</div>
        </div>
      </mat-card-content>
    </mat-card>
  </div>
</section>
```

If the old `admin-dashboard.component.scss` exists and is empty, leave it untouched. If it contains M1 stub styles that no longer apply, overwrite its contents with:

```scss
:host { display: block; }
```

- [ ] **Step 3: Home page autocomplete**

Open `features/public/home/home.component.ts`. Add the following imports at the top (leave existing imports in place):

```typescript
import { signal } from '@angular/core';
import { CityAutocompleteComponent } from '../../../shared/components/city-autocomplete/city-autocomplete.component';
import { CityDto } from '../../../core/api/cities.api';
```

Add `CityAutocompleteComponent` to the component's `imports` array. Add two signals inside the class body:

```typescript
  readonly source = signal<CityDto | null>(null);
  readonly destination = signal<CityDto | null>(null);
```

In `home.component.html`, **above** the existing M0 "backend online" block, insert:

```html
<section class="p-6 max-w-3xl mx-auto space-y-4">
  <h2 class="text-xl font-medium">Where to?</h2>
  <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
    <app-city-autocomplete label="From" (citySelected)="source.set($event)" />
    <app-city-autocomplete label="To"   (citySelected)="destination.set($event)" />
  </div>
  @if (source() && destination()) {
    <p class="text-sm text-gray-600">
      Selected: <strong>{{ source()!.name }}</strong> → <strong>{{ destination()!.name }}</strong>
    </p>
  }
</section>
```

(The actual search action lands in M4. M2 only proves the autocomplete talks to the cities endpoint.)

- [ ] **Step 4: Build**

```bash
cd frontend/bus-booking-web && npx ng build --configuration development
```

Expected: build succeeds.

- [ ] **Step 5: Commit**

```bash
git add frontend/bus-booking-web/src/app/app.routes.ts \
        frontend/bus-booking-web/src/app/features/admin/admin-dashboard/ \
        frontend/bus-booking-web/src/app/features/public/home/
git commit -m "feat(web): wire admin catalog routes and home autocomplete"
```

---

## Task 15: Full-suite verification

- [ ] **Step 1: Run the full backend suite**

```bash
cd backend/BusBooking.Api.Tests && dotnet test
```

Expected: every test passes.

- [ ] **Step 2: Run the full frontend suite**

```bash
cd frontend/bus-booking-web && npx ng test --watch=false
```

Expected: every spec passes.

- [ ] **Step 3: Manual smoke**

In one terminal:
```bash
cd backend/BusBooking.Api && dotnet run
```

In another:
```bash
cd frontend/bus-booking-web && npx ng serve
```

1. Open `http://localhost:4200`.
2. Sign in as the seeded admin (`admin@busbooking.local` / whatever M1 configured).
3. Navigate to **Admin Console**. Three tiles appear: Cities, Routes, Platform fee.
4. **Cities**: add `Bangalore`, `Chennai`, `Mumbai`, `Pune`. Toggle `Mumbai` inactive.
5. **Routes**: select `Bangalore` → `Chennai`, set distance 350, submit. The row appears.
6. **Platform fee**: change to `Percent`, value `4.5`, save. The "Currently active" card updates to `4.5%`.
7. Log out. On the public home page, type `ba` into the **From** autocomplete. `Bangalore` appears, `Mumbai` does not.

- [ ] **Step 4: No extra commit needed**

If any manual step failed, open a follow-up task rather than amending. If all passed, push:

```bash
git push
```

---

## Acceptance criteria

- `dotnet test` under `backend/BusBooking.Api.Tests` passes (all M0 + M1 + M2 unit + integration tests).
- `npx ng test --watch=false` under `frontend/bus-booking-web` passes (all specs).
- Migration `AddCitiesRoutesAndPlatformFee` creates tables `cities`, `routes`, `platform_fee_config`, and a GIN index `ix_cities_name_trgm` on `cities.name` using `gin_trgm_ops`.
- Fresh boot seeds a default platform fee of `fixed ₹25.00`; second boot logs "already present".
- `GET /api/v1/cities?q=ban` returns `Bangalore` when present and active; returns `[]` for `q` length &lt; 2; excludes inactive cities.
- `GET/POST/PATCH /api/v1/admin/cities` require `admin`; return 401 anonymous, 403 non-admin, 409 on duplicate name (case-insensitive).
- `GET/POST/PATCH /api/v1/admin/routes` require `admin`; return 400 when source equals destination, 404 when a city id is unknown, 409 on duplicate `(source, destination)`.
- `GET /api/v1/admin/platform-fee` returns the most recent row whose `effective_from <= now()`; `PUT` inserts a new row rather than mutating.
- Frontend admin dashboard shows three tiles; each loads the corresponding page; navigation is gated by `roleGuard(['admin'])`.
- Home page exposes two `CityAutocomplete` inputs that hit `/api/v1/cities` after 200 ms debounce and only when the typed query is at least two characters long.

---

## Risks and open questions

- **pg_trgm vs ILIKE performance tradeoff.** The GIN index with `gin_trgm_ops` accelerates `ILIKE '%x%'` on small result sets. For future scale, consider switching the service to `% similarity(c.name, @q) > threshold` ordered by `similarity(...) DESC` and a `SET pg_trgm.similarity_threshold` session parameter; that branch can swap in without changing the public API.
- **EF Core `Route` naming.** If any later module adds `using Microsoft.AspNetCore.Routing;` at file scope, introduce a `RouteModel` alias in that file to avoid ambiguity. No change needed inside M2 itself.
- **Seeded platform fee in tests.** `IntegrationFixture.ResetAsync` re-runs the platform-fee seeder after truncation to keep tests realistic. If a future test wants to start with an empty `platform_fee_config`, add an explicit `ResetAsync(seedPlatformFee: false)` overload at that time.
