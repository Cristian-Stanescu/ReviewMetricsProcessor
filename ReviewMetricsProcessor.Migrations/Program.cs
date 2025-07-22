using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReviewMetricsProcessor.Data;

namespace ReviewMetricsProcessor.Migrations;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // Add logging
        builder.Services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Information);
        });

        // Add the DbContext using the factory
        builder.Services.AddSingleton<ReviewMetricsDbContextFactory>();
        builder.Services.AddScoped(serviceProvider =>
        {
            var factory = serviceProvider.GetRequiredService<ReviewMetricsDbContextFactory>();
            return factory.CreateDbContext(args);
        });

        using var host = builder.Build();

        // Apply migrations
        try
        {
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            var context = host.Services.GetRequiredService<ReviewMetricsDbContext>();

            logger.LogInformation("Starting database migration process...");

            // Check if database exists and can be connected to
            logger.LogInformation("Testing database connection...");
            await context.Database.CanConnectAsync();
            logger.LogInformation("Database connection successful.");

            // Check if there are pending migrations
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                logger.LogInformation("Found {Count} pending migrations: {Migrations}",
                    pendingMigrations.Count(),
                    string.Join(", ", pendingMigrations));

                await context.Database.MigrateAsync();
                logger.LogInformation("Database migrations applied successfully.");
            }
            else
            {
                logger.LogInformation("No pending migrations found. Database is up to date.");
            }

            // Show current migration status
            var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
            logger.LogInformation("Total applied migrations: {Count}", appliedMigrations.Count());

            logger.LogInformation("Migration process completed successfully.");
        }
        catch (Exception ex)
        {
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while applying database migrations.");
            logger.LogError("Migration process failed. Please check the error details above.");
            Environment.Exit(1);
        }
    }
}
