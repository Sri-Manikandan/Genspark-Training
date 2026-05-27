using ExcelAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ExcelAPI.Contexts
{
    public class UserContext: DbContext
    {
        public UserContext(DbContextOptions<UserContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(u =>
            {
                u.HasKey(u => u.Id).HasName("PK_UserId");
                u.Property(u => u.Name).HasMaxLength(100).IsRequired();
                u.Property(u => u.Email).HasMaxLength(100).IsRequired();
                u.Property(u => u.Phone).HasMaxLength(12).IsRequired();
            });
        }
    }
}