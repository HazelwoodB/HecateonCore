using Lullaby.Components;
using Lullaby.Data;
using Lullaby.Models;
using Lullaby.Services;
using Hecateon.Core.EventStore;
using Hecateon.Core.DeviceRegistry;
using Hecateon.Core.Security;
using Hecateon.Modules.Nyphos.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

public partial class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveWebAssemblyComponents();

        // Add database context
        builder.Services.AddDbContext<ChatDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("ChatDb")));

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
        builder.Services.AddSingleton<Lullaby.Services.NyphosRiskEngine>();
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
            .AddAdditionalAssemblies(typeof(Lullaby.Client._Imports).Assembly);

        // Minimal API endpoints for chatbot and model prediction

        // Debug endpoint
        app.MapGet("/api/test", () =>
        {
            return Results.Ok(new { message = "API is reachable (client-side processing mode)", timestamp = DateTime.UtcNow });
        });

        app.MapPost("/api/devices/enroll", async ([FromServices] TrustedDeviceRegistryService trustedDevices, HttpContext http, [FromBody] DeviceEnrollRequest request, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.DeviceId))
            {
                return Results.BadRequest(new { error = "DeviceId is required." });
            }

            var record = await trustedDevices.EnrollOrUpdatePendingAsync(request.DeviceId, request.DisplayName, cancellationToken);
            return Results.Ok(new { record.DeviceId, record.DisplayName, record.IsApproved, record.Scopes, record.LastSeenUtc });
        });

        app.MapGet("/api/devices", ([FromServices] TrustedDeviceRegistryService trustedDevices, HttpContext http, IConfiguration configuration) =>
        {
            var recoveryCode = configuration["Security:RecoveryCode"] ?? "CHANGE_THIS_RECOVERY_CODE";
            var suppliedCode = http.Request.Headers["X-Recovery-Code"].FirstOrDefault();
            if (!string.Equals(recoveryCode, suppliedCode, StringComparison.Ordinal))
            {
                return Results.Unauthorized();
            }

            return Results.Ok(trustedDevices.GetAllDevices());
        });

        app.MapPost("/api/devices/approve", async ([FromServices] TrustedDeviceRegistryService trustedDevices, HttpContext http, IConfiguration configuration, [FromBody] DeviceApprovalRequest request, CancellationToken cancellationToken) =>
        {
            var recoveryCode = configuration["Security:RecoveryCode"] ?? "CHANGE_THIS_RECOVERY_CODE";
            var suppliedCode = http.Request.Headers["X-Recovery-Code"].FirstOrDefault();
            if (!string.Equals(recoveryCode, suppliedCode, StringComparison.Ordinal))
            {
                return Results.Unauthorized();
            }

            var approved = await trustedDevices.ApproveAsync(request.DeviceId, request.Scopes, cancellationToken);
            return approved ? Results.Ok(new { approved = true, request.DeviceId }) : Results.NotFound();
        });

        app.MapPost("/api/devices/revoke", async ([FromServices] TrustedDeviceRegistryService trustedDevices, HttpContext http, IConfiguration configuration, [FromBody] DeviceRevokeRequest request, CancellationToken cancellationToken) =>
        {
            var recoveryCode = configuration["Security:RecoveryCode"] ?? "CHANGE_THIS_RECOVERY_CODE";
            var suppliedCode = http.Request.Headers["X-Recovery-Code"].FirstOrDefault();
            if (!string.Equals(recoveryCode, suppliedCode, StringComparison.Ordinal))
            {
                return Results.Unauthorized();
            }

            var revoked = await trustedDevices.RevokeAsync(request.DeviceId, cancellationToken);
            return revoked ? Results.Ok(new { revoked = true, request.DeviceId }) : Results.NotFound();
        });

        // Legacy endpoints kept for compatibility but note they're not used in client-side mode
        app.MapPost("/api/chat", async ([FromServices] AssistantChatModel model, [FromBody] ChatRequest req) =>
        {
            if (req is null || string.IsNullOrWhiteSpace(req.Message))
            {
                return Results.BadRequest(new { error = "Message is required." });
            }

            try
            {
                var response = await model.ProcessUserMessageAsync(req.Message).ConfigureAwait(false);
                return Results.Ok(response);
            }
            catch
            {
                return Results.StatusCode(500);
            }
        });

        // NEW: Get all messages (for cross-client sync)
        app.MapGet("/api/history", async (HttpContext http, [FromServices] TrustedDeviceRegistryService trustedDevices, [FromServices] EventLogService eventLogService, [FromServices] ChatLogService chatLogService, [FromQuery] int limit = 200, CancellationToken cancellationToken = default) =>
        {
            try
            {
                var deviceId = http.Request.Headers["X-Device-Id"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(deviceId))
                {
                    return Results.Unauthorized();
                }

                await trustedDevices.EnrollOrUpdatePendingAsync(deviceId, deviceId, cancellationToken);
                if (!trustedDevices.IsApproved(deviceId))
                {
                    return Results.StatusCode(StatusCodes.Status403Forbidden);
                }

                var projection = await eventLogService.GetChatHistoryProjectionAsync(limit, cancellationToken);
                if (projection.Count > 0)
                {
                    return Results.Ok(projection);
                }

                var legacyHistory = chatLogService.GetHistory(limit);
                return Results.Ok(legacyHistory);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error in /api/history: {ex}");
                return Results.StatusCode(500);
            }
        });

        // NEW: Sync single message to server
        app.MapPost("/api/messages/sync", async (HttpContext http, [FromServices] TrustedDeviceRegistryService trustedDevices, [FromServices] EventLogService eventLogService, [FromServices] ChatLogService chatLogService, [FromBody] ChatMessage message, CancellationToken cancellationToken) =>
        {
            if (message is null)
            {
                return Results.BadRequest(new { error = "Message is required." });
            }

            try
            {
                var deviceId = http.Request.Headers["X-Device-Id"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(deviceId))
                {
                    return Results.Unauthorized();
                }

                await trustedDevices.EnrollOrUpdatePendingAsync(deviceId, deviceId, cancellationToken);
                if (!trustedDevices.IsApproved(deviceId))
                {
                    return Results.StatusCode(StatusCodes.Status403Forbidden);
                }

                await eventLogService.AppendChatMessageAsync(message, deviceId, cancellationToken);
                chatLogService.AddMessage(message);

                Console.WriteLine($"[API] Message synced: {message.Role} - {message.Message.Substring(0, Math.Min(50, message.Message.Length))}");
                return Results.Ok(new { id = message.Id, synced = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error in /api/messages/sync: {ex}");
                return Results.StatusCode(500);
            }
        });

        // Health tracking endpoints
        app.MapPost("/api/health/events", async (HttpContext http, [FromServices] TrustedDeviceRegistryService trustedDevices, [FromServices] HealthTrackingService healthTracking, [FromBody] HealthEventRequest request, CancellationToken cancellationToken) =>
        {
            try
            {
                var deviceId = http.Request.Headers["X-Device-Id"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(deviceId))
                {
                    deviceId = "unknown-device";
                }

                var healthEvent = new HealthEvent
                {
                    EventType = request.EventType,
                    RecordedAtUtc = request.RecordedAtUtc,
                    DeviceId = deviceId,
                    Note = request.Note,
                    SleepStartUtc = request.SleepStartUtc,
                    SleepEndUtc = request.SleepEndUtc,
                    SleepQuality = request.SleepQuality,
                    MoodScore = request.MoodScore,
                    MoodLabel = request.MoodLabel,
                    RoutineName = request.RoutineName,
                    RoutineCompleted = request.RoutineCompleted,
                    MedicationName = request.MedicationName,
                    MedicationTaken = request.MedicationTaken,
                    MedicationScheduledTime = request.MedicationScheduledTime,
                    ActivityType = request.ActivityType,
                    DurationMinutes = request.DurationMinutes
                };

                var recorded = await healthTracking.RecordHealthEventAsync(healthEvent, deviceId, cancellationToken);
                return recorded ? Results.Ok(new { id = healthEvent.Id, recorded = true }) : Results.Conflict();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error recording health event: {ex}");
                return Results.StatusCode(500);
            }
        });

        app.MapGet("/api/health/trends", async ([FromServices] HealthTrackingService healthTracking, [FromQuery] int days = 7, CancellationToken cancellationToken = default) =>
        {
            try
            {
                var trendScore = await healthTracking.CalculateTrendScoreAsync(days, cancellationToken);
                return Results.Ok(trendScore);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error calculating trends: {ex}");
                return Results.StatusCode(500);
            }
        });

        app.MapGet("/api/health/history", async ([FromServices] HealthTrackingService healthTracking, [FromQuery] int days = 30, CancellationToken cancellationToken = default) =>
        {
            try
            {
                var history = await healthTracking.GetHealthHistoryAsync(days, cancellationToken);
                return Results.Ok(history);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error fetching health history: {ex}");
                return Results.StatusCode(500);
            }
        });

        // Weekly report export endpoint
        app.MapGet("/api/reports/weekly", async (HttpContext http, [FromServices] WeeklyReportService reportService, IConfiguration configuration, [FromQuery] string? format = "markdown", CancellationToken cancellationToken = default) =>
        {
            try
            {
                // Require recovery code for report generation
                var recoveryCode = configuration["Security:RecoveryCode"] ?? "CHANGE_THIS_RECOVERY_CODE";
                var suppliedCode = http.Request.Headers["X-Recovery-Code"].FirstOrDefault();
                if (!string.Equals(recoveryCode, suppliedCode, StringComparison.Ordinal))
                {
                    return Results.Unauthorized();
                }

                var report = await reportService.GenerateWeeklyReportAsync(null, cancellationToken);
                
                var exportFormat = Enum.TryParse<ExportFormat>(format, true, out var fmt) ? fmt : ExportFormat.Markdown;
                var filePath = await reportService.ExportReportAsync(report, exportFormat, cancellationToken);
                
                return Results.Ok(new { reportId = report.ReportId, filePath, format = exportFormat.ToString() });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error generating report: {ex}");
                return Results.StatusCode(500);
            }
        });

        app.MapGet("/api/reports/weekly/preview", async ([FromServices] WeeklyReportService reportService, CancellationToken cancellationToken = default) =>
        {
            try
            {
                var report = await reportService.GenerateWeeklyReportAsync(null, cancellationToken);
                return Results.Ok(report);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error generating report preview: {ex}");
                return Results.StatusCode(500);
            }
        });

        // Nyphos risk assessment endpoints (legacy)
        app.MapGet("/api/nyphos/assessment", async ([FromServices] Lullaby.Services.NyphosRiskEngine riskEngine, CancellationToken cancellationToken, [FromQuery] int days = 7) =>
        {
            try
            {
                var assessment = await riskEngine.CalculateRiskStateAsync(days, cancellationToken);
                return Results.Ok(assessment);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error calculating Nyphos assessment: {ex}");
                return Results.StatusCode(500);
            }
        });

        // Downshift Protocol endpoints
        app.MapPost("/api/downshift/activate", async ([FromServices] DownshiftProtocolService downshiftService, [FromBody] ActivateProtocolRequest request, CancellationToken cancellationToken = default) =>
        {
            try
            {
                var protocol = await downshiftService.ActivateProtocolAsync(request.TriggeringState, cancellationToken);
                return Results.Ok(protocol);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error activating downshift protocol: {ex}");
                return Results.StatusCode(500);
            }
        });

        app.MapPost("/api/downshift/{protocolId}/complete-item", async ([FromServices] DownshiftProtocolService downshiftService, Guid protocolId, [FromBody] CompleteItemRequest request, CancellationToken cancellationToken = default) =>
        {
            try
            {
                var completed = await downshiftService.CompleteChecklistItemAsync(protocolId, request.ItemId, request.Note, cancellationToken);
                return Results.Ok(new { completed });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error completing checklist item: {ex}");
                return Results.StatusCode(500);
            }
        });

        app.MapPost("/api/downshift/{protocolId}/delay-decision", async ([FromServices] DownshiftProtocolService downshiftService, Guid protocolId, [FromBody] DelayDecisionRequest request, CancellationToken cancellationToken = default) =>
        {
            try
            {
                var recorded = await downshiftService.RecordDelayedDecisionAsync(protocolId, request.Description, request.HoursToDelay, cancellationToken);
                return Results.Ok(new { recorded });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error recording delayed decision: {ex}");
                return Results.StatusCode(500);
            }
        });

        // Crisis Plan endpoints
        app.MapGet("/api/crisis-plan", async ([FromServices] DownshiftProtocolService downshiftService, CancellationToken cancellationToken = default) =>
        {
            try
            {
                var plan = await downshiftService.GetCrisisPlanAsync(cancellationToken);
                return Results.Ok(plan);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error fetching crisis plan: {ex}");
                return Results.StatusCode(500);
            }
        });

        app.MapPost("/api/crisis-plan", async ([FromServices] DownshiftProtocolService downshiftService, [FromBody] Lullaby.Models.CrisisPlan plan, CancellationToken cancellationToken = default) =>
        {
            try
            {
                var saved = await downshiftService.SaveCrisisPlanAsync(plan, cancellationToken);
                return Results.Ok(new { saved });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error saving crisis plan: {ex}");
                return Results.StatusCode(500);
            }
        });

        // ====== HECATEON CORE ENDPOINTS ======

        // Device enrollment (no recovery code needed for initial enrollment)
        app.MapPost("/api/hecateon/device/enroll", async ([FromServices] IDeviceRegistry deviceRegistry, [FromBody] EnrollDeviceRequest request, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.DeviceId))
                return Results.BadRequest(new { error = "DeviceId is required" });

            var device = await deviceRegistry.EnrollAsync(request.DeviceId, request.DisplayName, cancellationToken);
            return Results.Ok(new
            {
                deviceId = device.DeviceId,
                displayName = device.DisplayName,
                isApproved = device.IsApproved,
                enrolledUtc = device.EnrolledUtc
            });
        });

        // Approve device (requires recovery code)
        app.MapPost("/api/hecateon/device/approve", async ([FromServices] IDeviceRegistry deviceRegistry, HttpContext http, IConfiguration configuration, [FromBody] ApproveDeviceRequest request, CancellationToken cancellationToken) =>
        {
            var recoveryCode = configuration["Security:RecoveryCode"] ?? "CHANGE_THIS_RECOVERY_CODE";
            var suppliedCode = http.Request.Headers["X-Recovery-Code"].FirstOrDefault();
            if (!string.Equals(recoveryCode, suppliedCode, StringComparison.Ordinal))
                return Results.Unauthorized();

            var device = await deviceRegistry.ApproveAsync(request.DeviceId, request.Scopes, cancellationToken);
            return device != null
                ? Results.Ok(new { deviceId = device.DeviceId, approved = device.IsApproved, scopes = device.Scopes })
                : Results.NotFound();
        });

        // Revoke device (requires recovery code)
        app.MapPost("/api/hecateon/device/revoke", async ([FromServices] IDeviceRegistry deviceRegistry, HttpContext http, IConfiguration configuration, [FromBody] RevokeDeviceRequest request, CancellationToken cancellationToken) =>
        {
            var recoveryCode = configuration["Security:RecoveryCode"] ?? "CHANGE_THIS_RECOVERY_CODE";
            var suppliedCode = http.Request.Headers["X-Recovery-Code"].FirstOrDefault();
            if (!string.Equals(recoveryCode, suppliedCode, StringComparison.Ordinal))
                return Results.Unauthorized();

            var revoked = await deviceRegistry.RevokeAsync(request.DeviceId, cancellationToken);
            return revoked ? Results.Ok(new { revoked = true }) : Results.NotFound();
        });

        // Get all devices (requires recovery code)
        app.MapGet("/api/hecateon/devices", async ([FromServices] IDeviceRegistry deviceRegistry, HttpContext http, IConfiguration configuration, CancellationToken cancellationToken) =>
        {
            var recoveryCode = configuration["Security:RecoveryCode"] ?? "CHANGE_THIS_RECOVERY_CODE";
            var suppliedCode = http.Request.Headers["X-Recovery-Code"].FirstOrDefault();
            if (!string.Equals(recoveryCode, suppliedCode, StringComparison.Ordinal))
                return Results.Unauthorized();

            var devices = await deviceRegistry.GetAllDevicesAsync(cancellationToken);
            return Results.Ok(devices.Select(d => new
            {
                d.DeviceId,
                d.DisplayName,
                d.IsApproved,
                d.Scopes,
                d.EnrolledUtc,
                d.ApprovedUtc,
                d.LastSeenUtc
            }));
        });

        // Event store query (requires hecateon:admin scope)
        app.MapGet("/api/hecateon/events", async ([FromServices] IEventStore eventStore, CancellationToken cancellationToken, [FromQuery] int skip = 0, [FromQuery] int take = 100) =>
        {
            var events = await eventStore.GetAllEventsAsync(skip, take, cancellationToken);
            return Results.Ok(events);
        });

        // ====== NYPHOS MODULE ENDPOINTS ======

        // Log sleep event
        app.MapPost("/api/nyphos/sleep", async (HttpContext http, [FromServices] INyphosRiskEngine riskEngine, [FromBody] LogSleepRequest request, CancellationToken cancellationToken) =>
        {
            var deviceId = http.Request.Headers["X-Device-Id"].FirstOrDefault() ?? "default-device";

            await riskEngine.LogSleepAsync(deviceId, request.SleepStart, request.SleepEnd, request.QualityScore, request.Interruptions, cancellationToken);
            return Results.Ok(new { logged = true });
        });

        // Log mood event
        app.MapPost("/api/nyphos/mood", async (HttpContext http, [FromServices] INyphosRiskEngine riskEngine, [FromBody] LogMoodRequest request, CancellationToken cancellationToken) =>
        {
            var deviceId = http.Request.Headers["X-Device-Id"].FirstOrDefault() ?? "default-device";

            await riskEngine.LogMoodAsync(deviceId, request.EnergyLevel, request.MoodScore, request.MoodLabel, request.Notes, cancellationToken);
            return Results.Ok(new { logged = true });
        });

        // Get current state
        app.MapGet("/api/nyphos/state", async (HttpContext http, [FromServices] INyphosRiskEngine riskEngine, CancellationToken cancellationToken) =>
        {
            var deviceId = http.Request.Headers["X-Device-Id"].FirstOrDefault() ?? "default-device";

            var state = await riskEngine.ComputeStateAsync(deviceId, cancellationToken);
            return Results.Ok(state);
        });

        app.Run();
    }
}

// ====== REQUEST/RESPONSE DTOs ======

public record EnrollDeviceRequest(string DeviceId, string? DisplayName);
public record ApproveDeviceRequest(string DeviceId, string[] Scopes);
public record RevokeDeviceRequest(string DeviceId);

public record LogSleepRequest(DateTime SleepStart, DateTime SleepEnd, int? QualityScore, string[]? Interruptions);
public record LogMoodRequest(int? EnergyLevel, int? MoodScore, string? MoodLabel, string? Notes);