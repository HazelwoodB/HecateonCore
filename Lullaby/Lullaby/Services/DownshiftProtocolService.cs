using System.Text.Json;
using Hecateon.Models;

namespace Hecateon.Services;

/// <summary>
/// Downshift Protocol Service - Manages structured intervention checklists
/// Activated during Orange/Red states to slow momentum and restore stability
/// </summary>
public class DownshiftProtocolService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    
    private readonly string _protocolsPath;
    private readonly string _crisisPlanPath;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public DownshiftProtocolService(IWebHostEnvironment environment)
    {
        var dataDirectory = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDirectory);
        _protocolsPath = Path.Combine(dataDirectory, "downshift-protocols.jsonl");
        _crisisPlanPath = Path.Combine(dataDirectory, "crisis-plan.json");
    }

    public async Task<DownshiftProtocol> ActivateProtocolAsync(NyphosRiskState triggeringState, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var protocol = new DownshiftProtocol
            {
                ActivatedAtUtc = DateTime.UtcNow,
                TriggeringState = triggeringState,
                ChecklistItems = GenerateChecklistItems(triggeringState)
            };

            // Persist protocol activation
            var line = JsonSerializer.Serialize(protocol, JsonOptions) + Environment.NewLine;
            await File.AppendAllTextAsync(_protocolsPath, line, cancellationToken);

            return protocol;
        }
        finally
        {
            _gate.Release();
        }
    }

    private List<DownshiftItem> GenerateChecklistItems(NyphosRiskState state)
    {
        var items = new List<DownshiftItem>();

        // Core items (always included)
        items.Add(new DownshiftItem
        {
            Category = "Sleep",
            Description = "Protect sleep tonight - set bedtime alarm for 9pm",
            Type = DownshiftItemType.ProtectSleep,
            Priority = 1
        });

        items.Add(new DownshiftItem
        {
            Category = "Hydration",
            Description = "Drink a full glass of water now",
            Type = DownshiftItemType.HydrationCheck,
            Priority = 2
        });

        items.Add(new DownshiftItem
        {
            Category = "Medication",
            Description = "Verify today's medication was taken",
            Type = DownshiftItemType.MedicationCheck,
            Priority = 2
        });

        items.Add(new DownshiftItem
        {
            Category = "Stimulation",
            Description = "Stop caffeine/stimulants for rest of day",
            Type = DownshiftItemType.ReduceStimulation,
            Priority = 3
        });

        items.Add(new DownshiftItem
        {
            Category = "Schedule",
            Description = "Review schedule - cancel non-essential commitments today/tomorrow",
            Type = DownshiftItemType.SimplifySchedule,
            Priority = 4
        });

        items.Add(new DownshiftItem
        {
            Category = "Digital",
            Description = "Limit social media and news to 15 min total today",
            Type = DownshiftItemType.LimitSocialMedia,
            Priority = 5
        });

        items.Add(new DownshiftItem
        {
            Category = "Grounding",
            Description = "Take a 10-minute walk outside or practice box breathing",
            Type = DownshiftItemType.GroundingExercise,
            Priority = 6
        });

        // State-specific additions
        if (state == NyphosRiskState.Orange || state == NyphosRiskState.Red)
        {
            items.Add(new DownshiftItem
            {
                Category = "Decisions",
                Description = "Delay major purchases or commitments for 48-72 hours",
                Type = DownshiftItemType.DelayDecision,
                Priority = 3
            });

            items.Add(new DownshiftItem
            {
                Category = "Support",
                Description = "Consider reaching out to therapist or trusted support person",
                Type = DownshiftItemType.ContactSupport,
                Priority = 7
            });
        }

        if (state == NyphosRiskState.Red)
        {
            items.Insert(0, new DownshiftItem
            {
                Category = "Crisis",
                Description = "Review crisis plan and have 988 number ready",
                Type = DownshiftItemType.ContactSupport,
                Priority = 1
            });
        }

        return items.OrderBy(i => i.Priority).ToList();
    }

    public async Task<bool> CompleteChecklistItemAsync(Guid protocolId, Guid itemId, string? note, CancellationToken cancellationToken = default)
    {
        // In a real implementation, we'd update the protocol in a database
        // For now, we'll log the completion
        Console.WriteLine($"[DownshiftProtocol] Item {itemId} completed in protocol {protocolId}");
        await Task.CompletedTask;
        return true;
    }

    public async Task<bool> RecordDelayedDecisionAsync(Guid protocolId, string description, int hoursToDelay, CancellationToken cancellationToken = default)
    {
        var decision = new DelayedDecision
        {
            Description = description,
            LoggedAtUtc = DateTime.UtcNow,
            ReviewAfterUtc = DateTime.UtcNow.AddHours(hoursToDelay)
        };

        Console.WriteLine($"[DownshiftProtocol] Delayed decision recorded: {description} (review after {hoursToDelay}h)");
        await Task.CompletedTask;
        return true;
    }

    public async Task<bool> RecordFeedbackAsync(Guid protocolId, string feedback, bool wasHelpful, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[DownshiftProtocol] Feedback for {protocolId}: Helpful={wasHelpful}, Note={feedback}");
        await Task.CompletedTask;
        return true;
    }

    // Crisis Plan Management
    public async Task<CrisisPlan> GetCrisisPlanAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(_crisisPlanPath))
            {
                return CreateDefaultCrisisPlan();
            }

            var json = await File.ReadAllTextAsync(_crisisPlanPath, cancellationToken);
            return JsonSerializer.Deserialize<CrisisPlan>(json, JsonOptions) ?? CreateDefaultCrisisPlan();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> SaveCrisisPlanAsync(CrisisPlan plan, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            plan.LastUpdatedAtUtc = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(plan, JsonOptions);
            await File.WriteAllTextAsync(_crisisPlanPath, json, cancellationToken);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    private CrisisPlan CreateDefaultCrisisPlan()
    {
        return new CrisisPlan
        {
            AutoActivateOnRed = false,
            AllowEmergencyContact = false,
            SafetySteps = new List<SafetyStep>
            {
                new() { Order = 1, Description = "Find a safe, quiet place" },
                new() { Order = 2, Description = "Practice grounding: 5 things you see, 4 you touch, 3 you hear, 2 you smell, 1 you taste" },
                new() { Order = 3, Description = "Call 988 if thoughts of self-harm are present" },
                new() { Order = 4, Description = "Reach out to trusted support person" },
                new() { Order = 5, Description = "Follow clinician's emergency instructions" }
            }
        };
    }

    // Consent Management
    public async Task<List<ConsentRecord>> GetActiveConsentsAsync(CancellationToken cancellationToken = default)
    {
        // Placeholder - implement consent storage
        await Task.CompletedTask;
        return new List<ConsentRecord>();
    }

    public async Task<bool> GrantConsentAsync(ConsentType type, string description, string? scope, CancellationToken cancellationToken = default)
    {
        var consent = new ConsentRecord
        {
            Type = type,
            Description = description,
            Scope = scope,
            GrantedAtUtc = DateTime.UtcNow
        };

        Console.WriteLine($"[DownshiftProtocol] Consent granted: {type} - {description}");
        await Task.CompletedTask;
        return true;
    }

    public async Task<bool> RevokeConsentAsync(Guid consentId, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[DownshiftProtocol] Consent revoked: {consentId}");
        await Task.CompletedTask;
        return true;
    }
}
