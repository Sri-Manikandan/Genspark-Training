namespace BusBooking.Api.Infrastructure.Seeding;

public interface IPlatformFeeSeeder
{
    Task SeedAsync(CancellationToken ct);
}
