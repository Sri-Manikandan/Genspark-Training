using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Day_2_Part_2.Models;

namespace Day_2_Part_2.Contexts;

public partial class TransactionDbContext : DbContext
{
    public TransactionDbContext()
    {
    }

    public TransactionDbContext(DbContextOptions<TransactionDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<Tran> Trans { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=transaction_db;Username=postgres;Password=Poornima290178@");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("account");

            entity.Property(e => e.Aacno).HasColumnName("aacno");
            entity.Property(e => e.Balance).HasColumnName("balance");
        });

        modelBuilder.Entity<Tran>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("trans_pkey");

            entity.ToTable("trans");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Amount).HasColumnName("amount");
            entity.Property(e => e.Fromacc).HasColumnName("fromacc");
            entity.Property(e => e.Toacc).HasColumnName("toacc");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
