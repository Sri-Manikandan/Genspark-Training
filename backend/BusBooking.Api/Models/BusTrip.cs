namespace BusBooking.Api.Models;

public class BusTrip
{
    public Guid Id { get; set; }
    public Guid ScheduleId { get; set; }
    public DateOnly TripDate { get; set; }
    public string Status { get; set; } = TripStatus.Scheduled;
    public string? CancelReason { get; set; }

    public BusSchedule? Schedule { get; set; }
}
