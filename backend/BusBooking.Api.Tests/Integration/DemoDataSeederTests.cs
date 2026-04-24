using BusBooking.Api.Infrastructure;
using BusBooking.Api.Infrastructure.Seeding;
using BusBooking.Api.Models;
using BusBooking.Api.Tests.Support;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace BusBooking.Api.Tests.Integration;

[Collection("Integration")]
public class DemoDataSeederTests : IAsyncLifetime
{
    private readonly IntegrationFixture _fx;
    public DemoDataSeederTests(IntegrationFixture fx) { _fx = fx; }

    public Task InitializeAsync() => _fx.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SeedAsync_is_a_no_op_when_disabled()
    {
        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var seeder = BuildSeeder(scope, enabled: false);
        await seeder.SeedAsync(CancellationToken.None);

        (await db.Cities.CountAsync()).Should().Be(0);
        (await db.Users.CountAsync()).Should().Be(0);
        (await db.Buses.CountAsync()).Should().Be(0);
        (await db.BusSchedules.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task SeedAsync_populates_expected_demo_rows_when_enabled()
    {
        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var seeder = BuildSeeder(scope, enabled: true);
        await seeder.SeedAsync(CancellationToken.None);

        (await db.Cities.CountAsync()).Should().Be(2);
        (await db.Cities.AnyAsync(c => c.Name == "Bangalore")).Should().BeTrue();
        (await db.Cities.AnyAsync(c => c.Name == "Chennai")).Should().BeTrue();

        (await db.Routes.CountAsync()).Should().Be(1);

        var op = await db.Users.SingleAsync(u => u.Email == "operator@demo.local");
        var opRoles = await db.UserRoles.Where(r => r.UserId == op.Id).Select(r => r.Role).ToListAsync();
        opRoles.Should().Contain(new[] { Roles.Customer, Roles.Operator });

        (await db.Users.AnyAsync(u => u.Email == "customer@demo.local")).Should().BeTrue();

        var opReq = await db.OperatorRequests.SingleAsync(r => r.UserId == op.Id);
        opReq.Status.Should().Be(OperatorRequestStatus.Approved);

        (await db.OperatorOffices.CountAsync(o => o.OperatorUserId == op.Id)).Should().Be(2);

        var bus = await db.Buses.SingleAsync(b => b.RegistrationNumber == "KA01DEMO1234");
        bus.ApprovalStatus.Should().Be(BusApprovalStatus.Approved);
        bus.Capacity.Should().Be(40);
        (await db.SeatDefinitions.CountAsync(s => s.BusId == bus.Id)).Should().Be(40);

        var sched = await db.BusSchedules.SingleAsync(s => s.BusId == bus.Id);
        sched.FarePerSeat.Should().Be(500m);
        sched.DaysOfWeek.Should().Be(0b111_1111);
    }

    [Fact]
    public async Task SeedAsync_is_idempotent_when_run_twice()
    {
        using var scope = _fx.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var seeder = BuildSeeder(scope, enabled: true);
        await seeder.SeedAsync(CancellationToken.None);
        await seeder.SeedAsync(CancellationToken.None);

        (await db.Cities.CountAsync()).Should().Be(2);
        (await db.Routes.CountAsync()).Should().Be(1);
        (await db.Users.CountAsync(u => u.Email == "operator@demo.local")).Should().Be(1);
        (await db.Users.CountAsync(u => u.Email == "customer@demo.local")).Should().Be(1);
        (await db.OperatorRequests.CountAsync()).Should().Be(1);
        (await db.OperatorOffices.CountAsync()).Should().Be(2);
        (await db.Buses.CountAsync()).Should().Be(1);
        (await db.SeatDefinitions.CountAsync()).Should().Be(40);
        (await db.BusSchedules.CountAsync()).Should().Be(1);
    }

    private static IDemoDataSeeder BuildSeeder(IServiceScope scope, bool enabled)
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<BusBooking.Api.Infrastructure.Auth.IPasswordHasher>();
        var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DemoDataSeeder>>();
        var opts = Options.Create(new DemoDataSeedOptions { Enabled = enabled });
        return new DemoDataSeeder(db, hasher, opts, clock, logger);
    }
}
