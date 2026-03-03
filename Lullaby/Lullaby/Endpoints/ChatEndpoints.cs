using Hecateon.Models;
using Hecateon.Services;
using Microsoft.AspNetCore.Mvc;

namespace Hecateon.Endpoints;

public static class ChatEndpoints
{
    public static void MapChatEndpoints(this WebApplication app)
    {
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
    }
}
