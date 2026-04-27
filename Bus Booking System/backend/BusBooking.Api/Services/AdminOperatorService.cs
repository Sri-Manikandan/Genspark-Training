using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Infrastructure.Errors;
using BusBooking.Api.Infrastructure.Razorpay;
using BusBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Api.Services;

public class AdminOperatorService : IAdminOperatorService
{
    private readonly AppDbContext _db;
    private readonly IAuditLogWriter _audit;
    private readonly INotificationSender _notifier;
    private readonly IRazorpayClient _razorpay;
    private readonly TimeProvider _time;
    private readonly ILogger<AdminOperatorService> _log;

    public AdminOperatorService(
        AppDbContext db,
        IAuditLogWriter audit,
        INotificationSender notifier,
        IRazorpayClient razorpay,
        TimeProvider time,
        ILogger<AdminOperatorService> log)
    {
        _db = db;
        _audit = audit;
        _notifier = notifier;
        _razorpay = razorpay;
        _time = time;
        _log = log;
    }

    public async Task<IReadOnlyList<AdminOperatorListItemDto>> ListAsync(CancellationToken ct)
    {
        var rows = await _db.Users
            .AsNoTracking()
            .Where(u => u.Roles.Any(r => r.Role == Roles.Operator))
            .Select(u => new
            {
                u.Id, u.Name, u.Email, u.CreatedAt, u.OperatorDisabledAt,
                Buses = _db.Buses.Where(b => b.OperatorUserId == u.Id)
                    .Select(b => b.OperationalStatus).ToList()
            })
            .ToListAsync(ct);

        return rows.Select(r => new AdminOperatorListItemDto(
            r.Id, r.Name, r.Email, r.CreatedAt,
            IsDisabled: r.OperatorDisabledAt.HasValue,
            DisabledAt: r.OperatorDisabledAt,
            TotalBuses: r.Buses.Count,
            ActiveBuses: r.Buses.Count(s => s == BusOperationalStatus.Active),
            RetiredBuses: r.Buses.Count(s => s == BusOperationalStatus.Retired)))
            .OrderBy(o => o.Name)
            .ToList();
    }

    public async Task<AdminOperatorListItemDto> DisableAsync(
        Guid adminId, Guid operatorUserId, string? reason, CancellationToken ct)
    {
        var op = await _db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == operatorUserId, ct)
            ?? throw new NotFoundException("Operator not found");

        if (!op.Roles.Any(r => r.Role == Roles.Operator))
            throw new NotFoundException("User is not an operator");

        if (op.OperatorDisabledAt.HasValue)
            return await LoadDtoAsync(op.Id, ct);

