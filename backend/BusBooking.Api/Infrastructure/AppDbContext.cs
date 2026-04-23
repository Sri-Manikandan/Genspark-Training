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
    public DbSet<OperatorRequest> OperatorRequests => Set<OperatorRequest>();
    public DbSet<OperatorOffice> OperatorOffices => Set<OperatorOffice>();
    public DbSet<Bus> Buses => Set<Bus>();
    public DbSet<SeatDefinition> SeatDefinitions => Set<SeatDefinition>();
    public DbSet<AuditLogEntry> AuditLog => Set<AuditLogEntry>();

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

        modelBuilder.Entity<OperatorRequest>(b =>
        {
            b.ToTable("operator_requests");
            b.HasKey(r => r.Id);
            b.Property(r => r.Id).HasColumnName("id");
            b.Property(r => r.UserId).HasColumnName("user_id");
            b.Property(r => r.Status).HasColumnName("status").IsRequired().HasMaxLength(16);
            b.Property(r => r.CompanyName).HasColumnName("company_name").IsRequired().HasMaxLength(160);
            b.Property(r => r.RequestedAt).HasColumnName("requested_at");
            b.Property(r => r.ReviewedAt).HasColumnName("reviewed_at");
            b.Property(r => r.ReviewedByAdminId).HasColumnName("reviewed_by_admin_id");
            b.Property(r => r.RejectReason).HasColumnName("reject_reason").HasMaxLength(500);
            b.HasIndex(r => new { r.UserId, r.Status });
            b.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OperatorOffice>(b =>
        {
            b.ToTable("operator_offices");
            b.HasKey(o => o.Id);
            b.Property(o => o.Id).HasColumnName("id");
            b.Property(o => o.OperatorUserId).HasColumnName("operator_user_id");
            b.Property(o => o.CityId).HasColumnName("city_id");
            b.Property(o => o.AddressLine).HasColumnName("address_line").IsRequired().HasMaxLength(300);
            b.Property(o => o.Phone).HasColumnName("phone").IsRequired().HasMaxLength(32);
            b.Property(o => o.IsActive).HasColumnName("is_active");
            b.HasIndex(o => new { o.OperatorUserId, o.CityId }).IsUnique();
            b.HasOne(o => o.Operator)
                .WithMany()
                .HasForeignKey(o => o.OperatorUserId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(o => o.City)
                .WithMany()
                .HasForeignKey(o => o.CityId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Bus>(b =>
        {
            b.ToTable("buses");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.OperatorUserId).HasColumnName("operator_user_id");
            b.Property(x => x.RegistrationNumber).HasColumnName("registration_number")
                .HasColumnType("citext").IsRequired().HasMaxLength(32);
            b.Property(x => x.BusName).HasColumnName("bus_name").IsRequired().HasMaxLength(120);
            b.Property(x => x.BusType).HasColumnName("bus_type").IsRequired().HasMaxLength(16);
            b.Property(x => x.Capacity).HasColumnName("capacity");
            b.Property(x => x.ApprovalStatus).HasColumnName("approval_status").IsRequired().HasMaxLength(16);
            b.Property(x => x.OperationalStatus).HasColumnName("operational_status").IsRequired().HasMaxLength(20);
            b.Property(x => x.CreatedAt).HasColumnName("created_at");
            b.Property(x => x.ApprovedAt).HasColumnName("approved_at");
            b.Property(x => x.ApprovedByAdminId).HasColumnName("approved_by_admin_id");
            b.Property(x => x.RejectReason).HasColumnName("reject_reason").HasMaxLength(500);
            b.HasIndex(x => x.RegistrationNumber).IsUnique();
            b.HasIndex(x => new { x.OperatorUserId, x.ApprovalStatus });
            b.HasOne(x => x.Operator)
                .WithMany()
                .HasForeignKey(x => x.OperatorUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SeatDefinition>(b =>
        {
            b.ToTable("seat_definitions");
            b.HasKey(s => s.Id);
            b.Property(s => s.Id).HasColumnName("id");
            b.Property(s => s.BusId).HasColumnName("bus_id");
            b.Property(s => s.SeatNumber).HasColumnName("seat_number").IsRequired().HasMaxLength(8);
            b.Property(s => s.RowIndex).HasColumnName("row_index");
            b.Property(s => s.ColumnIndex).HasColumnName("column_index");
            b.Property(s => s.SeatCategory).HasColumnName("seat_category").IsRequired().HasMaxLength(16);
            b.HasIndex(s => new { s.BusId, s.SeatNumber }).IsUnique();
            b.HasOne(s => s.Bus)
                .WithMany(x => x.Seats)
                .HasForeignKey(s => s.BusId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditLogEntry>(b =>
        {
            b.ToTable("audit_log");
            b.HasKey(a => a.Id);
            b.Property(a => a.Id).HasColumnName("id");
            b.Property(a => a.ActorUserId).HasColumnName("actor_user_id");
            b.Property(a => a.Action).HasColumnName("action").IsRequired().HasMaxLength(64);
            b.Property(a => a.TargetType).HasColumnName("target_type").IsRequired().HasMaxLength(64);
            b.Property(a => a.TargetId).HasColumnName("target_id");
            b.Property(a => a.MetadataJson).HasColumnName("metadata").HasColumnType("jsonb");
            b.Property(a => a.CreatedAt).HasColumnName("created_at");
            b.HasIndex(a => new { a.TargetType, a.TargetId });
        });
    }
}
