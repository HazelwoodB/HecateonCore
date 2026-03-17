using Hecateon.Modules.Nyphos.Services;
using Hecateon.Core.EventStore;
using Hecateon.Events;
using Hecateon.Services;
using System.Text.Json;

namespace Hecateon.Endpoints;

public static class OperatorEndpoints
{
    public static void MapOperatorEndpoints(this WebApplication app)
    {
        app.MapGet("/api/operator/status", async (
            HttpContext http,
            ILocalSyncStateService syncState,
            IModeEventService modeService,
            INyphosRiskEngine nyphosRiskEngine,
            TrustedDeviceRegistryService trustedDevices,
            CancellationToken cancellationToken) =>
        {
            var deviceId = http.Request.Headers["X-Device-Id"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return Results.Unauthorized();
            }

            var userId = http.Request.Headers["X-User-Id"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(userId))
            {
                userId = "local-operator";
            }

            var queueSize = await syncState.GetQueueSizeAsync(cancellationToken);
            var dueQueueSize = await syncState.GetDueQueueSizeAsync(cancellationToken: cancellationToken);

            var mode = await modeService.GetCurrentModeAsync(userId, deviceId, cancellationToken);
            var nyphos = await nyphosRiskEngine.ComputeStateAsync(deviceId, cancellationToken);

            var trustedRecord = trustedDevices.GetAllDevices()
                .FirstOrDefault(d => string.Equals(d.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase));

            return Results.Ok(new
            {
                identity = new
                {
                    userId,
                    deviceId
                },
                device = new
                {
                    isApproved = trustedRecord?.IsApproved ?? false,
                    displayName = trustedRecord?.DisplayName ?? deviceId,
                    scopes = trustedRecord?.Scopes ?? Array.Empty<string>(),
                    lastSeenUtc = trustedRecord?.LastSeenUtc
                },
                sync = new
                {
                    queueSize,
                    dueQueueSize
                },
                mode = new
                {
                    current = mode?.CurrentMode,
                    previous = mode?.PreviousMode,
                    confidence = mode?.LastConfidence,
                    updatedUtc = mode?.UpdatedUtc
                },
                nyphos = new
                {
                    state = nyphos.State,
                    sleepIntegrity = nyphos.SleepIntegrity,
                    moodRisk = nyphos.MoodRisk,
                    overloadIndex = nyphos.OverloadIndex,
                    updatedUtc = nyphos.LastUpdateUtc
                },
                correlationId = http.TraceIdentifier
            });
        });

        app.MapGet("/api/operator/events", async (
            HttpContext http,
            IEventStore eventStore,
            IGraphProjectionService graphProjection,
            IPrometheonExtractionService prometheonExtraction,
            [AsParameters] OperatorEventsQuery query,
            CancellationToken cancellationToken) =>
        {
            var limit = Math.Clamp(query.Limit ?? 100, 1, 500);
            var streams = ResolveStreams(query.Stream);

            var streamHeads = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            var streamEvents = new Dictionary<string, IReadOnlyList<EventEnvelope>>(StringComparer.OrdinalIgnoreCase);

            foreach (var stream in streams)
            {
                var head = await eventStore.GetStreamHeadSeqAsync(stream, cancellationToken);
                streamHeads[stream] = head;

                var events = await eventStore.GetStreamEventsAsync(stream, sinceSeq: null, limit: limit, cancellationToken: cancellationToken);
                streamEvents[stream] = events
                    .OrderByDescending(e => e.Seq ?? 0)
                    .ToArray();
            }

            var flattened = streamEvents
                .SelectMany(kv => kv.Value.Select(ev => new
                {
                    stream = kv.Key,
                    ev.Seq,
                    ev.EventId,
                    ev.UserId,
                    ev.DeviceId,
                    ev.Type,
                    ev.TimestampUtc,
                    ev.SchemaVersion,
                    ev.ClientMsgId,
                    ev.PayloadJson
                }))
                .OrderByDescending(e => e.Seq ?? 0)
                .Take(limit)
                .ToArray();

            var graphLastAppliedSeq = await graphProjection.GetLastAppliedSeqAsync(cancellationToken);
            var prometheonLastProcessedSeq = await prometheonExtraction.GetLastProcessedSeqAsync(cancellationToken);

            return Results.Ok(new
            {
                streams,
                limit,
                events = flattened,
                projectionHealth = new
                {
                    graphLastAppliedSeq,
                    prometheonLastProcessedSeq,
                    streamHeads
                },
                correlationId = http.TraceIdentifier
            });
        });

        app.MapGet("/api/operator/panel", async (
            HttpContext http,
            ILocalSyncStateService syncState,
            IModeEventService modeService,
            INyphosRiskEngine nyphosRiskEngine,
            TrustedDeviceRegistryService trustedDevices,
            IEventStore eventStore,
            IGraphProjectionService graphProjection,
            IPrometheonExtractionService prometheonExtraction,
            [AsParameters] OperatorEventsQuery query,
            CancellationToken cancellationToken) =>
        {
            var deviceId = http.Request.Headers["X-Device-Id"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return Results.Unauthorized();
            }

            var userId = http.Request.Headers["X-User-Id"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(userId))
            {
                userId = "local-operator";
            }

            var queueSize = await syncState.GetQueueSizeAsync(cancellationToken);
            var dueQueueSize = await syncState.GetDueQueueSizeAsync(cancellationToken: cancellationToken);
            var mode = await modeService.GetCurrentModeAsync(userId, deviceId, cancellationToken);
            var nyphos = await nyphosRiskEngine.ComputeStateAsync(deviceId, cancellationToken);

            var trustedRecord = trustedDevices.GetAllDevices()
                .FirstOrDefault(d => string.Equals(d.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase));

            var limit = Math.Clamp(query.Limit ?? 100, 1, 500);
            var streams = ResolveStreams(query.Stream);
            var streamHeads = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            var streamEvents = new Dictionary<string, IReadOnlyList<EventEnvelope>>(StringComparer.OrdinalIgnoreCase);

            foreach (var stream in streams)
            {
                var head = await eventStore.GetStreamHeadSeqAsync(stream, cancellationToken);
                streamHeads[stream] = head;

                var events = await eventStore.GetStreamEventsAsync(stream, sinceSeq: null, limit: limit, cancellationToken: cancellationToken);
                streamEvents[stream] = events
                    .OrderByDescending(e => e.Seq ?? 0)
                    .ToArray();
            }

            var flattened = streamEvents
                .SelectMany(kv => kv.Value.Select(ev => new
                {
                    stream = kv.Key,
                    ev.Seq,
                    ev.EventId,
                    ev.UserId,
                    ev.DeviceId,
                    ev.Type,
                    ev.TimestampUtc,
                    ev.SchemaVersion,
                    ev.ClientMsgId,
                    ev.PayloadJson
                }))
                .OrderByDescending(e => e.Seq ?? 0)
                .Take(limit)
                .ToArray();

            var graphLastAppliedSeq = await graphProjection.GetLastAppliedSeqAsync(cancellationToken);
            var prometheonLastProcessedSeq = await prometheonExtraction.GetLastProcessedSeqAsync(cancellationToken);

            var runbookActions = new[]
            {
                new { id = "rebuild-projections", method = "POST", path = "/api/operator/rebuild/projections", query = new[] { "batchSize" }, safeAnytime = true },
                new { id = "rebuild-graph", method = "POST", path = "/api/operator/rebuild/graph", query = new[] { "batchSize" }, safeAnytime = true },
                new { id = "snapshot-export", method = "POST", path = "/api/operator/snapshot/export", query = new[] { "recentEventsPerStream" }, safeAnytime = true },
                new { id = "snapshot-import", method = "POST", path = "/api/operator/snapshot/import", query = new[] { "fileName", "apply" }, safeAnytime = false }
            };

            return Results.Ok(new
            {
                status = new
                {
                    identity = new
                    {
                        userId,
                        deviceId
                    },
                    device = new
                    {
                        isApproved = trustedRecord?.IsApproved ?? false,
                        displayName = trustedRecord?.DisplayName ?? deviceId,
                        scopes = trustedRecord?.Scopes ?? Array.Empty<string>(),
                        lastSeenUtc = trustedRecord?.LastSeenUtc
                    },
                    sync = new
                    {
                        queueSize,
                        dueQueueSize
                    },
                    mode = new
                    {
                        current = mode?.CurrentMode,
                        previous = mode?.PreviousMode,
                        confidence = mode?.LastConfidence,
                        updatedUtc = mode?.UpdatedUtc
                    },
                    nyphos = new
                    {
                        state = nyphos.State,
                        sleepIntegrity = nyphos.SleepIntegrity,
                        moodRisk = nyphos.MoodRisk,
                        overloadIndex = nyphos.OverloadIndex,
                        updatedUtc = nyphos.LastUpdateUtc
                    }
                },
                events = flattened,
                projectionHealth = new
                {
                    graphLastAppliedSeq,
                    prometheonLastProcessedSeq,
                    streamHeads
                },
                runbookActions,
                correlationId = http.TraceIdentifier
            });
        });

        app.MapPost("/api/operator/rebuild/projections", async (
            HttpContext http,
            IPrometheonExtractionService prometheonExtraction,
            IGraphProjectionService graphProjection,
            [FromQuery] int batchSize,
            CancellationToken cancellationToken) =>
        {
            var safeBatchSize = batchSize <= 0 ? 200 : Math.Clamp(batchSize, 1, 1000);

            var extraction = await prometheonExtraction.ProcessPendingChatEventsAsync(safeBatchSize, cancellationToken);
            var graphApply = await graphProjection.ApplyPendingAsync(safeBatchSize, cancellationToken);

            return Results.Ok(new
            {
                action = "rebuild-projections",
                batchSize = safeBatchSize,
                prometheon = extraction,
                graph = graphApply,
                correlationId = http.TraceIdentifier
            });
        });

        app.MapPost("/api/operator/rebuild/graph", async (
            HttpContext http,
            IGraphProjectionService graphProjection,
            [FromQuery] int batchSize,
            CancellationToken cancellationToken) =>
        {
            var safeBatchSize = batchSize <= 0 ? 200 : Math.Clamp(batchSize, 1, 1000);
            var result = await graphProjection.RebuildAsync(safeBatchSize, cancellationToken);

            return Results.Ok(new
            {
                action = "rebuild-graph",
                batchSize = safeBatchSize,
                result,
                correlationId = http.TraceIdentifier
            });
        });

        app.MapPost("/api/operator/snapshot/export", async (
            HttpContext http,
            IWebHostEnvironment environment,
            IEventStore eventStore,
            ILocalSyncStateService syncState,
            IModeEventService modeService,
            INyphosRiskEngine nyphosRiskEngine,
            [FromQuery] int recentEventsPerStream,
            CancellationToken cancellationToken) =>
        {
            var deviceId = http.Request.Headers["X-Device-Id"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return Results.Unauthorized();
            }

            var userId = http.Request.Headers["X-User-Id"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(userId))
            {
                userId = "local-operator";
            }

            var safeRecent = recentEventsPerStream <= 0 ? 50 : Math.Clamp(recentEventsPerStream, 1, 500);

            var dataDirectory = Path.Combine(environment.ContentRootPath, "App_Data", "snapshots");
            Directory.CreateDirectory(dataDirectory);

            var streamHeads = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            var streams = new[] { EventStream.Chat, EventStream.Graph, EventStream.Nyphos, EventStream.System, EventStream.Devices, EventStream.Identity };

            var recentEvents = new Dictionary<string, object[]>(StringComparer.OrdinalIgnoreCase);
            foreach (var stream in streams)
            {
                var head = await eventStore.GetStreamHeadSeqAsync(stream, cancellationToken);
                streamHeads[stream] = head;

                var events = await eventStore.GetStreamEventsAsync(stream, null, safeRecent, cancellationToken);
                recentEvents[stream] = events
                    .OrderByDescending(e => e.Seq ?? 0)
                    .Select(e => new
                    {
                        e.Seq,
                        e.EventId,
                        e.Type,
                        e.UserId,
                        e.DeviceId,
                        e.TimestampUtc,
                        e.SchemaVersion,
                        e.ClientMsgId,
                        e.PayloadJson
                    })
                    .Cast<object>()
                    .ToArray();
            }

            var snapshot = new
            {
                exportedUtc = DateTimeOffset.UtcNow,
                userId,
                deviceId,
                streamHeads,
                sync = new
                {
                    queueSize = await syncState.GetQueueSizeAsync(cancellationToken),
                    dueQueueSize = await syncState.GetDueQueueSizeAsync(cancellationToken: cancellationToken),
                    lastSeenSeq = await syncState.GetLastSeenMapAsync(cancellationToken)
                },
                mode = await modeService.GetCurrentModeAsync(userId, deviceId, cancellationToken),
                nyphos = await nyphosRiskEngine.ComputeStateAsync(deviceId, cancellationToken),
                recentEvents
            };

            var fileName = $"hecateon-snapshot-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
            var fullPath = Path.Combine(dataDirectory, fileName);
            var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(fullPath, json, cancellationToken);

            return Results.Ok(new
            {
                action = "snapshot-export",
                fileName,
                relativePath = Path.Combine("App_Data", "snapshots", fileName),
                correlationId = http.TraceIdentifier
            });
        });

        app.MapPost("/api/operator/snapshot/import", async (
            HttpContext http,
            IWebHostEnvironment environment,
            ILocalSyncStateService syncState,
            IModeEventService modeService,
            [FromQuery] string fileName,
            [FromQuery] bool apply,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return Results.BadRequest(new { error = "fileName is required." });
            }

            var safeFileName = Path.GetFileName(fileName);
            var snapshotPath = Path.Combine(environment.ContentRootPath, "App_Data", "snapshots", safeFileName);
            if (!File.Exists(snapshotPath))
            {
                return Results.NotFound(new { error = "Snapshot file not found.", fileName = safeFileName });
            }

            var json = await File.ReadAllTextAsync(snapshotPath, cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var importedUserId = root.TryGetProperty("userId", out var userIdEl) ? userIdEl.GetString() : null;
            var importedDeviceId = root.TryGetProperty("deviceId", out var deviceIdEl) ? deviceIdEl.GetString() : null;

            var importedLastSeen = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            if (root.TryGetProperty("sync", out var syncEl) && syncEl.TryGetProperty("lastSeenSeq", out var lastSeenEl) && lastSeenEl.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in lastSeenEl.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Number && prop.Value.TryGetInt64(out var seq))
                    {
                        importedLastSeen[prop.Name] = seq;
                    }
                }
            }

            string? importedMode = null;
            if (root.TryGetProperty("mode", out var modeEl) && modeEl.ValueKind == JsonValueKind.Object)
            {
                if (modeEl.TryGetProperty("current", out var currentMode) && currentMode.ValueKind == JsonValueKind.String)
                {
                    importedMode = currentMode.GetString();
                }
            }

            if (apply)
            {
                foreach (var (stream, seq) in importedLastSeen)
                {
                    await syncState.SetLastSeenSeqAsync(stream, seq, cancellationToken);
                }

                var requestDeviceId = http.Request.Headers["X-Device-Id"].FirstOrDefault();
                var resolvedDeviceId = string.IsNullOrWhiteSpace(requestDeviceId) ? importedDeviceId : requestDeviceId;
                var resolvedUserId = string.IsNullOrWhiteSpace(importedUserId) ? "local-operator" : importedUserId;

                if (!string.IsNullOrWhiteSpace(importedMode) && !string.IsNullOrWhiteSpace(resolvedDeviceId))
                {
                    _ = await modeService.TagModeAsync(resolvedUserId!, resolvedDeviceId!, importedMode, cancellationToken);
                }
            }

            return Results.Ok(new
            {
                action = "snapshot-import",
                fileName = safeFileName,
                apply,
                imported = new
                {
                    userId = importedUserId,
                    deviceId = importedDeviceId,
                    mode = importedMode,
                    lastSeenStreams = importedLastSeen.Keys.OrderBy(x => x).ToArray()
                },
                correlationId = http.TraceIdentifier
            });
        });
    }

    private static string[] ResolveStreams(string? stream)
    {
        if (!string.IsNullOrWhiteSpace(stream) && EventStream.All.Contains(stream, StringComparer.OrdinalIgnoreCase))
        {
            return [stream];
        }

        return [EventStream.Chat, EventStream.Graph, EventStream.Nyphos, EventStream.System];
    }

    private sealed record OperatorEventsQuery(string? Stream, int? Limit);
}
