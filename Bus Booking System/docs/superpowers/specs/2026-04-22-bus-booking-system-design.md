# Bus Booking System — Design Spec

**Date:** 2026-04-22
**Scope tier:** Balanced (≈1–2 weeks) — everything in the acceptance summary (§12 of the requirements doc) plus revenue views, platform-fee config, and operator-disable cascade with email. Optional enhancements from §11 of the requirements doc are out of scope.

---

## 1. Context

Training case study: a full-stack bus-booking web app with three roles (customer, operator, admin). Local-only deployment. Mandated stack: Angular frontend, .NET Web API backend, PostgreSQL database.

## 2. Decisions locked in during brainstorming

| # | Area | Decision |
|---|------|---|
| 1 | Scope tier | Balanced — acceptance-summary features + revenue, platform-fee, cascade. No round-trip, coupons, alternative-bus suggestions, or polished-UI extras. |
| 2 | Authentication | Email + password; bcrypt (work factor 11); HS256 JWT, 1-hour expiry, no refresh tokens. Admin seeded via migration, not self-register. Customer self-registers; anonymous browsing/search allowed; login prompted at booking. Operator role is acquired by an existing customer submitting a request that an admin approves — the user then holds both `customer` and `operator` roles. |
| 3 | Seat-locking concurrency | Postgres `seat_locks` table with `UNIQUE(trip_id, seat_number)`. 7-minute window. Background `IHostedService` cleans expired rows every 60s; reads also filter by `expires_at > now()` to handle the cleanup lag. |
| 4 | Payment | Razorpay test mode. Orders API + checkout.js on the frontend + signature verify on the backend. `RAZORPAY_KEY_ID` + `RAZORPAY_KEY_SECRET` in config. |
| 5 | Email | Resend HTTP API (not SMTP). `RESEND_API_KEY` in config. Free-tier-without-domain caveat: only delivers to the account-owner address unless a domain is verified — flagged in the README. |
| 6 | Fuzzy city search | Postgres `pg_trgm` extension + GIN index on `cities.name`. Debounced 200ms on the client. |
| 7 | Angular | Angular 20, standalone components, new control flow, Signals for state. |
| 8 | UI | Angular Material (dialogs/forms/tables/stepper) + Tailwind (layout/spacing). Single primary color. |
| 9 | .NET | .NET 9, traditional layered Web API (Controllers → Services → Repositories → EF Core DbContext + entities). FluentValidation, Serilog, Swagger. |
| 10 | Repo layout | Single folder with `/backend`, `/frontend`, `/docs`. No Docker — user has a local Postgres. |
| 11 | Refund policy | ≥24h before departure → 80% refund. 12–24h → 50%. <12h → 0% and cancellation blocked (422). Values in `appsettings.json` under `RefundPolicy`. |
| 12 | Razorpay refunds | Call the Razorpay refund API for realism. Test mode, so no real money moves but the flow is exercised end-to-end. |

## 3. Architecture

```
┌──────────────────────────────────────────────────────────────────────┐
│                        Angular 20 SPA (port 4200)                    │
│   Public site (home, search, bus list, seat select)                  │
│   Customer area (login, booking, history, profile)                   │
│   Operator dashboard (buses, schedules, bookings, revenue)           │
│   Admin console (operator approvals, bus approvals, routes,          │
│                  locations, platform-fee, revenue)                   │
└───────────────────────────┬──────────────────────────────────────────┘
                            │ HTTPS / JSON (JWT Bearer)
                            ▼
┌──────────────────────────────────────────────────────────────────────┐
│             .NET 9 Web API  (port 5080, Kestrel)                     │
│  Controllers  →  Services  →  Repositories  →  EF Core DbContext     │
│  Cross-cutting: JwtAuth middleware, Exception middleware,            │
│                 Swagger, FluentValidation, Serilog                   │
│  External clients: RazorpayClient, ResendClient, PdfTicketGenerator  │
│  Background: SeatLockCleanupService (IHostedService, 60s)            │
└───────────┬──────────────────────────┬──────────────────────────┬────┘
            │                          │                          │
            ▼                          ▼                          ▼
   ┌────────────────┐           ┌─────────────┐           ┌─────────────┐
   │  PostgreSQL    │           │  Razorpay   │           │   Resend    │
   │  (local, 5432) │           │  (test key) │           │  (HTTP API) │
   │  db: bus_      │           │  Orders +   │           │  Booking /  │
   │      booking   │           │  Signature  │           │  cancel /   │
   │  pg_trgm ext.  │           │  verify     │           │  refund     │
   └────────────────┘           └─────────────┘           │  mails      │
                                                         └─────────────┘
```

