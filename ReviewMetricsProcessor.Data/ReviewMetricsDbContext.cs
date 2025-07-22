using Microsoft.EntityFrameworkCore;
using ReviewMetricsProcessor.Data.Entities;

namespace ReviewMetricsProcessor.Data;

public class ReviewMetricsDbContext(DbContextOptions<ReviewMetricsDbContext> options) : DbContext(options)
{
    public DbSet<Author> Authors { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Author>(b => b.ToTable("authors"));
        modelBuilder.Entity<Review>(b => b.ToTable("reviews"));
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSnakeCaseNamingConvention();
    }
}