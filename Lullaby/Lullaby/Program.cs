using Hecateon.Components;
using Hecateon.Data;
using Hecateon.Models;
using Hecateon.Services;
using Hecateon.Core.EventStore;
using Hecateon.Core.DeviceRegistry;
using Hecateon.Core.Security;
using Hecateon.Modules.Nyphos.Services;
using Hecateon.Endpoints;
using Microsoft.EntityFrameworkCore;

public partial class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveWebAssemblyComponents();

        // Add database context
        var useInMemoryDb = IsTruthy(Environment.GetEnvironmentVariable("HECATEON_USE_INMEMORY_DB"))
                            || builder.Configuration.GetValue<bool>("Hecateon:UseInMemoryDb");
        var configuredProvider = Environment.GetEnvironmentVariable("HECATEON_DB_PROVIDER")
                                 ?? builder.Configuration["Hecateon:DatabaseProvider"];
        var databaseProvider = string.IsNullOrWhiteSpace(configuredProvider)
            ? "sqlite"
            : configuredProvider.Trim().ToLowerInvariant();

        if (useInMemoryDb)
        {
            builder.Services.AddDbContext<ChatDbContext>(options =>
                options.UseInMemoryDatabase("HecateonChatDb"));
            Console.WriteLine("[DB] Using in-memory database mode (HECATEON_USE_INMEMORY_DB).");
        }
        else if (databaseProvider == "sqlserver")
        {
            builder.Services.AddDbContext<ChatDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("ChatDb")));
            Console.WriteLine("[DB] Using SQL Server provider.");
        }
        else
        {
            var sqliteConnectionString = builder.Configuration.GetConnectionString("ChatDbSqlite")
                                         ?? "Data Source=App_Data/hecateon-chat.db";
            builder.Services.AddDbContext<ChatDbContext>(options =>
                options.UseSqlite(sqliteConnectionString));
            Console.WriteLine($"[DB] Using SQLite provider ({sqliteConnectionString}).");
        }

        // Register Hecateon Core services
        builder.Services.AddSingleton<IEventStore, InMemoryEventStore>();
        builder.Services.AddSingleton<IDeviceRegistry, InMemoryDeviceRegistry>();
        builder.Services.AddSingleton<EncryptionService>(sp => 
            new EncryptionService(builder.Configuration["Security:MasterKey"]));

        // Register Nyphos services (using fully qualified name to avoid ambiguity)
        builder.Services.AddSingleton<INyphosRiskEngine, Hecateon.Modules.Nyphos.Services.NyphosRiskEngine>();

        // Register legacy services (for migration)
        builder.Services.AddSingleton<SimpleSentimentModel>();
        builder.Services.AddHttpClient("llm");
        builder.Services.AddSingleton<LLMAssistantService>();
        builder.Services.AddSingleton<EventLogService>();
        builder.Services.AddSingleton<TrustedDeviceRegistryService>();
        builder.Services.AddSingleton<HealthTrackingService>();
        builder.Services.AddSingleton<WeeklyReportService>();
        builder.Services.AddSingleton<Hecateon.Services.NyphosRiskEngine>();
        builder.Services.AddSingleton<DownshiftProtocolService>();
        builder.Services.AddScoped<ChatLogService>();
        builder.Services.AddScoped<AssistantChatModel>();

        WebApplication app = builder.Build();

        // Initialize database on startup
        app.Services.InitializeDatabase();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }

        // Use device authentication middleware (after exception handler, before routing)
        app.UseDeviceAuthentication();

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        app.UseHttpsRedirection();

        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveWebAssemblyRenderMode()
            .AddAdditionalAssemblies(typeof(Hecateon.Client._Imports).Assembly);

        app.MapSystemEndpoints();
        app.MapChatEndpoints();
        app.MapHealthEndpoints();
        app.MapReportEndpoints();
        app.MapDownshiftEndpoints();
        app.MapHecateonCoreEndpoints();
        app.MapNyphosEndpoints();

        app.Run();
    }

    private static bool IsTruthy(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1";
    }
}