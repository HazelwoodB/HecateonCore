namespace Hecateon.Modules.Nyphos.Models;

/// <summary>
/// Explainable risk assessment with clinician-ready format.
/// Provides transparent reasoning for risk scoring and actionable recommendations.
/// </summary>
public class ExplainableRiskAssessment
{
    /// <summary>
    /// Risk level: GREEN (0-30), YELLOW (31-60), RED (61-85), CRISIS (86-100).
    /// </summary>
    public string RiskLevel { get; set; } = "GREEN";

    /// <summary>
    /// Numeric score 0-100 (deterministic).
    /// </summary>
    public int RiskScore { get; set; }

    /// <summary>
    /// Primary factors contributing to risk (explainability).
    /// </summary>
    public List<RiskFactor> IdentifiedFactors { get; set; } = new();

    /// <summary>
    /// Recommended interventions in priority order (deterministic ladder).
    /// </summary>
    public List<Intervention> RecommendedInterventions { get; set; } = new();

    /// <summary>
    /// Key insights for clinician review.
    /// </summary>
    public string ClinicalSummary { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of assessment.
    /// </summary>
    public DateTime AssessedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Confidence in assessment (0.0-1.0 based on data availability).
    /// </summary>
    public double Confidence { get; set; }
}

/// <summary>
/// A single risk contributing factor with context.
/// </summary>
public class RiskFactor
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // sleep, mood, activity, social, etc.
    public int ScoreContribution { get; set; } // 0-100, how much this factor adds to total
    public string Description { get; set; } = string.Empty; // Explainable context
    public string Severity { get; set; } = "mild"; // mild, moderate, severe
    public DateTime? FirstObservedUtc { get; set; }
    public DateTime? LastObservedUtc { get; set; }
}

/// <summary>
/// A recommended intervention with explicit consent boundaries.
/// </summary>
public class Intervention
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public int Priority { get; set; } // 1=immediate, 2=urgent, 3=recommended, 4=optional
    public string Type { get; set; } = string.Empty; // mood_check, sleep_routine, medication, contact_provider, etc.
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Rationale { get; set; } = string.Empty; // Why this intervention
    public List<string> ActionSteps { get; set; } = new(); // Explicit steps for user
    public DateTime SuggestedUtc { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// User agency: can skip, delay, or accept.
    /// </summary>
    public string Status { get; set; } = "suggested"; // suggested, accepted, delayed, skipped, completed
}

/// <summary>
/// Downshift Protocol: deterministic intervention ladder for crisis.
/// </summary>
public class DownshiftProtocolRecommendation
{
    public int StepNumber { get; set; } // 1, 2, 3, 4, 5
    public string StepName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Actions { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime ActivatedUtc { get; set; }
}

/// <summary>
/// Trend analysis over time for clinician insights.
/// </summary>
public class HealthTrend
{
    public string MetricName { get; set; } = string.Empty;
    public double CurrentValue { get; set; }
    public double? PreviousValue { get; set; }
    public double? TrendAverage { get; set; }
    public string Direction { get; set; } = string.Empty; // improving, stable, declining
    public int DaysOfData { get; set; }
    public List<DataPoint> RecentDataPoints { get; set; } = new();

    public class DataPoint
    {
        public DateTime RecordedUtc { get; set; }
        public double Value { get; set; }
    }
}
