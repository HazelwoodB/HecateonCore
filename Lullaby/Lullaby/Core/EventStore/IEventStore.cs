namespace Hecateon.Core.EventStore;

using Hecateon.Core.Models;

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
}

/// <summary>
/// In-memory implementation for MVP (will be replaced with SQL/encrypted store).
/// </summary>
public class InMemoryEventStore : IEventStore
{
    private readonly List<Event> _events = new();
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
}
