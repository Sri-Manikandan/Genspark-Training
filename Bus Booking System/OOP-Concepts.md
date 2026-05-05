# OOP Concepts – Bus Booking System

## Why this project fits OOP

In class today we talked about how OOP is meant for complex, real-world problems — not simple stuff like adding two numbers. A bus booking system is a good example of this. A booking cant just be a loose set of variables — it needs to carry its trip, user, seats, and payment all together. Same with a bus — registration, capacity, operator, seats, and status all belong to the same object. Thats exactly what OOP is for, modeling real-world entities as wholesome objects.

---

## Encapsulation

**Already done.**

Every model uses C# properties instead of raw public fields:

```csharp
// Models/User.cs
public required string Name { get; set; }
public required string PasswordHash { get; set; }  // raw password never stored
public bool IsActive { get; set; } = true;
```

The `required` keyword is the same thing we discussed — mandatory fields. The `= true` default is the same as setting defaults in a constructor.

Internal service logic is always private. `BookingService` keeps its database, payment client, and PDF generator as private fields. It also has two private helpers — `LoadForDetailAsync` and `MapDetail` — that other methods reuse internaly but nothing outside the class can touch.

`BCryptPasswordHasher` is another good example. It hides the hashing algoritm and the work factor entirely. The rest of the system just calls `Hash()` and `Verify()` and doesnt need to know anything else.

**What I understand better now:** I used to make things private just because it was "best practice." Now I get why — the object should own its own state and show only what makes sense to the outside world. The password hash being locked inside the User object is a real example of that.

---

## Abstraction

**Already done.**

Almost every service in the project has a matching interface. The controller only ever sees `IBookingService`, never `BookingService`. `BookingService` only sees `INotificationSender`, never the actual email sender class. The runtime wires them together in `Program.cs`.

`INotificationSender` is the clearest example. It has methods for operator events, bus events, and booking events — but `BookingService` only calls the booking ones, and the admin service only calls the operator ones. Same implmentation, different face depending on who's using it. Thats exactly what was taught today.

**What I understand better now:** Abstraction isnt just hiding things — its about showing the right thing to the right caller. I wrote these interfaces before class, but I now realise I was doing abstraction without fully understanding that was the point.

---

## Inheritance

**Already done.**

The exception heirarchy is the clearest example in this project:

```csharp
public abstract class AppException : Exception  // common base
public class NotFoundException     : AppException(404)
public class ForbiddenException    : AppException(403)
public class BusinessRuleException : AppException(422)
// + 2 more
```

All five exceptions share `Code`, `HttpStatus`, and `Details` from the base class. The error-handling middleware works with `AppException` — it doesn't care which specific type is thrown. This is hierarchical inheritance, which we covered as one of the main types used in practice.

`SeatLockCleanupService` inheriting from .NET's `BackgroundService` is single inheritance — the framework handles the lifecycle, and the class just adds the cleanup logic.

**What I understand better now:** The base class carrying the common stuff means adding a new exception type tommorrow is literally one line. Before today I understood that, but I didn't connect it to the principle of "common properties in base class, specific ones in derived class."

---

## Polymorphism

**Both types are now done.**

**Runtime (already existed):** The notification sender is registered as `INotificationSender` in `Program.cs`. Today its `LoggingNotificationSender`, but swapping it for a real email sender is a one line change and nothing else in the codebase changes. The middleware reading `.HttpStatus` from `AppException` also doesnt know whether it got a 404 or 403 until runtime — same method, different result.

**Compile-time (added after class):** `ITripService` now has three overloads of `SearchAsync`:

```csharp
SearchAsync(src, dst, ct)        // defaults to today
SearchAsync(src, dst, date, ct)           // specific date
SearchAsync(src, dst, date, busType, ct)    // specific date + filter by bus type
```

The controller calls the right one based on which query parameters the user provides. The compiler resolves this at build time — thats early binding, same as the `login(username, password)` vs `login(biometrics)` example from today.

Before this, there was one method with nullable parameters and internal if-checks. Now each overload is clean and focused, and the compiler enforces that the right arguments are passed.

---

## ToString() Override

**Added after class.**

None of the models had this before. Added it to `User`, `Bus`, and `Booking`:

```csharp
// Booking.cs
public override string ToString() =>
    $"Booking {BookingCode} | Status: {Status} | Seats: {SeatCount} | Amount: INR {TotalAmount:0.00}";

// Bus.cs
public override string ToString() =>
    $"[{BusType}] {BusName} ({RegistrationNumber}) — {Capacity} seats — {ApprovalStatus}";
```

Before this, logging a booking would print `BusBooking.Api.Models.Booking`. Now it prints something useful. This is the same point from the banking demo — the default `ToString()` is useless, and overriding it is one of the most pratical things you can do for debugging. Its also runtime polymorphism since the derived class overrides the base `Object.ToString()`.

---

## Summary

All six concepts are covered in the project:

- **Encapsulation** — model properties, private service fields, `BCryptPasswordHasher`
- **Abstraction** — all `IXxxService` interfaces, `INotificationSender`
- **Inheritance** — `AppException` → 5 types, `SeatLockCleanupService` → `BackgroundService`
- **Runtime Polymorphism** — interface injection, exception middleware
- **Compile-time Polymorphism** — 3 `SearchAsync` overloads in `TripService`
- **ToString() Override** — `User`, `Bus`, `Booking` models
