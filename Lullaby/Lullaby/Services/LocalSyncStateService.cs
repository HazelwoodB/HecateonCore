using System.Collections.Concurrent;
using System.Text.Json;
using Hecateon.Events;

namespace Hecateon.Services;

public interface ILocalSyncStateService
{
    Task EnqueueAsync(EventEnvelope envelope, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EventEnvelope>> PeekBatchAsync(string stream, int maxItems, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EventEnvelope>> PeekDueBatchAsync(string stream, int maxItems, CancellationToken cancellationToken = default);
    Task RemoveQueuedAsync(IEnumerable<string> clientMsgIds, CancellationToken cancellationToken = default);
    Task ApplyPushOutcomeAsync(IReadOnlyList<AppendItemResult> results, CancellationToken cancellationToken = default);
    Task<int> GetQueueSizeAsync(CancellationToken cancellationToken = default);
    Task<int> GetDueQueueSizeAsync(string? stream = null, CancellationToken cancellationToken = default);
    Task<long> GetLastSeenSeqAsync(string stream, CancellationToken cancellationToken = default);
    Task SetLastSeenSeqAsync(string stream, long seq, CancellationToken cancellationToken = default);
    Task<IDictionary<string, long>> GetLastSeenMapAsync(CancellationToken cancellationToken = default);
}

public sealed class LocalSyncStateService : ILocalSyncStateService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const int RetryBaseSeconds = 5;
    private const int RetryMaxSeconds = 300;

    private readonly string _statePath;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ConcurrentDictionary<string, long> _lastSeenSeq = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<QueuedEnvelopeRecord> _queue = [];

    public LocalSyncStateService(IWebHostEnvironment environment)
    {
        var dataDirectory = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDirectory);
        _statePath = Path.Combine(dataDirectory, "sync-state.json");

        LoadFromDisk();
    }

    public async Task EnqueueAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var existing = _queue.FirstOrDefault(q =>
                string.Equals(q.UserId, envelope.UserId, StringComparison.Ordinal) &&
                string.Equals(q.DeviceId, envelope.DeviceId, StringComparison.Ordinal) &&
                string.Equals(q.ClientMsgId, envelope.ClientMsgId, StringComparison.Ordinal));

            if (existing is null)
            {
                _queue.Add(new QueuedEnvelopeRecord
                {
                    EventId = envelope.EventId,
                    UserId = envelope.UserId,
                    DeviceId = envelope.DeviceId,
                    Stream = envelope.Stream,
                    Type = envelope.Type,
                    Seq = envelope.Seq,
                    TimestampUtc = envelope.TimestampUtc,
                    SchemaVersion = envelope.SchemaVersion,
                    PayloadJson = envelope.PayloadJson,
                    ClientMsgId = envelope.ClientMsgId,
                    EnqueuedUtc = DateTimeOffset.UtcNow,
                    LastAttemptUtc = null,
                    NextAttemptUtc = null,
                    AttemptCount = 0
                });
            }

