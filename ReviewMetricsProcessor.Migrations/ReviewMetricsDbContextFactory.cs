using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using ReviewMetricsProcessor.Data;

namespace ReviewMetricsProcessor.Migrations;

public class ReviewMetricsDbContextFactory : IDesignTimeDbContextFactory<ReviewMetricsDbContext>
{
    public ReviewMetricsDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.Development.json"), true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=postgres;Database=reviewmetrics;Username=postgres;Password=YourStrong@Passw0rd";

        var optionsBuilder = new DbContextOptionsBuilder<ReviewMetricsDbContext>()
            .UseNpgsql(connectionString, sql =>
            {
                sql.MigrationsHistoryTable("__efmigrations_review_metrics");
                sql.MigrationsAssembly(typeof(ReviewMetricsDbContextFactory).Assembly.FullName);
            });

        return new ReviewMetricsDbContext(optionsBuilder.Options);
    }
}
