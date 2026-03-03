namespace Lullaby.Models;

/// <summary>
/// Health event types for sleep, mood, and routine tracking
/// </summary>
public enum HealthEventType
{
    Sleep,
    Mood,
    Routine,
    Medication,
    Activity,
    Note
}

/// <summary>
/// Core health event model for tracking sleep, mood, routines
/// Stored in append-only event log for full history
/// </summary>
public class HealthEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public HealthEventType EventType { get; set; }
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
    public string? DeviceId { get; set; }
    public string? Note { get; set; }
    
    // Sleep-specific
    public DateTime? SleepStartUtc { get; set; }
    public DateTime? SleepEndUtc { get; set; }
    public int? SleepQuality { get; set; } // 1-5 scale
    
    // Mood-specific
    public int? MoodScore { get; set; } // -2 (very low) to +2 (very high)
    public string? MoodLabel { get; set; } // "anxious", "calm", "energized", etc.
    
    // Routine-specific
    public string? RoutineName { get; set; } // "morning_routine", "medication", etc.
    public bool? RoutineCompleted { get; set; }
    
    // Medication-specific
    public string? MedicationName { get; set; }
    public bool? MedicationTaken { get; set; }
    public TimeSpan? MedicationScheduledTime { get; set; }
    
    // Activity-specific
    public string? ActivityType { get; set; } // "exercise", "social", "work", etc.
    public int? DurationMinutes { get; set; }
}

/// <summary>
/// Request to log a health event from client
/// </summary>
public class HealthEventRequest
{
    public HealthEventType EventType { get; set; }
    public DateTime RecordedAtUtc { get; set; }
    public string? Note { get; set; }
    
    // Sleep
    public DateTime? SleepStartUtc { get; set; }
    public DateTime? SleepEndUtc { get; set; }
    public int? SleepQuality { get; set; }
    
    // Mood
    public int? MoodScore { get; set; }
    public string? MoodLabel { get; set; }
    
    // Routine
    public string? RoutineName { get; set; }
    public bool? RoutineCompleted { get; set; }
    
    // Medication
    public string? MedicationName { get; set; }
    public bool? MedicationTaken { get; set; }
    public TimeSpan? MedicationScheduledTime { get; set; }
    
    // Activity
    public string? ActivityType { get; set; }
    public int? DurationMinutes { get; set; }
}
