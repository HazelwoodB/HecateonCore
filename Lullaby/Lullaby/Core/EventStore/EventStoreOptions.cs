namespace Hecateon.Core.EventStore;

public sealed class EventStoreOptions
{
    public const string SectionName = "Hecateon:EventStore";

    public int MaxPayloadBytes { get; set; } = 262_144;
    public int MaxPullLimit { get; set; } = 1_000;
}