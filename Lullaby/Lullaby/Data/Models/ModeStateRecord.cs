namespace Hecateon.Data.Models;

public sealed class ModeStateRecord
{
    public string UserId { get; set; } = null!;
    public string DeviceId { get; set; } = null!;
    public string CurrentMode { get; set; } = null!;
    public string? PreviousMode { get; set; }
    public double LastConfidence { get; set; }
    public string LastSource { get; set; } = "user";
    public string LastEvidenceEventIdsJson { get; set; } = "[]";
    public DateTimeOffset UpdatedUtc { get; set; }
}
