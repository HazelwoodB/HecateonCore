namespace Lullaby.Models;

/// <summary>
/// Downshift Protocol - structured intervention checklist for Orange/Red states
/// Designed to slow momentum and restore stability
/// </summary>
public class DownshiftProtocol
{
    public Guid ProtocolId { get; set; } = Guid.NewGuid();
    public DateTime ActivatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public NyphosRiskState TriggeringState { get; set; }
    
    // Core checklist items
    public List<DownshiftItem> ChecklistItems { get; set; } = new();
    
    // Decision delays (48-72 hour holds)
    public List<DelayedDecision> DelayedDecisions { get; set; } = new();
    
    // Feedback loop
    public string? UserFeedback { get; set; }
    public bool WasHelpful { get; set; }
    public DateTime? FeedbackRecordedAtUtc { get; set; }
}

/// <summary>
/// Individual downshift checklist item
/// </summary>
public class DownshiftItem
{
    public Guid ItemId { get; set; } = Guid.NewGuid();
    public string Category { get; set; } = string.Empty; // "Sleep", "Stimulation", "Routine", etc.
    public string Description { get; set; } = string.Empty;
    public DownshiftItemType Type { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? CompletionNote { get; set; }
    public int Priority { get; set; } // 1 = highest
}

public enum DownshiftItemType
{
    ProtectSleep,           // Ensure sleep hygiene
    ReduceStimulation,      // Lower caffeine, screens, etc.
    SimplifySchedule,       // Cancel non-essential commitments
    HydrationCheck,         // Drink water reminder
    MedicationCheck,        // Verify medication taken
    ContactSupport,         // Optional: reach out to trusted person
    DelayDecision,          // Hold major purchases/plans
    LimitSocialMedia,       // Reduce digital stimulation
    GroundingExercise       // Breathing, mindfulness, walk
}

/// <summary>
/// Delayed decision tracking (48-72 hour holds)
/// </summary>
public class DelayedDecision
{
    public Guid DecisionId { get; set; } = Guid.NewGuid();
    public string Description { get; set; } = string.Empty;
    public DateTime LoggedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ReviewAfterUtc { get; set; } // When to reconsider
    public bool WasReviewed { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewOutcome { get; set; } // "proceeded", "cancelled", "delayed_again"
}

/// <summary>
/// Crisis plan configuration (user-defined, consent-based)
/// </summary>
public class CrisisPlan
{
    public Guid PlanId { get; set; } = Guid.NewGuid();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdatedAtUtc { get; set; } = DateTime.UtcNow;
    
    // Consent settings
    public bool AutoActivateOnRed { get; set; } = false;
    public bool AllowEmergencyContact { get; set; } = false;
    
    // Support contacts (opt-in, user-configured)
    public List<SupportContact> SupportContacts { get; set; } = new();
    
    // Clinician information
    public string? ClinicianName { get; set; }
    public string? ClinicianPhone { get; set; }
    public string? ClinicianScript { get; set; } // Pre-written message template
    
    // Crisis resources
    public List<CrisisResource> CrisisResources { get; set; } = new()
    {
        new CrisisResource { Name = "988 Suicide & Crisis Lifeline", Phone = "988", Type = "Hotline" },
        new CrisisResource { Name = "911 Emergency Services", Phone = "911", Type = "Emergency" },
        new CrisisResource { Name = "Crisis Text Line", Phone = "741741", Type = "Text", Instructions = "Text HELLO" }
    };
    
    // Safety plan steps
    public List<SafetyStep> SafetySteps { get; set; } = new();
}

/// <summary>
/// Support contact (trusted person, opt-in only)
/// </summary>
public class SupportContact
{
    public Guid ContactId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty; // "Friend", "Family", "Therapist", etc.
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool CanAutoContact { get; set; } = false; // Explicit consent required
    public string? PreferredContactMethod { get; set; }
    public string? MessageTemplate { get; set; }
}

/// <summary>
/// Crisis resource (hotlines, services)
/// </summary>
public class CrisisResource
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "Hotline", "Emergency", "Text"
    public string? Instructions { get; set; }
    public string? Website { get; set; }
}

/// <summary>
/// Safety plan step (user-defined)
/// </summary>
public class SafetyStep
{
    public Guid StepId { get; set; } = Guid.NewGuid();
    public int Order { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Details { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

/// <summary>
/// Consent record for tracking user permissions
/// </summary>
public class ConsentRecord
{
    public Guid ConsentId { get; set; } = Guid.NewGuid();
    public DateTime GrantedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAtUtc { get; set; }
    public bool IsActive => RevokedAtUtc == null;
    
    public ConsentType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Scope { get; set; } // "emergency_contact", "clinician_report", "data_sharing", etc.
}

public enum ConsentType
{
    EmergencyContact,
    ClinicianSharing,
    DataExport,
    AutomatedInterventions,
    ThirdPartyIntegration
}
