namespace Hecateon.Data.Models;

public sealed class GraphNodeRecord
{
    public string NodeId { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string CanonicalLabel { get; set; } = null!;
    public string AliasesJson { get; set; } = "[]";
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
    public double Salience { get; set; }
}
