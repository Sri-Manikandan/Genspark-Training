using BusBooking.Api.Infrastructure;
using BusBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Api.Infrastructure.Seeding;

public class PlatformFeeSeeder : IPlatformFeeSeeder
{
    private readonly AppDbContext _db;
    private readonly ILogger<PlatformFeeSeeder> _log;

    public PlatformFeeSeeder(AppDbContext db, ILogger<PlatformFeeSeeder> log)
    {
        _db = db;
        _log = log;
    }

    public async Task SeedAsync(CancellationToken ct)
    {
        if (await _db.PlatformFeeConfigs.AnyAsync(ct))
        {
            _log.LogInformation("Platform fee config already present; skipping seed");
            return;
        }

        _db.PlatformFeeConfigs.Add(new PlatformFeeConfig
        {
            Id = Guid.NewGuid(),
            FeeType = PlatformFeeType.Fixed,
            Value = 25.00m,
            EffectiveFrom = DateTime.UtcNow,
            CreatedByAdminId = Guid.Empty
        });
        await _db.SaveChangesAsync(ct);
        _log.LogInformation("Seeded default platform fee: fixed ₹25.00");
    }
}
