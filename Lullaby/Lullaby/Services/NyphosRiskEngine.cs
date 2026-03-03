using System.Collections.Concurrent;
using System.Text.Json;
using Hecateon.Models;

namespace Hecateon.Services;

/// <summary>
/// Nyphos Risk Engine - Enhanced trend-based risk assessment with hysteresis
/// Implements Green/Yellow/Orange/Red state machine with explainable transitions
/// Sleep-protective, trend-based, consent-bound
/// </summary>
public class NyphosRiskEngine
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    
    private readonly HealthTrackingService _healthTracking;
    private readonly string _stateHistoryPath;
    private readonly SemaphoreSlim _gate = new(1, 1);
    
    // State persistence
    private NyphosRiskState _currentState = NyphosRiskState.Green;
    private DateTime _lastStateChange = DateTime.UtcNow;
    private readonly List<StateTransition> _transitionHistory = new();
    
    // Hysteresis configuration
    private readonly TimeSpan _minimumCooldown = TimeSpan.FromHours(12); // Prevent rapid state flipping
    private readonly TimeSpan _yellowCooldown = TimeSpan.FromHours(6);
    private readonly TimeSpan _orangeCooldown = TimeSpan.FromHours(12);
    private readonly TimeSpan _redCooldown = TimeSpan.FromHours(24);

    public NyphosRiskEngine(HealthTrackingService healthTracking, IWebHostEnvironment environment)
    {
        _healthTracking = healthTracking;
        var dataDirectory = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDirectory);
        _stateHistoryPath = Path.Combine(dataDirectory, "nyphos-state-history.jsonl");
        
        LoadStateHistory();
    }

    private void LoadStateHistory()
    {
        if (!File.Exists(_stateHistoryPath))
        {
            return;
        }

        try
        {
            var lines = File.ReadAllLines(_stateHistoryPath);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                var transition = JsonSerializer.Deserialize<StateTransition>(line, JsonOptions);
                if (transition is not null)
                {
                    _transitionHistory.Add(transition);
                }
            }

            // Restore last state
            var lastTransition = _transitionHistory.OrderByDescending(t => t.TransitionedAtUtc).FirstOrDefault();
            if (lastTransition is not null)
            {
                _currentState = lastTransition.ToState;
                _lastStateChange = lastTransition.TransitionedAtUtc;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NyphosRiskEngine] Error loading state history: {ex.Message}");
        }
    }

    public async Task<NyphosRiskAssessment> CalculateRiskStateAsync(int daysBack = 7, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var events = await _healthTracking.GetHealthHistoryAsync(daysBack, cancellationToken);
            
            var assessment = new NyphosRiskAssessment
            {
                DaysAnalyzed = daysBack,
                CurrentState = _currentState,
                PreviousState = _transitionHistory.Count > 0 ? _transitionHistory[^1].FromState : null,
                StateChangedAtUtc = _lastStateChange,
                TimeSinceLastTransition = DateTime.UtcNow - _lastStateChange,
                MinimumCooldown = GetCooldownForState(_currentState)
            };

            // Calculate detailed scores
            CalculateSleepMetrics(events, assessment);
            CalculateMoodMetrics(events, assessment);
            CalculateRoutineMetrics(events, assessment);
            CalculateActivityMetrics(events, assessment);

            // Build ranked contributing factors
            BuildContributingFactors(assessment);

            // Determine new state (with hysteresis)
            var proposedState = DetermineProposedState(assessment);
            var canTransition = CanTransitionToState(proposedState, assessment.TimeSinceLastTransition!.Value);
            
            if (canTransition && proposedState != _currentState)
            {
                await TransitionToStateAsync(proposedState, assessment, cancellationToken);
            }

            assessment.CurrentState = _currentState;
            assessment.IsStableState = assessment.TimeSinceLastTransition >= assessment.MinimumCooldown;
            assessment.StateExplanation = BuildStateExplanation(assessment);
            assessment.RecommendedActions = GetRecommendedActions(assessment);

            return assessment;
        }
        finally
        {
            _gate.Release();
        }
    }

    private void CalculateSleepMetrics(IReadOnlyList<HealthEvent> events, NyphosRiskAssessment assessment)
    {
        var sleepEvents = events.Where(e => e.EventType == HealthEventType.Sleep && e.SleepStartUtc.HasValue && e.SleepEndUtc.HasValue).ToList();
        
        if (sleepEvents.Count == 0)
        {
            assessment.SleepScore = 0.5; // Neutral when no data
            assessment.SleepExplanation = "No sleep data available";
            assessment.SleepTrend = SleepTrend.Stable;
            return;
        }

        var avgHours = sleepEvents.Average(e => (e.SleepEndUtc!.Value - e.SleepStartUtc!.Value).TotalHours);
        var avgQuality = sleepEvents.Where(e => e.SleepQuality.HasValue).DefaultIfEmpty().Average(e => e?.SleepQuality ?? 3);
        var variance = CalculateVariance(sleepEvents.Select(e => (e.SleepEndUtc!.Value - e.SleepStartUtc!.Value).TotalHours));

        // Sleep score (0-1)
        var hoursScore = avgHours switch
        {
            >= 7 and <= 9 => 1.0,
            >= 6 and < 7 => 0.7,
            >= 5 and < 6 => 0.4,
            >= 4 and < 5 => 0.2,
            _ => 0.1
        };

        var qualityScore = avgQuality / 5.0;
        var consistencyScore = Math.Max(0, 1.0 - (variance / 4.0)); // Penalize high variance

        assessment.SleepScore = (hoursScore * 0.5) + (qualityScore * 0.3) + (consistencyScore * 0.2);

        // Determine trend
        assessment.SleepTrend = DetermineSleepTrend(sleepEvents, avgHours, variance);
        assessment.SleepExplanation = $"Avg {avgHours:F1}h/night, quality {avgQuality:F1}/5, {sleepEvents.Count}/{assessment.DaysAnalyzed} nights tracked. Trend: {assessment.SleepTrend}";
    }

    private SleepTrend DetermineSleepTrend(List<HealthEvent> sleepEvents, double avgHours, double variance)
    {
        if (sleepEvents.Count < 3) return SleepTrend.Stable;

        // Split into first half vs second half
        var midpoint = sleepEvents.Count / 2;
        var firstHalf = sleepEvents.Take(midpoint).Select(e => (e.SleepEndUtc!.Value - e.SleepStartUtc!.Value).TotalHours).Average();
        var secondHalf = sleepEvents.Skip(midpoint).Select(e => (e.SleepEndUtc!.Value - e.SleepStartUtc!.Value).TotalHours).Average();
        
        var change = secondHalf - firstHalf;

        if (variance > 2.0) return SleepTrend.Disrupted; // High irregularity
        if (avgHours < 6) return SleepTrend.Insufficient; // Chronically low
        if (change > 1.0) return SleepTrend.Improving;
        if (change < -1.0) return SleepTrend.Declining;
        
        return SleepTrend.Stable;
    }

    private void CalculateMoodMetrics(IReadOnlyList<HealthEvent> events, NyphosRiskAssessment assessment)
    {
        var moodEvents = events.Where(e => e.EventType == HealthEventType.Mood && e.MoodScore.HasValue).ToList();
        
        if (moodEvents.Count == 0)
        {
            assessment.MoodScore = 0.5;
            assessment.MoodExplanation = "No mood data available";
            assessment.MoodTrend = MoodTrend.Stable;
            return;
        }

        var avgMood = moodEvents.Average(e => e.MoodScore!.Value);
        var variance = CalculateVariance(moodEvents.Select(e => (double)e.MoodScore!.Value));
        var highMoodCount = moodEvents.Count(e => e.MoodScore >= 2);
        var lowMoodCount = moodEvents.Count(e => e.MoodScore <= -2);

        // Normalize mood to 0-1 scale
        var normalizedMood = (avgMood + 2) / 4.0;
        var stabilityScore = Math.Max(0, 1.0 - (variance / 2.0));

        assessment.MoodScore = (normalizedMood * 0.6) + (stabilityScore * 0.4);

        // Determine trend (Bipolar I specific: watch for elevation)
        assessment.MoodTrend = DetermineMoodTrend(moodEvents, avgMood, variance, highMoodCount, lowMoodCount);
        assessment.MoodExplanation = $"Avg mood {avgMood:F1}/2, variance {variance:F2}, {moodEvents.Count} entries. Trend: {assessment.MoodTrend}";
    }

    private MoodTrend DetermineMoodTrend(List<HealthEvent> moodEvents, double avgMood, double variance, int highMoodCount, int lowMoodCount)
    {
        var totalEvents = moodEvents.Count;
        var highMoodRatio = (double)highMoodCount / totalEvents;
        var lowMoodRatio = (double)lowMoodCount / totalEvents;

        // Bipolar I specific: prioritize detecting elevation
        if (highMoodRatio > 0.4 || avgMood > 1.5) return MoodTrend.Elevated; // Manic warning
        if (lowMoodRatio > 0.4 || avgMood < -1.0) return MoodTrend.Depressed;
        if (variance > 1.5) return MoodTrend.Volatile; // High variability
        
        // Check trend direction
        if (moodEvents.Count >= 4)
        {
            var midpoint = moodEvents.Count / 2;
            var firstHalf = moodEvents.Take(midpoint).Average(e => e.MoodScore!.Value);
            var secondHalf = moodEvents.Skip(midpoint).Average(e => e.MoodScore!.Value);
            var change = secondHalf - firstHalf;

            if (change > 1.0) return MoodTrend.Improving;
            if (change < -1.0) return MoodTrend.Declining;
        }

        return MoodTrend.Stable;
    }

    private void CalculateRoutineMetrics(IReadOnlyList<HealthEvent> events, NyphosRiskAssessment assessment)
    {
        var routineEvents = events.Where(e => e.EventType == HealthEventType.Routine).ToList();
        
        if (routineEvents.Count == 0)
        {
            assessment.RoutineScore = 0.5;
            assessment.RoutineExplanation = "No routine data available";
            return;
        }

        var completionRate = routineEvents.Count(e => e.RoutineCompleted == true) / (double)routineEvents.Count;
        var consistency = Math.Min(1.0, routineEvents.Count / (double)assessment.DaysAnalyzed);

        assessment.RoutineScore = (completionRate * 0.7) + (consistency * 0.3);
        assessment.RoutineExplanation = $"{completionRate:P0} completion rate, {routineEvents.Count} tracked";
    }

    private void CalculateActivityMetrics(IReadOnlyList<HealthEvent> events, NyphosRiskAssessment assessment)
    {
        var activityEvents = events.Where(e => e.EventType == HealthEventType.Activity).ToList();
        
        if (activityEvents.Count == 0)
        {
            assessment.ActivityScore = 0.5;
            assessment.ActivityExplanation = "No activity data available";
            return;
        }

        var totalMinutes = activityEvents.Sum(e => e.DurationMinutes ?? 0);
        var avgMinutesPerDay = totalMinutes / (double)assessment.DaysAnalyzed;
        
        // Balanced activity (not too much, not too little)
        var activityScore = avgMinutesPerDay switch
        {
            >= 30 and <= 120 => 1.0, // Healthy range
            >= 15 and < 30 => 0.7,
            > 120 => 0.6, // Too much (possible mania indicator)
            _ => 0.4
        };

        assessment.ActivityScore = activityScore;
        assessment.ActivityExplanation = $"{avgMinutesPerDay:F0} min/day avg, {activityEvents.Count} activities";
    }

    private void BuildContributingFactors(NyphosRiskAssessment assessment)
    {
        var factors = new List<RiskFactor>();

        // Sleep factors
        if (assessment.SleepScore < 0.6)
        {
            factors.Add(new RiskFactor
            {
                Factor = "Sleep disruption",
                Weight = 1.0 - assessment.SleepScore,
                Explanation = assessment.SleepExplanation,
                Category = "Sleep"
            });
        }

        if (assessment.SleepTrend == SleepTrend.Insufficient || assessment.SleepTrend == SleepTrend.Disrupted)
        {
            factors.Add(new RiskFactor
            {
                Factor = $"Sleep pattern: {assessment.SleepTrend}",
                Weight = 0.8,
                Explanation = "Sleep irregularity detected",
                Category = "Sleep"
            });
        }

        // Mood factors (Bipolar I specific)
        if (assessment.MoodTrend == MoodTrend.Elevated)
        {
            factors.Add(new RiskFactor
            {
                Factor = "Mood elevation detected",
                Weight = 0.9,
                Explanation = assessment.MoodExplanation,
                Category = "Mood"
            });
        }

        if (assessment.MoodTrend == MoodTrend.Depressed)
        {
            factors.Add(new RiskFactor
            {
                Factor = "Depressive trend",
                Weight = 0.8,
                Explanation = assessment.MoodExplanation,
                Category = "Mood"
            });
        }

        if (assessment.MoodTrend == MoodTrend.Volatile)
        {
            factors.Add(new RiskFactor
            {
                Factor = "Mood volatility",
                Weight = 0.7,
                Explanation = "High mood variability",
                Category = "Mood"
            });
        }

        // Routine factors
        if (assessment.RoutineScore < 0.5)
        {
            factors.Add(new RiskFactor
            {
                Factor = "Routine disruption",
                Weight = 1.0 - assessment.RoutineScore,
                Explanation = assessment.RoutineExplanation,
                Category = "Routine"
            });
        }

        // Activity factors
        if (assessment.ActivityScore < 0.7 && assessment.ActivityExplanation.Contains("min/day"))
        {
            var avgMinutes = double.Parse(System.Text.RegularExpressions.Regex.Match(assessment.ActivityExplanation, @"(\d+)").Value);
            if (avgMinutes > 120)
            {
                factors.Add(new RiskFactor
                {
                    Factor = "Excessive activity",
                    Weight = 0.6,
                    Explanation = "Activity level elevated (possible mania indicator)",
                    Category = "Activity"
                });
            }
        }

        // Sort by weight descending
        assessment.ContributingFactors = factors.OrderByDescending(f => f.Weight).ToList();
    }

    private NyphosRiskState DetermineProposedState(NyphosRiskAssessment assessment)
    {
        // Priority: Sleep protection (core to Bipolar I stability)
        var sleepCritical = assessment.SleepTrend == SleepTrend.Insufficient || assessment.SleepScore < 0.3;
        var sleepDisrupted = assessment.SleepTrend == SleepTrend.Disrupted || assessment.SleepScore < 0.5;
        
        // Mood elevation (manic indicators)
        var moodElevated = assessment.MoodTrend == MoodTrend.Elevated;
        var moodDepressed = assessment.MoodTrend == MoodTrend.Depressed;
        var moodVolatile = assessment.MoodTrend == MoodTrend.Volatile;
        
        // Multiple risk factors
        var highRiskFactors = assessment.ContributingFactors.Count(f => f.Weight > 0.7);
        var moderateRiskFactors = assessment.ContributingFactors.Count(f => f.Weight > 0.5);

        // State determination (with bias towards stability)
        if (sleepCritical || (moodElevated && sleepDisrupted) || highRiskFactors >= 2)
        {
            return NyphosRiskState.Red; // Crisis
        }

        if (sleepDisrupted || moodElevated || moodDepressed || highRiskFactors >= 1)
        {
            return NyphosRiskState.Orange; // Downshift needed
        }

        if (moodVolatile || moderateRiskFactors >= 2 || assessment.SleepScore < 0.6)
        {
            return NyphosRiskState.Yellow; // Attention required
        }

        return NyphosRiskState.Green; // Stable
    }

    private bool CanTransitionToState(NyphosRiskState proposedState, TimeSpan timeSinceLastTransition)
    {
        // Always allow transitions to more severe states (safety first)
        if (proposedState > _currentState)
        {
            return true;
        }

        // Downward transitions require cooldown period (prevent oscillation)
        var requiredCooldown = GetCooldownForState(_currentState);
        return timeSinceLastTransition >= requiredCooldown;
    }

    private TimeSpan GetCooldownForState(NyphosRiskState state) => state switch
    {
        NyphosRiskState.Red => _redCooldown,
        NyphosRiskState.Orange => _orangeCooldown,
        NyphosRiskState.Yellow => _yellowCooldown,
        _ => TimeSpan.Zero
    };

    private async Task TransitionToStateAsync(NyphosRiskState newState, NyphosRiskAssessment assessment, CancellationToken cancellationToken)
    {
        var transition = new StateTransition
        {
            FromState = _currentState,
            ToState = newState,
            TransitionedAtUtc = DateTime.UtcNow,
            Reason = BuildTransitionReason(assessment),
            TriggeringFactors = assessment.ContributingFactors.Take(3).ToList(),
            ConfidenceAtTransition = assessment.Confidence
        };

        _transitionHistory.Add(transition);
        _currentState = newState;
        _lastStateChange = DateTime.UtcNow;

        // Persist transition
        var line = JsonSerializer.Serialize(transition, JsonOptions) + Environment.NewLine;
        await File.AppendAllTextAsync(_stateHistoryPath, line, cancellationToken);
    }

    private string BuildTransitionReason(NyphosRiskAssessment assessment)
    {
        if (assessment.ContributingFactors.Count == 0)
        {
            return "Metrics returned to healthy range";
        }

        var topFactors = assessment.ContributingFactors.Take(2).Select(f => f.Factor);
        return $"Triggered by: {string.Join(", ", topFactors)}";
    }

    private string BuildStateExplanation(NyphosRiskAssessment assessment) => assessment.CurrentState switch
    {
        NyphosRiskState.Green => "All metrics within healthy range. Maintain current routines and anchors.",
        NyphosRiskState.Yellow => $"Attention required. {string.Join(", ", assessment.ContributingFactors.Take(2).Select(f => f.Factor))}. Protect sleep and reduce stimulation.",
        NyphosRiskState.Orange => $"Downshift Protocol recommended. {string.Join(", ", assessment.ContributingFactors.Take(2).Select(f => f.Factor))}. Delay major decisions 48-72 hours.",
        NyphosRiskState.Red => $"Crisis state. {string.Join(", ", assessment.ContributingFactors.Take(2).Select(f => f.Factor))}. Activate emergency plan.",
        _ => "Unknown state"
    };

    private List<string> GetRecommendedActions(NyphosRiskAssessment assessment) => assessment.CurrentState switch
    {
        NyphosRiskState.Green => new List<string>
        {
            "✅ Continue current routines",
            "✅ Maintain sleep schedule",
            "✅ Stay connected with supports"
        },
        NyphosRiskState.Yellow => new List<string>
        {
            "😴 Prioritize sleep tonight",
            "☕ Reduce caffeine after 2pm",
            "📱 Limit screen time before bed",
            "💧 Stay hydrated",
            "🧘 Practice grounding exercises"
        },
        NyphosRiskState.Orange => new List<string>
        {
            "🛑 Activate Downshift Protocol",
            "⏸️ Delay major purchases/decisions 48-72 hours",
            "📅 Simplify schedule - cancel non-essentials",
            "☕ Stop caffeine/stimulants",
            "😴 Sleep is top priority",
            "👥 Consider checking in with therapist",
            "📵 Reduce social media and digital stimulation"
        },
        NyphosRiskState.Red => new List<string>
        {
            "🚨 Activate Crisis Plan",
            "📞 Contact clinician or crisis line (988)",
            "👥 Reach out to trusted support person",
            "🛌 Rest in safe environment",
            "💊 Verify medications taken as prescribed",
            "🚫 No major decisions right now",
            "❤️ You are not alone - help is available"
        },
        _ => new List<string>()
    };

    private double CalculateVariance(IEnumerable<double> values)
    {
        var valuesList = values.ToList();
        if (valuesList.Count == 0) return 0;
        
        var mean = valuesList.Average();
        return valuesList.Average(v => Math.Pow(v - mean, 2));
    }
}
