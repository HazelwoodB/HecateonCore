namespace Hecateon.Events;

public enum AppendDisposition
{
    Accepted = 1,
    Duplicate = 2,
    Rejected = 3
}

public sealed record AppendItemResult
{
    public required string ClientMsgId { get; init; }
    public required AppendDisposition Disposition { get; init; }
    public string? EventId { get; init; }
    public long? Seq { get; init; }
    public string? Reason { get; init; }
    public bool Retryable { get; init; }

    public static AppendItemResult Accepted(string clientMsgId, string eventId, long seq) =>
        new()
        {
            ClientMsgId = clientMsgId,
            Disposition = AppendDisposition.Accepted,
            EventId = eventId,
            Seq = seq
        };

    public static AppendItemResult Duplicate(string clientMsgId, string eventId, long seq) =>
        new()
        {
            ClientMsgId = clientMsgId,
            Disposition = AppendDisposition.Duplicate,
            EventId = eventId,
            Seq = seq
        };

    public static AppendItemResult Rejected(string clientMsgId, string reason, bool retryable = false) =>
        new()
        {
            ClientMsgId = clientMsgId,
            Disposition = AppendDisposition.Rejected,
            Reason = reason,
            Retryable = retryable
        };
}
