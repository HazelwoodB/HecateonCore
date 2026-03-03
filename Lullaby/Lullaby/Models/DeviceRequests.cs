namespace Lullaby.Models;

public class DeviceEnrollRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
}

public class DeviceApprovalRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public string[]? Scopes { get; set; }
}

public class DeviceRevokeRequest
{
    public string DeviceId { get; set; } = string.Empty;
}

// Nyphos API request models
public class ActivateProtocolRequest
{
    public NyphosRiskState TriggeringState { get; set; }
}

public class CompleteItemRequest
{
    public Guid ItemId { get; set; }
    public string? Note { get; set; }
}

public class DelayDecisionRequest
{
    public string Description { get; set; } = string.Empty;
    public int HoursToDelay { get; set; } = 48; // Default to 48 hours
}
