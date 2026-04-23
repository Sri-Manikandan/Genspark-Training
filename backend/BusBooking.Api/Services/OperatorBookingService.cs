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
        => throw new NotImplementedException("Implemented in Task 7");
}
