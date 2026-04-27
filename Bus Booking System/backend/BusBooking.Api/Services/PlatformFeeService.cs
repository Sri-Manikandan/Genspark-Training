using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Infrastructure.Errors;
using BusBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Api.Services;

public class PlatformFeeService : IPlatformFeeService
{
    private readonly AppDbContext _db;
    private readonly TimeProvider _time;

    public PlatformFeeService(AppDbContext db, TimeProvider time)
    {
        _db = db;
        _time = time;
    }

    public async Task<PlatformFeeDto> GetActiveAsync(CancellationToken ct)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var active = await _db.PlatformFeeConfigs
            .AsNoTracking()
            .Where(p => p.EffectiveFrom <= now)
            .OrderByDescending(p => p.EffectiveFrom)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("No active platform fee configured");
        return new PlatformFeeDto(active.FeeType, active.Value, active.EffectiveFrom);
    }

    public async Task<PlatformFeeDto> UpdateAsync(Guid adminUserId, UpdatePlatformFeeRequest request, CancellationToken ct)
    {
        if (request.FeeType == PlatformFeeType.Percent && request.Value > 100m)
            throw new BusinessRuleException("PLATFORM_FEE_OUT_OF_RANGE", "Percent fee cannot exceed 100");

        var row = new PlatformFeeConfig
        {
            Id = Guid.NewGuid(),
            FeeType = request.FeeType,
            Value = request.Value,
            EffectiveFrom = _time.GetUtcNow().UtcDateTime,
            CreatedByAdminId = adminUserId
        };
        _db.PlatformFeeConfigs.Add(row);
        await _db.SaveChangesAsync(ct);
        return new PlatformFeeDto(row.FeeType, row.Value, row.EffectiveFrom);
    }
}