### 3.1 Repo layout

```
BusBookingSystem/
├── backend/
│   └── BusBooking.Api/
│       ├── Controllers/
│       ├── Services/
│       ├── Repositories/
│       ├── Models/                 # EF entities
│       ├── Dtos/
│       ├── Infrastructure/         # DbContext, auth, razorpay, resend, pdf
│       ├── Validators/             # FluentValidation
│       ├── Migrations/
│       └── Program.cs
├── frontend/
│   └── bus-booking-web/
│       ├── src/app/core/           # auth, interceptors, guards
│       ├── src/app/shared/         # components, pipes
│       ├── src/app/features/       # public, customer, operator, admin
│       └── src/environments/
├── docs/
│   └── superpowers/specs/
├── README.md
└── .gitignore
```

### 3.2 Run loop

1. `createdb bus_booking && psql -d bus_booking -c "CREATE EXTENSION IF NOT EXISTS pg_trgm;"`
2. `cd backend/BusBooking.Api && dotnet ef database update && dotnet run`
3. `cd frontend/bus-booking-web && npm install && ng serve`

## 4. Data model

All tables in PostgreSQL; EF Core entities in PascalCase, table names `snake_case`.

### 4.1 Entities

| Table | Purpose |
|---|---|
| `users` | `id (uuid pk), name, email (citext unique), password_hash, phone (nullable), created_at, is_active, operator_disabled_at (nullable)`. `operator_disabled_at` lets an admin disable the operator role without touching the customer role. |
| `user_roles` | `(user_id, role)` composite PK. `role ∈ ('customer','operator','admin')`. Multi-role supported. |
| `operator_requests` | `id, user_id, status ∈ ('pending','approved','rejected'), company_name, requested_at, reviewed_at, reviewed_by_admin_id, reject_reason` |
| `operator_offices` | `id, operator_user_id, city_id, address_line, phone, is_active`. `UNIQUE(operator_user_id, city_id)` |
| `cities` | `id, name (citext), state, is_active`. GIN index `USING gin (name gin_trgm_ops)` |
| `routes` | `id, source_city_id, destination_city_id, distance_km (nullable), is_active`. `UNIQUE(source_city_id, destination_city_id)` |
| `buses` | `id, operator_user_id, registration_number (citext unique), bus_name, bus_type ∈ ('seater','sleeper','semi_sleeper'), capacity, approval_status ∈ ('pending','approved','rejected'), operational_status ∈ ('active','under_maintenance','retired'), created_at, approved_at, approved_by_admin_id, reject_reason` |
| `seat_definitions` | `id, bus_id, seat_number (e.g. 'A1'), row_index, column_index, seat_category ∈ ('regular')`. `UNIQUE(bus_id, seat_number)` |
| `bus_schedules` | `id, bus_id, route_id, departure_time, arrival_time, fare_per_seat (decimal(10,2)), valid_from, valid_to, days_of_week (int bitmask Mon..Sun), is_active`. Index `(route_id, is_active)` |
| `bus_trips` | `id, schedule_id, trip_date, status ∈ ('scheduled','cancelled','completed'), cancel_reason`. `UNIQUE(schedule_id, trip_date)`. Materialised on first search for that date. |
| `seat_locks` | `id, trip_id, seat_number, session_id (uuid), user_id (nullable), created_at, expires_at`. **`UNIQUE(trip_id, seat_number)`** — the concurrency guarantee. |
| `bookings` | `id, booking_code (unique human-readable), trip_id, user_id, total_fare, platform_fee, total_amount, seat_count, status ∈ ('pending_payment','confirmed','cancelled','cancelled_by_operator','completed'), cancellation_reason, cancelled_at, refund_amount, refund_status, created_at, confirmed_at` |
| `booking_seats` | `id, booking_id, seat_number, passenger_name, passenger_age, passenger_gender ∈ ('male','female','other')`. `UNIQUE(booking_id, seat_number)` |
| `payments` | `id, booking_id, razorpay_order_id, razorpay_payment_id, razorpay_signature, amount, currency ('INR'), status ∈ ('created','captured','failed','refunded'), created_at, captured_at, refunded_at, raw_response (jsonb)` |
| `refunds` | `id, booking_id, amount, reason ∈ ('user_cancel','operator_disabled','trip_cancelled'), razorpay_refund_id, status ∈ ('pending','processed','failed'), processed_at` |
| `notifications` | `id, user_id, type ∈ ('booking_confirmed','cancelled','refund','operator_approved','operator_disabled'), channel ∈ ('email'), to_address, subject, resend_message_id, status ∈ ('sent','failed'), created_at, error` |
| `platform_fee_config` | `id, fee_type ∈ ('fixed','percent'), value (decimal), effective_from, created_by_admin_id`. History table; most recent `effective_from <= now()` is active. |
| `audit_log` | `id, actor_user_id, action, target_type, target_id, metadata (jsonb), created_at` |

