namespace BusBooking.Api.Models;

public class Route
{
    public Guid Id { get; set; }
    public Guid SourceCityId { get; set; }
    public Guid DestinationCityId { get; set; }
    public int? DistanceKm { get; set; }
    public bool IsActive { get; set; } = true;

    public City? SourceCity { get; set; }
    public City? DestinationCity { get; set; }
}
