namespace Hecateon.Core.EventStore;

using Hecateon.Core.Models;
using Hecateon.Events;

/// <summary>
/// Interface for the append-only event store.
/// This is the source of truth for all domain state.
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Append an event to the store (immutable write-once).
    /// </summary>
    Task AppendAsync(Event @event, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all events for a given module (namespace filtering).
    /// </summary>
    Task<IEnumerable<Event>> GetEventsByModuleAsync(string module, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all events for a given device.
    /// </summary>
    Task<IEnumerable<Event>> GetEventsByDeviceAsync(string deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all events (paginated for large stores).
    /// </summary>
    Task<IEnumerable<Event>> GetAllEventsAsync(int skip = 0, int take = 1000, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get event count.
    /// </summary>
    Task<long> GetEventCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Append canonical envelopes to a stream with idempotency semantics.
    /// </summary>
    Task<BatchAppendResult> AppendToStreamAsync(string stream, IReadOnlyList<EventEnvelope> envelopes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pull canonical envelopes from a stream ordered by Seq.
    /// </summary>
    Task<IReadOnlyList<EventEnvelope>> GetStreamEventsAsync(string stream, long? sinceSeq = null, int limit = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the current stream head sequence.
    /// </summary>
    Task<long> GetStreamHeadSeqAsync(string stream, CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory implementation for MVP (will be replaced with SQL/encrypted store).
/// </summary>
public class InMemoryEventStore : IEventStore
{
    private readonly List<Event> _events = new();
    private readonly List<EventEnvelope> _streamEvents = new();
    private readonly Dictionary<IdempotencyKey, EventEnvelope> _idempotencyIndex = new();
    private readonly Dictionary<string, long> _streamHeads = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public Task AppendAsync(Event @event, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _events.Add(@event);
        }
        return Task.CompletedTask;
    }

    public Task<IEnumerable<Event>> GetEventsByModuleAsync(string module, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_events.Where(e => e.Module == module).AsEnumerable());
        }
    }

    public Task<IEnumerable<Event>> GetEventsByDeviceAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_events.Where(e => e.DeviceId == deviceId).AsEnumerable());
        }
    }

    public Task<IEnumerable<Event>> GetAllEventsAsync(int skip = 0, int take = 1000, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_events.Skip(skip).Take(take).AsEnumerable());
        }
    }

    public Task<long> GetEventCountAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult((long)_events.Count);
        }
    }

    public Task<BatchAppendResult> AppendToStreamAsync(string stream, IReadOnlyList<EventEnvelope> envelopes, CancellationToken cancellationToken = default)
    {
        if (envelopes.Count == 0)
        {
            return Task.FromResult(new BatchAppendResult { Items = [] });
        }

        var results = new List<AppendItemResult>(envelopes.Count);

        lock (_lock)
        {
            if (!_streamHeads.TryGetValue(stream, out var streamHeadSeq))
            {
                streamHeadSeq = 0;
                _streamHeads[stream] = streamHeadSeq;
            }

            foreach (var envelope in envelopes)
            {
                if (!string.Equals(envelope.Stream, stream, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(AppendItemResult.Rejected(envelope.ClientMsgId, "stream_mismatch", retryable: false));
                    continue;
                }

                var validation = EventEnvelopeValidator.Validate(envelope, requireServerAssignedFields: false);
                if (!validation.IsValid)
                {
                    var reason = string.Join(";", validation.Errors.Select(e => e.Code));
                    results.Add(AppendItemResult.Rejected(envelope.ClientMsgId, reason, retryable: false));
                    continue;
                }

                var key = new IdempotencyKey(envelope.UserId, envelope.DeviceId, envelope.ClientMsgId);
                if (_idempotencyIndex.TryGetValue(key, out var existing))
                {
                    results.Add(AppendItemResult.Duplicate(envelope.ClientMsgId, existing.EventId, existing.Seq ?? 0));
                    continue;
                }

                streamHeadSeq++;
                var persistedEnvelope = envelope with
                {
                    EventId = Guid.NewGuid().ToString(),
                    Seq = streamHeadSeq,
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Stream = stream
                };

                _streamHeads[stream] = streamHeadSeq;
                _idempotencyIndex[key] = persistedEnvelope;
                _streamEvents.Add(persistedEnvelope);

                results.Add(AppendItemResult.Accepted(envelope.ClientMsgId, persistedEnvelope.EventId, streamHeadSeq));
            }
        }

        return Task.FromResult(new BatchAppendResult { Items = results });
    }

    public Task<IReadOnlyList<EventEnvelope>> GetStreamEventsAsync(string stream, long? sinceSeq = null, int limit = 100, CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
        {
            limit = 100;
        }

        if (limit > 1000)
        {
            limit = 1000;
        }

        lock (_lock)
        {
            IEnumerable<EventEnvelope> query = _streamEvents
                .Where(e => string.Equals(e.Stream, stream, StringComparison.OrdinalIgnoreCase))
                .OrderBy(e => e.Seq);

            if (sinceSeq.HasValue)
            {
                query = query.Where(e => (e.Seq ?? 0) > sinceSeq.Value);
            }

            var events = query
                .Take(limit)
                .ToArray();

            return Task.FromResult<IReadOnlyList<EventEnvelope>>(events);
        }
    }

    public Task<long> GetStreamHeadSeqAsync(string stream, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_streamHeads.TryGetValue(stream, out var seq) ? seq : 0L);
        }
    }
}
