namespace Hecateon.Data.Models;

public sealed class GraphEdgeRecord
{
    public string EdgeId { get; set; } = null!;
    public string FromId { get; set; } = null!;
    public string ToId { get; set; } = null!;
    public string Type { get; set; } = null!;
    public double Weight { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
}
