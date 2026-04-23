using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Infrastructure.Errors;
using BusBooking.Api.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
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
        try
        {
            await _db.SaveChangesAsync(ct);
            return trip;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == "23505")
        {
            // Concurrent request already inserted the row — detach and re-query
            _db.Entry(trip).State = EntityState.Detached;
            return await _db.BusTrips
                .FirstAsync(t => t.ScheduleId == scheduleId && t.TripDate == date, ct);
        }
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