            await PersistUnsafeAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<EventEnvelope>> PeekBatchAsync(string stream, int maxItems, CancellationToken cancellationToken = default)
    {
        var safeMax = Math.Clamp(maxItems, 1, 1000);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var batch = _queue
                .Where(q => string.Equals(q.Stream, stream, StringComparison.OrdinalIgnoreCase))
                .OrderBy(q => q.EnqueuedUtc)
                .Take(safeMax)
                .Select(ToEnvelope)
                .ToArray();

            return batch;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<EventEnvelope>> PeekDueBatchAsync(string stream, int maxItems, CancellationToken cancellationToken = default)
    {
        var safeMax = Math.Clamp(maxItems, 1, 1000);
        var now = DateTimeOffset.UtcNow;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var batch = _queue
                .Where(q => string.Equals(q.Stream, stream, StringComparison.OrdinalIgnoreCase))
                .Where(q => !q.NextAttemptUtc.HasValue || q.NextAttemptUtc.Value <= now)
                .OrderBy(q => q.EnqueuedUtc)
                .ThenBy(q => q.AttemptCount)
                .Take(safeMax)
                .Select(ToEnvelope)
                .ToArray();

            return batch;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RemoveQueuedAsync(IEnumerable<string> clientMsgIds, CancellationToken cancellationToken = default)
    {
        var toRemove = new HashSet<string>(clientMsgIds, StringComparer.Ordinal);
        if (toRemove.Count == 0)
        {
            return;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            _queue.RemoveAll(x => toRemove.Contains(x.ClientMsgId));
            await PersistUnsafeAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ApplyPushOutcomeAsync(IReadOnlyList<AppendItemResult> results, CancellationToken cancellationToken = default)
    {
        if (results.Count == 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var byClientMsgId = _queue.ToDictionary(x => x.ClientMsgId, x => x, StringComparer.Ordinal);
            var removable = new HashSet<string>(StringComparer.Ordinal);

            foreach (var result in results)
            {
                if (!byClientMsgId.TryGetValue(result.ClientMsgId, out var record))
                {
                    continue;
                }

                if (result.Disposition == AppendDisposition.Accepted ||
                    result.Disposition == AppendDisposition.Duplicate ||
                    (result.Disposition == AppendDisposition.Rejected && !result.Retryable))
                {
                    removable.Add(record.ClientMsgId);
                    continue;
                }

                record.AttemptCount += 1;
                record.LastAttemptUtc = now;
                var backoff = ComputeBackoff(record.AttemptCount);
                record.NextAttemptUtc = now.Add(backoff);
            }

            if (removable.Count > 0)
            {
                _queue.RemoveAll(x => removable.Contains(x.ClientMsgId));
            }

            await PersistUnsafeAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<int> GetQueueSizeAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return _queue.Count;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<int> GetDueQueueSizeAsync(string? stream = null, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            return _queue.Count(q =>
                (string.IsNullOrWhiteSpace(stream) || string.Equals(q.Stream, stream, StringComparison.OrdinalIgnoreCase)) &&
                (!q.NextAttemptUtc.HasValue || q.NextAttemptUtc.Value <= now));
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<long> GetLastSeenSeqAsync(string stream, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return _lastSeenSeq.TryGetValue(stream, out var seq) ? seq : 0L;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SetLastSeenSeqAsync(string stream, long seq, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var current = _lastSeenSeq.TryGetValue(stream, out var existing) ? existing : 0L;
            if (seq > current)
            {
                _lastSeenSeq[stream] = seq;
                await PersistUnsafeAsync(cancellationToken);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IDictionary<string, long>> GetLastSeenMapAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return _lastSeenSeq.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
        }
        finally
        {
            _gate.Release();
        }
    }

    private void LoadFromDisk()
    {
        if (!File.Exists(_statePath))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(_statePath);
            var state = JsonSerializer.Deserialize<LocalSyncStateSnapshot>(json, JsonOptions);
            if (state is null)
            {
                return;
            }

            _queue.Clear();
            _queue.AddRange(state.Queue);

            _lastSeenSeq.Clear();
            foreach (var (stream, seq) in state.LastSeenSeq)
            {
                _lastSeenSeq[stream] = seq;
            }
        }
        catch
        {
        }
    }

    private async Task PersistUnsafeAsync(CancellationToken cancellationToken)
    {
        var snapshot = new LocalSyncStateSnapshot
        {
            Queue = [.. _queue],
            LastSeenSeq = _lastSeenSeq.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase)
        };

        var json = JsonSerializer.Serialize(snapshot, JsonOptions);
        await File.WriteAllTextAsync(_statePath, json, cancellationToken);
    }

    private static EventEnvelope ToEnvelope(QueuedEnvelopeRecord record)
    {
        return new EventEnvelope
        {
            EventId = string.IsNullOrWhiteSpace(record.EventId) ? Guid.NewGuid().ToString() : record.EventId,
            UserId = record.UserId,
            DeviceId = record.DeviceId,
            Stream = record.Stream,
            Type = record.Type,
            Seq = record.Seq,
            TimestampUtc = record.TimestampUtc,
            SchemaVersion = record.SchemaVersion,
            PayloadJson = record.PayloadJson,
            ClientMsgId = record.ClientMsgId
        };
    }

    private static TimeSpan ComputeBackoff(int attemptCount)
    {
        var exponent = Math.Max(0, Math.Min(6, attemptCount - 1));
        var seconds = RetryBaseSeconds * (int)Math.Pow(2, exponent);
        return TimeSpan.FromSeconds(Math.Min(RetryMaxSeconds, seconds));
    }

    private sealed class LocalSyncStateSnapshot
    {
        public List<QueuedEnvelopeRecord> Queue { get; set; } = [];
        public Dictionary<string, long> LastSeenSeq { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class QueuedEnvelopeRecord
    {
        public string EventId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public string Stream { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public long? Seq { get; set; }
        public DateTimeOffset? TimestampUtc { get; set; }
        public int SchemaVersion { get; set; }
        public string PayloadJson { get; set; } = "{}";
        public string ClientMsgId { get; set; } = string.Empty;
        public DateTimeOffset EnqueuedUtc { get; set; }
        public DateTimeOffset? LastAttemptUtc { get; set; }
        public DateTimeOffset? NextAttemptUtc { get; set; }
        public int AttemptCount { get; set; }
    }
}
