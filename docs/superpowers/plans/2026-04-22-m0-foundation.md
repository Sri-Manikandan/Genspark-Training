# M0 — Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Both the .NET 9 backend and the Angular 20 frontend boot, talk to each other via a `/api/v1/health` endpoint, and are wired with the cross-cutting middleware/libraries we'll need throughout the project.

**Architecture:** Monorepo with `backend/` (one .NET 9 Web API project + one xUnit test project in a single solution) and `frontend/` (one Angular 20 workspace). Backend uses EF Core 9 + Npgsql pointing at a local PostgreSQL database. Frontend uses Angular Material + Tailwind. A health-check endpoint is implemented TDD-style and exercised by an integration test (backend) and a unit test (frontend service).

**Tech Stack:**
- Backend: .NET 9, ASP.NET Core Web API (controllers), EF Core 9, Npgsql, Serilog, FluentValidation, JWT Bearer, Swashbuckle, BCrypt.Net-Next (stub), xUnit + FluentAssertions + `Microsoft.AspNetCore.Mvc.Testing`.
- Frontend: Angular 20 (standalone components), Angular Material, Tailwind CSS v3, Jasmine + Karma.
- Database: existing local PostgreSQL (≥14) with `pg_trgm` extension.

**Prerequisites on the dev machine:**
- .NET 9 SDK (`dotnet --version` → 9.x)
- Node.js ≥20 + npm ≥10 (`node -v`, `npm -v`)
- Angular CLI 20 will be installed on demand via `npx`
- PostgreSQL running locally (`psql --version`), able to `createdb`
- `dotnet-ef` global tool (installed in Task 6 if missing)

---

### Task 1: Create backend solution and Web API project

**Files:**
- Create: `backend/BusBookingSystem.sln`
- Create: `backend/BusBooking.Api/BusBooking.Api.csproj`
- Create: `backend/BusBooking.Api/Program.cs`
- Create: `backend/BusBooking.Api/appsettings.json`
- Create: `backend/BusBooking.Api/appsettings.Development.json`
- Create: `backend/BusBooking.Api/Properties/launchSettings.json`
- Delete: the default `WeatherForecast.cs` + `Controllers/WeatherForecastController.cs` that the template generates.

- [ ] **Step 1: Create the solution and project**

Run from the repo root:
```bash
mkdir -p backend
cd backend
dotnet new sln -n BusBookingSystem
dotnet new webapi -n BusBooking.Api -f net9.0 --use-controllers
dotnet sln add BusBooking.Api/BusBooking.Api.csproj
```

Expected: `BusBookingSystem.sln` and `BusBooking.Api/` appear. No errors.

- [ ] **Step 2: Remove the default WeatherForecast scaffold**

```bash
rm BusBooking.Api/WeatherForecast.cs
rm BusBooking.Api/Controllers/WeatherForecastController.cs
```

Expected: both files gone.

- [ ] **Step 3: Fix the listening URL in launchSettings.json**

Open `backend/BusBooking.Api/Properties/launchSettings.json` and replace the `profiles` block so the API listens on `http://localhost:5080` (no HTTPS locally — simpler for Angular dev-server CORS):

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "http://localhost:5080",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

- [ ] **Step 4: Verify it builds**

Run:
```bash
cd backend
dotnet build
```

Expected: `Build succeeded. 0 Warning(s). 0 Error(s).`

- [ ] **Step 5: Commit**

```bash
cd "/Users/srimanikandanr/My Files/Presidio Tasks/Bus Booking System"
git add backend/BusBookingSystem.sln backend/BusBooking.Api/
git commit -m "feat(backend): scaffold BusBooking.Api project on .NET 9"
```

---

### Task 2: Add NuGet packages to the Web API project

**Files:**
- Modify: `backend/BusBooking.Api/BusBooking.Api.csproj`

- [ ] **Step 1: Add every package needed across M0–M1**

Run from `backend/BusBooking.Api/`:
```bash
cd backend/BusBooking.Api
dotnet add package Microsoft.EntityFrameworkCore --version 9.0.*
dotnet add package Microsoft.EntityFrameworkCore.Design --version 9.0.*
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 9.0.*
dotnet add package Serilog.AspNetCore --version 9.*
dotnet add package Serilog.Sinks.Console --version 6.*
dotnet add package Serilog.Sinks.File --version 6.*
dotnet add package FluentValidation --version 11.*
dotnet add package FluentValidation.DependencyInjectionExtensions --version 11.*
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 9.0.*
dotnet add package Swashbuckle.AspNetCore --version 7.*
```

Expected: each command ends with `info : PackageReference for package '…' version '…' added`.

- [ ] **Step 2: Verify the project still restores and builds**

```bash
cd backend
dotnet restore
dotnet build
```

Expected: `Build succeeded. 0 Error(s).`

- [ ] **Step 3: Commit**

```bash
cd "/Users/srimanikandanr/My Files/Presidio Tasks/Bus Booking System"
git add backend/BusBooking.Api/BusBooking.Api.csproj
git commit -m "chore(backend): add EF Core, Serilog, JWT, FluentValidation packages"
```

---

### Task 3: Create backend folder structure and stub classes

