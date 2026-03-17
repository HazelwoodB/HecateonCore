namespace Hecateon.Events;

public static class EventStream
{
    public const string Identity = "identity";
    public const string Devices = "devices";
    public const string Chat = "chat";
    public const string Graph = "graph";
    public const string Nyphos = "nyphos";
    public const string System = "system";

    public static readonly string[] All =
    [
        Identity,
        Devices,
        Chat,
        Graph,
        Nyphos,
        System
    ];
}
