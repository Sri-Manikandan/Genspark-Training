namespace BusBooking.Api.Models;

public class BusSchedule
{
    public Guid Id { get; set; }
    public Guid BusId { get; set; }
    public Guid RouteId { get; set; }
    public TimeOnly DepartureTime { get; set; }
    public TimeOnly ArrivalTime { get; set; }
    public decimal FarePerSeat { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly ValidTo { get; set; }
    /// <summary>Bitmask: Mon=1,Tue=2,Wed=4,Thu=8,Fri=16,Sat=32,Sun=64</summary>
    public int DaysOfWeek { get; set; }
    public bool IsActive { get; set; } = true;

    public Bus? Bus { get; set; }
    public Route? Route { get; set; }
}
