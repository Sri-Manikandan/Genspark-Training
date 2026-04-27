namespace BusBooking.Api.Models;

public class Booking
{
    public Guid Id { get; set; }
    public required string BookingCode { get; set; }
    public Guid TripId { get; set; }
    public Guid UserId { get; set; }
    public Guid LockId { get; set; } // seat-lock group that must be deleted on confirm
    public decimal TotalFare { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal TotalAmount { get; set; }
    public int SeatCount { get; set; }
    public string Status { get; set; } = BookingStatus.PendingPayment;
    public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public decimal? RefundAmount { get; set; }
    public string? RefundStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }

    public BusTrip Trip { get; set; } = null!;
    public User User { get; set; } = null!;
    public ICollection<BookingSeat> Seats { get; set; } = new List<BookingSeat>();
    public Payment? Payment { get; set; }
}

