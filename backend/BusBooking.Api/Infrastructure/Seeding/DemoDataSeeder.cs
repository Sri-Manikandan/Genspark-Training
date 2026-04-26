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

        // ── Users ──────────────────────────────────────────────────────────
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
                RequestedAt = _clock.GetUtcNow().UtcDateTime.AddDays(-14),
                ReviewedAt = _clock.GetUtcNow().UtcDateTime.AddDays(-13),
                ReviewedByAdminId = null
            });
            await _db.SaveChangesAsync(ct);
        }

        // ── Cities ─────────────────────────────────────────────────────────
        var bangalore    = await UpsertCityAsync("Bangalore",    "Karnataka",        ct);
        var chennai      = await UpsertCityAsync("Chennai",      "Tamil Nadu",       ct);
        var hyderabad    = await UpsertCityAsync("Hyderabad",    "Telangana",        ct);
        var kochi        = await UpsertCityAsync("Kochi",        "Kerala",           ct);
        var coimbatore   = await UpsertCityAsync("Coimbatore",   "Tamil Nadu",       ct);
        var madurai      = await UpsertCityAsync("Madurai",      "Tamil Nadu",       ct);
        var mysore       = await UpsertCityAsync("Mysore",       "Karnataka",        ct);
        var tirupati     = await UpsertCityAsync("Tirupati",     "Andhra Pradesh",   ct);
        var vijayawada   = await UpsertCityAsync("Vijayawada",   "Andhra Pradesh",   ct);
        var mangalore    = await UpsertCityAsync("Mangalore",    "Karnataka",        ct);
        var pondicherry  = await UpsertCityAsync("Pondicherry",  "Puducherry",       ct);
        var trichy       = await UpsertCityAsync("Trichy",       "Tamil Nadu",       ct);

        // ── Operator offices ───────────────────────────────────────────────
        await EnsureOfficeAsync(operatorUser.Id, bangalore.Id,   "Kempegowda Bus Terminal, Bay 12",         "+91-80-2337-5566", ct);
        await EnsureOfficeAsync(operatorUser.Id, chennai.Id,     "CMBT, Platform 7",                        "+91-44-2478-9900", ct);
        await EnsureOfficeAsync(operatorUser.Id, hyderabad.Id,   "Mahatma Gandhi Bus Station, Counter 14",  "+91-40-2461-7890", ct);
        await EnsureOfficeAsync(operatorUser.Id, kochi.Id,       "KSRTC Bus Stand, Bay 3",                  "+91-484-237-6600", ct);
        await EnsureOfficeAsync(operatorUser.Id, coimbatore.Id,  "Gandhipuram Bus Stand, Counter 8",        "+91-422-230-1122", ct);
        await EnsureOfficeAsync(operatorUser.Id, madurai.Id,     "Mattuthavani Bus Terminal, Bay 5",        "+91-452-253-4411", ct);
        await EnsureOfficeAsync(operatorUser.Id, mysore.Id,      "Central Bus Stand, Counter 2",            "+91-821-252-7700", ct);
        await EnsureOfficeAsync(operatorUser.Id, mangalore.Id,   "KSRTC Bus Stand, Platform 6",             "+91-824-242-3300", ct);

        // ── Routes ─────────────────────────────────────────────────────────
        var blrChennai   = await UpsertRouteAsync(bangalore,   chennai,     342, ct);
        var chennaiBLR   = await UpsertRouteAsync(chennai,     bangalore,   342, ct);
        var blrHyd       = await UpsertRouteAsync(bangalore,   hyderabad,   568, ct);
        var hydBLR       = await UpsertRouteAsync(hyderabad,   bangalore,   568, ct);
        var blrKochi     = await UpsertRouteAsync(bangalore,   kochi,       540, ct);
        var kochiBLR     = await UpsertRouteAsync(kochi,       bangalore,   540, ct);
        var blrCbe       = await UpsertRouteAsync(bangalore,   coimbatore,  360, ct);
        var cbeBLR       = await UpsertRouteAsync(coimbatore,  bangalore,   360, ct);
        var blrMysore    = await UpsertRouteAsync(bangalore,   mysore,      143, ct);
        var mysoreBLR    = await UpsertRouteAsync(mysore,      bangalore,   143, ct);
        var blrMangalore = await UpsertRouteAsync(bangalore,   mangalore,   352, ct);
        var mangaloreBLR = await UpsertRouteAsync(mangalore,   bangalore,   352, ct);
        var blrTirupati  = await UpsertRouteAsync(bangalore,   tirupati,    280, ct);
        var tirupatiBLR  = await UpsertRouteAsync(tirupati,    bangalore,   280, ct);
        var chenCbe      = await UpsertRouteAsync(chennai,     coimbatore,  490, ct);
        var cbeChen      = await UpsertRouteAsync(coimbatore,  chennai,     490, ct);
        var chenMadurai  = await UpsertRouteAsync(chennai,     madurai,     460, ct);
        var maduraiChen  = await UpsertRouteAsync(madurai,     chennai,     460, ct);
        var chenPondy    = await UpsertRouteAsync(chennai,     pondicherry, 160, ct);
        var pondyChen    = await UpsertRouteAsync(pondicherry, chennai,     160, ct);
        var chenTrichy   = await UpsertRouteAsync(chennai,     trichy,      330, ct);
        var trichyChen   = await UpsertRouteAsync(trichy,      chennai,     330, ct);
        var hydVij       = await UpsertRouteAsync(hyderabad,   vijayawada,  275, ct);
        var vijHyd       = await UpsertRouteAsync(vijayawada,  hyderabad,   275, ct);
        var cbeMadurai   = await UpsertRouteAsync(coimbatore,  madurai,     210, ct);
        var maduraiCbe   = await UpsertRouteAsync(madurai,     coimbatore,  210, ct);

        // ── Buses ──────────────────────────────────────────────────────────
        var today = DateOnly.FromDateTime(_clock.GetUtcNow().UtcDateTime);

        // 1 — Demo Express (Seater 8×5 = 40)
        var demoExpress = await UpsertBusAsync(operatorUser.Id,
            "KA-01-DE-1001", "Demo Express",
            BusType.Seater, rows: 8, cols: 5, ct);

        // 2 — Sri Renuka Travels (Sleeper 9×4 = 36)
        var sriRenuka = await UpsertBusAsync(operatorUser.Id,
            "KA-01-SR-2001", "Sri Renuka Travels",
            BusType.Sleeper, rows: 9, cols: 4, ct);

        // 3 — Parveen Travels (Semi-Sleeper 10×5 = 50)
        var parveen = await UpsertBusAsync(operatorUser.Id,
            "KA-01-PT-3001", "Parveen Travels",
            BusType.SemiSleeper, rows: 10, cols: 5, ct);

        // 4 — SRS Travels (Sleeper 9×4 = 36)
        var srs = await UpsertBusAsync(operatorUser.Id,
            "KA-01-SRS-4001", "SRS Travels",
            BusType.Sleeper, rows: 9, cols: 4, ct);

        // 5 — VRL Travels (Seater 10×5 = 50)
        var vrl = await UpsertBusAsync(operatorUser.Id,
            "KA-01-VRL-5001", "VRL Travels",
            BusType.Seater, rows: 10, cols: 5, ct);

        // 6 — Chintamani Travels (Sleeper 8×4 = 32)
        var chintamani = await UpsertBusAsync(operatorUser.Id,
            "TN-09-CT-6001", "Chintamani Travels",
            BusType.Sleeper, rows: 8, cols: 4, ct);

        // 7 — Raj Shree Bus (Semi-Sleeper 9×5 = 45)
        var rajShree = await UpsertBusAsync(operatorUser.Id,
            "AP-28-RS-7001", "Raj Shree Bus",
            BusType.SemiSleeper, rows: 9, cols: 5, ct);

        // 8 — Kerala Express (Semi-Sleeper 8×5 = 40)
        var keralaExp = await UpsertBusAsync(operatorUser.Id,
            "KL-07-KE-8001", "Kerala Express",
            BusType.SemiSleeper, rows: 8, cols: 5, ct);

        // ── Schedules ──────────────────────────────────────────────────────
        // Bangalore ↔ Chennai
        await UpsertScheduleAsync(demoExpress.Id, blrChennai.Id,  "09:00", "14:00",  550m, today, ct);
        await UpsertScheduleAsync(sriRenuka.Id,   blrChennai.Id,  "22:00", "05:00",  900m, today, ct);
        await UpsertScheduleAsync(parveen.Id,     blrChennai.Id,  "06:30", "12:00",  700m, today, ct);
        await UpsertScheduleAsync(srs.Id,         chennaiBLR.Id,  "21:30", "04:00",  950m, today, ct);
        await UpsertScheduleAsync(vrl.Id,         chennaiBLR.Id,  "07:00", "12:30",  550m, today, ct);
        await UpsertScheduleAsync(chintamani.Id,  chennaiBLR.Id,  "23:00", "05:30",  850m, today, ct);

        // Bangalore ↔ Hyderabad
        await UpsertScheduleAsync(vrl.Id,       blrHyd.Id,  "20:30", "07:00", 1200m, today, ct);
        await UpsertScheduleAsync(srs.Id,       blrHyd.Id,  "21:30", "08:00", 1350m, today, ct);
        await UpsertScheduleAsync(sriRenuka.Id, hydBLR.Id,  "20:00", "07:00", 1300m, today, ct);
        await UpsertScheduleAsync(vrl.Id,       hydBLR.Id,  "22:00", "09:00", 1200m, today, ct);

        // Bangalore ↔ Kochi
        await UpsertScheduleAsync(keralaExp.Id, blrKochi.Id, "20:30", "08:00", 1100m, today, ct);
        await UpsertScheduleAsync(parveen.Id,   blrKochi.Id, "21:00", "09:00", 1050m, today, ct);
        await UpsertScheduleAsync(keralaExp.Id, kochiBLR.Id, "19:00", "06:30", 1100m, today, ct);
        await UpsertScheduleAsync(srs.Id,       kochiBLR.Id, "20:30", "08:00", 1150m, today, ct);

        // Bangalore ↔ Coimbatore
        await UpsertScheduleAsync(parveen.Id,  blrCbe.Id, "21:00", "03:30",  700m, today, ct);
        await UpsertScheduleAsync(rajShree.Id, blrCbe.Id, "22:30", "05:00",  750m, today, ct);
        await UpsertScheduleAsync(parveen.Id,  cbeBLR.Id, "22:00", "04:30",  700m, today, ct);
        await UpsertScheduleAsync(vrl.Id,      cbeBLR.Id, "08:00", "14:30",  600m, today, ct);

        // Bangalore ↔ Mysore
        await UpsertScheduleAsync(demoExpress.Id, blrMysore.Id, "07:00", "10:00", 200m, today, ct);
        await UpsertScheduleAsync(demoExpress.Id, blrMysore.Id, "14:00", "17:00", 200m, today, ct);
        await UpsertScheduleAsync(demoExpress.Id, mysoreBLR.Id, "08:00", "11:00", 200m, today, ct);
        await UpsertScheduleAsync(demoExpress.Id, mysoreBLR.Id, "16:00", "19:00", 200m, today, ct);

        // Bangalore ↔ Mangalore
        await UpsertScheduleAsync(srs.Id,       blrMangalore.Id, "22:00", "05:30",  850m, today, ct);
        await UpsertScheduleAsync(vrl.Id,        blrMangalore.Id, "21:00", "04:30",  800m, today, ct);
        await UpsertScheduleAsync(srs.Id,        mangaloreBLR.Id, "21:00", "04:30",  850m, today, ct);
        await UpsertScheduleAsync(keralaExp.Id,  mangaloreBLR.Id, "22:30", "06:00",  800m, today, ct);

        // Bangalore ↔ Tirupati
        await UpsertScheduleAsync(sriRenuka.Id, blrTirupati.Id,  "23:00", "05:30", 750m, today, ct);
        await UpsertScheduleAsync(rajShree.Id,  blrTirupati.Id,  "21:30", "04:00", 700m, today, ct);
        await UpsertScheduleAsync(sriRenuka.Id, tirupatiBLR.Id,  "22:00", "04:30", 750m, today, ct);
        await UpsertScheduleAsync(srs.Id,       tirupatiBLR.Id,  "23:30", "06:00", 800m, today, ct);

        // Chennai ↔ Coimbatore
        await UpsertScheduleAsync(chintamani.Id, chenCbe.Id, "22:00", "04:30", 650m, today, ct);
        await UpsertScheduleAsync(rajShree.Id,   chenCbe.Id, "07:00", "13:30", 600m, today, ct);
        await UpsertScheduleAsync(chintamani.Id, cbeChen.Id, "21:00", "03:30", 650m, today, ct);
        await UpsertScheduleAsync(vrl.Id,        cbeChen.Id, "06:00", "12:30", 580m, today, ct);

        // Chennai ↔ Madurai
        await UpsertScheduleAsync(rajShree.Id,   chenMadurai.Id, "22:30", "04:00",  450m, today, ct);
        await UpsertScheduleAsync(chintamani.Id, chenMadurai.Id, "07:30", "13:00",  400m, today, ct);
        await UpsertScheduleAsync(rajShree.Id,   maduraiChen.Id, "21:00", "02:30",  450m, today, ct);
        await UpsertScheduleAsync(parveen.Id,    maduraiChen.Id, "08:00", "13:30",  420m, today, ct);

        // Chennai ↔ Pondicherry
        await UpsertScheduleAsync(demoExpress.Id, chenPondy.Id, "07:00", "10:00", 180m, today, ct);
        await UpsertScheduleAsync(demoExpress.Id, chenPondy.Id, "14:00", "17:00", 180m, today, ct);
        await UpsertScheduleAsync(demoExpress.Id, pondyChen.Id, "08:00", "11:00", 180m, today, ct);
        await UpsertScheduleAsync(demoExpress.Id, pondyChen.Id, "16:00", "19:00", 180m, today, ct);

        // Chennai ↔ Trichy
        await UpsertScheduleAsync(chintamani.Id, chenTrichy.Id, "22:00", "04:00", 500m, today, ct);
        await UpsertScheduleAsync(vrl.Id,        chenTrichy.Id, "07:00", "13:00", 450m, today, ct);
        await UpsertScheduleAsync(chintamani.Id, trichyChen.Id, "21:00", "03:00", 500m, today, ct);
        await UpsertScheduleAsync(rajShree.Id,   trichyChen.Id, "06:30", "12:30", 460m, today, ct);

        // Hyderabad ↔ Vijayawada
        await UpsertScheduleAsync(vrl.Id,      hydVij.Id, "07:00", "12:00", 550m, today, ct);
        await UpsertScheduleAsync(srs.Id,      hydVij.Id, "14:00", "19:00", 500m, today, ct);
        await UpsertScheduleAsync(vrl.Id,      vijHyd.Id, "08:00", "13:00", 550m, today, ct);
        await UpsertScheduleAsync(rajShree.Id, vijHyd.Id, "15:00", "20:00", 520m, today, ct);

        // Coimbatore ↔ Madurai
        await UpsertScheduleAsync(chintamani.Id, cbeMadurai.Id,  "08:00", "11:30", 280m, today, ct);
        await UpsertScheduleAsync(chintamani.Id, cbeMadurai.Id,  "14:00", "17:30", 280m, today, ct);
        await UpsertScheduleAsync(rajShree.Id,   maduraiCbe.Id,  "09:00", "12:30", 280m, today, ct);
        await UpsertScheduleAsync(rajShree.Id,   maduraiCbe.Id,  "15:00", "18:30", 280m, today, ct);

        _logger.LogInformation("DemoSeed: South India bus network seeded successfully");
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private async Task<City> UpsertCityAsync(string name, string state, CancellationToken ct)
    {
        var existing = await _db.Cities.FirstOrDefaultAsync(c => c.Name == name, ct);
        if (existing is not null) return existing;
        var city = new City { Id = Guid.NewGuid(), Name = name, State = state, IsActive = true };
        _db.Cities.Add(city);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("DemoSeed: city {Name}", name);
        return city;
    }

    private async Task<Route> UpsertRouteAsync(City src, City dst, int distanceKm, CancellationToken ct)
    {
        var existing = await _db.Routes.FirstOrDefaultAsync(
            r => r.SourceCityId == src.Id && r.DestinationCityId == dst.Id, ct);
        if (existing is not null) return existing;
        var route = new Route
        {
            Id = Guid.NewGuid(),
            SourceCityId = src.Id,
            DestinationCityId = dst.Id,
            DistanceKm = distanceKm,
            IsActive = true
        };
        _db.Routes.Add(route);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("DemoSeed: route {Src} → {Dst}", src.Name, dst.Name);
        return route;
    }

    private async Task<Bus> UpsertBusAsync(Guid operatorUserId,
        string regNumber, string name, string busType,
        int rows, int cols, CancellationToken ct)
    {
        var existing = await _db.Buses.FirstOrDefaultAsync(b => b.RegistrationNumber == regNumber, ct);
        if (existing is not null) return existing;

        var bus = new Bus
        {
            Id = Guid.NewGuid(),
            OperatorUserId = operatorUserId,
            RegistrationNumber = regNumber,
            BusName = name,
            BusType = busType,
            Capacity = rows * cols,
            ApprovalStatus = BusApprovalStatus.Approved,
            OperationalStatus = BusOperationalStatus.Active,
            CreatedAt = _clock.GetUtcNow().UtcDateTime.AddDays(-10),
            ApprovedAt = _clock.GetUtcNow().UtcDateTime.AddDays(-9),
            ApprovedByAdminId = null
        };
        _db.Buses.Add(bus);
        _db.SeatDefinitions.AddRange(SeatLayoutGenerator.Generate(bus.Id, rows, cols));
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("DemoSeed: bus {Name} ({Reg})", name, regNumber);
        return bus;
    }

    private async Task UpsertScheduleAsync(Guid busId, Guid routeId,
        string departure, string arrival, decimal fare,
        DateOnly today, CancellationToken ct)
    {
        var dep = TimeOnly.Parse(departure);
        var arr = TimeOnly.Parse(arrival);

        var exists = await _db.BusSchedules.AnyAsync(
            s => s.BusId == busId && s.RouteId == routeId
              && s.DepartureTime == dep && s.ArrivalTime == arr, ct);
        if (exists) return;

        _db.BusSchedules.Add(new BusSchedule
        {
            Id = Guid.NewGuid(),
            BusId = busId,
            RouteId = routeId,
            DepartureTime = dep,
            ArrivalTime = arr,
            FarePerSeat = fare,
            ValidFrom = today,
            ValidTo = today.AddDays(90),
            DaysOfWeek = 0b111_1111,   // all 7 days
            IsActive = true
        });
        await _db.SaveChangesAsync(ct);
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
