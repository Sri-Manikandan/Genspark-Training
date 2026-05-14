using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Day_2.models;

public partial class BankingdbContext : DbContext
{
    public BankingdbContext()
    {
    }

    public BankingdbContext(DbContextOptions<BankingdbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=bankingdb;Username=postgres;Password=Poornima290178@");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.AccountNumber);

            entity.ToTable("accounts");

            entity.HasIndex(e => e.CustomerId, "IX_accounts_CustomerId");

            entity.HasOne(d => d.Customer).WithMany(p => p.Accounts).HasForeignKey(d => d.CustomerId);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("customers");

            entity.Property(e => e.DateOfBirth).HasColumnType("timestamp without time zone");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