**Files:**
- Create: `backend/BusBooking.Api/Controllers/.gitkeep`
- Create: `backend/BusBooking.Api/Services/.gitkeep`
- Create: `backend/BusBooking.Api/Repositories/.gitkeep`
- Create: `backend/BusBooking.Api/Models/.gitkeep`
- Create: `backend/BusBooking.Api/Dtos/.gitkeep`
- Create: `backend/BusBooking.Api/Validators/.gitkeep`
- Create: `backend/BusBooking.Api/Migrations/.gitkeep`
- Create: `backend/BusBooking.Api/Infrastructure/AppDbContext.cs`
- Create: `backend/BusBooking.Api/Infrastructure/Errors/AppException.cs`
- Create: `backend/BusBooking.Api/Infrastructure/Errors/ExceptionMiddleware.cs`
- Create: `backend/BusBooking.Api/Infrastructure/Errors/ErrorResponse.cs`

- [ ] **Step 1: Create the empty folders with .gitkeep markers**

```bash
cd backend/BusBooking.Api
mkdir -p Controllers Services Repositories Models Dtos Validators Migrations Infrastructure Infrastructure/Errors
touch Controllers/.gitkeep Services/.gitkeep Repositories/.gitkeep Models/.gitkeep Dtos/.gitkeep Validators/.gitkeep Migrations/.gitkeep
```

- [ ] **Step 2: Write the empty AppDbContext**

Create `backend/BusBooking.Api/Infrastructure/AppDbContext.cs`:

```csharp
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Api.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.HasPostgresExtension("pg_trgm");
    }
}
```

- [ ] **Step 3: Write the error-response DTO**

Create `backend/BusBooking.Api/Infrastructure/Errors/ErrorResponse.cs`:

```csharp
namespace BusBooking.Api.Infrastructure.Errors;

public record ErrorEnvelope(string Code, string Message, string CorrelationId, object? Details = null);

public record ErrorResponse(ErrorEnvelope Error);
```

- [ ] **Step 4: Write the domain exception hierarchy**

Create `backend/BusBooking.Api/Infrastructure/Errors/AppException.cs`:

```csharp
namespace BusBooking.Api.Infrastructure.Errors;

public abstract class AppException : Exception
{
    protected AppException(string code, string message, int httpStatus, object? details = null) : base(message)
    {
        Code = code;
        HttpStatus = httpStatus;
        Details = details;
    }

    public string Code { get; }
    public int HttpStatus { get; }
    public object? Details { get; }
}

public class NotFoundException(string message) : AppException("NOT_FOUND", message, 404);
public class ConflictException(string code, string message, object? details = null) : AppException(code, message, 409, details);
public class BusinessRuleException(string code, string message, object? details = null) : AppException(code, message, 422, details);
public class ForbiddenException(string message = "Forbidden") : AppException("FORBIDDEN", message, 403);
public class UnauthorizedException(string message = "Unauthorized") : AppException("UNAUTHORIZED", message, 401);
```

- [ ] **Step 5: Write the global exception middleware**

Create `backend/BusBooking.Api/Infrastructure/Errors/ExceptionMiddleware.cs`:

```csharp
using System.Text.Json;
using FluentValidation;

namespace BusBooking.Api.Infrastructure.Errors;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await Handle(context, ex);
        }
    }

    private async Task Handle(HttpContext context, Exception ex)
    {
        var correlationId = context.TraceIdentifier;
        ErrorEnvelope envelope;
        int status;

        switch (ex)
        {
            case ValidationException vex:
                status = 400;
                var details = vex.Errors.Select(e => new { field = e.PropertyName, error = e.ErrorMessage });
                envelope = new ErrorEnvelope("VALIDATION_ERROR", "Request validation failed", correlationId, details);
                logger.LogWarning(ex, "Validation failed {CorrelationId}", correlationId);
                break;
            case AppException aex:
                status = aex.HttpStatus;
                envelope = new ErrorEnvelope(aex.Code, aex.Message, correlationId, aex.Details);
                logger.LogWarning(ex, "App exception {Code} {CorrelationId}", aex.Code, correlationId);
                break;
            default:
                status = 500;
                envelope = new ErrorEnvelope("INTERNAL_ERROR", "Something went wrong", correlationId);
                logger.LogError(ex, "Unhandled exception {CorrelationId}", correlationId);
                break;
        }

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(
            new ErrorResponse(envelope),
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
```

- [ ] **Step 6: Verify it still builds**

```bash
cd backend
dotnet build
```

Expected: `Build succeeded. 0 Error(s).`

- [ ] **Step 7: Commit**

```bash
cd "/Users/srimanikandanr/My Files/Presidio Tasks/Bus Booking System"
git add backend/BusBooking.Api/
git commit -m "feat(backend): add folder skeleton, AppDbContext, exception middleware"
```

---

### Task 4: Configure appsettings + example file

**Files:**
- Modify: `backend/BusBooking.Api/appsettings.json`
- Modify: `backend/BusBooking.Api/appsettings.Development.json`
- Create: `backend/BusBooking.Api/appsettings.Development.example.json`

- [ ] **Step 1: Replace `appsettings.json` with production defaults + placeholders**

