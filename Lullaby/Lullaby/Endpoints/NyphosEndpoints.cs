using Hecateon.Modules.Nyphos.Services;
using Hecateon.Models.Api.Endpoints;
using Hecateon.Services;
using Microsoft.AspNetCore.Mvc;

namespace Hecateon.Endpoints;

public static class NyphosEndpoints
{
    public static void MapNyphosEndpoints(this WebApplication app)
    {
        app.MapGet("/api/nyphos/assessment", async ([FromServices] Hecateon.Services.NyphosRiskEngine riskEngine, CancellationToken cancellationToken, [FromQuery] int days = 7) =>
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

        app.MapPost("/api/nyphos/sleep", async (HttpContext http, [FromServices] INyphosRiskEngine riskEngine, [FromBody] LogSleepRequest request, CancellationToken cancellationToken) =>
        {
            var deviceId = http.Request.Headers["X-Device-Id"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return Results.Unauthorized();
            }

            await riskEngine.LogSleepAsync(deviceId, request.SleepStart, request.SleepEnd, request.QualityScore, request.Interruptions, cancellationToken);
            return Results.Ok(new { logged = true });
        });

        app.MapPost("/api/nyphos/mood", async (HttpContext http, [FromServices] INyphosRiskEngine riskEngine, [FromBody] LogMoodRequest request, CancellationToken cancellationToken) =>
        {
            var deviceId = http.Request.Headers["X-Device-Id"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return Results.Unauthorized();
            }

            await riskEngine.LogMoodAsync(deviceId, request.EnergyLevel, request.MoodScore, request.MoodLabel, request.Notes, cancellationToken);
            return Results.Ok(new { logged = true });
        });

        app.MapGet("/api/nyphos/state", async (HttpContext http, [FromServices] INyphosRiskEngine riskEngine, CancellationToken cancellationToken) =>
        {
            var deviceId = http.Request.Headers["X-Device-Id"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return Results.Unauthorized();
            }

            var state = await riskEngine.ComputeStateAsync(deviceId, cancellationToken);

            var userId = http.Request.Headers["X-User-Id"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(userId))
            {
                userId = "local-operator";
            }

            var emitter = http.RequestServices.GetRequiredService<INyphosSignalEmitterService>();
            var (signalSeq, safeguardSeq) = await emitter.EmitFromStateAsync(userId, deviceId, state, cancellationToken);

            return Results.Ok(new
            {
                state,
                emitted = new
                {
                    nyphosSignalSeq = signalSeq,
                    safeguardRecommendedSeq = safeguardSeq
                }
            });
        });

        app.MapGet("/api/nyphos/settings", async (HttpContext http, [FromServices] INyphosPreferenceService preferences, [FromQuery] string? userId, CancellationToken cancellationToken) =>
        {
            var deviceId = http.Request.Headers["X-Device-Id"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return Results.Unauthorized();
            }

            var resolvedUserId = string.IsNullOrWhiteSpace(userId) ? "local-operator" : userId;
            var settings = await preferences.GetOrDefaultAsync(resolvedUserId, deviceId, cancellationToken);
            return Results.Ok(settings);
        });

        app.MapPut("/api/nyphos/settings/tone", async (HttpContext http, [FromBody] NyphosToneRequest request, [FromServices] INyphosPreferenceService preferences, CancellationToken cancellationToken) =>
        {
            var deviceId = http.Request.Headers["X-Device-Id"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return Results.Unauthorized();
            }

            var resolvedUserId = string.IsNullOrWhiteSpace(request.UserId) ? "local-operator" : request.UserId;
            try
            {
                var settings = await preferences.SetToneAsync(resolvedUserId, deviceId, request.Tone, cancellationToken);
                return Results.Ok(settings);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        app.MapPost("/api/nyphos/settings/mute", async (HttpContext http, [FromBody] NyphosMuteRequest request, [FromServices] INyphosPreferenceService preferences, CancellationToken cancellationToken) =>
        {
            var deviceId = http.Request.Headers["X-Device-Id"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return Results.Unauthorized();
            }

            var resolvedUserId = string.IsNullOrWhiteSpace(request.UserId) ? "local-operator" : request.UserId;
            var hours = request.Hours ?? 24;
            var settings = await preferences.SetMuteAsync(resolvedUserId, deviceId, hours, cancellationToken);
            return Results.Ok(settings);
        });

        app.MapDelete("/api/nyphos/settings/mute", async (HttpContext http, [FromServices] INyphosPreferenceService preferences, [FromQuery] string? userId, CancellationToken cancellationToken) =>
        {
            var deviceId = http.Request.Headers["X-Device-Id"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return Results.Unauthorized();
            }

            var resolvedUserId = string.IsNullOrWhiteSpace(userId) ? "local-operator" : userId;
            var settings = await preferences.ClearMuteAsync(resolvedUserId, deviceId, cancellationToken);
            return Results.Ok(settings);
        });
    }
}
