using DaoPlanImport.Entities;
using Microsoft.EntityFrameworkCore;

namespace DaoPlanImport.Data;

public class DaoPlanDbContext : DbContext
{
    public DaoPlanDbContext(DbContextOptions<DaoPlanDbContext> options) : base(options)
    {
    }

    public DbSet<Liga> Ligas { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Liga table
        modelBuilder.Entity<Liga>()
            .HasKey(x => x.Id);

        // Create index on FileName for faster filtering by source file
        modelBuilder.Entity<Liga>()
            .HasIndex(x => x.FileName);

        // Create index on ImportDate for chronological queries
        modelBuilder.Entity<Liga>()
            .HasIndex(x => x.ImportDate);

        // Create composite index for common queries
        modelBuilder.Entity<Liga>()
            .HasIndex(x => new { x.FileName, x.ImportDate });
    }
}
