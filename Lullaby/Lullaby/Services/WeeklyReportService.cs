using System.Text;
using System.Text.Json;
using Lullaby.Models;

namespace Lullaby.Services;

/// <summary>
/// Service for generating weekly wellness reports for clinicians
/// Provides privacy-preserving summaries with explainable trends
/// </summary>
public class WeeklyReportService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    
    private readonly HealthTrackingService _healthTracking;
    private readonly EventLogService _eventLog;
    private readonly string _reportsPath;

    public WeeklyReportService(HealthTrackingService healthTracking, EventLogService eventLog, IWebHostEnvironment environment)
    {
        _healthTracking = healthTracking;
        _eventLog = eventLog;
        
        var dataDirectory = Path.Combine(environment.ContentRootPath, "App_Data", "Reports");
        Directory.CreateDirectory(dataDirectory);
        _reportsPath = dataDirectory;
    }

    public async Task<WeeklyReport> GenerateWeeklyReportAsync(DateTime? weekEndingDate = null, CancellationToken cancellationToken = default)
    {
        var endDate = weekEndingDate ?? DateTime.UtcNow;
        var startDate = endDate.AddDays(-7);

        var report = new WeeklyReport
        {
            ReportId = Guid.NewGuid(),
            GeneratedAtUtc = DateTime.UtcNow,
            WeekStartUtc = startDate,
            WeekEndUtc = endDate
        };

        // Get health data
        var healthEvents = await _healthTracking.GetHealthHistoryAsync(7, cancellationToken);
        var trendScore = await _healthTracking.CalculateTrendScoreAsync(7, cancellationToken);

        // Sleep summary
        var sleepEvents = healthEvents.Where(e => e.EventType == HealthEventType.Sleep).ToList();
        report.SleepSummary = new SleepSummary
        {
            NightsTracked = sleepEvents.Count,
            AverageHours = sleepEvents.Any() ? sleepEvents.Average(e => (e.SleepEndUtc!.Value - e.SleepStartUtc!.Value).TotalHours) : 0,
            AverageQuality = sleepEvents.Any(e => e.SleepQuality.HasValue) ? sleepEvents.Where(e => e.SleepQuality.HasValue).Average(e => e.SleepQuality!.Value) : 0,
            QualityTrend = CalculateTrend(sleepEvents.Where(e => e.SleepQuality.HasValue).Select(e => (double)e.SleepQuality!.Value).ToList())
        };

        // Mood summary
        var moodEvents = healthEvents.Where(e => e.EventType == HealthEventType.Mood && e.MoodScore.HasValue).ToList();
        report.MoodSummary = new MoodSummary
        {
            EntriesCount = moodEvents.Count,
            AverageMood = moodEvents.Any() ? moodEvents.Average(e => e.MoodScore!.Value) : 0,
            MoodVariance = CalculateVariance(moodEvents.Select(e => (double)e.MoodScore!.Value)),
            LowMoodDays = moodEvents.Count(e => e.MoodScore < 0),
            HighMoodDays = moodEvents.Count(e => e.MoodScore > 0),
            CommonLabels = moodEvents.Where(e => !string.IsNullOrWhiteSpace(e.MoodLabel)).GroupBy(e => e.MoodLabel).OrderByDescending(g => g.Count()).Take(5).Select(g => g.Key!).ToArray()
        };

        // Routine summary
        var routineEvents = healthEvents.Where(e => e.EventType == HealthEventType.Routine).ToList();
        report.RoutineSummary = new RoutineSummary
        {
            TotalRoutines = routineEvents.Count,
            CompletedRoutines = routineEvents.Count(e => e.RoutineCompleted == true),
            CompletionRate = routineEvents.Count > 0 ? routineEvents.Count(e => e.RoutineCompleted == true) / (double)routineEvents.Count : 0,
            MostConsistentRoutines = routineEvents.Where(e => e.RoutineCompleted == true).GroupBy(e => e.RoutineName).OrderByDescending(g => g.Count()).Take(5).Select(g => g.Key!).ToArray()
        };

        // Risk assessment
        report.RiskAssessment = new RiskAssessment
        {
            OverallRiskLevel = trendScore.OverallRiskLevel,
            SleepScore = trendScore.SleepScore,
            MoodScore = trendScore.MoodScore,
            RoutineScore = trendScore.RoutineScore,
            Explanation = trendScore.RiskExplanation,
            RecommendedActions = GenerateRecommendations(trendScore)
        };

        // Activity summary
        var activityEvents = healthEvents.Where(e => e.EventType == HealthEventType.Activity).ToList();
        report.ActivitySummary = new ActivitySummary
        {
            TotalActivities = activityEvents.Count,
            TotalMinutes = activityEvents.Sum(e => e.DurationMinutes ?? 0),
            ActivityTypes = activityEvents.Where(e => !string.IsNullOrWhiteSpace(e.ActivityType)).GroupBy(e => e.ActivityType).ToDictionary(g => g.Key!, g => g.Sum(e => e.DurationMinutes ?? 0))
        };

        // Medication adherence
        var medEvents = healthEvents.Where(e => e.EventType == HealthEventType.Medication).ToList();
        report.MedicationAdherence = new MedicationAdherence
        {
            TotalDoses = medEvents.Count,
            DosesTaken = medEvents.Count(e => e.MedicationTaken == true),
            AdherenceRate = medEvents.Count > 0 ? medEvents.Count(e => e.MedicationTaken == true) / (double)medEvents.Count : 0,
            MissedDoses = medEvents.Where(e => e.MedicationTaken == false).Select(e => new MissedDose
            {
                MedicationName = e.MedicationName ?? "Unknown",
                ScheduledTime = e.MedicationScheduledTime,
                Date = e.RecordedAtUtc
            }).ToArray()
        };

        return report;
    }

    public async Task<string> ExportReportAsync(WeeklyReport report, ExportFormat format, CancellationToken cancellationToken = default)
    {
        var fileName = $"wellness-report-{report.WeekEndUtc:yyyy-MM-dd}.{format.ToString().ToLowerInvariant()}";
        var filePath = Path.Combine(_reportsPath, fileName);

        switch (format)
        {
            case ExportFormat.Json:
                await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(report, JsonOptions), cancellationToken);
                break;

            case ExportFormat.Markdown:
                var markdown = GenerateMarkdownReport(report);
                await File.WriteAllTextAsync(filePath, markdown, cancellationToken);
                break;

            case ExportFormat.Csv:
                var csv = GenerateCsvReport(report);
                await File.WriteAllTextAsync(filePath, csv, cancellationToken);
                break;
        }

        return filePath;
    }

    private string GenerateMarkdownReport(WeeklyReport report)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine($"# Weekly Wellness Report");
        sb.AppendLine($"**Period:** {report.WeekStartUtc:yyyy-MM-dd} to {report.WeekEndUtc:yyyy-MM-dd}");
        sb.AppendLine($"**Generated:** {report.GeneratedAtUtc:yyyy-MM-dd HH:mm} UTC");
        sb.AppendLine();

        // Risk Assessment
        sb.AppendLine("## 🎯 Risk Assessment");
        sb.AppendLine($"- **Overall Status:** {report.RiskAssessment.OverallRiskLevel}");
        sb.AppendLine($"- **Explanation:** {report.RiskAssessment.Explanation}");
        sb.AppendLine($"- **Sleep Score:** {report.RiskAssessment.SleepScore:P0}");
        sb.AppendLine($"- **Mood Score:** {report.RiskAssessment.MoodScore:P0}");
        sb.AppendLine($"- **Routine Score:** {report.RiskAssessment.RoutineScore:P0}");
        sb.AppendLine();

        if (report.RiskAssessment.RecommendedActions.Length > 0)
        {
            sb.AppendLine("### Recommended Actions");
            foreach (var action in report.RiskAssessment.RecommendedActions)
            {
                sb.AppendLine($"- {action}");
            }
            sb.AppendLine();
        }

        // Sleep
        sb.AppendLine("## 😴 Sleep Summary");
        sb.AppendLine($"- **Nights Tracked:** {report.SleepSummary.NightsTracked}/7");
        sb.AppendLine($"- **Average Hours:** {report.SleepSummary.AverageHours:F1}h");
        sb.AppendLine($"- **Average Quality:** {report.SleepSummary.AverageQuality:F1}/5");
        sb.AppendLine($"- **Trend:** {report.SleepSummary.QualityTrend}");
        sb.AppendLine();

        // Mood
        sb.AppendLine("## 💚 Mood Summary");
        sb.AppendLine($"- **Entries:** {report.MoodSummary.EntriesCount}");
        sb.AppendLine($"- **Average Mood:** {report.MoodSummary.AverageMood:F1} (scale: -2 to +2)");
        sb.AppendLine($"- **Low Mood Days:** {report.MoodSummary.LowMoodDays}");
        sb.AppendLine($"- **High Mood Days:** {report.MoodSummary.HighMoodDays}");
        if (report.MoodSummary.CommonLabels.Length > 0)
        {
            sb.AppendLine($"- **Common Feelings:** {string.Join(", ", report.MoodSummary.CommonLabels)}");
        }
        sb.AppendLine();

        // Routines
        sb.AppendLine("## ✅ Routine Summary");
        sb.AppendLine($"- **Completion Rate:** {report.RoutineSummary.CompletionRate:P0}");
        sb.AppendLine($"- **Completed:** {report.RoutineSummary.CompletedRoutines}/{report.RoutineSummary.TotalRoutines}");
        if (report.RoutineSummary.MostConsistentRoutines.Length > 0)
        {
            sb.AppendLine($"- **Most Consistent:** {string.Join(", ", report.RoutineSummary.MostConsistentRoutines)}");
        }
        sb.AppendLine();

        // Medication
        if (report.MedicationAdherence.TotalDoses > 0)
        {
            sb.AppendLine("## 💊 Medication Adherence");
            sb.AppendLine($"- **Adherence Rate:** {report.MedicationAdherence.AdherenceRate:P0}");
            sb.AppendLine($"- **Doses Taken:** {report.MedicationAdherence.DosesTaken}/{report.MedicationAdherence.TotalDoses}");
            if (report.MedicationAdherence.MissedDoses.Length > 0)
            {
                sb.AppendLine($"- **Missed Doses:** {report.MedicationAdherence.MissedDoses.Length}");
            }
            sb.AppendLine();
        }

        // Activity
        if (report.ActivitySummary.TotalActivities > 0)
        {
            sb.AppendLine("## 🏃 Activity Summary");
            sb.AppendLine($"- **Total Activities:** {report.ActivitySummary.TotalActivities}");
            sb.AppendLine($"- **Total Time:** {report.ActivitySummary.TotalMinutes} minutes");
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine("*This report is generated for clinical review and patient care coordination.*");

        return sb.ToString();
    }

    private string GenerateCsvReport(WeeklyReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Metric,Value");
        sb.AppendLine($"Report Period,{report.WeekStartUtc:yyyy-MM-dd} to {report.WeekEndUtc:yyyy-MM-dd}");
        sb.AppendLine($"Risk Level,{report.RiskAssessment.OverallRiskLevel}");
        sb.AppendLine($"Sleep Score,{report.RiskAssessment.SleepScore:F2}");
        sb.AppendLine($"Mood Score,{report.RiskAssessment.MoodScore:F2}");
        sb.AppendLine($"Routine Score,{report.RiskAssessment.RoutineScore:F2}");
        sb.AppendLine($"Nights Tracked,{report.SleepSummary.NightsTracked}");
        sb.AppendLine($"Avg Sleep Hours,{report.SleepSummary.AverageHours:F1}");
        sb.AppendLine($"Avg Sleep Quality,{report.SleepSummary.AverageQuality:F1}");
        sb.AppendLine($"Mood Entries,{report.MoodSummary.EntriesCount}");
        sb.AppendLine($"Avg Mood,{report.MoodSummary.AverageMood:F1}");
        sb.AppendLine($"Routine Completion Rate,{report.RoutineSummary.CompletionRate:F2}");
        sb.AppendLine($"Medication Adherence,{report.MedicationAdherence.AdherenceRate:F2}");
        return sb.ToString();
    }

    private string[] GenerateRecommendations(HealthTrendScore score)
    {
        var recommendations = new List<string>();

        if (score.SleepScore < 0.6)
        {
            recommendations.Add("Consider sleep hygiene interventions");
        }

        if (score.MoodScore < 0.5)
        {
            recommendations.Add("Recommend mental health check-in");
        }

        if (score.RoutineScore < 0.6)
        {
            recommendations.Add("Review and simplify routine structure");
        }

        if (score.OverallRiskLevel >= RiskLevel.High)
        {
            recommendations.Add("Schedule urgent follow-up");
        }

        return recommendations.ToArray();
    }

    private string CalculateTrend(List<double> values)
    {
        if (values.Count < 2) return "Insufficient data";
        
        var firstHalf = values.Take(values.Count / 2).Average();
        var secondHalf = values.Skip(values.Count / 2).Average();
        
        var change = secondHalf - firstHalf;
        return change switch
        {
            > 0.5 => "Improving ↑",
            < -0.5 => "Declining ↓",
            _ => "Stable →"
        };
    }

    private double CalculateVariance(IEnumerable<double> values)
    {
        var valuesList = values.ToList();
        if (valuesList.Count == 0) return 0;
        
        var mean = valuesList.Average();
        return valuesList.Average(v => Math.Pow(v - mean, 2));
    }
}

