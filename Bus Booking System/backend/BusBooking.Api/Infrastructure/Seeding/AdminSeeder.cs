using BusBooking.Api.Infrastructure.Auth;
using BusBooking.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BusBooking.Api.Infrastructure.Seeding;

public class AdminSeeder : IAdminSeeder
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly AdminSeedOptions _options;
    private readonly ILogger<AdminSeeder> _logger;

    public AdminSeeder(
        AppDbContext db,
        IPasswordHasher hasher,
        IOptions<AdminSeedOptions> options,
        ILogger<AdminSeeder> logger)
    {
        _db = db;
        _hasher = hasher;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.Email)
            || string.IsNullOrWhiteSpace(_options.Password)
            || string.IsNullOrWhiteSpace(_options.Name))
        {
            _logger.LogWarning("AdminSeed config incomplete; skipping admin seed");
            return;
        }

        var exists = await _db.Users.AnyAsync(u => u.Email == _options.Email, ct);
        if (exists)
        {
            _logger.LogInformation("Admin {Email} already exists; seed skipped", _options.Email);
            return;
        }

        var admin = new User
        {
            Id = Guid.NewGuid(),
            Name = _options.Name,
            Email = _options.Email,
            PasswordHash = _hasher.Hash(_options.Password),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        admin.Roles.Add(new UserRole { UserId = admin.Id, Role = Roles.Admin });

        _db.Users.Add(admin);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Seeded admin {Email}", _options.Email);
    }
}
