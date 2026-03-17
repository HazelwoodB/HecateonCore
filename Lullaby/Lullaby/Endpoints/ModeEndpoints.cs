using Hecateon.Models.Api.Endpoints;
using Hecateon.Services;
using Microsoft.AspNetCore.Mvc;

namespace Hecateon.Endpoints;

public static class ModeEndpoints
{
    public static void MapModeEndpoints(this WebApplication app)
    {
        app.MapPost("/api/modes/tag", async (
            HttpContext http,
            [FromBody] ModeTagRequest request,
            [FromServices] IModeEventService modeService,
            [FromServices] ILogger<Program> logger,
            CancellationToken cancellationToken) =>
        {
            var deviceId = http.Request.Headers["X-Device-Id"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return Problem(http, StatusCodes.Status401Unauthorized, "Device required", "X-Device-Id header is required.", "https://hecateon.dev/problems/device-required");
            }

            if (string.IsNullOrWhiteSpace(request.Mode))
            {
                return Problem(http, StatusCodes.Status400BadRequest, "Mode required", "Mode is required.", "https://hecateon.dev/problems/mode-required");
            }

            var userId = string.IsNullOrWhiteSpace(request.UserId) ? "local-operator" : request.UserId.Trim();

            try
            {
                var result = await modeService.TagModeAsync(userId, deviceId, request.Mode.Trim(), cancellationToken);
                logger.LogInformation(
                    "Mode tagged. UserId={UserId} DeviceId={DeviceId} Mode={Mode} ShiftDetected={ShiftDetected} CorrelationId={CorrelationId}",
                    userId,
                    deviceId,
                    result.CurrentMode,
                    result.ShiftDetected,
                    http.TraceIdentifier);

                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Problem(http, StatusCodes.Status400BadRequest, "Invalid mode", ex.Message, "https://hecateon.dev/problems/mode-invalid");
            }
        });

        app.MapGet("/api/modes/current", async (
            HttpContext http,
            [FromServices] IModeEventService modeService,
            [FromQuery] string? userId,
            CancellationToken cancellationToken) =>
        {
            var deviceId = http.Request.Headers["X-Device-Id"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return Problem(http, StatusCodes.Status401Unauthorized, "Device required", "X-Device-Id header is required.", "https://hecateon.dev/problems/device-required");
            }

            var resolvedUserId = string.IsNullOrWhiteSpace(userId) ? "local-operator" : userId.Trim();
            var state = await modeService.GetCurrentModeAsync(resolvedUserId, deviceId, cancellationToken);

            if (state is null)
            {
                return Results.NotFound(new { userId = resolvedUserId, deviceId, mode = (string?)null });
            }

            return Results.Ok(state);
        });
    }

    private static IResult Problem(HttpContext http, int statusCode, string title, string detail, string type)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = type,
            Instance = http.Request.Path
        };

        problem.Extensions["correlationId"] = http.TraceIdentifier;
        return Results.Problem(problem);
    }
}
