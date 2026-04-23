using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Infrastructure.Errors;
using BusBooking.Api.Infrastructure.Pdf;
using BusBooking.Api.Infrastructure.Razorpay;
using BusBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Api.Services;

public class BookingService : IBookingService
{
    private readonly AppDbContext _db;
    private readonly ISeatLockService _locks;
    private readonly IPlatformFeeService _platformFee;
    private readonly IRazorpayClient _razorpay;
    private readonly IPdfTicketGenerator _pdf;
    private readonly INotificationSender _notifications;
    private readonly TimeProvider _time;
    private readonly ILogger<BookingService> _log;

    public BookingService(
        AppDbContext db,
        ISeatLockService locks,
        IPlatformFeeService platformFee,
        IRazorpayClient razorpay,
        IPdfTicketGenerator pdf,
        INotificationSender notifications,
        TimeProvider time,
        ILogger<BookingService> log)
    {
        _db = db;
        _locks = locks;
        _platformFee = platformFee;
        _razorpay = razorpay;
        _pdf = pdf;
        _notifications = notifications;
        _time = time;
        _log = log;
    }

    public async Task<CreateBookingResponseDto> CreateAsync(Guid userId, CreateBookingRequest req, CancellationToken ct)
    {
        var now = _time.GetUtcNow().UtcDateTime;

        var lockRows = await _db.SeatLocks
            .Where(l => l.LockId == req.LockId)
            .ToListAsync(ct);
        if (lockRows.Count == 0)
            throw new ConflictException("LOCK_EXPIRED", "Your seat reservation has expired");
        if (lockRows.Any(l => l.ExpiresAt <= now))
            throw new ConflictException("LOCK_EXPIRED", "Your seat reservation has expired");
        if (lockRows.Any(l => l.TripId != req.TripId))
            throw new BusinessRuleException("LOCK_TRIP_MISMATCH", "Lock does not belong to this trip");
        if (lockRows.Any(l => l.SessionId != req.SessionId))
            throw new ForbiddenException("Lock session mismatch");

        var lockedSeats = lockRows.Select(l => l.SeatNumber).OrderBy(x => x).ToList();
        var requestedSeats = req.Passengers.Select(p => p.SeatNumber).OrderBy(x => x).ToList();
        if (!lockedSeats.SequenceEqual(requestedSeats, StringComparer.OrdinalIgnoreCase))
            throw new BusinessRuleException("PASSENGER_SEAT_MISMATCH",
                "Passenger seat list does not match the locked seats",
                new { expected = lockedSeats, actual = requestedSeats });

        var trip = await _db.BusTrips
            .Include(t => t.Schedule)
            .FirstAsync(t => t.Id == req.TripId, ct);

        var fee = await _platformFee.GetActiveAsync(ct);
        var seatCount = req.Passengers.Count;
        var totalFare = Math.Round(trip.Schedule!.FarePerSeat * seatCount, 2);
        var platformFee = fee.FeeType == PlatformFeeType.Fixed
            ? fee.Value
            : Math.Round(totalFare * fee.Value / 100m, 2);
        var totalAmount = totalFare + platformFee;
        var paise = (long)(totalAmount * 100m);

        var bookingCode = $"BK-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

        var order = await _razorpay.CreateOrderAsync(paise, bookingCode, ct);

        var bookingId = Guid.NewGuid();
        var booking = new Booking
        {
            Id = bookingId,
            BookingCode = bookingCode,
            TripId = req.TripId,
            UserId = userId,
            LockId = req.LockId,
            TotalFare = totalFare,
            PlatformFee = platformFee,
            TotalAmount = totalAmount,
            SeatCount = seatCount,
            Status = BookingStatus.PendingPayment,
            CreatedAt = now
        };
        _db.Bookings.Add(booking);

        foreach (var p in req.Passengers)
        {
            _db.BookingSeats.Add(new BookingSeat
            {
                Id = Guid.NewGuid(),
                BookingId = bookingId,
                SeatNumber = p.SeatNumber,
                PassengerName = p.PassengerName,
                PassengerAge = p.PassengerAge,
                PassengerGender = p.PassengerGender
            });
        }

        _db.Payments.Add(new Payment
        {
            Id = Guid.NewGuid(),
            BookingId = bookingId,
            RazorpayOrderId = order.Id,
            Amount = totalAmount,
            Currency = "INR",
            Status = PaymentStatus.Created,
            CreatedAt = now
        });

        foreach (var l in lockRows) l.UserId = userId;

        await _db.SaveChangesAsync(ct);

        return new CreateBookingResponseDto(
            bookingId, bookingCode, order.Id, _razorpay.KeyId, paise, "INR");
    }

