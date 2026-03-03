using Hecateon.Modules.Nyphos.Services;
using Lullaby.Models.Api.Endpoints;
using Lullaby.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lullaby.Endpoints;

public static class NyphosEndpoints
{
    public static void MapNyphosEndpoints(this WebApplication app)
    {
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
            return Results.Ok(state);
        });
    }
}