Open `backend/BusBooking.Api/appsettings.json` and replace its contents with:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=bus_booking;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Issuer": "bus-booking",
    "Audience": "bus-booking-clients",
    "SigningKey": "REPLACE_ME_WITH_32+_BYTE_RANDOM_STRING",
    "ExpiryMinutes": 60
  },
  "Cors": {
    "AllowedOrigins": [ "http://localhost:4200" ]
  },
  "RefundPolicy": {
    "Tiers": [
      { "MinHoursBeforeDeparture": 24, "RefundPercent": 80 },
      { "MinHoursBeforeDeparture": 12, "RefundPercent": 50 }
    ],
    "BlockBelowHours": 12
  },
  "Razorpay": {
    "KeyId": "",
    "KeySecret": ""
  },
  "Resend": {
    "ApiKey": "",
    "FromAddress": "onboarding@resend.dev",
    "FromName": "Bus Booking"
  },
  "AdminSeed": {
    "Email": "admin@busbooking.local",
    "Password": "ChangeMeOnFirstBoot!",
    "Name": "Platform Admin"
  },
  "SeatLock": {
    "DurationMinutes": 7
  }
}
```

- [ ] **Step 2: Write `appsettings.Development.json` (gitignored; safe to check in empty overrides shape, but .gitignore already excludes it)**

Create `backend/BusBooking.Api/appsettings.Development.json` with local dev values — this file is already ignored by `.gitignore`:

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=bus_booking;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "SigningKey": "dev-only-32-byte-signing-key-do-not-ship-xxxxxxxxxx"
  },
  "Razorpay": {
    "KeyId": "",
    "KeySecret": ""
  },
  "Resend": {
    "ApiKey": ""
  }
}
```

This file stays on your machine. If you already changed your local Postgres credentials, update `Username`/`Password` accordingly.

- [ ] **Step 3: Create the committed example file**

Create `backend/BusBooking.Api/appsettings.Development.example.json` with placeholder values:

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=bus_booking;Username=__FILL_ME_IN__;Password=__FILL_ME_IN__"
  },
  "Jwt": {
    "SigningKey": "__FILL_ME_IN__ (32+ random bytes, e.g. `openssl rand -base64 48`)"
  },
  "Razorpay": {
    "KeyId": "rzp_test___FILL_ME_IN__",
    "KeySecret": "__FILL_ME_IN__"
  },
  "Resend": {
    "ApiKey": "re___FILL_ME_IN__"
  }
}
```

- [ ] **Step 4: Commit**

```bash
cd "/Users/srimanikandanr/My Files/Presidio Tasks/Bus Booking System"
git add backend/BusBooking.Api/appsettings.json backend/BusBooking.Api/appsettings.Development.example.json
git commit -m "chore(backend): add production appsettings + Development example file"
```

`appsettings.Development.json` is not committed (covered by `.gitignore`).

---

### Task 5: Configure Program.cs (Serilog, Swagger, CORS, DbContext, middleware wiring)

**Files:**
- Modify: `backend/BusBooking.Api/Program.cs`

- [ ] **Step 1: Replace Program.cs entirely**

Open `backend/BusBooking.Api/Program.cs` and replace its contents with:

```csharp
using System.Text;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Infrastructure.Errors;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ──────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, cfg) => cfg
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/bus-booking-.log", rollingInterval: RollingInterval.Day, shared: true));

// ── Services ────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddPolicy("Frontend", p => p
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