    public async Task<BookingDetailDto> VerifyPaymentAsync(
        Guid userId, Guid bookingId, VerifyPaymentRequest req, CancellationToken ct)
    {
        var booking = await _db.Bookings
            .Include(b => b.Seats)
            .Include(b => b.Payment)
            .Include(b => b.Trip).ThenInclude(t => t!.Schedule).ThenInclude(s => s!.Bus).ThenInclude(b => b!.Operator)
            .Include(b => b.Trip).ThenInclude(t => t!.Schedule).ThenInclude(s => s!.Route).ThenInclude(r => r!.SourceCity)
            .Include(b => b.Trip).ThenInclude(t => t!.Schedule).ThenInclude(s => s!.Route).ThenInclude(r => r!.DestinationCity)
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == bookingId, ct)
            ?? throw new NotFoundException("Booking not found");

        if (booking.UserId != userId)
            throw new ForbiddenException("Not your booking");

        var payment = booking.Payment
            ?? throw new BusinessRuleException("PAYMENT_MISSING", "No payment on this booking");

        if (booking.Status == BookingStatus.Confirmed && payment.Status == PaymentStatus.Captured)
        {
            if (!string.Equals(payment.RazorpayPaymentId, req.RazorpayPaymentId, StringComparison.Ordinal))
                throw new ConflictException("PAYMENT_MISMATCH",
                    "Booking is already confirmed with a different payment id");
            return MapDetail(booking);
        }

        if (!_razorpay.VerifySignature(payment.RazorpayOrderId, req.RazorpayPaymentId, req.RazorpaySignature))
            throw new BusinessRuleException("SIGNATURE_INVALID", "Razorpay signature did not verify");

        var now = _time.GetUtcNow().UtcDateTime;
        payment.RazorpayPaymentId = req.RazorpayPaymentId;
        payment.RazorpaySignature = req.RazorpaySignature;
        payment.Status = PaymentStatus.Captured;
        payment.CapturedAt = now;

        booking.Status = BookingStatus.Confirmed;
        booking.ConfirmedAt = now;

        var locks = await _db.SeatLocks.Where(l => l.LockId == booking.LockId).ToListAsync(ct);
        _db.SeatLocks.RemoveRange(locks);

        await _db.SaveChangesAsync(ct);

        var dto = MapDetail(booking);

        try
        {
            var pdf = _pdf.Generate(dto);
            await _notifications.SendBookingConfirmedAsync(booking.User, dto, pdf, ct);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Post-confirm notification failed for {BookingCode}", booking.BookingCode);
        }

        return dto;
    }

    public async Task<BookingDetailDto> GetAsync(Guid userId, Guid bookingId, CancellationToken ct)
    {
        var booking = await LoadForDetailAsync(bookingId, ct)
            ?? throw new NotFoundException("Booking not found");
        if (booking.UserId != userId)
            throw new ForbiddenException("Not your booking");
        return MapDetail(booking);
    }

    public async Task<byte[]> GetTicketPdfAsync(Guid userId, Guid bookingId, CancellationToken ct)
    {
        var booking = await LoadForDetailAsync(bookingId, ct)
            ?? throw new NotFoundException("Booking not found");
        if (booking.UserId != userId)
            throw new ForbiddenException("Not your booking");
        if (booking.Status != BookingStatus.Confirmed && booking.Status != BookingStatus.Completed)
            throw new BusinessRuleException("TICKET_NOT_AVAILABLE", "Ticket is only available for confirmed bookings");
        return _pdf.Generate(MapDetail(booking));
    }

    private async Task<Booking?> LoadForDetailAsync(Guid bookingId, CancellationToken ct) =>
        await _db.Bookings
            .Include(b => b.Seats)
            .Include(b => b.Payment)
            .Include(b => b.Trip).ThenInclude(t => t!.Schedule).ThenInclude(s => s!.Bus).ThenInclude(b => b!.Operator)
            .Include(b => b.Trip).ThenInclude(t => t!.Schedule).ThenInclude(s => s!.Route).ThenInclude(r => r!.SourceCity)
            .Include(b => b.Trip).ThenInclude(t => t!.Schedule).ThenInclude(s => s!.Route).ThenInclude(r => r!.DestinationCity)
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == bookingId, ct);

    private static BookingDetailDto MapDetail(Booking b)
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
            b.Seats
                .OrderBy(s => s.SeatNumber)
                .Select(s => new BookingSeatDto(s.SeatNumber, s.PassengerName, s.PassengerAge, s.PassengerGender))
                .ToList());
    }
}

