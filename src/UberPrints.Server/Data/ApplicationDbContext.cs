using Microsoft.EntityFrameworkCore;
using UberPrints.Server.Models;

namespace UberPrints.Server.Data;

public class ApplicationDbContext : DbContext
{
  public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

  public DbSet<User> Users { get; set; }
  public DbSet<PrintRequest> PrintRequests { get; set; }
  public DbSet<StatusHistory> StatusHistories { get; set; }
  public DbSet<Filament> Filaments { get; set; }
  public DbSet<FilamentRequest> FilamentRequests { get; set; }
  public DbSet<FilamentRequestStatusHistory> FilamentRequestStatusHistories { get; set; }
  public DbSet<PrintRequestChange> PrintRequestChanges { get; set; }
  public DbSet<Printer> Printers { get; set; }
  public DbSet<PrinterStatusHistory> PrinterStatusHistories { get; set; }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    // UUID defaults
    modelBuilder.Entity<User>().Property(u => u.Id).HasDefaultValueSql("uuidv7()");
    modelBuilder.Entity<PrintRequest>().Property(p => p.Id).HasDefaultValueSql("uuidv7()");
    modelBuilder.Entity<StatusHistory>().Property(s => s.Id).HasDefaultValueSql("uuidv7()");
    modelBuilder.Entity<Filament>().Property(f => f.Id).HasDefaultValueSql("uuidv7()");
    modelBuilder.Entity<FilamentRequest>().Property(f => f.Id).HasDefaultValueSql("uuidv7()");
    modelBuilder.Entity<FilamentRequestStatusHistory>().Property(f => f.Id).HasDefaultValueSql("uuidv7()");
    modelBuilder.Entity<PrintRequestChange>().Property(p => p.Id).HasDefaultValueSql("uuidv7()");
    modelBuilder.Entity<Printer>().Property(p => p.Id).HasDefaultValueSql("uuidv7()");
    modelBuilder.Entity<PrinterStatusHistory>().Property(p => p.Id).HasDefaultValueSql("uuidv7()");

    // Enum conversions
    modelBuilder.Entity<PrintRequest>().Property(p => p.CurrentStatus).HasConversion<string>();
    modelBuilder.Entity<StatusHistory>().Property(s => s.Status).HasConversion<string>();
    modelBuilder.Entity<FilamentRequest>().Property(f => f.CurrentStatus).HasConversion<string>();
    modelBuilder.Entity<FilamentRequestStatusHistory>().Property(f => f.Status).HasConversion<string>();
    modelBuilder.Entity<Printer>().Property(p => p.CurrentState).HasConversion<string>();
    modelBuilder.Entity<PrinterStatusHistory>().Property(p => p.State).HasConversion<string>();

    // Indexes
    modelBuilder.Entity<User>().HasIndex(u => u.DiscordId).IsUnique();
    modelBuilder.Entity<User>().HasIndex(u => u.GuestSessionToken).IsUnique();
    modelBuilder.Entity<PrintRequest>().HasIndex(p => p.GuestTrackingToken).IsUnique();

  }
}