        var now = _time.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);

        op.OperatorDisabledAt = now;

        var buses = await _db.Buses
            .Where(b => b.OperatorUserId == op.Id
                     && b.OperationalStatus != BusOperationalStatus.Retired)
            .ToListAsync(ct);
        foreach (var bus in buses) bus.OperationalStatus = BusOperationalStatus.Retired;

        var bookingsToCancel = await _db.Bookings
            .Include(b => b.Payment)
            .Include(b => b.User)
            .Include(b => b.Seats)
            .Include(b => b.Trip).ThenInclude(t => t!.Schedule).ThenInclude(s => s!.Bus).ThenInclude(b => b!.Operator)
            .Include(b => b.Trip).ThenInclude(t => t!.Schedule).ThenInclude(s => s!.Route).ThenInclude(r => r!.SourceCity)
            .Include(b => b.Trip).ThenInclude(t => t!.Schedule).ThenInclude(s => s!.Route).ThenInclude(r => r!.DestinationCity)
            .Where(b => b.Trip!.Schedule!.Bus!.OperatorUserId == op.Id
                     && b.Status == BookingStatus.Confirmed
                     && b.Trip!.TripDate >= today)
            .ToListAsync(ct);

        foreach (var b in bookingsToCancel)
        {
            b.Status = BookingStatus.CancelledByOperator;
            b.CancelledAt = now;
            b.CancellationReason = reason ?? "Operator disabled by admin";
            b.RefundAmount = b.TotalAmount;
            b.RefundStatus = RefundStatus.Pending;
        }

        await _db.SaveChangesAsync(ct);
        await _audit.WriteAsync(adminId, AuditAction.OperatorDisabled,
            "user", op.Id,
            new { reason, cascadedBookings = bookingsToCancel.Count, retiredBuses = buses.Count }, ct);

        foreach (var b in bookingsToCancel)
        {
            await RefundBookingPostCommitAsync(b, now, ct);
        }

        try { await _notifier.SendOperatorDisabledAsync(op, reason, ct); }
        catch (Exception ex) { _log.LogError(ex, "Operator-disabled email failed for {Email}", op.Email); }

        foreach (var b in bookingsToCancel)
        {
            try
            {
                var detail = MapBookingDetail(b);
                await _notifier.SendBookingCancelledByOperatorAsync(
                    b.User, detail, b.RefundAmount ?? 0m, ct);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Operator-cancel email failed for booking {Code}", b.BookingCode);
            }
        }

        return await LoadDtoAsync(op.Id, ct);
    }

    public async Task<AdminOperatorListItemDto> EnableAsync(
        Guid adminId, Guid operatorUserId, CancellationToken ct)
    {
        var op = await _db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == operatorUserId, ct)
            ?? throw new NotFoundException("Operator not found");

        if (!op.Roles.Any(r => r.Role == Roles.Operator))
            throw new NotFoundException("User is not an operator");

        if (!op.OperatorDisabledAt.HasValue)
            return await LoadDtoAsync(op.Id, ct);

        op.OperatorDisabledAt = null;
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(adminId, AuditAction.OperatorEnabled,
            "user", op.Id, metadata: null, ct);

        return await LoadDtoAsync(op.Id, ct);
    }

    private async Task RefundBookingPostCommitAsync(Booking b, DateTime now, CancellationToken ct)
    {
        var refundAmount = b.RefundAmount ?? 0m;
        if (refundAmount <= 0m
            || b.Payment is not { Status: PaymentStatus.Captured, RazorpayPaymentId: { } payId })
        {
            b.RefundStatus = RefundStatus.Processed;
            await _db.SaveChangesAsync(ct);
            return;
        }

        try
        {
            await _razorpay.CreateRefundAsync(payId, (long)(refundAmount * 100m), ct);
            b.Payment.Status = PaymentStatus.Refunded;
            b.Payment.RefundedAt = now;
            b.RefundStatus = RefundStatus.Processed;
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Operator-cascade Razorpay refund failed for booking {Code}", b.BookingCode);
            b.RefundStatus = RefundStatus.Failed;
            await _db.SaveChangesAsync(ct);
        }
    }

    private async Task<AdminOperatorListItemDto> LoadDtoAsync(Guid operatorUserId, CancellationToken ct)
    {
        var row = (await ListAsync(ct)).FirstOrDefault(o => o.UserId == operatorUserId)
            ?? throw new NotFoundException("Operator not found");
        return row;
    }

    private static BookingDetailDto MapBookingDetail(Booking b)
    {
        var schedule = b.Trip!.Schedule!;
        var route = schedule.Route!;
        var bus = schedule.Bus!;
        return new BookingDetailDto(
            b.Id,
            b.BookingCode,
            b.TripId,
            b.Trip.TripDate,
            route.SourceCity!.Name,
            route.DestinationCity!.Name,
            bus.BusName,
            bus.Operator!.Name,
            schedule.DepartureTime,
            schedule.ArrivalTime,
            b.TotalFare,
            b.PlatformFee,
            b.TotalAmount,
            b.SeatCount,
            b.Status,
            b.ConfirmedAt,
            b.CreatedAt,
            b.CancelledAt,
            b.CancellationReason,
            b.RefundAmount,
            b.RefundStatus,
            b.Seats
                .OrderBy(s => s.SeatNumber)
                .Select(s => new BookingSeatDto(s.SeatNumber, s.PassengerName, s.PassengerAge, s.PassengerGender))
                .ToList());
    }
}
