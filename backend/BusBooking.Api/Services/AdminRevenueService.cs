using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Api.Services;

public class AdminRevenueService : IAdminRevenueService
{
    private readonly AppDbContext _db;

    public AdminRevenueService(AppDbContext db) => _db = db;

    public async Task<AdminRevenueResponseDto> GetAsync(
        DateOnly from, DateOnly to, CancellationToken ct)
    {
        var rows = await _db.Bookings
            .AsNoTracking()
            .Where(b => (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Completed)
                     && b.Trip!.TripDate >= from
                     && b.Trip!.TripDate <= to)
            .Include(b => b.Trip).ThenInclude(t => t!.Schedule).ThenInclude(s => s!.Bus).ThenInclude(b => b!.Operator)
            .ToListAsync(ct);

        var byOperator = rows
            .GroupBy(b => new
            {
                OpId = b.Trip!.Schedule!.Bus!.Operator!.Id,
                OpName = b.Trip!.Schedule!.Bus!.Operator!.Name
            })
            .Select(g => new AdminRevenueOperatorItemDto(
                g.Key.OpId,
                g.Key.OpName,
                g.Count(),
                g.Sum(b => b.TotalFare),
                g.Sum(b => b.PlatformFee)))
            .OrderByDescending(x => x.Gmv)
            .ToList();

        return new AdminRevenueResponseDto(
            from,
            to,
            rows.Count,
            rows.Sum(b => b.TotalFare),
            rows.Sum(b => b.PlatformFee),
            byOperator);
    }
}
