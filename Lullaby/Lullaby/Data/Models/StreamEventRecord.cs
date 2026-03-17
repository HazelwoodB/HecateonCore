namespace Hecateon.Data.Models;

public sealed class StreamEventRecord
{
    public long Id { get; set; }
    public string EventId { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string DeviceId { get; set; } = null!;
    public string Stream { get; set; } = null!;
    public string Type { get; set; } = null!;
    public long Seq { get; set; }
    public DateTimeOffset TimestampUtc { get; set; }
    public int SchemaVersion { get; set; }
    public string PayloadJson { get; set; } = null!;
    public string ClientMsgId { get; set; } = null!;
}