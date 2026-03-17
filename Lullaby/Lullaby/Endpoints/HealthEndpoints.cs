using Hecateon.Models;
using Hecateon.Models.Api.Endpoints;
using Hecateon.Services;
using Microsoft.AspNetCore.Mvc;

namespace Hecateon.Endpoints;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this WebApplication app)
    {

        app.MapPost("/api/health/events", async (HttpContext http, [FromServices] TrustedDeviceRegistryService trustedDevices, [FromServices] HealthTrackingService healthTracking, [FromBody] HealthEventRequest request, CancellationToken cancellationToken) =>
        {
            try
            {
                var deviceId = http.Request.Headers["X-Device-Id"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(deviceId))
                {
                    return Results.Unauthorized();
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
    }
}
