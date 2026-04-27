using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Infrastructure.Errors;
using BusBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Api.Services;

public class CityService : ICityService
{
    private const int MinQueryLength = 2;
    private const int MaxSearchResults = 20;

    private readonly AppDbContext _db;

    public CityService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CityDto>> SearchActiveAsync(string query, int limit, CancellationToken ct)
    {
        var q = (query ?? string.Empty).Trim();
        if (q.Length < MinQueryLength)
            return Array.Empty<CityDto>();

        var take = Math.Clamp(limit <= 0 ? 10 : limit, 1, MaxSearchResults);
        var pattern = $"%{q}%";

        var results = await _db.Cities
            .AsNoTracking()
            .Where(c => c.IsActive && EF.Functions.ILike(c.Name, pattern))
            .OrderBy(c => c.Name)
            .Take(take)
            .Select(c => new CityDto(c.Id, c.Name, c.State, c.IsActive))
            .ToListAsync(ct);

        return results;
    }

    public async Task<IReadOnlyList<CityDto>> ListAllAsync(CancellationToken ct)
    {
        return await _db.Cities
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CityDto(c.Id, c.Name, c.State, c.IsActive))
            .ToListAsync(ct);
    }

    public async Task<CityDto> GetAsync(Guid id, CancellationToken ct)
    {
        var c = await _db.Cities.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("City not found");
        return new CityDto(c.Id, c.Name, c.State, c.IsActive);
    }

    public async Task<CityDto> CreateAsync(CreateCityRequest request, CancellationToken ct)
    {
        var name = request.Name.Trim();
        var state = request.State.Trim();

        if (await _db.Cities.AnyAsync(c => c.Name == name, ct))
            throw new ConflictException("CITY_NAME_TAKEN", "A city with that name already exists");

        var city = new City
        {
            Id = Guid.NewGuid(),
            Name = name,
            State = state,
            IsActive = true
        };
        _db.Cities.Add(city);
        await _db.SaveChangesAsync(ct);
        return new CityDto(city.Id, city.Name, city.State, city.IsActive);
    }

    public async Task<CityDto> UpdateAsync(Guid id, UpdateCityRequest request, CancellationToken ct)
    {
        var city = await _db.Cities.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException("City not found");

        if (request.Name is not null)
        {
            var newName = request.Name.Trim();
            if (!string.Equals(newName, city.Name, StringComparison.OrdinalIgnoreCase)
                && await _db.Cities.AnyAsync(c => c.Name == newName, ct))
            {
                throw new ConflictException("CITY_NAME_TAKEN", "A city with that name already exists");
            }
            city.Name = newName;
        }
        if (request.State is not null) city.State = request.State.Trim();
        if (request.IsActive is not null) city.IsActive = request.IsActive.Value;

        await _db.SaveChangesAsync(ct);
        return new CityDto(city.Id, city.Name, city.State, city.IsActive);
    }
}
