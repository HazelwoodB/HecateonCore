namespace Hecateon.Modules.Nyphos.Services;

using Hecateon.Core.EventStore;
using Hecateon.Modules.Nyphos.Models;

/// <summary>
/// Nyphos Risk Engine
/// Interprets recent events and produces explainable risk assessments.
/// Deterministic, hysteresis-aware, scope-limited.
/// </summary>
public interface INyphosRiskEngine
{
    /// <summary>
    /// Compute current risk state for a device.
    /// </summary>
    Task<NyphosState> ComputeStateAsync(string deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Log a sleep event.
    /// </summary>
    Task LogSleepAsync(string deviceId, DateTime sleepStart, DateTime sleepEnd, int? qualityScore, string[]? interruptions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Log a mood event.
    /// </summary>
    Task LogMoodAsync(string deviceId, int? energyLevel, int? moodScore, string? moodLabel, string? notes, CancellationToken cancellationToken = default);
}

public class NyphosRiskEngine : INyphosRiskEngine
{
    private readonly IEventStore _eventStore;
    private const int RecentDaysWindow = 30;
    private const int TrendDaysWindow = 7;

    public NyphosRiskEngine(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task<NyphosState> ComputeStateAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        // Get recent events for this device
        var cutoff = DateTime.UtcNow.AddDays(-RecentDaysWindow);
        var deviceEvents = await _eventStore.GetEventsByDeviceAsync(deviceId, cancellationToken);
        var recentEvents = deviceEvents
            .Where(e => e.Module == "nyphos" && e.OccurredUtc >= cutoff)
            .OrderByDescending(e => e.OccurredUtc)
            .ToList();

        // Extract sleep logs
        var sleepLogs = recentEvents
            .OfType<SleepLoggedEvent>()
            .ToList();

        // Extract mood logs
        var moodLogs = recentEvents
            .OfType<MoodLoggedEvent>()
            .ToList();

        // Compute metrics with hysteresis and trend awareness
        int sleepIntegrity = ComputeSleepIntegrity(sleepLogs);
        int moodRisk = ComputeMoodRisk(moodLogs);
        int overloadIndex = ComputeOverloadIndex(sleepIntegrity, moodRisk, sleepLogs, moodLogs);

        // Determine risk level with thresholds
        string riskLevel = DetermineRiskLevel(sleepIntegrity, moodRisk, overloadIndex);

        // Extract top factors
        var topFactors = ExtractTopFactors(sleepLogs, moodLogs, riskLevel);

        // Determine recommended actions
        var recommendedActions = DetermineActions(riskLevel, topFactors);

        return new NyphosState
        {
            State = riskLevel,
            SleepIntegrity = sleepIntegrity,
            MoodRisk = moodRisk,
            OverloadIndex = overloadIndex,
            TopFactors = topFactors.ToArray(),
            RecommendedActions = recommendedActions.ToArray(),
            LastUpdateUtc = DateTime.UtcNow
        };
    }

    public async Task LogSleepAsync(string deviceId, DateTime sleepStart, DateTime sleepEnd, int? qualityScore, string[]? interruptions, CancellationToken cancellationToken = default)
    {
        var @event = new SleepLoggedEvent
        {
            Module = "nyphos",
            EventType = nameof(SleepLoggedEvent),
            DeviceId = deviceId,
            SleepStart = sleepStart,
            SleepEnd = sleepEnd,
            QualityScore = qualityScore,
            Interruptions = interruptions
        };

        await _eventStore.AppendAsync(@event, cancellationToken);
    }

    public async Task LogMoodAsync(string deviceId, int? energyLevel, int? moodScore, string? moodLabel, string? notes, CancellationToken cancellationToken = default)
    {
        var @event = new MoodLoggedEvent
        {
            Module = "nyphos",
            EventType = nameof(MoodLoggedEvent),
            DeviceId = deviceId,
            EnergyLevel = energyLevel,
            MoodScore = moodScore,
            MoodLabel = moodLabel,
            Notes = notes
        };

        await _eventStore.AppendAsync(@event, cancellationToken);
    }

    // ========== SLEEP INTEGRITY CALCULATION ==========
    private int ComputeSleepIntegrity(List<SleepLoggedEvent> sleepLogs)
    {
        if (!sleepLogs.Any())
            return 100; // No data = assume good

        // Split into recent (7 days) and historical (full window) for trend awareness
        var trendCutoff = DateTime.UtcNow.AddDays(-TrendDaysWindow);
        var recentSleep = sleepLogs.Where(s => s.OccurredUtc >= trendCutoff).ToList();
        var usableLogs = recentSleep.Any() ? recentSleep : sleepLogs.Take(7).ToList();

        // Quality score (1-10 scale, higher is better)
        var qualityScores = usableLogs
            .Where(s => s.QualityScore.HasValue)
            .Select(s => s.QualityScore.GetValueOrDefault(5))
            .ToList();

        if (!qualityScores.Any())
            return 100;

        var avgQuality = qualityScores.Average();
        
        // Duration check: reasonable sleep is 6-9 hours
        var avgDuration = usableLogs
            .Select(s => (s.SleepEnd - s.SleepStart).TotalHours)
            .DefaultIfEmpty(7.5) // Default to good duration if no data
            .Average();

        int durationScore = avgDuration switch
        {
            < 5 => 30,   // Very short sleep
            < 6 => 50,   // Short
            < 7 => 70,   // A bit short
            <= 9 => 95,  // Ideal
            < 10 => 80,  // A bit long
            _ => 60      // Very long
        };

        // Interruptions penalty
        int interruptionPenalty = usableLogs
            .Count(s => s.Interruptions?.Any() == true) * 5;

        int integrity = (int)(avgQuality * 10 + durationScore) / 2 - interruptionPenalty;
        return Math.Max(0, Math.Min(100, integrity));
    }

    // ========== MOOD RISK CALCULATION ==========
    private int ComputeMoodRisk(List<MoodLoggedEvent> moodLogs)
    {
        if (!moodLogs.Any())
            return 0; // No data = no signal

        var trendCutoff = DateTime.UtcNow.AddDays(-TrendDaysWindow);
        var recentMood = moodLogs.Where(m => m.OccurredUtc >= trendCutoff).ToList();
        var usableLogs = recentMood.Any() ? recentMood : moodLogs.Take(7).ToList();

        // Mood score deviation (1-10, 5.5 is neutral)
        var moodScores = usableLogs
            .Where(m => m.MoodScore.HasValue)
            .Select(m => m.MoodScore.GetValueOrDefault(5))
            .ToList();

        if (!moodScores.Any())
            return 0;

        var avgMood = moodScores.Average();
        var deviation = Math.Abs(avgMood - 5.5);

        // Energy extremes increase risk (both very high and very low)
        var energyLevels = usableLogs
            .Where(m => m.EnergyLevel.HasValue)
            .Select(m => Math.Abs(m.EnergyLevel.GetValueOrDefault(5)))
            .ToList();

        int energyRisk = energyLevels.Any() 
            ? (int)(energyLevels.Average() * 12) 
            : 0;

        // Label-based risk detection
        var riskLabels = usableLogs
            .Where(m => !string.IsNullOrEmpty(m.MoodLabel))
            .Select(m => m.MoodLabel ?? string.Empty)
            .ToList();

        int labelRisk = 0;
        if (riskLabels.Contains("manic") || riskLabels.Contains("psychotic")) labelRisk = 80;
        else if (riskLabels.Contains("elevated") || riskLabels.Contains("irritable")) labelRisk = 50;
        else if (riskLabels.Contains("depressed") || riskLabels.Contains("suicidal")) labelRisk = 70;
        else if (riskLabels.Contains("anxious")) labelRisk = 40;

        int moodRisk = (int)((deviation * 15) + energyRisk + labelRisk) / 3;
        return Math.Max(0, Math.Min(100, moodRisk));
    }

    // ========== OVERLOAD INDEX (combination metric) ==========
    private int ComputeOverloadIndex(int sleepIntegrity, int moodRisk, List<SleepLoggedEvent> sleepLogs, List<MoodLoggedEvent> moodLogs)
    {
        // Base overload = poor sleep + mood risk
        int baseOverload = (100 - sleepIntegrity + moodRisk) / 2;

        // Frequency boost: too many mood swings = overload signal
        var trendCutoff = DateTime.UtcNow.AddDays(-TrendDaysWindow);
        var recentMood = moodLogs.Where(m => m.OccurredUtc >= trendCutoff).ToList();

        int frequencyRisk = recentMood.Count switch
        {
            > 20 => 20,  // Obsessive tracking = anxiety signal
            > 10 => 15,
            > 5 => 5,
            _ => 0
        };

        int overload = baseOverload + frequencyRisk;
        return Math.Max(0, Math.Min(100, overload));
    }

    // ========== RISK LEVEL DETERMINATION ==========
    private string DetermineRiskLevel(int sleepIntegrity, int moodRisk, int overloadIndex)
    {
        // Red: Critical risk
        if (sleepIntegrity < 40 || moodRisk > 75 || overloadIndex > 80)
            return "Red";

        // Orange: Elevated risk
        if (sleepIntegrity < 55 || moodRisk > 55 || overloadIndex > 60)
            return "Orange";

        // Yellow: Warning signs
        if (sleepIntegrity < 75 || moodRisk > 35 || overloadIndex > 40)
            return "Yellow";

        // Green: Stable
        return "Green";
    }

    // ========== FACTOR EXTRACTION ==========
    private List<string> ExtractTopFactors(List<SleepLoggedEvent> sleepLogs, List<MoodLoggedEvent> moodLogs, string riskLevel)
    {
        var factors = new List<string>();

        // Sleep factors
        if (sleepLogs.Any())
        {
            var avgQuality = sleepLogs
                .Where(s => s.QualityScore.HasValue)
                .Select(s => s.QualityScore.GetValueOrDefault(5))
                .DefaultIfEmpty(5)
                .Average();

            if (avgQuality < 4)
                factors.Add("poor_sleep_quality");

            var avgDuration = sleepLogs
                .Select(s => (s.SleepEnd - s.SleepStart).TotalHours)
                .Average();

            if (avgDuration < 6)
                factors.Add("insufficient_sleep");

            if (sleepLogs.Any(s => s.Interruptions?.Any() == true))
                factors.Add("sleep_disruptions");
        }

        // Mood factors
        if (moodLogs.Any())
        {
            var avgEnergy = moodLogs
                .Where(m => m.EnergyLevel.HasValue)
                .Select(m => m.EnergyLevel.GetValueOrDefault(0))
                .DefaultIfEmpty(0)
                .Average();

            if (avgEnergy > 3)
                factors.Add("elevated_energy");
            else if (avgEnergy < -2)
                factors.Add("depressed_mood");

            if (moodLogs.Any(m => m.MoodLabel?.Contains("irritable") == true))
                factors.Add("irritability");

            if (moodLogs.Any(m => m.MoodLabel?.Contains("anxious") == true))
                factors.Add("anxiety");
        }

        return factors.Take(4).ToList();
    }

    // ========== RECOMMENDED ACTIONS ==========
    private List<string> DetermineActions(string riskLevel, List<string> topFactors)
    {
        var actions = new List<string>();

        // Risk-based actions
        switch (riskLevel)
        {
            case "Red":
                actions.Add("review_crisis_plan");
                actions.Add("consider_professional_support");
                break;
            case "Orange":
                actions.Add("activate_downshift_protocol");
                actions.Add("contact_support_person");
                break;
            case "Yellow":
                actions.Add("review_sleep_schedule");
                actions.Add("reduce_commitments");
                break;
        }

        // Factor-based actions
        foreach (var factor in topFactors)
        {
            switch (factor)
            {
                case "poor_sleep_quality":
                    actions.Add("sleep_hygiene_check");
                    break;
                case "insufficient_sleep":
                    actions.Add("increase_sleep_time");
                    break;
                case "sleep_disruptions":
                    actions.Add("identify_interruption_source");
                    break;
                case "elevated_energy":
                    actions.Add("schedule_physical_activity");
                    break;
                case "depressed_mood":
                    actions.Add("do_one_joyful_activity");
                    break;
                case "irritability":
                    actions.Add("take_calming_break");
                    break;
                case "anxiety":
                    actions.Add("practice_grounding");
                    break;
            }
        }

        // Always suggest data review
        if (!actions.Any())
            actions.Add("continue_monitoring");

        return actions.Distinct().Take(5).ToList();
    }
}
