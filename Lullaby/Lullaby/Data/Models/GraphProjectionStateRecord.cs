namespace Hecateon.Data.Models;

public sealed class GraphProjectionStateRecord
{
    public string ProjectionName { get; set; } = null!;
    public long LastAppliedSeq { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
}