### 4.2 Relationships

```
users ──┬── user_roles
        ├── operator_requests
        ├── operator_offices
        └── buses (as operator)
routes ── cities (src, dst)
buses ── seat_definitions (1..n)
      └── bus_schedules (1..n)
            └── bus_trips (1..n per date)
                  ├── seat_locks (transient)
                  └── bookings
                        ├── booking_seats
                        ├── payments
                        └── refunds (0..1)
```

### 4.3 Design rules

- `bus_trips` is materialised on demand. A search for a date finds or creates trip rows for every matching active schedule on that date.
- `bookings.total_fare` + `bookings.platform_fee` are snapshotted at booking time so later changes to fare or fee config do not rewrite history.
- Pickup / drop addresses are not columns. They are derived at read time from `operator_offices` where `operator_user_id = bus.operator_user_id` and `city_id = route.source_city_id` (for pickup) or `city_id = route.destination_city_id` (for drop). If the operator has no office in a route city, schedule creation for that route is blocked server-side.
- `seat_locks.user_id` is nullable. Anonymous users hold a lock via `session_id` (browser-generated uuid, passed back on every call). On booking, `session_id` must match the lock row; the booking itself carries `user_id` (post-login), which is how the lock gets associated with a user. No separate "claim lock" endpoint is needed.
- Buses are soft-retired (`operational_status='retired'`) rather than deleted, so booking history stays intact. Cities / routes use `is_active` flags. Bookings are never deleted; only status-transitioned.
- Admin disable cascade is one DB transaction: buses retired, future confirmed bookings → `cancelled_by_operator`, refunds queued, notifications queued. Email / Razorpay refund calls happen after commit.

## 5. API surface

All routes under `/api/v1`. JSON. Bearer-token auth unless marked `[AllowAnonymous]`.

### 5.1 Public

| Method | Path | Purpose |
|---|---|---|
| POST | `/auth/register` | Customer signup. |
| POST | `/auth/login` | Returns JWT with `roles[]`. |
| GET | `/cities?q=...` | Fuzzy autocomplete (pg_trgm). |
| GET | `/search?src=&dst=&date=` | Materialises trips, returns `[{trip_id, bus, departure, arrival, fare, seats_left, operator_name}]`. |
| GET | `/trips/{id}` | Trip detail incl. pickup/drop derived from operator offices. |
| GET | `/trips/{id}/seats` | Seat layout + per-seat status (available/locked/booked). |
| POST | `/trips/{id}/seat-locks` | Body: `{session_id, seats:[]}`. Returns `{lock_id, expires_at}`. 409 on conflict. |
| DELETE | `/seat-locks/{id}` | User-initiated release before payment. |

