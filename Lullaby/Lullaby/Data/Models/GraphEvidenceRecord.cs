namespace Hecateon.Data.Models;

public sealed class GraphEvidenceRecord
{
    public string EvidenceId { get; set; } = null!;
    public string? NodeId { get; set; }
    public string? EdgeId { get; set; }
    public string SourceEventId { get; set; } = null!;
    public string Snippet { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
}
