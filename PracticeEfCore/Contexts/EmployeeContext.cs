using Microsoft.EntityFrameworkCore;

namespace PracticeEfCore
{
    public class EmployeeContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=employeedb;Username=postgres;Password=Poornima290178@");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Employee>().HasKey(e => e.EmployeeId);
        }

        public DbSet<Employee> Employees { get; set; }
    }
}