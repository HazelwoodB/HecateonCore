using System.Collections.Concurrent;
using System.Text.Json;
using Hecateon.Models;

namespace Hecateon.Services;

/// <summary>
/// Append-only event log (JSONL) used as canonical server event source.
/// </summary>
public class EventLogService
{
    private const string ChatMessageRecordedEventType = "chat.message.recorded";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly string _eventLogPath;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ConcurrentDictionary<Guid, byte> _seenMessageIds = new();

    public EventLogService(IWebHostEnvironment environment)
    {
        var dataDirectory = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDirectory);
        _eventLogPath = Path.Combine(dataDirectory, "event-log.jsonl");

        HydrateIndexesFromDisk();
    }

    public async Task<bool> AppendChatMessageAsync(ChatMessage message, string? deviceId, CancellationToken cancellationToken = default)
    {
        if (!_seenMessageIds.TryAdd(message.Id, 0))
        {
            return false;
        }

        var envelope = new EventEnvelope
        {
            EventId = Guid.NewGuid(),
            EventType = ChatMessageRecordedEventType,
            EventVersion = 1,
            EntityId = message.Id.ToString("N"),
            DeviceId = string.IsNullOrWhiteSpace(deviceId) ? "unknown-device" : deviceId,
            OccurredAtUtc = DateTime.UtcNow,
            PayloadJson = JsonSerializer.Serialize(message, JsonOptions)
        };

        var line = JsonSerializer.Serialize(envelope, JsonOptions) + Environment.NewLine;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            await File.AppendAllTextAsync(_eventLogPath, line, cancellationToken);
            return true;
        }
        catch
        {
            _seenMessageIds.TryRemove(message.Id, out _);
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<ChatMessage>> GetChatHistoryProjectionAsync(int limit = 200, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(_eventLogPath))
            {
                return [];
            }

            var lines = await File.ReadAllLinesAsync(_eventLogPath, cancellationToken);
            if (lines.Length == 0)
            {
                return [];
            }

            var messages = new List<ChatMessage>(lines.Length);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var envelope = JsonSerializer.Deserialize<EventEnvelope>(line, JsonOptions);
                if (envelope is null || envelope.EventType != ChatMessageRecordedEventType)
                {
                    continue;
                }

                var message = JsonSerializer.Deserialize<ChatMessage>(envelope.PayloadJson, JsonOptions);
                if (message is null)
                {
                    continue;
                }

                messages.Add(message);
            }

            return messages
                .GroupBy(m => m.Id)
                .Select(g => g.Last())
                .OrderBy(m => m.Timestamp)
                .TakeLast(Math.Max(1, limit))
                .ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    private void HydrateIndexesFromDisk()
    {
        if (!File.Exists(_eventLogPath))
        {
            return;
        }

        foreach (var line in File.ReadLines(_eventLogPath))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                var envelope = JsonSerializer.Deserialize<EventEnvelope>(line, JsonOptions);
                if (envelope is null || envelope.EventType != ChatMessageRecordedEventType)
                {
                    continue;
                }

                var message = JsonSerializer.Deserialize<ChatMessage>(envelope.PayloadJson, JsonOptions);
                if (message is not null)
                {
                    _seenMessageIds.TryAdd(message.Id, 0);
                }
            }
            catch
            {
            }
        }
    }
}

public class EventEnvelope
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public int EventVersion { get; set; }
    public string EntityId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    public string PayloadJson { get; set; } = string.Empty;
}
