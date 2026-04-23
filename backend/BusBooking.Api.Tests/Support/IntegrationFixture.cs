using BusBooking.Api.Infrastructure;
using BusBooking.Api.Infrastructure.Razorpay;
using BusBooking.Api.Infrastructure.Resend;
using BusBooking.Api.Infrastructure.Seeding;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace BusBooking.Api.Tests.Support;

public class IntegrationFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private HttpClient? _client;
    public HttpClient Client => _client ??= CreateClient();

    public FakeRazorpayClient Razorpay { get; } = new();
    public FakeResendEmailClient Email { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            // AddDbContext registers three service descriptors — strip all of them before re-adding.
            var toRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                || d.ServiceType == typeof(DbContextOptions)
                || d.ServiceType == typeof(AppDbContext)
                || d.ServiceType == typeof(IRazorpayClient)
                || d.ServiceType == typeof(IResendEmailClient)).ToList();
            foreach (var d in toRemove) services.Remove(d);

            using var tmp = services.BuildServiceProvider();
            var cfg = tmp.GetRequiredService<IConfiguration>();
            var devConn = cfg.GetConnectionString("Default")
                ?? throw new InvalidOperationException("ConnectionStrings:Default missing in test environment");
            var csb = new NpgsqlConnectionStringBuilder(devConn)
            {
                SearchPath = "bus_booking_test,public"
            };
            var testConn = csb.ConnectionString;

            EnsureSchemaExistsAsync(devConn).GetAwaiter().GetResult();

            services.AddDbContext<AppDbContext>(o => o.UseNpgsql(testConn));
            services.AddSingleton<IRazorpayClient>(_ => Razorpay);
            services.AddSingleton<IResendEmailClient>(_ => Email);
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
            "TRUNCATE TABLE "
            + "bus_booking_test.notifications, "
            + "bus_booking_test.payments, "
            + "bus_booking_test.booking_seats, "
            + "bus_booking_test.bookings, "
            + "bus_booking_test.seat_locks, "
            + "bus_booking_test.bus_trips, "
            + "bus_booking_test.bus_schedules, "
            + "bus_booking_test.audit_log, "
            + "bus_booking_test.seat_definitions, "
            + "bus_booking_test.buses, "
            + "bus_booking_test.operator_offices, "
            + "bus_booking_test.operator_requests, "
            + "bus_booking_test.platform_fee_config, "
            + "bus_booking_test.routes, "
            + "bus_booking_test.cities, "
            + "bus_booking_test.user_roles, "
            + "bus_booking_test.users "
            + "RESTART IDENTITY CASCADE");
        while (Razorpay.CreatedOrders.Count > 0) Razorpay.CreatedOrders.Clear();
        while (Email.Sent.TryDequeue(out _)) { }
        var seeder = scope.ServiceProvider.GetRequiredService<IPlatformFeeSeeder>();
        await seeder.SeedAsync(CancellationToken.None);
    }

    private static async Task EnsureSchemaExistsAsync(string devConn)
    {
        await using var conn = new NpgsqlConnection(devConn);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "CREATE SCHEMA IF NOT EXISTS bus_booking_test AUTHORIZATION CURRENT_USER;";
        await cmd.ExecuteNonQueryAsync();
        NpgsqlConnection.ClearAllPools();
    }
}
