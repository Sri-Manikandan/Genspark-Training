using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Infrastructure.Errors;
using BusBooking.Api.Models;
using Microsoft.EntityFrameworkCore;
using Route = BusBooking.Api.Models.Route;

namespace BusBooking.Api.Services;

public class RouteService : IRouteService
{
    private readonly AppDbContext _db;

    public RouteService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<RouteDto>> ListAllAsync(CancellationToken ct)
    {
        return await _db.Routes
            .AsNoTracking()
            .Include(r => r.SourceCity)
            .Include(r => r.DestinationCity)
            .OrderBy(r => r.SourceCity!.Name).ThenBy(r => r.DestinationCity!.Name)
            .Select(r => ToDto(r))
            .ToListAsync(ct);
    }

    public async Task<RouteDto> GetAsync(Guid id, CancellationToken ct)
    {
        var r = await _db.Routes.AsNoTracking()
            .Include(x => x.SourceCity).Include(x => x.DestinationCity)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Route not found");
        return ToDto(r);
    }

    public async Task<RouteDto> CreateAsync(CreateRouteRequest request, CancellationToken ct)
    {
        var source = await _db.Cities.FindAsync([request.SourceCityId], ct)
            ?? throw new NotFoundException("Source city not found");
        var destination = await _db.Cities.FindAsync([request.DestinationCityId], ct)
            ?? throw new NotFoundException("Destination city not found");

        var duplicate = await _db.Routes.AnyAsync(
            r => r.SourceCityId == request.SourceCityId
              && r.DestinationCityId == request.DestinationCityId, ct);
        if (duplicate)
            throw new ConflictException("ROUTE_EXISTS", "A route between these cities already exists");

        var route = new Route
        {
            Id = Guid.NewGuid(),
            SourceCityId = request.SourceCityId,
            DestinationCityId = request.DestinationCityId,
            DistanceKm = request.DistanceKm,
            IsActive = true,
            SourceCity = source,
            DestinationCity = destination
        };
        _db.Routes.Add(route);
        await _db.SaveChangesAsync(ct);
        return ToDto(route);
    }

    public async Task<RouteDto> UpdateAsync(Guid id, UpdateRouteRequest request, CancellationToken ct)
    {
        var route = await _db.Routes
            .Include(r => r.SourceCity).Include(r => r.DestinationCity)
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException("Route not found");

        if (request.DistanceKm is not null) route.DistanceKm = request.DistanceKm;
        if (request.IsActive is not null) route.IsActive = request.IsActive.Value;
        await _db.SaveChangesAsync(ct);
        return ToDto(route);
    }

    private static RouteDto ToDto(Route r) => new(
        r.Id,
        new CityDto(r.SourceCity!.Id, r.SourceCity.Name, r.SourceCity.State, r.SourceCity.IsActive),
        new CityDto(r.DestinationCity!.Id, r.DestinationCity.Name, r.DestinationCity.State, r.DestinationCity.IsActive),
        r.DistanceKm,
        r.IsActive);
}
