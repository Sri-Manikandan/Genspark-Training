using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Infrastructure.Errors;
using BusBooking.Api.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BusBooking.Api.Services;

public class SeatLockService : ISeatLockService
{
    public static readonly TimeSpan LockWindow = TimeSpan.FromMinutes(7);
    private readonly AppDbContext _db;
    private readonly TimeProvider _time;

    public SeatLockService(AppDbContext db, TimeProvider time)
    {
        _db = db;
        _time = time;
    }

    public async Task<SeatLockResponseDto> LockAsync(
        Guid tripId, Guid? userId, LockSeatsRequest req, CancellationToken ct)
    {
        var trip = await _db.BusTrips
            .Include(t => t.Schedule).ThenInclude(s => s!.Bus)
            .FirstOrDefaultAsync(t => t.Id == tripId, ct)
            ?? throw new NotFoundException("Trip not found");

        if (trip.Status != TripStatus.Scheduled)
            throw new BusinessRuleException("TRIP_NOT_AVAILABLE", "Trip is not available for booking");

        var validSeats = await _db.SeatDefinitions
            .Where(s => s.BusId == trip.Schedule!.BusId)
            .Select(s => s.SeatNumber)
            .ToListAsync(ct);

        var unknown = req.Seats.Except(validSeats, StringComparer.OrdinalIgnoreCase).ToList();
        if (unknown.Count > 0)
            throw new BusinessRuleException("UNKNOWN_SEATS", "Seat numbers do not belong to this bus",
                new { unknown });

        var bookedSeats = await _db.BookingSeats
            .Where(bs => bs.Booking.TripId == tripId
                         && bs.Booking.Status != BookingStatus.Cancelled
                         && bs.Booking.Status != BookingStatus.CancelledByOperator)
            .Select(bs => bs.SeatNumber)
            .ToListAsync(ct);

        var alreadyBooked = req.Seats.Intersect(bookedSeats, StringComparer.OrdinalIgnoreCase).ToList();
        if (alreadyBooked.Count > 0)
            throw new ConflictException("SEAT_UNAVAILABLE", "One or more seats are already booked",
                new { unavailable = alreadyBooked });

        var now = _time.GetUtcNow().UtcDateTime;
        var expiresAt = now + LockWindow;
        var lockId = Guid.NewGuid();

        foreach (var seat in req.Seats)
        {
            _db.SeatLocks.Add(new SeatLock
            {
                Id = Guid.NewGuid(),
                TripId = tripId,
                SeatNumber = seat,
                LockId = lockId,
                SessionId = req.SessionId,
                UserId = userId,
                CreatedAt = now,
                ExpiresAt = expiresAt
            });
        }

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == "23505")
        {
            foreach (var entry in _db.ChangeTracker.Entries<SeatLock>().ToList())
                entry.State = EntityState.Detached;

            var currentLockOwners = await _db.SeatLocks
                .Where(l => l.TripId == tripId && l.ExpiresAt > now && req.Seats.Contains(l.SeatNumber))
                .Select(l => l.SeatNumber)
                .ToListAsync(ct);

            throw new ConflictException("SEAT_UNAVAILABLE", "One or more seats are currently locked",
                new { unavailable = currentLockOwners });
        }

        return new SeatLockResponseDto(lockId, req.SessionId, req.Seats, expiresAt);
    }

    public async Task ReleaseAsync(Guid lockId, Guid sessionId, Guid? userId, CancellationToken ct)
    {
        var rows = await _db.SeatLocks.Where(l => l.LockId == lockId).ToListAsync(ct);
        if (rows.Count == 0)
            throw new NotFoundException("Lock not found");

        var any = rows[0];
        var sessionMatches = any.SessionId == sessionId;
        var userMatches = userId.HasValue && any.UserId == userId;
        if (!sessionMatches && !userMatches)
            throw new ForbiddenException("Not owner of this lock");

        _db.SeatLocks.RemoveRange(rows);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<SeatLock>> GetActiveLocksAsync(Guid lockId, CancellationToken ct)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        return await _db.SeatLocks
            .Where(l => l.LockId == lockId && l.ExpiresAt > now)
            .ToListAsync(ct);
    }
}

