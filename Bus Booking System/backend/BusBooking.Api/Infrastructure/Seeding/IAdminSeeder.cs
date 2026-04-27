namespace BusBooking.Api.Infrastructure.Seeding;

public interface IAdminSeeder
{
    Task SeedAsync(CancellationToken ct);
}
