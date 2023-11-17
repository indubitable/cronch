using cronch.Models;
using Microsoft.EntityFrameworkCore;

namespace cronch;

public class CronchDbContext(DbContextOptions<CronchDbContext> options) : DbContext(options)
{
    public DbSet<ExecutionModel> Executions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExecutionModel>()
            .ToTable("Execution");

        modelBuilder.Entity<ExecutionModel>()
            .Property(e => e.StartedOn)
            .HasConversion(
                p => p.UtcDateTime,
                p => new DateTimeOffset(DateTime.SpecifyKind(p, DateTimeKind.Utc))
            );
        modelBuilder.Entity<ExecutionModel>()
            .Property(e => e.CompletedOn)
            .HasConversion(
                p => p.HasValue ? p.Value.UtcDateTime : (DateTime?)null,
                p => p.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(p.Value, DateTimeKind.Utc)) : (DateTimeOffset?)null
            );

        modelBuilder.Entity<ExecutionModel>()
            .Property(e => e.StartReason)
            .HasConversion<string>();
        modelBuilder.Entity<ExecutionModel>()
            .Property(e => e.Status)
            .HasConversion<string>();
        modelBuilder.Entity<ExecutionModel>()
            .Property(e => e.StopReason)
            .HasConversion<string>();
    }
}
