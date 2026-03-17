using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Hecateon.Core.EventStore;
using Hecateon.Data;
using Hecateon.Data.Models;
using Hecateon.Events;
using Microsoft.EntityFrameworkCore;

namespace Hecateon.Services;

public sealed record PrometheonExtractionRunResult(int ProcessedChatEvents, int EmittedGraphEvents, long LastProcessedSeq);

public interface IPrometheonExtractionService
{
    Task<PrometheonExtractionRunResult> ProcessPendingChatEventsAsync(int batchSize = 100, CancellationToken cancellationToken = default);
    Task<long> GetLastProcessedSeqAsync(CancellationToken cancellationToken = default);
}

public sealed class PrometheonExtractionService(ChatDbContext dbContext, IEventStore eventStore) : IPrometheonExtractionService
{
    private const string ProjectionName = "prometheon-chat-to-graph";
    private const string PrometheonVersion = "v1.0.0";

    private static readonly HashSet<string> StopWords =
    [
        "the", "and", "for", "that", "with", "this", "have", "from", "your", "about", "what", "when", "where", "which",
        "they", "them", "then", "than", "into", "over", "under", "just", "like", "been", "were", "will", "would", "could",
        "should", "there", "their", "because", "while", "after", "before", "again", "also", "only", "very", "more", "most",
        "some", "such", "much", "many", "here", "make", "made", "does", "doing", "done", "cant", "dont", "im", "youre",
        "hecateon", "nyphos", "prometheon"
    ];