// ── JWT Bearer ──────────────────────────────────────────────────
var jwtSection = builder.Configuration.GetSection("Jwt");
var signingKey = jwtSection["SigningKey"] ?? throw new InvalidOperationException("Jwt:SigningKey missing");
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// ── Pipeline ────────────────────────────────────────────────────
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { } // needed so the integration test project can reference it
```

- [ ] **Step 2: Verify the app builds**

```bash
cd backend
dotnet build
```

Expected: `Build succeeded. 0 Error(s).`

- [ ] **Step 3: Commit**

```bash
cd "/Users/srimanikandanr/My Files/Presidio Tasks/Bus Booking System"
git add backend/BusBooking.Api/Program.cs
git commit -m "feat(backend): wire Serilog, Swagger, CORS, DbContext, exception middleware"
```

---

### Task 6: Create initial EF Core migration and prove the DB connects

**Files:**
- Create: `backend/BusBooking.Api/Migrations/00000000000000_InitialCreate.cs` (auto-generated — don't hand-write)

- [ ] **Step 1: Ensure your local Postgres has the database and extensions**

Run these in your terminal (adjust `-U` if your superuser isn't `postgres`):
```bash
createdb bus_booking 2>/dev/null || echo "database already exists, moving on"
psql -d bus_booking -c "CREATE EXTENSION IF NOT EXISTS citext;"
psql -d bus_booking -c "CREATE EXTENSION IF NOT EXISTS pg_trgm;"
```

Expected:
```
CREATE EXTENSION
CREATE EXTENSION
```
(or "extension … already exists, skipping")

- [ ] **Step 2: Install the `dotnet-ef` global tool if missing**

```bash
dotnet tool install --global dotnet-ef --version 9.0.*
```

If it says "already installed", run:
```bash
dotnet tool update --global dotnet-ef --version 9.0.*
```

- [ ] **Step 3: Add the initial empty migration**

```bash
cd backend/BusBooking.Api
dotnet ef migrations add InitialCreate --output-dir Migrations
```

Expected: new `Migrations/YYYYMMDDHHMMSS_InitialCreate.cs` + `AppDbContextModelSnapshot.cs` files. The migration body will declare only the `citext` and `pg_trgm` extensions (because the DbContext currently has no entities).

- [ ] **Step 4: Apply the migration**

```bash
dotnet ef database update
```

Expected (truncated):
```
Applying migration 'YYYYMMDDHHMMSS_InitialCreate'.
Done.
```

If this fails with a connection error, fix `appsettings.Development.json`'s `ConnectionStrings.Default` before continuing.

- [ ] **Step 5: Verify the DB has an `__EFMigrationsHistory` table**

```bash
psql -d bus_booking -c "SELECT migration_id FROM \"__EFMigrationsHistory\";"
```

Expected: one row whose `migration_id` ends in `_InitialCreate`.

- [ ] **Step 6: Commit**

```bash
cd "/Users/srimanikandanr/My Files/Presidio Tasks/Bus Booking System"
git add backend/BusBooking.Api/Migrations/
git commit -m "feat(backend): add initial EF Core migration (extensions only)"
```

---

### Task 7: Create the xUnit integration test project

**Files:**
- Create: `backend/BusBooking.Api.Tests/BusBooking.Api.Tests.csproj`
- Create: `backend/BusBooking.Api.Tests/Usings.cs`
- Create: `backend/BusBooking.Api.Tests/Integration/HealthEndpointTests.cs` (failing first)

- [ ] **Step 1: Generate the test project and add it to the solution**

```bash
cd backend
dotnet new xunit -n BusBooking.Api.Tests -f net9.0
dotnet sln add BusBooking.Api.Tests/BusBooking.Api.Tests.csproj
cd BusBooking.Api.Tests
dotnet add reference ../BusBooking.Api/BusBooking.Api.csproj
dotnet add package Microsoft.AspNetCore.Mvc.Testing --version 9.0.*
dotnet add package FluentAssertions --version 6.*
```

Expected: `Build succeeded` on the reference adds.

- [ ] **Step 2: Replace the default `UnitTest1.cs` with proper using aliases**

```bash
rm backend/BusBooking.Api.Tests/UnitTest1.cs
```

Create `backend/BusBooking.Api.Tests/Usings.cs`:

```csharp
global using FluentAssertions;
global using Xunit;
```

- [ ] **Step 3: Write the failing health-endpoint test**

Create `backend/BusBooking.Api.Tests/Integration/HealthEndpointTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BusBooking.Api.Tests.Integration;

public class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_health_returns_200_with_status_ok()
    {
        var response = await _client.GetAsync("/api/v1/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();
        payload.Should().NotBeNull();
        payload!.Status.Should().Be("ok");
        payload.Service.Should().Be("bus-booking-api");
    }

    private record HealthResponse(string Status, string Service, string Version, DateTime TimestampUtc);
}
```

- [ ] **Step 4: Run the test and verify it FAILS**

```bash
cd backend
dotnet test BusBooking.Api.Tests/BusBooking.Api.Tests.csproj --filter HealthEndpointTests
```

Expected: the test fails, most likely with `Expected response.StatusCode to be OK, but found NotFound`. That's the correct failure — no controller yet.

- [ ] **Step 5: Commit the failing test**

```bash
cd "/Users/srimanikandanr/My Files/Presidio Tasks/Bus Booking System"
git add backend/BusBookingSystem.sln backend/BusBooking.Api.Tests/
git commit -m "test(backend): add failing integration test for /api/v1/health"
```

---

### Task 8: Implement the `/api/v1/health` endpoint

**Files:**
- Create: `backend/BusBooking.Api/Dtos/HealthResponseDto.cs`
- Create: `backend/BusBooking.Api/Controllers/HealthController.cs`

- [ ] **Step 1: Write the DTO**

Create `backend/BusBooking.Api/Dtos/HealthResponseDto.cs`:

```csharp
namespace BusBooking.Api.Dtos;

public record HealthResponseDto(string Status, string Service, string Version, DateTime TimestampUtc);
```

- [ ] **Step 2: Write the controller**

Create `backend/BusBooking.Api/Controllers/HealthController.cs`:

```csharp
using BusBooking.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.Api.Controllers;

