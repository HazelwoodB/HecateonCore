using Lullaby.Models;
using Lullaby.Models.Api.Endpoints;
using Lullaby.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lullaby.Endpoints;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
        app.MapPost("/api/health/log", async (HttpContext http, [FromServices] TrustedDeviceRegistryService trustedDevices, [FromServices] HealthTrackingService healthTracking, [FromBody] LegacyHealthLogRequest request, CancellationToken cancellationToken) =>
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

                var healthEvent = new HealthEvent
                {
                    EventType = HealthEventType.Mood,
                    RecordedAtUtc = request.Timestamp,
                    DeviceId = deviceId,
                    MoodScore = Math.Clamp(request.Mood - 3, -2, 2),
                    MoodLabel = request.Mood switch
                    {
                        1 => "awful",
                        2 => "bad",
                        3 => "okay",
                        4 => "good",
                        5 => "great",
                        _ => "unknown"
                    },
                    Note = request.Sleep.HasValue
                        ? $"Legacy log entry: sleep={request.Sleep.Value:F1}h"
                        : "Legacy log entry"
                };

                var recorded = await healthTracking.RecordHealthEventAsync(healthEvent, deviceId, cancellationToken);
                return recorded ? Results.Ok(new { id = healthEvent.Id, recorded = true }) : Results.Conflict();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error recording legacy health log: {ex}");
                return Results.StatusCode(500);
            }
        });

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