### 5.2 Customer (`[Authorize(Roles="customer")]`)

| Method | Path | Purpose |
|---|---|---|
| GET/PUT | `/me` | Profile. |
| POST | `/me/become-operator` | Creates `operator_requests` row. |
| POST | `/bookings` | Validates lock, creates `booking (pending_payment)`, creates Razorpay order. |
| POST | `/bookings/{id}/verify-payment` | Idempotent. Verifies HMAC signature, flips to confirmed, fires email + PDF. |
| GET | `/bookings?filter=upcoming|past|cancelled` | Listing. |
| GET | `/bookings/{id}` | Detail. |
| GET | `/bookings/{id}/ticket` | PDF download. |
| POST | `/bookings/{id}/cancel` | Applies refund policy, calls Razorpay refund, emails. |

### 5.3 Operator (`[Authorize(Roles="operator")]`)

| Method | Path | Purpose |
|---|---|---|
| GET/POST/DELETE | `/operator/offices` | Operator's city presences. |
| GET/POST | `/operator/buses` | Bus list + add. Seat definitions auto-generated from rows × cols. |
| PATCH | `/operator/buses/{id}/status` | Toggle `active ↔ under_maintenance`. |
| DELETE | `/operator/buses/{id}` | Soft retire. Blocked if future bookings exist. |
| GET/POST/PATCH/DELETE | `/operator/schedules` | Schedule CRUD. Rejected if operator has no offices at both route cities. |
| GET | `/operator/bookings` | Filterable by bus / date. |
| GET | `/operator/revenue` | Groups by bus, filterable by date range. |

### 5.4 Admin (`[Authorize(Roles="admin")]`)

| Method | Path | Purpose |
|---|---|---|
| GET | `/admin/operator-requests` | List (filter by status). |
| POST | `/admin/operator-requests/{id}/approve` | Grants operator role, emails user. |
| POST | `/admin/operator-requests/{id}/reject` | `{reason}`. |
| GET | `/admin/buses?status=pending` | Approval queue. |
| POST | `/admin/buses/{id}/approve` | Marks approved. |
| POST | `/admin/buses/{id}/reject` | `{reason}`. |
| GET | `/admin/operators` | List with enabled/disabled state. |
| POST | `/admin/operators/{id}/disable` | Cascades: retire buses, cancel future bookings, queue refunds, queue notifications. |
| POST | `/admin/operators/{id}/enable` | Reverses the role/flag only; does not reinstate cancelled bookings. |
| GET/POST/PATCH | `/admin/cities` | City CRUD. |
| GET/POST/PATCH | `/admin/routes` | Route CRUD. |
| GET/PUT | `/admin/platform-fee` | Read active config / insert new historical row. |
| GET | `/admin/revenue` | GMV + platform-fee income, filter by date range. |
| GET | `/admin/bookings` | Cross-operator listing. |

### 5.5 Cross-cutting behaviour

- **Error envelope:** `{ error: { code, message, correlationId, details? } }`.
- **Status codes:** 400 validation, 401 missing/invalid JWT, 403 wrong role, 404, 409 conflict, 422 business-rule failure (e.g. `CANCEL_WINDOW_CLOSED`, `OPERATOR_NOT_APPROVED`, `NO_OFFICE_AT_CITY`), 500 with correlationId.
- **Pagination:** `?page=1&page_size=20`, max 100.
- **Idempotency:** `/bookings/{id}/verify-payment` — re-runs return the already-confirmed booking; mismatched payment id returns 409 `PAYMENT_MISMATCH`.
- **Swagger:** `/swagger` in Development only.
- **Timestamps:** ISO-8601 UTC; frontend renders in user's local timezone.