[ApiController]
[Route("api/v1/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult<HealthResponseDto> Get()
    {
        return Ok(new HealthResponseDto(
            Status: "ok",
            Service: "bus-booking-api",
            Version: "0.1.0",
            TimestampUtc: DateTime.UtcNow));
    }
}
```

- [ ] **Step 3: Run the test and verify it PASSES**

```bash
cd backend
dotnet test BusBooking.Api.Tests/BusBooking.Api.Tests.csproj --filter HealthEndpointTests
```

Expected: `Passed!  - Failed: 0, Passed: 1, Skipped: 0, Total: 1`.

- [ ] **Step 4: Manually run the API and hit the endpoint**

Start the API:
```bash
cd backend/BusBooking.Api
dotnet run
```

Expected startup log mentions `Now listening on: http://localhost:5080`.

In a second terminal:
```bash
curl -s http://localhost:5080/api/v1/health | python3 -m json.tool
```

Expected:
```json
{
    "status": "ok",
    "service": "bus-booking-api",
    "version": "0.1.0",
    "timestampUtc": "2026-04-22T…Z"
}
```

Also open `http://localhost:5080/swagger` in a browser and confirm the `/api/v1/health` endpoint is listed. Stop the API with `Ctrl+C`.

- [ ] **Step 5: Commit**

```bash
cd "/Users/srimanikandanr/My Files/Presidio Tasks/Bus Booking System"
git add backend/BusBooking.Api/Controllers/HealthController.cs backend/BusBooking.Api/Dtos/HealthResponseDto.cs
git commit -m "feat(backend): implement GET /api/v1/health endpoint"
```

---

### Task 9: Scaffold the Angular 20 workspace

**Files:**
- Create: `frontend/bus-booking-web/` (full Angular workspace via CLI)

- [ ] **Step 1: Generate the Angular workspace**

Run from the repo root:
```bash
mkdir -p frontend
cd frontend
npx --yes @angular/cli@20 new bus-booking-web \
    --routing \
    --style=scss \
    --ssr=false \
    --standalone \
    --skip-git \
    --package-manager=npm
```

Answer `No` if prompted about Angular analytics. Answer `Yes` if prompted about zoneless (we want default zone for now — if only "yes/no" is offered, pick `No`).

Expected: command completes with `Successfully initialized git.` (from the CLI's internal git, which `--skip-git` actually prevents — that's fine), and `frontend/bus-booking-web/` is populated with Angular files.

- [ ] **Step 2: Smoke-test the app runs**

```bash
cd frontend/bus-booking-web
npm start
```

Expected: `Application bundle generation complete.` and `Local: http://localhost:4200/`. Open `http://localhost:4200` — you should see Angular's default welcome page. Stop with `Ctrl+C`.

- [ ] **Step 3: Commit**

```bash
cd "/Users/srimanikandanr/My Files/Presidio Tasks/Bus Booking System"
git add frontend/bus-booking-web/
git commit -m "feat(frontend): scaffold Angular 20 workspace"
```

---

### Task 10: Install Angular Material and Tailwind

**Files:**
- Modify: `frontend/bus-booking-web/package.json`
- Modify: `frontend/bus-booking-web/angular.json`
- Modify: `frontend/bus-booking-web/src/styles.scss`
- Create: `frontend/bus-booking-web/tailwind.config.js`
- Create: `frontend/bus-booking-web/postcss.config.js`

- [ ] **Step 1: Install Angular Material**

```bash
cd frontend/bus-booking-web
npx ng add @angular/material --skip-confirmation --defaults
```

Prompts and expected defaults: theme = `Azure/Blue`, typography = Yes, animations = `Include`. If it asks, accept them all.

Expected: `package.json` now has `@angular/material` and `@angular/cdk`. `src/styles.scss` has a theme import. `angular.json` has the Material icons font link.

- [ ] **Step 2: Install Tailwind v3**

```bash
cd frontend/bus-booking-web
npm install -D tailwindcss@^3 postcss@^8 autoprefixer@^10
npx tailwindcss init
```

Expected: `tailwind.config.js` appears.

- [ ] **Step 3: Write `tailwind.config.js`**

Replace the file contents:

```js
/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./src/**/*.{html,ts}"
  ],
  theme: {
    extend: {}
  },
  plugins: [],
  // Let Material's theme handle base styles; Tailwind only provides utilities.
  corePlugins: {
    preflight: false
  }
};
```

- [ ] **Step 4: Add the PostCSS config**

Create `frontend/bus-booking-web/postcss.config.js`:

```js
module.exports = {
  plugins: {
    tailwindcss: {},
    autoprefixer: {}
  }
};
```

- [ ] **Step 5: Add Tailwind directives to `src/styles.scss`**

Open `frontend/bus-booking-web/src/styles.scss`. At the very TOP of the file (above the Material theme imports that `ng add` inserted), add:

```scss
@tailwind base;
@tailwind components;
@tailwind utilities;
```

(Do not delete the Material theme block; the file should end up with Tailwind directives first, then whatever Angular Material added.)

- [ ] **Step 6: Run the app and verify both work together**

```bash
cd frontend/bus-booking-web
npm start
```

Expected: compiles with no errors. The welcome page still renders (Material font loaded means things look slightly different — that's fine). Stop with `Ctrl+C`.

- [ ] **Step 7: Commit**

```bash
cd "/Users/srimanikandanr/My Files/Presidio Tasks/Bus Booking System"
git add frontend/bus-booking-web/
git commit -m "feat(frontend): add Angular Material + Tailwind v3"
```

---

### Task 11: Set up environments, folder structure, and auth interceptor shell

**Files:**
- Create: `frontend/bus-booking-web/src/environments/environment.ts`
- Create: `frontend/bus-booking-web/src/environments/environment.development.ts`
- Modify: `frontend/bus-booking-web/angular.json` (add fileReplacements)
- Create: `frontend/bus-booking-web/src/app/core/auth/auth-token.store.ts`
- Create: `frontend/bus-booking-web/src/app/core/auth/auth.interceptor.ts`
- Create (empty `.gitkeep` markers only): `core/api/`, `core/http/`, `shared/components/`, `shared/pipes/`, `features/public/`, `features/auth/`, `features/customer/`, `features/operator/`, `features/admin/`

- [ ] **Step 1: Create the environment files**

Create `frontend/bus-booking-web/src/environments/environment.ts`:

```ts
export const environment = {
  production: true,
  apiBaseUrl: '/api/v1'
};
```

Create `frontend/bus-booking-web/src/environments/environment.development.ts`:

```ts
export const environment = {
  production: false,
  apiBaseUrl: 'http://localhost:5080/api/v1'
};
```

- [ ] **Step 2: Wire environment file-replacement in `angular.json`**

Open `frontend/bus-booking-web/angular.json`. Inside `projects.bus-booking-web.architect.build.configurations.development`, add or replace with:

```json
"development": {
  "optimization": false,
  "extractLicenses": false,
  "sourceMap": true,
  "namedChunks": true,
  "fileReplacements": [
    {
      "replace": "src/environments/environment.ts",
      "with": "src/environments/environment.development.ts"
    }
  ]
}
```

If a `production` configuration already exists and does not include `fileReplacements`, leave it as-is (it will use `environment.ts`).

- [ ] **Step 3: Create the folder skeleton**

```bash
cd frontend/bus-booking-web/src/app
mkdir -p core/auth core/api core/http shared/components shared/pipes features/public features/auth features/customer features/operator features/admin
touch core/api/.gitkeep core/http/.gitkeep shared/components/.gitkeep shared/pipes/.gitkeep features/public/.gitkeep features/auth/.gitkeep features/customer/.gitkeep features/operator/.gitkeep features/admin/.gitkeep
```

- [ ] **Step 4: Add the auth token store + interceptor shell**

The token store holds the JWT in memory (with a `localStorage` mirror so a page refresh keeps the user logged in until expiry). The interceptor attaches the token when present. Both are shells — real login flows come in M1, but the plumbing needs to exist so later tasks can slot into it.

Create `frontend/bus-booking-web/src/app/core/auth/auth-token.store.ts`:

```ts
import { Injectable, signal } from '@angular/core';

const STORAGE_KEY = 'bb.auth.token';

@Injectable({ providedIn: 'root' })
export class AuthTokenStore {
  readonly token = signal<string | null>(this.loadInitial());

  set(token: string | null): void {
    this.token.set(token);
    if (token) {
      localStorage.setItem(STORAGE_KEY, token);
    } else {
      localStorage.removeItem(STORAGE_KEY);
    }
  }

  clear(): void {
    this.set(null);
  }

  private loadInitial(): string | null {
    try {
      return localStorage.getItem(STORAGE_KEY);
    } catch {
      return null;
    }
  }
}
```

Create `frontend/bus-booking-web/src/app/core/auth/auth.interceptor.ts`:

```ts
import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthTokenStore } from './auth-token.store';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = inject(AuthTokenStore).token();
  if (!token) {
    return next(req);
  }
  return next(req.clone({
    setHeaders: { Authorization: `Bearer ${token}` }
  }));
};
```

- [ ] **Step 5: Verify the app still builds with the dev configuration**

```bash
cd frontend/bus-booking-web
npm start
```

Expected: compiles. Stop with `Ctrl+C`.

- [ ] **Step 6: Commit**

```bash
cd "/Users/srimanikandanr/My Files/Presidio Tasks/Bus Booking System"
git add frontend/bus-booking-web/
git commit -m "feat(frontend): add environments, folder skeleton, and auth interceptor shell"
```

---

### Task 12: Write failing unit test for `HealthApiService`

**Files:**
- Create: `frontend/bus-booking-web/src/app/core/api/health.api.spec.ts`

- [ ] **Step 1: Write the failing test**

Create `frontend/bus-booking-web/src/app/core/api/health.api.spec.ts`:

```ts
import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { HealthApiService, HealthResponse } from './health.api';
import { environment } from '../../../environments/environment';

describe('HealthApiService', () => {
  let service: HealthApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        HealthApiService
      ]
    });
    service = TestBed.inject(HealthApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('GETs the health endpoint and returns the payload', () => {
    const expected: HealthResponse = {
      status: 'ok',
      service: 'bus-booking-api',
      version: '0.1.0',
      timestampUtc: '2026-04-22T10:00:00.000Z'
    };

    let received: HealthResponse | undefined;
    service.ping().subscribe((r) => (received = r));

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/health`);
    expect(req.request.method).toBe('GET');
    req.flush(expected);

    expect(received).toEqual(expected);
  });
});
```

- [ ] **Step 2: Run the test and verify it FAILS**

```bash
cd frontend/bus-booking-web
npm test -- --watch=false --browsers=ChromeHeadless
```

Expected: the suite reports a compile error — `Module not found: './health.api'`. That's the failing state we want.

If you don't have Chrome installed, replace `ChromeHeadless` with whatever browser Karma can find (Chromium, Firefox, Edge).

- [ ] **Step 3: Commit the failing test**

```bash
cd "/Users/srimanikandanr/My Files/Presidio Tasks/Bus Booking System"
git add frontend/bus-booking-web/src/app/core/api/health.api.spec.ts
git commit -m "test(frontend): add failing unit test for HealthApiService"
```

---

### Task 13: Implement `HealthApiService` and verify the test passes

**Files:**
- Create: `frontend/bus-booking-web/src/app/core/api/health.api.ts`

- [ ] **Step 1: Write the minimal service**

Create `frontend/bus-booking-web/src/app/core/api/health.api.ts`:

```ts
import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface HealthResponse {
  status: string;
  service: string;
  version: string;
  timestampUtc: string;
}

