using Hecateon.Core.EventStore;
using Hecateon.Events;
using Hecateon.Infrastructure;
using Hecateon.Models.Api.Endpoints;
using Hecateon.Services;
using Microsoft.AspNetCore.Mvc;

namespace Hecateon.Endpoints;

public static class SyncEndpoints
{
    public static void MapSyncEndpoints(this WebApplication app)
    {
        app.MapGet("/api/sync/status", async (
            [FromServices] ILocalSyncStateService localSyncState,
            CancellationToken cancellationToken) =>
        {
            var queueSize = await localSyncState.GetQueueSizeAsync(cancellationToken);
            var dueQueueSize = await localSyncState.GetDueQueueSizeAsync(cancellationToken: cancellationToken);
            var lastSeen = await localSyncState.GetLastSeenMapAsync(cancellationToken);
            return Results.Ok(new
            {
                queueSize,
                dueQueueSize,
                lastSeenSeq = lastSeen
            });
        });

        app.MapPost("/api/sync/queue/events", async (
            HttpContext http,
            [FromBody] StreamAppendRequest request,
            [FromServices] ILocalSyncStateService localSyncState,
            [FromServices] ILogger<Program> logger,
            CancellationToken cancellationToken) =>
        {
            if (request.Events is null || request.Events.Count == 0)
            {
                return Problem(http, StatusCodes.Status400BadRequest, "Invalid queue payload", "At least one event envelope is required.", "https://hecateon.dev/problems/sync-queue-empty");
            }

            foreach (var envelope in request.Events)
            {
                await localSyncState.EnqueueAsync(envelope, cancellationToken);
            }

            var queueSize = await localSyncState.GetQueueSizeAsync(cancellationToken);
            logger.LogInformation("Local sync queue enqueued. Count={Count} QueueSize={QueueSize} CorrelationId={CorrelationId}", request.Events.Count, queueSize, http.TraceIdentifier);
            return Results.Ok(new { queued = request.Events.Count, queueSize });
        });

        app.MapPost("/api/sync/push/{stream}", async (
            HttpContext http,
            [FromRoute] string stream,
            [FromServices] ILocalSyncStateService localSyncState,
            [FromServices] IEventStore eventStore,
            [FromServices] ILogger<Program> logger,
            [FromQuery] int maxItems = 100,
            CancellationToken cancellationToken = default) =>
        {
            if (!EventStream.All.Contains(stream, StringComparer.OrdinalIgnoreCase))
            {
                return Problem(http, StatusCodes.Status400BadRequest, "Unsupported stream", "The requested stream is not supported.", "https://hecateon.dev/problems/stream-unsupported");
            }

            var batch = await localSyncState.PeekDueBatchAsync(stream, maxItems, cancellationToken);
            if (batch.Count == 0)
            {
                var queueSizeWhenNoDue = await localSyncState.GetQueueSizeAsync(cancellationToken);
                return Results.Ok(new { pushed = 0, queueEmpty = queueSizeWhenNoDue == 0, queueSize = queueSizeWhenNoDue, noDueItems = true });
            }

            var appendResult = await eventStore.AppendToStreamAsync(stream, batch, cancellationToken);
            await localSyncState.ApplyPushOutcomeAsync(appendResult.Items, cancellationToken);

            var queueSize = await localSyncState.GetQueueSizeAsync(cancellationToken);
            var dueQueueSize = await localSyncState.GetDueQueueSizeAsync(stream, cancellationToken);
            logger.LogInformation(
                "Local sync push completed. Stream={Stream} Requested={Requested} Accepted={Accepted} Duplicate={Duplicate} Rejected={Rejected} QueueSize={QueueSize} DueQueueSize={DueQueueSize} CorrelationId={CorrelationId}",
                stream,
                batch.Count,
                appendResult.AcceptedCount,
                appendResult.DuplicateCount,
                appendResult.RejectedCount,
                queueSize,
                dueQueueSize,
                http.TraceIdentifier);

            return Results.Ok(new
            {
                result = appendResult,
                queueSize,
                dueQueueSize
            });
        });

        app.MapPost("/api/sync/pull/{stream}", async (
            HttpContext http,
            [FromRoute] string stream,
            [FromServices] ILocalSyncStateService localSyncState,
            [FromServices] IEventStore eventStore,
            [FromServices] ILogger<Program> logger,
            [FromQuery] int limit = 100,
            CancellationToken cancellationToken = default) =>
        {
            if (!EventStream.All.Contains(stream, StringComparer.OrdinalIgnoreCase))
            {
                return Problem(http, StatusCodes.Status400BadRequest, "Unsupported stream", "The requested stream is not supported.", "https://hecateon.dev/problems/stream-unsupported");
            }

            var sinceSeq = await localSyncState.GetLastSeenSeqAsync(stream, cancellationToken);
            var events = await eventStore.GetStreamEventsAsync(stream, sinceSeq, limit, cancellationToken);

            var maxSeq = events
                .Select(e => e.Seq ?? 0L)
                .DefaultIfEmpty(sinceSeq)
                .Max();

            await localSyncState.SetLastSeenSeqAsync(stream, maxSeq, cancellationToken);

            logger.LogInformation(
                "Local sync pull completed. Stream={Stream} SinceSeq={SinceSeq} Received={Received} NewLastSeen={NewLastSeen} CorrelationId={CorrelationId}",
                stream,
                sinceSeq,
                events.Count,
                maxSeq,
                http.TraceIdentifier);

            return Results.Ok(new
            {
                stream,
                sinceSeq,
                lastSeenSeq = maxSeq,
                events
            });
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
