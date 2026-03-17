namespace Hecateon.Data.Models;

public sealed class NyphosPreferenceRecord
{
    public string UserId { get; set; } = null!;
    public string DeviceId { get; set; } = null!;
    public string Tone { get; set; } = "Gentle";
    public DateTimeOffset? MutedUntilUtc { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
}
