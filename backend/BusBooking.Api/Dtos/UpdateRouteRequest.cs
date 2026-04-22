namespace BusBooking.Api.Dtos;

public class UpdateRouteRequest
{
    public int? DistanceKm { get; set; }
    public bool? IsActive { get; set; }
}
