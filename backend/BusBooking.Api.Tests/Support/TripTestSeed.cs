using BusBooking.Api.Infrastructure;
using BusBooking.Api.Models;
using Microsoft.Extensions.DependencyInjection;
using Route = BusBooking.Api.Models.Route;

namespace BusBooking.Api.Tests.Support;

public record SeededTrip(
    Guid OperatorId,
    Guid BusId,
    Guid RouteId,
    Guid ScheduleId,
    Guid TripId,
    decimal FarePerSeat,
    int Capacity);

public static class TripTestSeed
{
    // Creates: two cities, one route, an approved bus with `capacity` seats (A1..An),
    // an active schedule on every day, and a materialized trip `daysAhead` days from today.
    public static async Task<SeededTrip> CreateAsync(
        IntegrationFixture fx,
        int capacity = 10,
        int daysAhead = 7,
        decimal farePerSeat = 500m)
    {
        var (op, _) = await OperatorTokenFactory.CreateAsync(fx, [Roles.Operator]);

        using var scope = fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var src = new City { Id = Guid.NewGuid(), Name = $"Src-{Guid.NewGuid():N}".Substring(0, 16), State = "KA", IsActive = true };
        var dst = new City { Id = Guid.NewGuid(), Name = $"Dst-{Guid.NewGuid():N}".Substring(0, 16), State = "TN", IsActive = true };
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
            new OperatorOffice { Id = Guid.NewGuid(), OperatorUserId = op.Id, CityId = src.Id, AddressLine = "Pickup hub", Phone = "100", IsActive = true },
            new OperatorOffice { Id = Guid.NewGuid(), OperatorUserId = op.Id, CityId = dst.Id, AddressLine = "Drop hub",   Phone = "101", IsActive = true });

        var bus = new Bus
        {
            Id = Guid.NewGuid(),
            OperatorUserId = op.Id,
            RegistrationNumber = $"TN-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            BusName = "Test Bus",
            BusType = BusType.Seater,
            Capacity = capacity,
            ApprovalStatus = BusApprovalStatus.Approved,
            OperationalStatus = BusOperationalStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        db.Buses.Add(bus);

        for (var i = 0; i < capacity; i++)
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
            Id = Guid.NewGuid(),
            BusId = bus.Id,
            RouteId = route.Id,
            DepartureTime = new TimeOnly(22, 0),
            ArrivalTime = new TimeOnly(6, 0),
            FarePerSeat = farePerSeat,
            ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            ValidTo   = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60)),
            DaysOfWeek = 127, // every day
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
}