@Injectable({ providedIn: 'root' })
export class HealthApiService {
  private readonly http = inject(HttpClient);

  ping(): Observable<HealthResponse> {
    return this.http.get<HealthResponse>(`${environment.apiBaseUrl}/health`);
  }
}
```

- [ ] **Step 2: Run the test and verify it PASSES**

```bash
cd frontend/bus-booking-web
npm test -- --watch=false --browsers=ChromeHeadless
```

Expected: `Executed 1 of 1 SUCCESS` (ignore any other App-component tests that may also run and pass).

- [ ] **Step 3: Commit**

```bash
cd "/Users/srimanikandanr/My Files/Presidio Tasks/Bus Booking System"
git add frontend/bus-booking-web/src/app/core/api/health.api.ts
git commit -m "feat(frontend): add HealthApiService"
```

---

### Task 14: Wire `provideHttpClient` in `app.config.ts`

**Files:**
- Modify: `frontend/bus-booking-web/src/app/app.config.ts`

- [ ] **Step 1: Replace the file**

Open `frontend/bus-booking-web/src/app/app.config.ts` and replace its contents with:

```ts
import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { routes } from './app.routes';
import { authInterceptor } from './core/auth/auth.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideAnimations(),
    provideHttpClient(withInterceptors([authInterceptor]))
  ]
};
```

- [ ] **Step 2: Verify the app still builds**

```bash
cd frontend/bus-booking-web
npm start
```

Expected: compiles. Stop with `Ctrl+C`.

- [ ] **Step 3: Commit**

```bash
cd "/Users/srimanikandanr/My Files/Presidio Tasks/Bus Booking System"
git add frontend/bus-booking-web/src/app/app.config.ts
git commit -m "feat(frontend): provide HttpClient with auth interceptor + animations in app.config"
```

---

### Task 15: Build the Home landing component that pings `/health`

**Files:**
- Create: `frontend/bus-booking-web/src/app/features/public/home/home.component.ts`
- Create: `frontend/bus-booking-web/src/app/features/public/home/home.component.html`
- Create: `frontend/bus-booking-web/src/app/features/public/home/home.component.scss`
- Modify: `frontend/bus-booking-web/src/app/app.routes.ts`
- Modify: `frontend/bus-booking-web/src/app/app.component.html`

- [ ] **Step 1: Write the component**

Create `frontend/bus-booking-web/src/app/features/public/home/home.component.ts`:

```ts
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { DatePipe } from '@angular/common';
import { HealthApiService, HealthResponse } from '../../../core/api/health.api';

