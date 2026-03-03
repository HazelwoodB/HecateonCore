namespace Lullaby.Client;

public class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Role { get; set; } = string.Empty; // "user" or "assistant"
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Sentiment { get; set; }
    public float? Score { get; set; }
}

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
}

public class ChatResponse
{
    public string Reply { get; set; } = string.Empty;
    public string Sentiment { get; set; } = string.Empty;
    public float Score { get; set; }
}

/// <summary>
/// User profile - stores user-specific data and preferences
/// </summary>
public class UserProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = "User";
    public string? DisplayName { get; set; }
    public string? TimeZone { get; set; }
    public string? Locale { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// User preferences - customizable settings for ARIA's behavior
/// </summary>
public class UserPreferences
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    
    // Communication Style
    public string CommunicationStyle { get; set; } = "casual"; // casual, formal, technical
    public int ResponseLength { get; set; } = 2; // 1=brief, 2=normal, 3=detailed
    public bool EnableEmojis { get; set; } = true;
    public bool EnableHumor { get; set; } = true;
    
    // Interaction Preferences
    public string[] Topics { get; set; } = Array.Empty<string>();
    public bool EnableGreetings { get; set; } = true;
    public bool EnableWellnessChecks { get; set; } = true;
    
    // Privacy & Data
    public bool SaveConversationHistory { get; set; } = true;
    public int MaxHistoryDays { get; set; } = 30;
    public bool AnonymizeData { get; set; } = false;
    
    // System Behavior
    public bool EnableContextAwareness { get; set; } = true;
    public bool EnableProactiveAlerts { get; set; } = true;
    public int AlertSensitivity { get; set; } = 5; // 1-10 scale
    
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// User feedback on responses - foundation for learning system
/// </summary>
public class ResponseFeedback
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid MessageId { get; set; }
    public int Rating { get; set; } // 1-5 stars
    public string? Comment { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsHelpful { get; set; }
}

/// <summary>
/// Interaction record - stores pattern data for learning
/// </summary>
public class InteractionRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string InteractionType { get; set; } = string.Empty; // "query", "feedback", "rating"
    public string Topic { get; set; } = string.Empty;
    public string Sentiment { get; set; } = string.Empty;
    public int SentimentScore { get; set; }
    public long DurationMs { get; set; }
    public bool WasSuccessful { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// Health tracking models (client-side mirrors of server models)
public enum HealthEventType
{
    Sleep,
    Mood,
    Routine,
    Medication,
    Activity,
    Note
}

public class HealthEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public HealthEventType EventType { get; set; }
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
    public string? DeviceId { get; set; }
    public string? Note { get; set; }
    
    public DateTime? SleepStartUtc { get; set; }
    public DateTime? SleepEndUtc { get; set; }
    public int? SleepQuality { get; set; }
    
    public int? MoodScore { get; set; }
    public string? MoodLabel { get; set; }
    
    public string? RoutineName { get; set; }
    public bool? RoutineCompleted { get; set; }
    
    public string? MedicationName { get; set; }
    public bool? MedicationTaken { get; set; }
    public TimeSpan? MedicationScheduledTime { get; set; }
    
    public string? ActivityType { get; set; }
    public int? DurationMinutes { get; set; }
}

public class HealthEventRequest
{
    public HealthEventType EventType { get; set; }
    public DateTime RecordedAtUtc { get; set; }
    public string? Note { get; set; }
    
    public DateTime? SleepStartUtc { get; set; }
    public DateTime? SleepEndUtc { get; set; }
    public int? SleepQuality { get; set; }
    
    public int? MoodScore { get; set; }
    public string? MoodLabel { get; set; }
    
    public string? RoutineName { get; set; }
    public bool? RoutineCompleted { get; set; }
    
    public string? MedicationName { get; set; }
    public bool? MedicationTaken { get; set; }
    public TimeSpan? MedicationScheduledTime { get; set; }
    
    public string? ActivityType { get; set; }
    public int? DurationMinutes { get; set; }
}

public class HealthTrendScore
{
    public DateTime CalculatedAtUtc { get; set; }
    public int DaysAnalyzed { get; set; }
    
    public double SleepScore { get; set; }
    public string SleepExplanation { get; set; } = string.Empty;
    
    public double MoodScore { get; set; }
    public string MoodExplanation { get; set; } = string.Empty;
    
    public double RoutineScore { get; set; }
    public string RoutineExplanation { get; set; } = string.Empty;
    
    public RiskLevel OverallRiskLevel { get; set; }
    public string RiskExplanation { get; set; } = string.Empty;
}

public enum RiskLevel
{
    Normal,
    Moderate,
    Elevated,
    High,
    Critical
}

// Nyphos risk state models (client-side)
public enum NyphosRiskState
{
    Green,
    Yellow,
    Orange,
    Red
}

public class NyphosRiskAssessment
{
    public Guid AssessmentId { get; set; }
    public DateTime CalculatedAtUtc { get; set; }
    public int DaysAnalyzed { get; set; }
    
    public NyphosRiskState CurrentState { get; set; }
    public NyphosRiskState? PreviousState { get; set; }
    public DateTime? StateChangedAtUtc { get; set; }
    public TimeSpan? TimeSinceLastTransition { get; set; }
    
    public double Confidence { get; set; }
    public bool IsStableState { get; set; }
    public TimeSpan MinimumCooldown { get; set; }
    
    public List<RiskFactor> ContributingFactors { get; set; } = new();
    
    public double SleepScore { get; set; }
    public string SleepExplanation { get; set; } = string.Empty;
    public string SleepTrend { get; set; } = string.Empty;
    
    public double MoodScore { get; set; }
    public string MoodExplanation { get; set; } = string.Empty;
    public string MoodTrend { get; set; } = string.Empty;
    
    public double RoutineScore { get; set; }
    public string RoutineExplanation { get; set; } = string.Empty;
    
    public double ActivityScore { get; set; }
    public string ActivityExplanation { get; set; } = string.Empty;
    
    public List<string> RecommendedActions { get; set; } = new();
    
    public bool DownshiftProtocolActive { get; set; }
    public DateTime? DownshiftActivatedAtUtc { get; set; }
    
    public string StateExplanation { get; set; } = string.Empty;
}

public class RiskFactor
{
    public string Factor { get; set; } = string.Empty;
    public double Weight { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

public class DownshiftProtocol
{
    public Guid ProtocolId { get; set; }
    public DateTime ActivatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public NyphosRiskState TriggeringState { get; set; }
    public List<DownshiftItem> ChecklistItems { get; set; } = new();
    public List<DelayedDecision> DelayedDecisions { get; set; } = new();
}

public class DownshiftItem
{
    public Guid ItemId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? CompletionNote { get; set; }
    public int Priority { get; set; }
}

public class DelayedDecision
{
    public Guid DecisionId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime LoggedAtUtc { get; set; }
    public DateTime ReviewAfterUtc { get; set; }
    public bool WasReviewed { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewOutcome { get; set; }
}

public class CrisisPlan
{
    public Guid PlanId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastUpdatedAtUtc { get; set; }
    public bool AutoActivateOnRed { get; set; }
    public bool AllowEmergencyContact { get; set; }
    public List<SupportContact> SupportContacts { get; set; } = new();
    public string? ClinicianName { get; set; }
    public string? ClinicianPhone { get; set; }
    public string? ClinicianScript { get; set; }
    public List<CrisisResource> CrisisResources { get; set; } = new();
    public List<SafetyStep> SafetySteps { get; set; } = new();
}

public class SupportContact
{
    public Guid ContactId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool CanAutoContact { get; set; }
    public string? PreferredContactMethod { get; set; }
    public string? MessageTemplate { get; set; }
}

public class CrisisResource
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    public string? Website { get; set; }
}

public class SafetyStep
{
    public Guid StepId { get; set; }
    public int Order { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Details { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}
