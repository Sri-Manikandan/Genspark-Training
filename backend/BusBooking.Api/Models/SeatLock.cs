namespace BusBooking.Api.Models;

public class SeatLock
{
    public Guid Id { get; set; }
    public Guid TripId { get; set; }
    public required string SeatNumber { get; set; }
    public Guid LockId { get; set; } // group id returned to client; shared across all seats in one POST
    public Guid SessionId { get; set; } // client-provided; validated on booking
    public Guid? UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    public BusTrip Trip { get; set; } = null!;
    public User? User { get; set; }
}

