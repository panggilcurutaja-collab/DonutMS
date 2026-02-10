using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DonutMS.Data.DbContext;

namespace DonutMS.Data.Extensions;

public static class DatabaseServiceExtensions
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
        await initializer.InitializeAsync();
    }

    public static async Task MigrateDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DonutMSDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DonutMSDbContext>>();

        try
        {
            logger.LogInformation("Applying database migrations...");
            await context.Database.GetDbConnection().OpenAsync();
            await context.Database.ExecuteSqlRawAsync("VACUUM");
            var migrations = (await context.Database.GetAppliedMigrationsAsync()).ToList();
            logger.LogInformation("Database migrations applied successfully. Applied migrations: {Count}", migrations.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating the database");
            throw;
        }
    }

    public static async Task ResetDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DonutMSDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DonutMSDbContext>>();

        try
        {
            logger.LogWarning("Deleting database...");
            await context.Database.EnsureDeletedAsync();

            logger.LogInformation("Creating database...");
            await context.Database.EnsureCreatedAsync();

            var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
            await initializer.InitializeAsync();

            logger.LogInformation("Database reset completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while resetting the database");
            throw;
        }
    }
}
