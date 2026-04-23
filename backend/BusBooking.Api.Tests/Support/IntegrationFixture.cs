using BusBooking.Api.Infrastructure;
using BusBooking.Api.Infrastructure.Seeding;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BusBooking.Api.Tests.Support;

public class IntegrationFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private HttpClient? _client;
    public HttpClient Client => _client ??= CreateClient();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            // AddDbContext registers three service descriptors — strip all of them before re-adding.
            var toRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                || d.ServiceType == typeof(DbContextOptions)
                || d.ServiceType == typeof(AppDbContext)).ToList();
            foreach (var d in toRemove) services.Remove(d);

            using var tmp = services.BuildServiceProvider();
            var cfg = tmp.GetRequiredService<IConfiguration>();
            var devConn = cfg.GetConnectionString("Default")
                ?? throw new InvalidOperationException("ConnectionStrings:Default missing in test environment");
            var testConn = devConn.Replace("Database=bus_booking", "Database=bus_booking_test");
            if (testConn == devConn)
                throw new InvalidOperationException("Could not derive test connection string from dev connection");

            services.AddDbContext<AppDbContext>(o => o.UseNpgsql(testConn));
        });
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await ResetAsync();
    }

    public new Task DisposeAsync() => Task.CompletedTask;

    public async Task ResetAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE bus_trips, bus_schedules, audit_log, seat_definitions, buses, operator_offices, operator_requests, "
            + "platform_fee_config, routes, cities, user_roles, users "
            + "RESTART IDENTITY CASCADE");
        var seeder = scope.ServiceProvider.GetRequiredService<IPlatformFeeSeeder>();
        await seeder.SeedAsync(CancellationToken.None);
    }
}
