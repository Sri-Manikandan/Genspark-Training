using BusBooking.Api.Models;
using Microsoft.EntityFrameworkCore;
using Route = BusBooking.Api.Models.Route;

namespace BusBooking.Api.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<Route> Routes => Set<Route>();
    public DbSet<PlatformFeeConfig> PlatformFeeConfigs => Set<PlatformFeeConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.HasPostgresExtension("pg_trgm");

        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("users");
            b.HasKey(u => u.Id);
            b.Property(u => u.Id).HasColumnName("id");
            b.Property(u => u.Name).HasColumnName("name").IsRequired().HasMaxLength(120);
            b.Property(u => u.Email).HasColumnName("email").HasColumnType("citext").IsRequired().HasMaxLength(254);
            b.Property(u => u.PasswordHash).HasColumnName("password_hash").IsRequired();
            b.Property(u => u.Phone).HasColumnName("phone").HasMaxLength(32);
            b.Property(u => u.CreatedAt).HasColumnName("created_at");
            b.Property(u => u.IsActive).HasColumnName("is_active");
            b.Property(u => u.OperatorDisabledAt).HasColumnName("operator_disabled_at");
            b.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<UserRole>(b =>
        {
            b.ToTable("user_roles");
            b.HasKey(r => new { r.UserId, r.Role });
            b.Property(r => r.UserId).HasColumnName("user_id");
            b.Property(r => r.Role).HasColumnName("role").IsRequired().HasMaxLength(20);
            b.HasOne(r => r.User)
                .WithMany(u => u.Roles)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<City>(b =>
        {
            b.ToTable("cities");
            b.HasKey(c => c.Id);
            b.Property(c => c.Id).HasColumnName("id");
            b.Property(c => c.Name).HasColumnName("name").HasColumnType("citext").IsRequired().HasMaxLength(120);
            b.Property(c => c.State).HasColumnName("state").IsRequired().HasMaxLength(120);
            b.Property(c => c.IsActive).HasColumnName("is_active");
            b.HasIndex(c => c.Name).IsUnique();
            // The gin_trgm_ops GIN index is added by raw SQL in the migration (see Task 2).
        });

        modelBuilder.Entity<Route>(b =>
        {
            b.ToTable("routes");
            b.HasKey(r => r.Id);
            b.Property(r => r.Id).HasColumnName("id");
            b.Property(r => r.SourceCityId).HasColumnName("source_city_id");
            b.Property(r => r.DestinationCityId).HasColumnName("destination_city_id");
            b.Property(r => r.DistanceKm).HasColumnName("distance_km");
            b.Property(r => r.IsActive).HasColumnName("is_active");
            b.HasIndex(r => new { r.SourceCityId, r.DestinationCityId }).IsUnique();
            b.HasOne(r => r.SourceCity)
                .WithMany()
                .HasForeignKey(r => r.SourceCityId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(r => r.DestinationCity)
                .WithMany()
                .HasForeignKey(r => r.DestinationCityId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PlatformFeeConfig>(b =>
        {
            b.ToTable("platform_fee_config");
            b.HasKey(p => p.Id);
            b.Property(p => p.Id).HasColumnName("id");
            b.Property(p => p.FeeType).HasColumnName("fee_type").IsRequired().HasMaxLength(16);
            b.Property(p => p.Value).HasColumnName("value").HasColumnType("decimal(10,2)");
            b.Property(p => p.EffectiveFrom).HasColumnName("effective_from");
            b.Property(p => p.CreatedByAdminId).HasColumnName("created_by_admin_id");
            b.HasIndex(p => p.EffectiveFrom);
        });
    }
}
