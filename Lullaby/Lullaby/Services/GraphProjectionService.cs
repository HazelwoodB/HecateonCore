using System.Text.Json;
using Hecateon.Core.EventStore;
using Hecateon.Data;
using Hecateon.Data.Models;
using Hecateon.Events;
using Microsoft.EntityFrameworkCore;

namespace Hecateon.Services;

public sealed record GraphProjectionRunResult(int AppliedEvents, long LastAppliedSeq, bool Rebuilt);

public interface IGraphProjectionService
{
    Task<GraphProjectionRunResult> ApplyPendingAsync(int batchSize = 200, CancellationToken cancellationToken = default);
    Task<GraphProjectionRunResult> RebuildAsync(int batchSize = 200, CancellationToken cancellationToken = default);
    Task<long> GetLastAppliedSeqAsync(CancellationToken cancellationToken = default);
}

public sealed class GraphProjectionService(ChatDbContext dbContext, IEventStore eventStore) : IGraphProjectionService
{
    private const string ProjectionName = "graph";

    public async Task<GraphProjectionRunResult> ApplyPendingAsync(int batchSize = 200, CancellationToken cancellationToken = default)
    {
        var safeBatchSize = Math.Clamp(batchSize, 1, 1000);
        var appliedEvents = 0;
        var lastAppliedSeq = await GetLastAppliedSeqAsync(cancellationToken);

        while (true)
        {
            var batch = await eventStore.GetStreamEventsAsync(EventStream.Graph, lastAppliedSeq, safeBatchSize, cancellationToken);
            if (batch.Count == 0)
            {
                break;
            }

            foreach (var envelope in batch)
            {
                await ApplyEnvelopeAsync(envelope, cancellationToken);
                lastAppliedSeq = envelope.Seq ?? lastAppliedSeq;
                appliedEvents++;
            }

            await UpsertProjectionStateAsync(lastAppliedSeq, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new GraphProjectionRunResult(appliedEvents, lastAppliedSeq, Rebuilt: false);
    }

    public async Task<GraphProjectionRunResult> RebuildAsync(int batchSize = 200, CancellationToken cancellationToken = default)
    {
        await dbContext.GraphNodes.ExecuteDeleteAsync(cancellationToken);
        await dbContext.GraphEdges.ExecuteDeleteAsync(cancellationToken);
        await dbContext.GraphEvidence.ExecuteDeleteAsync(cancellationToken);

        var state = await dbContext.GraphProjectionStates.FirstOrDefaultAsync(x => x.ProjectionName == ProjectionName, cancellationToken);
        if (state is null)
        {
            dbContext.GraphProjectionStates.Add(new GraphProjectionStateRecord
            {
                ProjectionName = ProjectionName,
                LastAppliedSeq = 0,
                UpdatedUtc = DateTimeOffset.UtcNow
            });
        }
        else
        {
            state.LastAppliedSeq = 0;
            state.UpdatedUtc = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var result = await ApplyPendingAsync(batchSize, cancellationToken);
        return result with { Rebuilt = true };
    }

    public async Task<long> GetLastAppliedSeqAsync(CancellationToken cancellationToken = default)
    {
        var state = await dbContext.GraphProjectionStates
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProjectionName == ProjectionName, cancellationToken);

        return state?.LastAppliedSeq ?? 0L;
    }

    private async Task ApplyEnvelopeAsync(EventEnvelope envelope, CancellationToken cancellationToken)
    {
        switch (envelope.Type)
        {
            case "GraphNodeUpserted":
                await ApplyNodeUpsertAsync(envelope, cancellationToken);
                break;
            case "GraphEdgeUpserted":
                await ApplyEdgeUpsertAsync(envelope, cancellationToken);
                break;
            case "GraphEvidenceAttached":
                await ApplyEvidenceUpsertAsync(envelope, cancellationToken);
                break;
            case "GraphNodeMerged":
                await ApplyNodeMergedAsync(envelope, cancellationToken);
                break;
            default:
                break;
        }
    }

    private async Task ApplyNodeUpsertAsync(EventEnvelope envelope, CancellationToken cancellationToken)
    {
        using var doc = JsonDocument.Parse(envelope.PayloadJson);
        var payload = doc.RootElement;

        var nodeId = GetString(payload, "nodeId", "id");
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            return;
        }

        var now = envelope.TimestampUtc ?? DateTimeOffset.UtcNow;
        var type = GetString(payload, "type") ?? "Concept";
        var canonicalLabel = GetString(payload, "canonicalLabel", "label") ?? nodeId;
        var aliasesJson = TryGetAliasesJson(payload) ?? "[]";
        var salience = GetDouble(payload, "salience") ?? 0;

        var node = await dbContext.GraphNodes.FirstOrDefaultAsync(n => n.NodeId == nodeId, cancellationToken);
        if (node is null)
        {
            dbContext.GraphNodes.Add(new GraphNodeRecord
            {
                NodeId = nodeId,
                Type = type,
                CanonicalLabel = canonicalLabel,
                AliasesJson = aliasesJson,
                Salience = salience,
                CreatedUtc = now,
                UpdatedUtc = now
            });
            return;
        }

        node.Type = type;
        node.CanonicalLabel = canonicalLabel;
        node.AliasesJson = aliasesJson;
        node.Salience = salience;
        node.UpdatedUtc = now;
    }

    private async Task ApplyEdgeUpsertAsync(EventEnvelope envelope, CancellationToken cancellationToken)
    {
        using var doc = JsonDocument.Parse(envelope.PayloadJson);
        var payload = doc.RootElement;

        var edgeId = GetString(payload, "edgeId", "id");
        var fromId = GetString(payload, "fromId", "from");
        var toId = GetString(payload, "toId", "to");
        if (string.IsNullOrWhiteSpace(edgeId) || string.IsNullOrWhiteSpace(fromId) || string.IsNullOrWhiteSpace(toId))
        {
            return;
        }

        var now = envelope.TimestampUtc ?? DateTimeOffset.UtcNow;
        var type = GetString(payload, "type") ?? "relates_to";
        var weight = GetDouble(payload, "weight") ?? 1;

        var edge = await dbContext.GraphEdges.FirstOrDefaultAsync(e => e.EdgeId == edgeId, cancellationToken);
        if (edge is null)
        {
            dbContext.GraphEdges.Add(new GraphEdgeRecord
            {
                EdgeId = edgeId,
                FromId = fromId,
                ToId = toId,
                Type = type,
                Weight = weight,
                CreatedUtc = now,
                UpdatedUtc = now
            });
            return;
        }

        edge.FromId = fromId;
        edge.ToId = toId;
        edge.Type = type;
        edge.Weight = weight;
        edge.UpdatedUtc = now;
    }

    private async Task ApplyEvidenceUpsertAsync(EventEnvelope envelope, CancellationToken cancellationToken)
    {
        using var doc = JsonDocument.Parse(envelope.PayloadJson);
        var payload = doc.RootElement;

        var evidenceId = GetString(payload, "evidenceId", "id");
        if (string.IsNullOrWhiteSpace(evidenceId))
        {
            return;
        }

        var now = envelope.TimestampUtc ?? DateTimeOffset.UtcNow;
        var nodeId = GetString(payload, "nodeId");
        var edgeId = GetString(payload, "edgeId");
        var sourceEventId = GetString(payload, "sourceEventId") ?? envelope.EventId;
        var snippet = GetString(payload, "snippet") ?? string.Empty;
        var confidence = GetDouble(payload, "confidence") ?? 0;

        var evidence = await dbContext.GraphEvidence.FirstOrDefaultAsync(e => e.EvidenceId == evidenceId, cancellationToken);
        if (evidence is null)
        {
            dbContext.GraphEvidence.Add(new GraphEvidenceRecord
            {
                EvidenceId = evidenceId,
                NodeId = nodeId,
                EdgeId = edgeId,
                SourceEventId = sourceEventId,
                Snippet = snippet,
                Confidence = confidence,
                CreatedUtc = now
            });
            return;
        }

        evidence.NodeId = nodeId;
        evidence.EdgeId = edgeId;
        evidence.SourceEventId = sourceEventId;
        evidence.Snippet = snippet;
        evidence.Confidence = confidence;
    }

    private async Task ApplyNodeMergedAsync(EventEnvelope envelope, CancellationToken cancellationToken)
    {
        using var doc = JsonDocument.Parse(envelope.PayloadJson);
        var payload = doc.RootElement;

        var fromNodeId = GetString(payload, "fromNodeId", "sourceNodeId");
        var intoNodeId = GetString(payload, "intoNodeId", "targetNodeId", "toNodeId");

        if (string.IsNullOrWhiteSpace(fromNodeId) || string.IsNullOrWhiteSpace(intoNodeId) || fromNodeId == intoNodeId)
        {
            return;
        }

        var fromEdges = await dbContext.GraphEdges.Where(e => e.FromId == fromNodeId).ToListAsync(cancellationToken);
        foreach (var edge in fromEdges)
        {
            edge.FromId = intoNodeId;
        }

        var toEdges = await dbContext.GraphEdges.Where(e => e.ToId == fromNodeId).ToListAsync(cancellationToken);
        foreach (var edge in toEdges)
        {
            edge.ToId = intoNodeId;
        }

        var evidence = await dbContext.GraphEvidence.Where(e => e.NodeId == fromNodeId).ToListAsync(cancellationToken);
        foreach (var item in evidence)
        {
            item.NodeId = intoNodeId;
        }

        var oldNode = await dbContext.GraphNodes.FirstOrDefaultAsync(n => n.NodeId == fromNodeId, cancellationToken);
        if (oldNode is not null)
        {
            dbContext.GraphNodes.Remove(oldNode);
        }
    }

    private async Task UpsertProjectionStateAsync(long lastSeq, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var state = await dbContext.GraphProjectionStates.FirstOrDefaultAsync(x => x.ProjectionName == ProjectionName, cancellationToken);
        if (state is null)
        {
            dbContext.GraphProjectionStates.Add(new GraphProjectionStateRecord
            {
                ProjectionName = ProjectionName,
                LastAppliedSeq = lastSeq,
                UpdatedUtc = now
            });
            return;
        }

        state.LastAppliedSeq = lastSeq;
        state.UpdatedUtc = now;
    }

    private static string? GetString(JsonElement payload, params string[] names)
    {
        foreach (var name in names)
        {
            if (!payload.TryGetProperty(name, out var element))
            {
                continue;
            }

            if (element.ValueKind == JsonValueKind.String)
            {
                return element.GetString();
            }

            if (element.ValueKind == JsonValueKind.Number)
            {
                return element.GetRawText();
            }
        }

        return null;
    }

    private static double? GetDouble(JsonElement payload, string name)
    {
        if (!payload.TryGetProperty(name, out var element))
        {
            return null;
        }

        if (element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out var value))
        {
            return value;
        }

        if (element.ValueKind == JsonValueKind.String && double.TryParse(element.GetString(), out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static string? TryGetAliasesJson(JsonElement payload)
    {
        if (!payload.TryGetProperty("aliases", out var aliases))
        {
            return null;
        }

        return aliases.ValueKind switch
        {
            JsonValueKind.Array => aliases.GetRawText(),
            JsonValueKind.String => JsonSerializer.Serialize(new[] { aliases.GetString() ?? string.Empty }),
            _ => "[]"
        };
    }
}
