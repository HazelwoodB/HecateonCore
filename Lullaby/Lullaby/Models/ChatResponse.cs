namespace Hecateon.Models;

public class ChatResponse
{
    public string Reply { get; set; } = string.Empty;
    public string Sentiment { get; set; } = string.Empty;
    public float Score { get; set; }
}
