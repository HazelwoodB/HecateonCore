namespace Lullaby.Models.Api;

/// <summary>
/// Standard API response envelope for consistent response structure.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; } = true;
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Message { get; set; }
    public long? Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    public static ApiResponse<T> Ok(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    public static ApiResponse<T> CreateError(string errorMessage, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            ErrorMessage = errorMessage,
            Message = message
        };
    }
}

/// <summary>
/// Device enrollment response.
/// </summary>
public class DeviceEnrollmentResponse
{
    public string DeviceId { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public bool IsApproved { get; set; }
    public string[] Scopes { get; set; } = [];
    public DateTime EnrolledUtc { get; set; }
}

/// <summary>
/// Risk state response for clinician consumption.
/// </summary>
public class RiskStateResponse
{
    public string RiskLevel { get; set; } = "GREEN"; // GREEN, YELLOW, RED, CRISIS
    public int RiskScore { get; set; } // 0-100
    public List<string> IdentifiedFactors { get; set; } = new();
    public List<InterventionRecommendation> RecommendedActions { get; set; } = new();
    public string ClinicalSummary { get; set; } = string.Empty;
    public DateTime AssessedUtc { get; set; }
    public double DataConfidence { get; set; } // 0.0-1.0
}

/// <summary>
/// Individual intervention recommendation.
/// </summary>
public class InterventionRecommendation
{
    public int Priority { get; set; } // 1=immediate, 2=urgent, 3=recommended
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> ActionSteps { get; set; } = new();
}

/// <summary>
/// Health metric trend for visualization.
/// </summary>
public class HealthMetricTrend
{
    public string MetricName { get; set; } = string.Empty;
    public double CurrentValue { get; set; }
    public string Direction { get; set; } = string.Empty; // improving, stable, declining
    public List<DataPoint> RecentPoints { get; set; } = new();

    public class DataPoint
    {
        public DateTime RecordedUtc { get; set; }
        public double Value { get; set; }
    }
}

/// <summary>
/// Weekly report summary for export/review.
/// </summary>
public class WeeklyReportResponse
{
    public string ReportId { get; set; } = Guid.NewGuid().ToString();
    public DateTime WeekStartUtc { get; set; }
    public DateTime WeekEndUtc { get; set; }
    public Dictionary<string, object> Metrics { get; set; } = new();
    public string ClinicalSummary { get; set; } = string.Empty;
    public List<string> NotableEvents { get; set; } = new();
    public List<InterventionRecommendation> RecommendedInterventions { get; set; } = new();
}

/// <summary>
/// Device approval request from recovery code holder.
/// </summary>
public class ApproveDeviceRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public string[]? Scopes { get; set; } = new[] { "health:read", "chat:read", "chat:write" };
}

/// <summary>
/// Device revocation request.
/// </summary>
public class RevokeDeviceRequest
{
    public string DeviceId { get; set; } = string.Empty;
}

/// <summary>
/// Enrollment request for pending device.
/// </summary>
public class EnrollDeviceRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
}
