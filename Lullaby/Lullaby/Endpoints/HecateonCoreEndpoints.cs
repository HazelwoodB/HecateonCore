using Hecateon.Core.DeviceRegistry;
using Hecateon.Core.EventStore;
using Hecateon.Events;
using Hecateon.Infrastructure;
using Hecateon.Models.Api.Endpoints;
using Hecateon.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Hecateon.Endpoints;

public static class HecateonCoreEndpoints
{
    public static void MapHecateonCoreEndpoints(this WebApplication app)
    {
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
        }).RequireRateLimiting("DeviceEnrollPolicy");

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

        app.MapPost("/api/hecateon/device/revoke", async ([FromServices] IDeviceRegistry deviceRegistry, HttpContext http, IConfiguration configuration, [FromBody] RevokeDeviceRequest request, CancellationToken cancellationToken) =>
        {
            var recoveryCode = configuration["Security:RecoveryCode"] ?? "CHANGE_THIS_RECOVERY_CODE";
            var suppliedCode = http.Request.Headers["X-Recovery-Code"].FirstOrDefault();
            if (!string.Equals(recoveryCode, suppliedCode, StringComparison.Ordinal))
                return Results.Unauthorized();

            var revoked = await deviceRegistry.RevokeAsync(request.DeviceId, cancellationToken);
            return revoked ? Results.Ok(new { revoked = true }) : Results.NotFound();
        });

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

        app.MapGet("/api/hecateon/events", async ([FromServices] IEventStore eventStore, CancellationToken cancellationToken, [FromQuery] int skip = 0, [FromQuery] int take = 100) =>
        {
            var events = await eventStore.GetAllEventsAsync(skip, take, cancellationToken);
            return Results.Ok(events);
        });

        app.MapPost("/streams/{stream}/events", async (
            HttpContext http,
            [FromRoute] string stream,
            [FromBody] StreamAppendRequest request,
            [FromServices] IEventStore eventStore,
            [FromServices] IPrometheonExtractionService extraction,
            [FromServices] IOptions<EventStoreOptions> eventStoreOptions,
            [FromServices] ILogger<Program> logger,
            CancellationToken cancellationToken) =>
        {
            if (!EventStream.All.Contains(stream, StringComparer.OrdinalIgnoreCase))
            {
                return Problem(http, StatusCodes.Status400BadRequest, "Unsupported stream", "The requested stream is not supported.", "https://hecateon.dev/problems/stream-unsupported");
            }

            if (request.Events is null || request.Events.Count == 0)
            {
                return Problem(http, StatusCodes.Status400BadRequest, "Invalid append payload", "At least one event envelope is required.", "https://hecateon.dev/problems/append-empty");
            }

            var payloadLimit = Math.Max(1024, eventStoreOptions.Value.MaxPayloadBytes);
            var validationErrors = request.Events
                .Select((envelope, index) => new
                {
                    index,
                    envelope.ClientMsgId,
                    validation = EventEnvelopeValidator.Validate(envelope, payloadLimit, requireServerAssignedFields: false)
                })
                .Where(x => !x.validation.IsValid)
                .Select(x => new
                {
                    x.index,
                    clientMsgIdHash = LogRedaction.Sha256Prefix(x.ClientMsgId),
                    codes = x.validation.Errors.Select(e => e.Code).ToArray()
                })
                .ToArray();

            if (validationErrors.Length > 0)
            {
                logger.LogWarning(
                    "Stream append validation failed. Stream={Stream} Count={Count} CorrelationId={CorrelationId} Errors={Errors}",
                    stream,
                    request.Events.Count,
                    http.TraceIdentifier,
                    validationErrors);

                return Problem(
                    http,
                    StatusCodes.Status400BadRequest,
                    "Invalid event envelope",
                    $"One or more envelopes failed validation (payload limit: {payloadLimit} bytes).",
                    "https://hecateon.dev/problems/envelope-invalid",
                    new Dictionary<string, object?>
                    {
                        ["errors"] = validationErrors
                    });
            }

            try
            {
                var result = await eventStore.AppendToStreamAsync(stream, request.Events, cancellationToken);

                logger.LogInformation(
                    "Stream append completed. Stream={Stream} Count={Count} Accepted={Accepted} Duplicate={Duplicate} Rejected={Rejected} CorrelationId={CorrelationId}",
                    stream,
                    request.Events.Count,
                    result.AcceptedCount,
                    result.DuplicateCount,
                    result.RejectedCount,
                    http.TraceIdentifier);

                if (string.Equals(stream, EventStream.Chat, StringComparison.OrdinalIgnoreCase) && result.AcceptedCount > 0)
                {
                    var extractionResult = await extraction.ProcessPendingChatEventsAsync(cancellationToken: cancellationToken);
                    logger.LogInformation(
                        "Prometheon extraction trigger run complete. ProcessedChat={ProcessedChat} EmittedGraph={EmittedGraph} LastSeq={LastSeq} CorrelationId={CorrelationId}",
                        extractionResult.ProcessedChatEvents,
                        extractionResult.EmittedGraphEvents,
                        extractionResult.LastProcessedSeq,
                        http.TraceIdentifier);
                }

                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Stream append failed. Stream={Stream} Count={Count} CorrelationId={CorrelationId}",
                    stream,
                    request.Events.Count,
                    http.TraceIdentifier);

                return Problem(http, StatusCodes.Status500InternalServerError, "Append failed", "An unexpected error occurred while appending stream events.", "https://hecateon.dev/problems/append-failed");
            }
        }).RequireRateLimiting("StreamAppendPolicy");

        app.MapGet("/streams/{stream}/events", async (
            HttpContext http,
            [FromRoute] string stream,
            [FromServices] IEventStore eventStore,
            [FromServices] IOptions<EventStoreOptions> eventStoreOptions,
            [FromServices] ILogger<Program> logger,
            [FromQuery] long? sinceSeq,
            [FromQuery] int limit,
            CancellationToken cancellationToken) =>
        {
            if (!EventStream.All.Contains(stream, StringComparer.OrdinalIgnoreCase))
            {
                return Problem(http, StatusCodes.Status400BadRequest, "Unsupported stream", "The requested stream is not supported.", "https://hecateon.dev/problems/stream-unsupported");
            }

            var maxPullLimit = Math.Max(100, eventStoreOptions.Value.MaxPullLimit);
            var safeLimit = limit <= 0 ? 100 : Math.Min(limit, maxPullLimit);
            try
            {
                var events = await eventStore.GetStreamEventsAsync(stream, sinceSeq, safeLimit, cancellationToken);

                logger.LogInformation(
                    "Stream pull completed. Stream={Stream} SinceSeq={SinceSeq} Limit={Limit} Returned={Returned} CorrelationId={CorrelationId}",
                    stream,
                    sinceSeq,
                    safeLimit,
                    events.Count,
                    http.TraceIdentifier);

                return Results.Ok(events);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Stream pull failed. Stream={Stream} SinceSeq={SinceSeq} Limit={Limit} CorrelationId={CorrelationId}",
                    stream,
                    sinceSeq,
                    safeLimit,
                    http.TraceIdentifier);

                return Problem(http, StatusCodes.Status500InternalServerError, "Pull failed", "An unexpected error occurred while pulling stream events.", "https://hecateon.dev/problems/pull-failed");
            }
        });

        app.MapGet("/streams/{stream}/head", async (
            HttpContext http,
            [FromRoute] string stream,
            [FromServices] IEventStore eventStore,
            [FromServices] ILogger<Program> logger,
            CancellationToken cancellationToken) =>
        {
            if (!EventStream.All.Contains(stream, StringComparer.OrdinalIgnoreCase))
            {
                return Problem(http, StatusCodes.Status400BadRequest, "Unsupported stream", "The requested stream is not supported.", "https://hecateon.dev/problems/stream-unsupported");
            }

            try
            {
                var head = await eventStore.GetStreamHeadSeqAsync(stream, cancellationToken);
                logger.LogInformation(
                    "Stream head lookup completed. Stream={Stream} HeadSeq={HeadSeq} CorrelationId={CorrelationId}",
                    stream,
                    head,
                    http.TraceIdentifier);
                return Results.Ok(new { stream, headSeq = head });
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Stream head lookup failed. Stream={Stream} CorrelationId={CorrelationId}",
                    stream,
                    http.TraceIdentifier);

                return Problem(http, StatusCodes.Status500InternalServerError, "Head lookup failed", "An unexpected error occurred while reading stream head.", "https://hecateon.dev/problems/head-failed");
            }
        });
    }

    private static IResult Problem(HttpContext http, int statusCode, string title, string detail, string type, IDictionary<string, object?>? extensions = null)
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

        if (extensions is not null)
        {
            foreach (var (key, value) in extensions)
            {
                problem.Extensions[key] = value;
            }
        }

        return Results.Problem(problem);
    }
}
