namespace Hecateon.Events;

public sealed record EventEnvelope
{
    public required string EventId { get; init; }
    public required string UserId { get; init; }
    public required string DeviceId { get; init; }
    public required string Stream { get; init; }
    public required string Type { get; init; }
    public long? Seq { get; init; }
    public DateTimeOffset? TimestampUtc { get; init; }
    public required int SchemaVersion { get; init; }
    public required string PayloadJson { get; init; }
    public required string ClientMsgId { get; init; }
}
