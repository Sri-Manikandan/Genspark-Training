namespace BusBooking.Api.Infrastructure.Seeding;

public interface IDemoDataSeeder
{
    Task SeedAsync(CancellationToken ct);
}