    public async Task<PrometheonExtractionRunResult> ProcessPendingChatEventsAsync(int batchSize = 100, CancellationToken cancellationToken = default)
    {
        var safeBatchSize = Math.Clamp(batchSize, 1, 1000);
        var processed = 0;
        var emitted = 0;
        var lastSeq = await GetLastProcessedSeqAsync(cancellationToken);

        while (true)
        {
            var chatEvents = await eventStore.GetStreamEventsAsync(EventStream.Chat, lastSeq, safeBatchSize, cancellationToken);
            if (chatEvents.Count == 0)
            {
                break;
            }

            foreach (var chatEvent in chatEvents)
            {
                var graphEvents = BuildGraphEvents(chatEvent);
                if (graphEvents.Count > 0)
                {
                    var appendResult = await eventStore.AppendToStreamAsync(EventStream.Graph, graphEvents, cancellationToken);
                    emitted += appendResult.AcceptedCount + appendResult.DuplicateCount;
                }

                lastSeq = chatEvent.Seq ?? lastSeq;
                processed++;
            }

            await UpsertProjectionStateAsync(lastSeq, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new PrometheonExtractionRunResult(processed, emitted, lastSeq);
    }

    public async Task<long> GetLastProcessedSeqAsync(CancellationToken cancellationToken = default)
    {
        var state = await dbContext.GraphProjectionStates
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProjectionName == ProjectionName, cancellationToken);

        return state?.LastAppliedSeq ?? 0L;
    }

    private List<EventEnvelope> BuildGraphEvents(EventEnvelope chatEvent)
    {
        var text = ExtractMessageText(chatEvent.PayloadJson);
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var concepts = ExtractConcepts(text)
            .Take(8)
            .ToArray();

        if (concepts.Length == 0)
        {
            return [];
        }

        var graphEvents = new List<EventEnvelope>(concepts.Length * 3);

        foreach (var concept in concepts)
        {
            var nodeId = $"concept:{StableHash(concept, 12)}";
            var nodePayload = JsonSerializer.Serialize(new
            {
                nodeId,
                type = "Concept",
                canonicalLabel = concept,
                aliases = new[] { concept },
                salience = 1.0,
                prometheonVersion = PrometheonVersion,
                sourceChatEventId = chatEvent.EventId
            });

            graphEvents.Add(NewGraphEnvelope(chatEvent, "GraphNodeUpserted", nodePayload, $"node:{chatEvent.EventId}:{nodeId}"));

            var evidenceId = $"evidence:{StableHash($"{chatEvent.EventId}:{nodeId}", 16)}";
            var evidencePayload = JsonSerializer.Serialize(new
            {
                evidenceId,
                nodeId,
                sourceEventId = chatEvent.EventId,
                snippet = BuildSnippet(text),
                confidence = 0.6,
                prometheonVersion = PrometheonVersion
            });

            graphEvents.Add(NewGraphEnvelope(chatEvent, "GraphEvidenceAttached", evidencePayload, $"evidence:{chatEvent.EventId}:{nodeId}"));
        }

        foreach (var (a, b) in concepts.Zip(concepts.Skip(1)))
        {
            var fromNodeId = $"concept:{StableHash(a, 12)}";
            var toNodeId = $"concept:{StableHash(b, 12)}";
            if (fromNodeId == toNodeId)
            {
                continue;
            }

            var edgeKey = string.CompareOrdinal(fromNodeId, toNodeId) <= 0
                ? $"{fromNodeId}|{toNodeId}"
                : $"{toNodeId}|{fromNodeId}";

            var edgeId = $"edge:{StableHash($"co_occurs:{edgeKey}", 16)}";
            var edgePayload = JsonSerializer.Serialize(new
            {
                edgeId,
                fromId = fromNodeId,
                toId = toNodeId,
                type = "co_occurs",
                weight = 1.0,
                prometheonVersion = PrometheonVersion,
                sourceChatEventId = chatEvent.EventId
            });

            graphEvents.Add(NewGraphEnvelope(chatEvent, "GraphEdgeUpserted", edgePayload, $"edge:{chatEvent.EventId}:{edgeId}"));
        }

        return graphEvents;
    }

    private static EventEnvelope NewGraphEnvelope(EventEnvelope source, string type, string payloadJson, string clientMsgId)
    {
        return new EventEnvelope
        {
            EventId = Guid.NewGuid().ToString(),
            UserId = source.UserId,
            DeviceId = source.DeviceId,
            Stream = EventStream.Graph,
            Type = type,
            Seq = null,
            TimestampUtc = null,
            SchemaVersion = 1,
            PayloadJson = payloadJson,
            ClientMsgId = clientMsgId
        };
    }

    private async Task UpsertProjectionStateAsync(long lastSeq, CancellationToken cancellationToken)
    {
        var state = await dbContext.GraphProjectionStates.FirstOrDefaultAsync(x => x.ProjectionName == ProjectionName, cancellationToken);
        if (state is null)
        {
            dbContext.GraphProjectionStates.Add(new GraphProjectionStateRecord
            {
                ProjectionName = ProjectionName,
                LastAppliedSeq = lastSeq,
                UpdatedUtc = DateTimeOffset.UtcNow
            });
            return;
        }

        state.LastAppliedSeq = lastSeq;
        state.UpdatedUtc = DateTimeOffset.UtcNow;
    }

    private static string ExtractMessageText(string payloadJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            var root = doc.RootElement;

            var candidates = new[] { "message", "content", "text", "body" };
            foreach (var key in candidates)
            {
                if (root.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String)
                {
                    var text = value.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        return text;
                    }
                }
            }
        }
        catch
        {
        }

        return string.Empty;
    }

    private static IEnumerable<string> ExtractConcepts(string text)
    {
        var normalized = text.ToLowerInvariant();
        var tokens = normalized
            .Split([' ', '\t', '\r', '\n', '.', ',', ';', ':', '!', '?', '"', '\'', '(', ')', '[', ']', '{', '}', '/', '\\', '-', '_'], StringSplitOptions.RemoveEmptyEntries)
            .Select(CleanToken)
            .Where(t => t.Length >= 4)
            .Where(t => !StopWords.Contains(t))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(t => t, StringComparer.Ordinal);

        return tokens;
    }

    private static string CleanToken(string token)
    {
        var chars = token.Where(char.IsLetterOrDigit).ToArray();
        return new string(chars);
    }

    private static string BuildSnippet(string text)
    {
        const int maxLen = 240;
        if (text.Length <= maxLen)
        {
            return text;
        }

        return text[..maxLen] + "…";
    }

    private static string StableHash(string value, int length)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        var hex = Convert.ToHexString(hash).ToLowerInvariant();
        return hex[..Math.Min(length, hex.Length)];
    }
}
