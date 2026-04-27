using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Infrastructure.Errors;
using BusBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Api.Services;

public class AdminBookingService : IAdminBookingService
{
    private readonly AppDbContext _db;

    public AdminBookingService(AppDbContext db) => _db = db;

    public async Task<AdminBookingListResponseDto> ListAsync(
        Guid? operatorUserId,
        string? status,
        DateOnly? date,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var p = page < 1 ? 1 : page;
        var size = pageSize is < 1 or > 100 ? 20 : pageSize;

        if (!string.IsNullOrEmpty(status) && !BookingStatus.All.Contains(status))
            throw new BusinessRuleException("INVALID_STATUS", "Unknown booking status filter");

        var q = _db.Bookings
            .AsNoTracking()
            .Include(b => b.Trip).ThenInclude(t => t!.Schedule).ThenInclude(s => s!.Bus).ThenInclude(b => b!.Operator)
            .Include(b => b.Trip).ThenInclude(t => t!.Schedule).ThenInclude(s => s!.Route).ThenInclude(r => r!.SourceCity)
            .Include(b => b.Trip).ThenInclude(t => t!.Schedule).ThenInclude(s => s!.Route).ThenInclude(r => r!.DestinationCity)
            .Include(b => b.User)
            .AsQueryable();

        if (string.IsNullOrEmpty(status))
            q = q.Where(b => b.Status != BookingStatus.PendingPayment);
        else
            q = q.Where(b => b.Status == status);

        if (operatorUserId.HasValue)
            q = q.Where(b => b.Trip!.Schedule!.Bus!.OperatorUserId == operatorUserId.Value);

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
            var op = bus.Operator!;
            return new AdminBookingListItemDto(
                b.Id,
                b.BookingCode,
                b.TripId,
                b.Trip.TripDate,
                route.SourceCity!.Name,
                route.DestinationCity!.Name,
                bus.Id,
                bus.BusName,
                op.Id,
                op.Name,
                b.User.Name,
                b.User.Email,
                b.SeatCount,
                b.TotalFare,
                b.PlatformFee,
                b.TotalAmount,
                b.Status,
                b.CreatedAt);
        }).ToList();

        return new AdminBookingListResponseDto(items, p, size, total);
    }
}
