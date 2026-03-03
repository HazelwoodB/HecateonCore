namespace Hecateon.Core.Models;

/// <summary>
/// Base event for the append-only event store.
/// All domain events must inherit from this.
/// </summary>
public abstract record Event
{
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public string EventType { get; init; } = null!;
    public string Module { get; init; } = null!; // nyphos, prometheon, hecateon
    public DateTime OccurredUtc { get; init; } = DateTime.UtcNow;
    public string? DeviceId { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Hecateon core events
/// </summary>
public record DeviceEnrolledEvent : Event
{
    public string? DisplayName { get; init; }
}

public record DeviceApprovedEvent : Event
{
    public string[] Scopes { get; init; } = [];
}

public record DeviceRevokedEvent : Event
{
}

public record BackupCreatedEvent : Event
{
    public string BackupId { get; init; } = null!;
    public long SizeBytes { get; init; }
    public string? EncryptionKeyHash { get; init; }
}