## 6. Frontend

### 6.1 Conventions

- Angular 20 standalone components, `@if` / `@for` control flow.
- State via Signals (`signal`, `computed`, `effect`). No NgRx.
- Reactive Forms + FluentValidation-equivalent validators.
- HTTP: functional `authInterceptor` + `errorInterceptor`.
- Routing: lazy-loaded feature routes + `canMatch` role guards.
- Angular Material + Tailwind. Single primary color in the Material theme.

### 6.2 Folder structure

```
src/app/
├── app.routes.ts
├── app.config.ts
├── core/
│   ├── auth/ (service, store, interceptor, role.guard)
│   ├── http/error.interceptor.ts
│   └── api/ (auth, search, trips, bookings, operator, admin).api.ts
├── shared/
│   ├── components/ (city-autocomplete, seat-map, countdown-timer, empty-state, page-header)
│   ├── pipes/ (inr, relative-date)
│   └── ui/ (common Material re-exports)
└── features/
    ├── public/ (home, search-results, trip-detail, passenger-details)
    ├── auth/ (login, register)
    ├── customer/ (bookings-list, booking-detail, profile, payment, become-operator)
    ├── operator/ (dashboard, offices, buses-list, bus-form, schedules-list, schedule-form, bookings, revenue)
    └── admin/ (dashboard, operator-requests, operators-list, bus-approvals, cities, routes, platform-fee, revenue, bookings)
```

### 6.3 Key UI pieces

- **Seat map** (reusable). Input `SeatLayout` (rows × cols + per-cell state). Emits `selectedSeats: string[]`. Box-style, color-coded (available / selected / locked / booked / aisle).
- **Checkout stepper** (Material stepper): seat → login-if-needed → passengers → payment. Sticky countdown in header, turns red at 1 min.
- **Razorpay wrapper**: after `POST /bookings` returns `{razorpay_order_id, key_id, amount}`, frontend opens `Razorpay.checkout(...)`. On success, sends `{razorpay_payment_id, razorpay_signature}` to `/bookings/{id}/verify-payment`. `checkout.js` script injected from `index.html`.
- **City autocomplete**: Material `mat-autocomplete`, 200ms debounce, min 2 chars, hits `/api/v1/cities?q=`.
- **Date picker**: `MatDatepicker`, min = today, max = today + 60 days.
- **Navbar**: logo left; "My Bookings" + avatar menu right when logged in. Avatar menu shows active roles and, when the user has multiple, a "Switch to Operator Console" link.

## 7. Key flows

### 7.1 Booking happy path

```
GET /search               → trips materialised, seats_left computed
GET /trips/{id}/seats     → layout with statuses
POST /trips/{id}/seat-locks
  UNIQUE constraint: first writer wins; loser → 409 SEAT_UNAVAILABLE
  returns {lock_id, expires_at=now+7m}
Frontend starts 7-min countdown. Prompts login if needed.
POST /bookings
  BEGIN
    SELECT FOR UPDATE seat_locks WHERE lock_id=? AND expires_at > now()
    INSERT booking (pending_payment), booking_seats, snapshot platform_fee
    RazorpayClient.createOrder
    INSERT payment (created)
  COMMIT
Razorpay modal → user pays with test card.
POST /bookings/{id}/verify-payment
  verify HMAC(order_id|payment_id, key_secret) == signature
  BEGIN
    UPDATE payment → captured
    UPDATE booking → confirmed
    DELETE seat_locks for lock_id
  COMMIT
  Resend send (email + PDF attached)
  INSERT notifications
```

### 7.2 Concurrent seat pick

Two users POST same seat at same instant. The unique constraint on `seat_locks(trip_id, seat_number)` means exactly one insert wins; the other throws, mapped to 409 `SEAT_UNAVAILABLE` with `details.unavailable = [...]`. Frontend shows a banner and refetches seat statuses.

### 7.3 Lock expiry