type Status = 'loading' | 'ok' | 'failed';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [MatButtonModule, MatCardModule, DatePipe],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent implements OnInit {
  private readonly api = inject(HealthApiService);

  readonly status = signal<Status>('loading');
  readonly payload = signal<HealthResponse | null>(null);
  readonly statusLabel = computed(() => {
    const s = this.status();
    if (s === 'loading') return 'checking…';
    if (s === 'ok') return 'backend online';
    return 'backend unreachable';
  });

  ngOnInit(): void {
    this.ping();
  }

  ping(): void {
    this.status.set('loading');
    this.api.ping().subscribe({
      next: (r) => {
        this.payload.set(r);
        this.status.set('ok');
      },
      error: () => {
        this.payload.set(null);
        this.status.set('failed');
      }
    });
  }
}
```

- [ ] **Step 2: Write the template**

Create `frontend/bus-booking-web/src/app/features/public/home/home.component.html`:

```html
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

- [ ] **Step 3: Add a stylesheet stub**

Create `frontend/bus-booking-web/src/app/features/public/home/home.component.scss` as an empty file:

```scss
// intentionally empty — Tailwind utilities do the work
```

- [ ] **Step 4: Wire the route**

Open `frontend/bus-booking-web/src/app/app.routes.ts` and replace its contents with:

```ts
import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/public/home/home.component').then(m => m.HomeComponent)
  },
  { path: '**', redirectTo: '' }
];
```

- [ ] **Step 5: Simplify `app.component.html`**

Open `frontend/bus-booking-web/src/app/app.component.html` and replace its contents with just:

```html
<router-outlet></router-outlet>
```

- [ ] **Step 6: Run the frontend and backend together**

Terminal 1:
```bash
cd backend/BusBooking.Api
dotnet run
```

Terminal 2:
```bash
cd frontend/bus-booking-web
npm start
```

Open `http://localhost:4200` in a browser. Expected:
- The page shows "Bus Booking System — Foundation milestone".
- The status indicator is green and reads "backend online".
- The card shows `Service: bus-booking-api`, `Version: 0.1.0`, and a timestamp.
- Clicking **Check again** re-fetches successfully.
- With the backend stopped, reloading shows red "backend unreachable" — confirms CORS error handling is functional.

Stop both servers with `Ctrl+C`.

- [ ] **Step 7: Commit**

