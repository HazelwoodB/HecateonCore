namespace Hecateon.Events;

public sealed record BatchAppendResult
{
    public required IReadOnlyList<AppendItemResult> Items { get; init; }

    public int AcceptedCount => Items.Count(i => i.Disposition == AppendDisposition.Accepted);
    public int DuplicateCount => Items.Count(i => i.Disposition == AppendDisposition.Duplicate);
    public int RejectedCount => Items.Count(i => i.Disposition == AppendDisposition.Rejected);
}
