using Lullaby.Data;
using Microsoft.EntityFrameworkCore;

namespace Lullaby.Data;

public static class DatabaseExtensions
{
    /// <summary>
    /// Applies pending migrations and initializes the database.
    /// Should be called during application startup.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();

        try
        {
            if (dbContext.Database.IsRelational())
            {
                await dbContext.Database.MigrateAsync();
            }
            else
            {
                await dbContext.Database.EnsureCreatedAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database initialization error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Synchronous version for use in Program.cs if async is not available.
    /// </summary>
    public static void InitializeDatabase(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();

        try
        {
            if (dbContext.Database.IsRelational())
            {
                dbContext.Database.Migrate();
            }
            else
            {
                dbContext.Database.EnsureCreated();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database initialization error: {ex.Message}");
            throw;
        }
    }
}
