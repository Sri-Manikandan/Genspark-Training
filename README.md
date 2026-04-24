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

## Demo script

With `DemoSeed:Enabled=true` in `appsettings.Development.json` (the default in
`appsettings.Development.example.json`), a fresh `dotnet run` populates:

- Cities: **Bangalore** (Karnataka), **Chennai** (Tamil Nadu).
- Route: Bangalore → Chennai, 350 km.
- Operator: `operator@demo.local` / `Operator123!` — roles `customer + operator`,
  offices at both cities, one approved 40-seater bus ("Demo Express", reg `KA01DEMO1234`),
  one daily schedule 09:00 → 14:00 at ₹500/seat for the next 60 days.
- Customer: `customer@demo.local` / `Customer123!`.
- Admin: `admin@busbooking.local` / `ChangeMeOnFirstBoot!` (seeded separately).

End-to-end walkthrough:

1. Visit `http://localhost:4200`, search **Bangalore → Chennai** for today or
   any date in the next 60 days. A single "Demo Express" trip appears.
2. Click the trip, pick two available seats. You're prompted to log in →
   sign in as the **customer**.
3. Enter passenger details → pay with Razorpay test card `4111 1111 1111 1111`,
   any future expiry, any CVV.
4. Confirmation page shows the booking code. The PDF ticket downloads and (if
   `Resend:ApiKey` is set and your address matches the Resend account owner)
   a confirmation email arrives.
5. Open **My Bookings** → click the new row → **Cancel booking**. The dialog
   shows the tier label (e.g. "80% refund (24h or more before departure)"),
   confirm cancel. A refund email arrives.
6. Log out, log back in as **admin**. Go to **Admin console → Operators**,
   click **Disable** on "Demo Operator". The single demo bus is retired; any
   future confirmed bookings flip to `cancelled_by_operator` with pending
   refunds; affected customers receive a cancellation email.

To reset: `dropdb bus_booking && createdb bus_booking && psql -d bus_booking
-c "CREATE EXTENSION IF NOT EXISTS citext; CREATE EXTENSION IF NOT EXISTS
pg_trgm;" && dotnet ef database update` (run from `backend/BusBooking.Api`),
then restart the API.

## Troubleshooting

- **Resend emails don't arrive.** On the free tier without a verified domain,
  Resend only delivers to the account-owner address. Either verify a domain
  at https://resend.com/domains or set `Resend:FromAddress` and your test
  `customer@demo.local` email to the same address on the Resend account.
- **Razorpay modal shows "Invalid key".** The test key in
  `appsettings.Development.json` (`Razorpay:KeyId` / `Razorpay:KeySecret`)
  must start with `rzp_test_`. Copy both from the Razorpay test dashboard.
- **Search returns no trips.** The seeded schedule is valid for today +60
  days. If your system date drifts outside that window, either restart the
  API (the 60-day window re-anchors to `now`) or add a new schedule through
  the operator portal.
- **`dotnet ef database update` fails with "extension citext does not exist".**
  Run `psql -d bus_booking -c "CREATE EXTENSION IF NOT EXISTS citext;"` as
  the Postgres superuser (not `bus_user`).
- **`DemoSeed:Enabled=true` but no demo data appears.** The seeder only runs
  in `Development`. Verify with `ASPNETCORE_ENVIRONMENT=Development dotnet run`.

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