`SeatLockCleanupService` (IHostedService) runs every 60s: `DELETE FROM seat_locks WHERE expires_at < now()`. Reads also filter by `expires_at > now()`, so even pre-cleanup, stale locks never block a new pick. A booking attempt against an expired lock returns 409 `LOCK_EXPIRED`.

### 7.4 User cancellation

`POST /bookings/{id}/cancel` → compute `hours_until_departure` → apply `RefundPolicy` (configurable: ≥24h → 80%, 12–24h → 50%, <12h → 0% + 422 block) → transaction: update booking, insert refund (pending), call Razorpay refund, update payment → refunded. Email via Resend. Seat becomes available (bookings filter excludes cancelled).

### 7.5 Operator onboarding

Customer signs up → `POST /me/become-operator {company_name}` → admin approves → insert `user_roles('operator')`, update request, audit log, send email. Next login returns a JWT with both roles. Operator adds offices → adds bus (pending) → admin approves → creates schedule (blocked without offices at both route cities).

### 7.6 Admin disable operator (cascade)

```
POST /admin/operators/{id}/disable
BEGIN
  stamp users.operator_disabled_at = now()  (customer role untouched)
  UPDATE buses → operational_status='retired' for this operator
  For each bus_trip with trip_date >= today and booking.status='confirmed':
    UPDATE booking → cancelled_by_operator
    INSERT refund (pending, reason='operator_disabled', amount=total_amount)
    INSERT notifications
  INSERT audit_log
COMMIT
Post-commit async: send Resend notifications, call Razorpay refund API, mark refunds processed.
```

Kept as a single DB transaction so platform state never diverges; side effects fire after commit so a Razorpay outage cannot roll back cancellations.

## 8. Error handling, concurrency, security

### 8.1 Error handling

- Global `ExceptionMiddleware` maps `ValidationException` → 400 with field-level `details`, `NotFoundException` → 404, `ConflictException` → 409, `BusinessRuleException` → 422, `UnauthorizedException` → 401, `ForbiddenException` → 403, else 500 (correlationId in response, full stack in Serilog).
- Serilog writes to console + rolling `logs/bus-booking-.log`.
- Frontend `errorInterceptor`: 401 wipes token and redirects to login with `returnUrl`; 403 shows a "not authorised" page; 409 in seat-lock flow refetches seat statuses; 5xx shows a snackbar with correlationId.

### 8.2 Concurrency guards

| Risk | Guard |
|---|---|
| Two users pick same seat | `UNIQUE(trip_id, seat_number)` on `seat_locks`; second insert → 409. |
| User pays after lock expires | `/bookings` re-validates with `SELECT FOR UPDATE ... WHERE expires_at > now()`. |
| Verify-payment called twice | Idempotent. Returns the same confirmed booking on repeat; mismatched `razorpay_payment_id` → 409 `PAYMENT_MISMATCH`. |
| Lock cleanup mid-booking | Booking txn runs `UPDATE seat_locks WHERE lock_id=?` — 0 rows affected aborts the booking. |

Isolation: `READ COMMITTED` (default) with explicit `FOR UPDATE` where needed.

### 8.3 Security baseline

- Passwords: bcrypt work factor 11. Never logged or returned.
- JWT HS256, 1-hour expiry, 30s clock skew, signing key ≥32 bytes from config. Claims: `sub`, `email`, `roles[]`, `name`.
- Role enforcement: `[Authorize(Roles="...")]` on every protected endpoint. Operator endpoints additionally scope by `operator_user_id = current_user_id`.
- Secrets: `appsettings.Development.json` gitignored. `appsettings.Development.example.json` committed with `__FILL_ME_IN__` placeholders.
- CORS: explicit allow-list, `http://localhost:4200` in dev. Never `*`.
- Rate limit: sliding-window on `/auth/*`, 10 req/s/IP (.NET 9 built-in `AddRateLimiter`).
- Razorpay: HMAC-SHA256 signature verification on every `verify-payment`. Client claim of success is never trusted.
- Angular default binding escapes HTML; no `innerHTML` or `bypassSecurityTrust*`. PDF generated server-side.
- **Out of scope** (per §3.4 / §10 of requirements): MFA, refresh tokens, token revocation on logout, audit-grade compliance logging.

