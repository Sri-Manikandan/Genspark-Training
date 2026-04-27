namespace BusBooking.Api.Dtos;

public class CreateRouteRequest
{
    public Guid SourceCityId { get; set; }
    public Guid DestinationCityId { get; set; }
    public int? DistanceKm { get; set; }
}