```bash
cd "/Users/srimanikandanr/My Files/Presidio Tasks/Bus Booking System"
git add frontend/bus-booking-web/src/
git commit -m "feat(frontend): add Home landing that pings /api/v1/health"
```

---

### Task 16: Run the full test suite and push

**Files:** (no file edits — verification only)

- [ ] **Step 1: Run backend tests**

```bash
cd backend
dotnet test
```

Expected: `Passed! - Failed: 0, Passed: 1, Skipped: 0, Total: 1`.

- [ ] **Step 2: Run frontend tests**

```bash
cd frontend/bus-booking-web
npm test -- --watch=false --browsers=ChromeHeadless
```

Expected: `Executed N of N SUCCESS` for some N ≥ 1. Zero failures.

- [ ] **Step 3: Push the milestone to GitHub**

```bash
cd "/Users/srimanikandanr/My Files/Presidio Tasks/Bus Booking System"
git push origin main
```

Expected: the commits from Tasks 1–16 all appear on GitHub under `main`.

---

### Task 17: Write the setup README

**Files:**
- Create: `README.md`

- [ ] **Step 1: Write the README**

Create `README.md` in the repo root:

```markdown
# Bus Booking System

A full-stack bus-booking web app. Stack: Angular 20 + .NET 9 + PostgreSQL.

This repository is being built milestone by milestone. See
`docs/superpowers/specs/2026-04-22-bus-booking-system-design.md` for the full
design and `docs/superpowers/plans/` for the per-milestone implementation
plans.

## Prerequisites

| Tool          | Version           | Install                                          |
|---------------|-------------------|--------------------------------------------------|
| .NET SDK      | 9.x               | https://dotnet.microsoft.com/download            |
| Node.js + npm | Node ≥20, npm ≥10 | https://nodejs.org                               |
| PostgreSQL    | ≥14               | `brew install postgresql@16` (macOS) or distro   |
| dotnet-ef     | 9.x               | `dotnet tool install --global dotnet-ef`         |

## First-time setup

**1. Clone and install**

```bash
git clone https://github.com/Sri-Manikandan/Genspark-Training.git
cd "Genspark-Training"
```

**2. Create the database and enable extensions**

```bash
createdb bus_booking
psql -d bus_booking -c "CREATE EXTENSION IF NOT EXISTS citext;"
psql -d bus_booking -c "CREATE EXTENSION IF NOT EXISTS pg_trgm;"
```

**3. Configure backend secrets**

Copy the example file and fill in values for your machine:
```bash
cp backend/BusBooking.Api/appsettings.Development.example.json backend/BusBooking.Api/appsettings.Development.json
# Then edit backend/BusBooking.Api/appsettings.Development.json and set:
#  - ConnectionStrings.Default → your Postgres username/password
#  - Jwt.SigningKey            → any 32+ byte random string (e.g. openssl rand -base64 48)
#  - Razorpay.* + Resend.*     → leave blank until the relevant milestone
```

**4. Apply migrations**

```bash
cd backend/BusBooking.Api
dotnet ef database update
```

**5. Install frontend deps**

```bash
cd ../../frontend/bus-booking-web
npm install
```

## Running

Open two terminals:

**Terminal 1 — backend**
```bash
cd backend/BusBooking.Api
dotnet run
# → http://localhost:5080 (Swagger at /swagger)
```

**Terminal 2 — frontend**
```bash
cd frontend/bus-booking-web
npm start
# → http://localhost:4200
```

Visit `http://localhost:4200` — you should see a page saying "backend online".

## Testing

Backend: `cd backend && dotnet test`
Frontend: `cd frontend/bus-booking-web && npm test -- --watch=false`

## Project layout

```
backend/
  BusBookingSystem.sln
  BusBooking.Api/          main Web API project
  BusBooking.Api.Tests/    xUnit + FluentAssertions
frontend/
  bus-booking-web/         Angular 20 workspace
docs/
  superpowers/
    specs/                 approved design docs
    plans/                 per-milestone implementation plans
```
```

- [ ] **Step 2: Commit and push**

```bash
cd "/Users/srimanikandanr/My Files/Presidio Tasks/Bus Booking System"
git add README.md
git commit -m "docs: add project README with setup + run instructions"
git push origin main
```

Expected: README appears in the GitHub repo.

---

## M0 completion criteria (verify all before moving on to M1)

- [ ] `dotnet build` in `backend/` succeeds with 0 errors, 0 warnings.
- [ ] `dotnet test` in `backend/` passes ≥1 integration test (`HealthEndpointTests`).
- [ ] `dotnet ef database update` runs cleanly against the local Postgres.
- [ ] `psql -d bus_booking -c "\\dx"` shows both `citext` and `pg_trgm` installed.
- [ ] `npm start` in `frontend/bus-booking-web/` serves on `http://localhost:4200`.
- [ ] `npm test -- --watch=false` in the frontend passes ≥1 unit test (`HealthApiService`).
- [ ] Opening `http://localhost:4200` with the API running shows the green "backend online" status.
- [ ] All commits on `main` are pushed to `https://github.com/Sri-Manikandan/Genspark-Training`.
- [ ] `README.md` exists at the repo root and its instructions work on a clean clone.

When all boxes are checked, tell me "M0 done" and I'll write the plan for M1 (auth + roles + admin seed).