## 9. Testing

Pragmatic to the timeline.

### 9.1 Backend

- xUnit + FluentAssertions + Testcontainers.PostgreSql.
- **Unit**: `RefundPolicyService`, `PlatformFeeCalculator`, `SeatLayoutGenerator`, `PdfTicketGenerator` (asserts expected content), `JwtTokenService`.
- **Integration** (must-have): concurrent seat lock (two parallel inserts, exactly one wins); expired lock filtered from seat-status; booking against expired lock → 409; refund math across the three hour-offset bands; operator-disable cascade (setup: operator + bus + schedule + future booking → disable → assert booking cancelled and refund queued); Razorpay signature verify (valid + invalid).

### 9.2 Frontend

- Karma + Jasmine (Angular default).
- Unit: `SeatMapComponent`, `CountdownTimerComponent`, `refundPolicyLabel` pipe, `authInterceptor`, `roleGuard`.
- No e2e. Manual demo script in README instead.

### 9.3 Manual demo script (lives in README)

Seed DB with admin → register customer → customer requests operator → admin approves → operator adds two offices + a 40-seater bus → admin approves bus → operator creates a Bangalore→Chennai schedule → logout → anonymous search → select 2 seats → login prompt → enter passengers → Razorpay test card `4111 1111 1111 1111` → confirmation → PDF downloads → email arrives in Resend → cancel booking → verify refund email.

## 10. Delivery milestones

Each milestone ends on a demoable checkpoint.

| # | Goal | Demoable outcome |
|---|---|---|
| M0 | Foundation | `/api/v1/health` returns 200; Angular home calls it and shows "backend online". |
| M1 | Auth + roles + admin seed | Register, login, role-gated nav. Seeded admin visible only to admin. |
| M2 | Admin: cities, routes, platform fee | Admin seeds cities; home search autocompletes them. |
| M3 | Operator onboarding: offices + buses + approvals | Customer requests operator; admin approves; operator adds offices + bus; admin approves bus. |
| M4 | Schedules + search + seat map (view-only) | Anonymous search returns a bus; trip detail shows the seat map. |
| M5 | Seat locking + booking + Razorpay + PDF + email | Full happy path: seat → login → passengers → pay → PDF + email. |
| M6 | Bookings list + user cancellation + refund | Tabs of bookings; cancel shows projected refund; email arrives. |
| M7 | Operator views: bookings + revenue | Operator sees bookings and monthly revenue total. |
| M8 | Admin cascade + platform revenue + cross-operator bookings | Admin disables an operator; customer's booking flips to cancelled + email + refund. |
| M9 | Polish | Remaining unit tests, README, seed data, empty/loading states, a11y pass. |

## 11. Risks and open questions

- **Resend free tier without verified domain**: only delivers to the account-owner address. Flagged for README; user to decide at setup whether to verify a domain or accept the constraint.
- **Razorpay test mode refund realism**: refund API can be called but test-mode does not move money; the call is made for realism, and the UI / booking state transitions as if real.
- **Timezone handling**: all timestamps are UTC server-side; frontend renders in the browser's local timezone. Trip dates (a calendar date without time) remain as plain `date`.
- **Multi-role JWT**: the JWT embeds all current roles at issue time. Role changes (e.g. operator approval) take effect on the user's next login. Acceptable for a 1-hour-token, local-only app; would need refresh or revocation to be production-grade.

## 12. Out of scope

Per requirements §10 and §11 of the requirements doc, and the Balanced scope tier: cloud deployment, MFA, dynamic pricing, per-seat pricing tiers, multi-stop routes, round-trip booking, coupons, alternative-bus suggestions on cancellation, female-seat restrictions, SMS, refresh-token rotation, polished visual theming beyond Material defaults.
