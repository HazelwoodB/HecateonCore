using Microsoft.Extensions.DependencyInjection;
using Hecateon.Data;
using Microsoft.EntityFrameworkCore;

namespace Hecateon;

public static class ServiceCollectionExtensions
{
    public static void InitializeDatabase(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
        
        try
        {
            // Ensure database is created and migrations are applied
            dbContext.Database.Migrate();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DB] Warning: Database initialization encountered an issue: {ex.Message}");
            // Continue anyway - some operations might still work
        }
    }
}
