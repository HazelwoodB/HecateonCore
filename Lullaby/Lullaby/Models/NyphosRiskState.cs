namespace Lullaby.Models;

/// <summary>
/// Nyphos risk state model with hysteresis and explainable transitions
/// Green → Yellow → Orange → Red (with cooldown periods to prevent oscillation)
/// </summary>
public enum NyphosRiskState
{
    Green,      // Stable - maintain routines
    Yellow,     // Attention - protect sleep, reduce stimulation
    Orange,     // Downshift - activate protocol, delay major decisions
    Red         // Crisis - emergency plan, no auto-contact without consent
}

/// <summary>
/// Risk state assessment with explainable factors and transition tracking
/// </summary>
public class NyphosRiskAssessment
{
    public Guid AssessmentId { get; set; } = Guid.NewGuid();
    public DateTime CalculatedAtUtc { get; set; } = DateTime.UtcNow;
    public int DaysAnalyzed { get; set; }
    
    // Current state
    public NyphosRiskState CurrentState { get; set; }
    public NyphosRiskState? PreviousState { get; set; }
    public DateTime? StateChangedAtUtc { get; set; }
    public TimeSpan? TimeSinceLastTransition { get; set; }
    
    // Confidence and stability
    public double Confidence { get; set; } // 0.0 to 1.0
    public bool IsStableState { get; set; } // Has state been stable for cooldown period?
    public TimeSpan MinimumCooldown { get; set; } = TimeSpan.FromHours(12); // Prevent rapid transitions
    
    // Ranked contributing factors
    public List<RiskFactor> ContributingFactors { get; set; } = new();
    
    // Detailed scores
    public double SleepScore { get; set; }
    public string SleepExplanation { get; set; } = string.Empty;
    public SleepTrend SleepTrend { get; set; }
    
    public double MoodScore { get; set; }
    public string MoodExplanation { get; set; } = string.Empty;
    public MoodTrend MoodTrend { get; set; }
    
    public double RoutineScore { get; set; }
    public string RoutineExplanation { get; set; } = string.Empty;
    
    public double ActivityScore { get; set; }
    public string ActivityExplanation { get; set; } = string.Empty;
    
    // Recommended actions (deterministic, state-based)
    public List<string> RecommendedActions { get; set; } = new();
    
    // Intervention tracking
    public bool DownshiftProtocolActive { get; set; }
    public DateTime? DownshiftActivatedAtUtc { get; set; }
    
    // Overall explanation
    public string StateExplanation { get; set; } = string.Empty;
}

/// <summary>
/// Individual risk factor with weight and explanation
/// </summary>
public class RiskFactor
{
    public string Factor { get; set; } = string.Empty;
    public double Weight { get; set; } // 0.0 to 1.0 (contribution to risk)
    public string Explanation { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // "Sleep", "Mood", "Activity", etc.
}

/// <summary>
/// Sleep trend indicators
/// </summary>
public enum SleepTrend
{
    Stable,
    Improving,
    Declining,
    Disrupted,      // Irregular patterns
    Insufficient    // Consistently low
}

/// <summary>
/// Mood trend indicators
/// </summary>
public enum MoodTrend
{
    Stable,
    Improving,
    Declining,
    Elevated,       // Manic indicators
    Depressed,      // Depressive indicators
    Volatile        // High variance
}

/// <summary>
/// State transition record for audit trail
/// </summary>
public class StateTransition
{
    public Guid TransitionId { get; set; } = Guid.NewGuid();
    public DateTime TransitionedAtUtc { get; set; } = DateTime.UtcNow;
    public NyphosRiskState FromState { get; set; }
    public NyphosRiskState ToState { get; set; }
    public string Reason { get; set; } = string.Empty;
    public List<RiskFactor> TriggeringFactors { get; set; } = new();
    public double ConfidenceAtTransition { get; set; }
}
