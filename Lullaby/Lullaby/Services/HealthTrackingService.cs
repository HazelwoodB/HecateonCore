using System.Collections.Concurrent;
using System.Text.Json;
using Hecateon.Models;

namespace Hecateon.Services;

/// <summary>
/// Service for managing health events (sleep, mood, routines)
/// Uses append-only event log as source of truth
/// Provides trend analysis with explainable scoring
/// </summary>
public class HealthTrackingService
{
    private const string HealthEventRecordedType = "health.event.recorded";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly EventLogService _eventLog;
    private readonly string _healthEventsPath;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ConcurrentDictionary<Guid, byte> _seenEventIds = new();

    public HealthTrackingService(EventLogService eventLog, IWebHostEnvironment environment)
    {
        _eventLog = eventLog;
        var dataDirectory = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDirectory);
        _healthEventsPath = Path.Combine(dataDirectory, "health-events.jsonl");
        
        HydrateIndexesFromDisk();
    }

    private void HydrateIndexesFromDisk()
    {
        if (!File.Exists(_healthEventsPath))
        {
            return;
        }

        try
        {
            var lines = File.ReadAllLines(_healthEventsPath);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var envelope = JsonSerializer.Deserialize<EventEnvelope>(line, JsonOptions);
                if (envelope is null)
                {
                    continue;
                }

                var healthEvent = JsonSerializer.Deserialize<HealthEvent>(envelope.PayloadJson, JsonOptions);
                if (healthEvent is not null)
                {
                    _seenEventIds.TryAdd(healthEvent.Id, 0);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HealthTracking] Error hydrating indexes: {ex.Message}");
        }
    }

    public async Task<bool> RecordHealthEventAsync(HealthEvent healthEvent, string? deviceId, CancellationToken cancellationToken = default)
    {
        if (!_seenEventIds.TryAdd(healthEvent.Id, 0))
        {
            return false; // Duplicate event
        }

        var envelope = new EventEnvelope
        {
            EventId = Guid.NewGuid(),
            EventType = HealthEventRecordedType,
            EventVersion = 1,
            EntityId = healthEvent.Id.ToString("N"),
            DeviceId = string.IsNullOrWhiteSpace(deviceId) ? "unknown-device" : deviceId,
            OccurredAtUtc = DateTime.UtcNow,
            PayloadJson = JsonSerializer.Serialize(healthEvent, JsonOptions)
        };

        var line = JsonSerializer.Serialize(envelope, JsonOptions) + Environment.NewLine;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            await File.AppendAllTextAsync(_healthEventsPath, line, cancellationToken);
            return true;
        }
        catch
        {
            _seenEventIds.TryRemove(healthEvent.Id, out _);
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<HealthEvent>> GetHealthHistoryAsync(int daysBack = 30, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(_healthEventsPath))
            {
                return [];
            }

            var lines = await File.ReadAllLinesAsync(_healthEventsPath, cancellationToken);
            var events = new List<HealthEvent>();
            var cutoffDate = DateTime.UtcNow.AddDays(-daysBack);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var envelope = JsonSerializer.Deserialize<EventEnvelope>(line, JsonOptions);
                if (envelope is null || envelope.EventType != HealthEventRecordedType)
                {
                    continue;
                }

                var healthEvent = JsonSerializer.Deserialize<HealthEvent>(envelope.PayloadJson, JsonOptions);
                if (healthEvent is not null && healthEvent.RecordedAtUtc >= cutoffDate)
                {
                    events.Add(healthEvent);
                }
            }

            return events.OrderByDescending(e => e.RecordedAtUtc).ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<HealthTrendScore> CalculateTrendScoreAsync(int daysBack = 7, CancellationToken cancellationToken = default)
    {
        var events = await GetHealthHistoryAsync(daysBack, cancellationToken);
        
        var score = new HealthTrendScore
        {
            CalculatedAtUtc = DateTime.UtcNow,
            DaysAnalyzed = daysBack
        };

        // Sleep analysis
        var sleepEvents = events.Where(e => e.EventType == HealthEventType.Sleep && e.SleepStartUtc.HasValue && e.SleepEndUtc.HasValue).ToList();
        if (sleepEvents.Count > 0)
        {
            var avgSleepHours = sleepEvents.Average(e => (e.SleepEndUtc!.Value - e.SleepStartUtc!.Value).TotalHours);
            var avgSleepQuality = sleepEvents.Where(e => e.SleepQuality.HasValue).Average(e => e.SleepQuality!.Value);
            
            score.SleepScore = CalculateSleepScore(avgSleepHours, avgSleepQuality, sleepEvents.Count, daysBack);
            score.SleepExplanation = $"Avg {avgSleepHours:F1}h/night, quality {avgSleepQuality:F1}/5, {sleepEvents.Count}/{daysBack} nights tracked";
        }

        // Mood analysis
        var moodEvents = events.Where(e => e.EventType == HealthEventType.Mood && e.MoodScore.HasValue).ToList();
        if (moodEvents.Count > 0)
        {
            var avgMood = moodEvents.Average(e => e.MoodScore!.Value);
            var moodVariance = CalculateVariance(moodEvents.Select(e => (double)e.MoodScore!.Value));
            
            score.MoodScore = CalculateMoodScore(avgMood, moodVariance, moodEvents.Count, daysBack);
            score.MoodExplanation = $"Avg mood {avgMood:F1}/2, variance {moodVariance:F2}, {moodEvents.Count} entries";
        }

        // Routine adherence
        var routineEvents = events.Where(e => e.EventType == HealthEventType.Routine).ToList();
        if (routineEvents.Count > 0)
        {
            var completionRate = routineEvents.Where(e => e.RoutineCompleted == true).Count() / (double)routineEvents.Count;
            score.RoutineScore = CalculateRoutineScore(completionRate, routineEvents.Count, daysBack);
            score.RoutineExplanation = $"{completionRate:P0} completion rate, {routineEvents.Count} tracked";
        }

        // Overall risk calculation
        score.OverallRiskLevel = CalculateOverallRisk(score);
        score.RiskExplanation = BuildRiskExplanation(score);

        return score;
    }

    private double CalculateSleepScore(double avgHours, double avgQuality, int recordCount, int daysBack)
    {
        // Ideal: 7-9 hours, quality 4-5
        var hoursScore = avgHours switch
        {
            >= 7 and <= 9 => 1.0,
            >= 6 and < 7 => 0.7,
            >= 5 and < 6 => 0.4,
            _ => 0.2
        };

        var qualityScore = avgQuality / 5.0;
        var consistencyScore = recordCount / (double)daysBack;

        return (hoursScore * 0.5 + qualityScore * 0.3 + consistencyScore * 0.2);
    }

    private double CalculateMoodScore(double avgMood, double variance, int recordCount, int daysBack)
    {
        // Ideal: positive mood (>0), low variance (<1)
        var avgScore = (avgMood + 2) / 4.0; // Normalize -2 to +2 -> 0 to 1
        var stabilityScore = Math.Max(0, 1.0 - (variance / 2.0));
        var consistencyScore = Math.Min(1.0, recordCount / (double)daysBack);

        return (avgScore * 0.5 + stabilityScore * 0.3 + consistencyScore * 0.2);
    }

    private double CalculateRoutineScore(double completionRate, int recordCount, int daysBack)
    {
        var completionScore = completionRate;
        var consistencyScore = Math.Min(1.0, recordCount / (double)daysBack);

        return (completionScore * 0.7 + consistencyScore * 0.3);
    }

    private RiskLevel CalculateOverallRisk(HealthTrendScore score)
    {
        var riskFactors = 0;
        var criticalFactors = 0;

        if (score.SleepScore < 0.4) criticalFactors++;
        else if (score.SleepScore < 0.6) riskFactors++;

        if (score.MoodScore < 0.3) criticalFactors++;
        else if (score.MoodScore < 0.5) riskFactors++;

        if (score.RoutineScore < 0.4) riskFactors++;

        return (criticalFactors, riskFactors) switch
        {
            (>= 2, _) => RiskLevel.Critical,
            (1, _) => RiskLevel.High,
            (0, >= 2) => RiskLevel.Elevated,
            (0, 1) => RiskLevel.Moderate,
            _ => RiskLevel.Normal
        };
    }

    private string BuildRiskExplanation(HealthTrendScore score)
    {
        var factors = new List<string>();

        if (score.SleepScore < 0.6) factors.Add($"Sleep disruption (score: {score.SleepScore:F2})");
        if (score.MoodScore < 0.5) factors.Add($"Mood instability (score: {score.MoodScore:F2})");
        if (score.RoutineScore < 0.6) factors.Add($"Routine inconsistency (score: {score.RoutineScore:F2})");

        return factors.Count > 0 
            ? $"Risk factors: {string.Join(", ", factors)}" 
            : "All metrics within healthy range";
    }

    private double CalculateVariance(IEnumerable<double> values)
    {
        var valuesList = values.ToList();
        if (valuesList.Count == 0) return 0;
        
        var mean = valuesList.Average();
        return valuesList.Average(v => Math.Pow(v - mean, 2));
    }
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
