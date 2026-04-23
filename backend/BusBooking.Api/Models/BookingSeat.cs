namespace BusBooking.Api.Models;

public class BookingSeat
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public required string SeatNumber { get; set; }
    public required string PassengerName { get; set; }
    public int PassengerAge { get; set; }
    public required string PassengerGender { get; set; }

    public Booking Booking { get; set; } = null!;
}

