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
