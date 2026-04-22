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
