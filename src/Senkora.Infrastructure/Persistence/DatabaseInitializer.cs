using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Senkora.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    /// <summary>
    /// Applies pending migrations and seeds initial data.
    /// Call this from Program.cs on startup.
    /// </summary>
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db     = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            logger.LogInformation("Applying database migrations...");
            await db.Database.MigrateAsync();
            logger.LogInformation("Migrations applied successfully");

            logger.LogInformation("Seeding initial data...");
            await SeedData.SeedAsync(db, logger);
            logger.LogInformation("Seeding completed");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Database initialization failed");
            throw;
        }
    }
}
