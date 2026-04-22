using BusBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Api.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();

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
    }
}
