namespace Hecateon.Core.EventStore;

using Hecateon.Core.Models;
using Hecateon.Data;
using Hecateon.Data.Models;
using Hecateon.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

public sealed class DbEventStore(ChatDbContext dbContext, IOptions<EventStoreOptions> options) : IEventStore
{
    // Legacy in-memory event logic removed; only database-backed implementation remains.
    private static readonly SemaphoreSlim AppendLock = new(1, 1);
    private readonly int _payloadLimit = Math.Max(1024, options.Value.MaxPayloadBytes);

    // All legacy in-memory event methods removed.

    public async Task<BatchAppendResult> AppendToStreamAsync(string stream, IReadOnlyList<EventEnvelope> envelopes, CancellationToken cancellationToken = default)
    {
        if (envelopes.Count == 0)
        {
            return new BatchAppendResult { Items = [] };
        }

        var results = new List<AppendItemResult>(envelopes.Count);

        await AppendLock.WaitAsync(cancellationToken);
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            var streamHead = await dbContext.StreamEvents
                .Where(e => e.Stream == stream)
                .Select(e => (long?)e.Seq)
                .MaxAsync(cancellationToken) ?? 0L;

            foreach (var envelope in envelopes)
            {
                if (!string.Equals(envelope.Stream, stream, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(AppendItemResult.Rejected(envelope.ClientMsgId, "stream_mismatch", retryable: false));
                    continue;
                }

                var validation = EventEnvelopeValidator.Validate(envelope, _payloadLimit, requireServerAssignedFields: false);
                if (!validation.IsValid)
                {
                    var reason = string.Join(";", validation.Errors.Select(e => e.Code));
                    results.Add(AppendItemResult.Rejected(envelope.ClientMsgId, reason, retryable: false));
                    continue;
                }

                var duplicate = await dbContext.StreamEvents
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e =>
                        e.UserId == envelope.UserId &&
                        e.DeviceId == envelope.DeviceId &&
                        e.ClientMsgId == envelope.ClientMsgId,
                        cancellationToken);

                if (duplicate is not null)
                {
                    results.Add(AppendItemResult.Duplicate(envelope.ClientMsgId, duplicate.EventId, duplicate.Seq));
                    continue;
                }

                streamHead++;
                var persisted = new StreamEventRecord
                {
                    EventId = Guid.NewGuid().ToString(),
                    UserId = envelope.UserId,
                    DeviceId = envelope.DeviceId,
                    Stream = stream,
                    Type = envelope.Type,
                    Seq = streamHead,
                    TimestampUtc = DateTimeOffset.UtcNow,
                    SchemaVersion = envelope.SchemaVersion,
                    PayloadJson = envelope.PayloadJson,
                    ClientMsgId = envelope.ClientMsgId
                };

                dbContext.StreamEvents.Add(persisted);
                results.Add(AppendItemResult.Accepted(envelope.ClientMsgId, persisted.EventId, persisted.Seq));
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            AppendLock.Release();
        }

        return new BatchAppendResult { Items = results };
    }

    public async Task<IReadOnlyList<EventEnvelope>> GetStreamEventsAsync(string stream, long? sinceSeq = null, int limit = 100, CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(limit, 1, Math.Max(100, options.Value.MaxPullLimit));

        var query = dbContext.StreamEvents
            .AsNoTracking()
            .Where(e => e.Stream == stream);

        if (sinceSeq.HasValue)
        {
            query = query.Where(e => e.Seq > sinceSeq.Value);
        }

        var rows = await query
            .OrderBy(e => e.Seq)
            .Take(safeLimit)
            .ToListAsync(cancellationToken);

        return rows
            .Select(e => new EventEnvelope
            {
                EventId = e.EventId,
                UserId = e.UserId,
                DeviceId = e.DeviceId,
                Stream = e.Stream,
                Type = e.Type,
                Seq = e.Seq,
                TimestampUtc = e.TimestampUtc,
                SchemaVersion = e.SchemaVersion,
                PayloadJson = e.PayloadJson,
                ClientMsgId = e.ClientMsgId
            })
            .ToArray();
    }

    public async Task<long> GetStreamHeadSeqAsync(string stream, CancellationToken cancellationToken = default)
    {
        return await dbContext.StreamEvents
                   .AsNoTracking()
                   .Where(e => e.Stream == stream)
                   .Select(e => (long?)e.Seq)
                   .MaxAsync(cancellationToken)
               ?? 0L;
    }
}