using BusBooking.Api.Infrastructure;
using BusBooking.Api.Models;
using BusBooking.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace BusBooking.Api.Tests.Unit;

public class PlatformFeeServiceTests
{
    private static AppDbContext NewInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"pf-{Guid.NewGuid():N}")
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Active_returns_most_recent_row_whose_EffectiveFrom_has_passed()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.Parse("2026-05-01T00:00:00Z"));
        await using var db = NewInMemoryDb();
        db.PlatformFeeConfigs.AddRange(
            new PlatformFeeConfig { Id = Guid.NewGuid(), FeeType = PlatformFeeType.Fixed,   Value =  20m,
                                    EffectiveFrom = DateTime.Parse("2026-01-01T00:00:00Z").ToUniversalTime() },
            new PlatformFeeConfig { Id = Guid.NewGuid(), FeeType = PlatformFeeType.Fixed,   Value =  25m,
                                    EffectiveFrom = DateTime.Parse("2026-03-01T00:00:00Z").ToUniversalTime() },
            new PlatformFeeConfig { Id = Guid.NewGuid(), FeeType = PlatformFeeType.Percent, Value =   5m,
                                    EffectiveFrom = DateTime.Parse("2026-06-01T00:00:00Z").ToUniversalTime() });
        await db.SaveChangesAsync();

        var svc = new PlatformFeeService(db, clock);
        var active = await svc.GetActiveAsync(CancellationToken.None);

        active.FeeType.Should().Be(PlatformFeeType.Fixed);
        active.Value.Should().Be(25m);
    }

    [Fact]
    public async Task Update_inserts_new_row_and_becomes_active()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.Parse("2026-05-01T00:00:00Z"));
        await using var db = NewInMemoryDb();
        var svc = new PlatformFeeService(db, clock);

        var adminId = Guid.NewGuid();
        var created = await svc.UpdateAsync(adminId,
            new Dtos.UpdatePlatformFeeRequest { FeeType = PlatformFeeType.Fixed, Value = 30m },
            CancellationToken.None);

        created.Value.Should().Be(30m);
        (await db.PlatformFeeConfigs.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Percent_value_above_100_is_rejected()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.Parse("2026-05-01T00:00:00Z"));
        await using var db = NewInMemoryDb();
        var svc = new PlatformFeeService(db, clock);

        var act = async () => await svc.UpdateAsync(Guid.NewGuid(),
            new Dtos.UpdatePlatformFeeRequest { FeeType = PlatformFeeType.Percent, Value = 101m },
            CancellationToken.None);

        await act.Should().ThrowAsync<BusBooking.Api.Infrastructure.Errors.BusinessRuleException>();
    }
}
