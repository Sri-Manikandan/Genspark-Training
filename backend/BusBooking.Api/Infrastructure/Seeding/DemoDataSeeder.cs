using BusBooking.Api.Infrastructure.Auth;
using BusBooking.Api.Models;
using BusBooking.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Route = BusBooking.Api.Models.Route;

namespace BusBooking.Api.Infrastructure.Seeding;

public class DemoDataSeeder : IDemoDataSeeder
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly DemoDataSeedOptions _options;
    private readonly ILogger<DemoDataSeeder> _logger;
    private readonly TimeProvider _clock;

    public DemoDataSeeder(
        AppDbContext db,
        IPasswordHasher hasher,
        IOptions<DemoDataSeedOptions> options,
        TimeProvider clock,
        ILogger<DemoDataSeeder> logger)
    {
        _db = db;
        _hasher = hasher;
        _options = options.Value;
        _clock = clock;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("DemoSeed disabled; skipping");
            return;
        }

        var bangalore = await UpsertCityAsync("Bangalore", "Karnataka", ct);
        var chennai = await UpsertCityAsync("Chennai", "Tamil Nadu", ct);

        var route = await _db.Routes.FirstOrDefaultAsync(
            r => r.SourceCityId == bangalore.Id && r.DestinationCityId == chennai.Id, ct);
        if (route is null)
        {
            route = new Route
            {
                Id = Guid.NewGuid(),
                SourceCityId = bangalore.Id,
                DestinationCityId = chennai.Id,
                DistanceKm = 350,
                IsActive = true
            };
            _db.Routes.Add(route);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("DemoSeed: created route Bangalore → Chennai");
        }

        var operatorUser = await UpsertUserAsync(
            _options.OperatorEmail, _options.OperatorName, _options.OperatorPassword, ct);
        await EnsureRoleAsync(operatorUser, Roles.Customer, ct);
        await EnsureRoleAsync(operatorUser, Roles.Operator, ct);

        var customerUser = await UpsertUserAsync(
            _options.CustomerEmail, _options.CustomerName, _options.CustomerPassword, ct);
        await EnsureRoleAsync(customerUser, Roles.Customer, ct);

        var opRequest = await _db.OperatorRequests.FirstOrDefaultAsync(r => r.UserId == operatorUser.Id, ct);
        if (opRequest is null)
        {
            _db.OperatorRequests.Add(new OperatorRequest
            {
                Id = Guid.NewGuid(),
                UserId = operatorUser.Id,
                Status = OperatorRequestStatus.Approved,
                CompanyName = _options.OperatorCompany,
                RequestedAt = _clock.GetUtcNow().UtcDateTime.AddDays(-7),
                ReviewedAt = _clock.GetUtcNow().UtcDateTime.AddDays(-6),
                ReviewedByAdminId = null
            });
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("DemoSeed: created approved operator request for {Email}", operatorUser.Email);
        }

        await EnsureOfficeAsync(operatorUser.Id, bangalore.Id,
            "Kempegowda Bus Terminal, Bay 12", "+91-9000000001", ct);
        await EnsureOfficeAsync(operatorUser.Id, chennai.Id,
            "CMBT, Bay 7", "+91-9000000002", ct);

        const string registrationNumber = "KA01DEMO1234";
        var bus = await _db.Buses.FirstOrDefaultAsync(b => b.RegistrationNumber == registrationNumber, ct);
        if (bus is null)
        {
            bus = new Bus
            {
                Id = Guid.NewGuid(),
                OperatorUserId = operatorUser.Id,
                RegistrationNumber = registrationNumber,
                BusName = "Demo Express",
                BusType = BusType.Seater,
                Capacity = 40,
                ApprovalStatus = BusApprovalStatus.Approved,
                OperationalStatus = BusOperationalStatus.Active,
                CreatedAt = _clock.GetUtcNow().UtcDateTime.AddDays(-5),
                ApprovedAt = _clock.GetUtcNow().UtcDateTime.AddDays(-4),
                ApprovedByAdminId = null
            };
            _db.Buses.Add(bus);
            _db.SeatDefinitions.AddRange(SeatLayoutGenerator.Generate(bus.Id, rows: 8, columns: 5));
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("DemoSeed: created bus {Reg} with 40 seats", registrationNumber);
        }

        var today = DateOnly.FromDateTime(_clock.GetUtcNow().UtcDateTime);
        var schedule = await _db.BusSchedules.FirstOrDefaultAsync(
            s => s.BusId == bus.Id && s.RouteId == route.Id, ct);
        if (schedule is null)
        {
            _db.BusSchedules.Add(new BusSchedule
            {
                Id = Guid.NewGuid(),
                BusId = bus.Id,
                RouteId = route.Id,
                DepartureTime = new TimeOnly(9, 0),
                ArrivalTime = new TimeOnly(14, 0),
                FarePerSeat = 500m,
                ValidFrom = today,
                ValidTo = today.AddDays(60),
                DaysOfWeek = 0b111_1111,
                IsActive = true
            });
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("DemoSeed: created daily Bangalore→Chennai schedule at 09:00");
        }

        _logger.LogInformation("DemoSeed complete");
    }

    private async Task<City> UpsertCityAsync(string name, string state, CancellationToken ct)
    {
        var existing = await _db.Cities.FirstOrDefaultAsync(c => c.Name == name, ct);
        if (existing is not null) return existing;
        var city = new City { Id = Guid.NewGuid(), Name = name, State = state, IsActive = true };
        _db.Cities.Add(city);
        await _db.SaveChangesAsync(ct);
        return city;
    }

    private async Task<User> UpsertUserAsync(string email, string name, string password, CancellationToken ct)
    {
        var existing = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (existing is not null) return existing;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            PasswordHash = _hasher.Hash(password),
            CreatedAt = _clock.GetUtcNow().UtcDateTime,
            IsActive = true
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return user;
    }

    private async Task EnsureRoleAsync(User user, string role, CancellationToken ct)
    {
        var exists = await _db.UserRoles.AnyAsync(r => r.UserId == user.Id && r.Role == role, ct);
        if (exists) return;
        _db.UserRoles.Add(new UserRole { UserId = user.Id, Role = role });
        await _db.SaveChangesAsync(ct);
    }

    private async Task EnsureOfficeAsync(Guid operatorUserId, Guid cityId,
        string address, string phone, CancellationToken ct)
    {
        var exists = await _db.OperatorOffices.AnyAsync(
            o => o.OperatorUserId == operatorUserId && o.CityId == cityId, ct);
        if (exists) return;
        _db.OperatorOffices.Add(new OperatorOffice
        {
            Id = Guid.NewGuid(),
            OperatorUserId = operatorUserId,
            CityId = cityId,
            AddressLine = address,
            Phone = phone,
            IsActive = true
        });
        await _db.SaveChangesAsync(ct);
    }
}