public class WeeklyReport
{
    public Guid ReportId { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
    public DateTime WeekStartUtc { get; set; }
    public DateTime WeekEndUtc { get; set; }
    
    public SleepSummary SleepSummary { get; set; } = new();
    public MoodSummary MoodSummary { get; set; } = new();
    public RoutineSummary RoutineSummary { get; set; } = new();
    public RiskAssessment RiskAssessment { get; set; } = new();
    public ActivitySummary ActivitySummary { get; set; } = new();
    public MedicationAdherence MedicationAdherence { get; set; } = new();
}

public class SleepSummary
{
    public int NightsTracked { get; set; }
    public double AverageHours { get; set; }
    public double AverageQuality { get; set; }
    public string QualityTrend { get; set; } = string.Empty;
}

public class MoodSummary
{
    public int EntriesCount { get; set; }
    public double AverageMood { get; set; }
    public double MoodVariance { get; set; }
    public int LowMoodDays { get; set; }
    public int HighMoodDays { get; set; }
    public string[] CommonLabels { get; set; } = Array.Empty<string>();
}

public class RoutineSummary
{
    public int TotalRoutines { get; set; }
    public int CompletedRoutines { get; set; }
    public double CompletionRate { get; set; }
    public string[] MostConsistentRoutines { get; set; } = Array.Empty<string>();
}

public class RiskAssessment
{
    public RiskLevel OverallRiskLevel { get; set; }
    public double SleepScore { get; set; }
    public double MoodScore { get; set; }
    public double RoutineScore { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public string[] RecommendedActions { get; set; } = Array.Empty<string>();
}

public class ActivitySummary
{
    public int TotalActivities { get; set; }
    public int TotalMinutes { get; set; }
    public Dictionary<string, int> ActivityTypes { get; set; } = new();
}

public class MedicationAdherence
{
    public int TotalDoses { get; set; }
    public int DosesTaken { get; set; }
    public double AdherenceRate { get; set; }
    public MissedDose[] MissedDoses { get; set; } = Array.Empty<MissedDose>();
}

public class MissedDose
{
    public string MedicationName { get; set; } = string.Empty;
    public TimeSpan? ScheduledTime { get; set; }
    public DateTime Date { get; set; }
}

public enum ExportFormat
{
    Json,
    Markdown,
    Csv
}
