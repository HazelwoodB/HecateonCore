using System.Text.Json;
using Hecateon.Core.EventStore;
using Hecateon.Events;
using Hecateon.Modules.Nyphos.Models;

namespace Hecateon.Services;

public interface INyphosSignalEmitterService
{
    Task<(long? SignalSeq, long? SafeguardSeq)> EmitFromStateAsync(string userId, string deviceId, NyphosState state, CancellationToken cancellationToken = default);
}

public sealed class NyphosSignalEmitterService(IEventStore eventStore, INyphosPreferenceService preferences) : INyphosSignalEmitterService
{
    public async Task<(long? SignalSeq, long? SafeguardSeq)> EmitFromStateAsync(string userId, string deviceId, NyphosState state, CancellationToken cancellationToken = default)
    {
        var prefs = await preferences.GetOrDefaultAsync(userId, deviceId, cancellationToken);
        var muted = prefs.MutedUntilUtc.HasValue && prefs.MutedUntilUtc.Value > DateTimeOffset.UtcNow;

        var level = MapLevel(state.State);
        var confidence = CalculateConfidence(state);
        var headSeq = await eventStore.GetStreamHeadSeqAsync(EventStream.Nyphos, cancellationToken);
        var recent = await eventStore.GetStreamEventsAsync(EventStream.Nyphos, Math.Max(0, headSeq - 250), 250, cancellationToken);

        var evidenceEventIds = recent
            .Where(e => e.DeviceId == deviceId && e.UserId == userId)
            .Where(e => e.Type is "SleepLogged" or "MoodLogged")
            .OrderByDescending(e => e.Seq)
            .Take(5)
            .Select(e => e.EventId)
            .ToArray();

        var category = state.TopFactors.FirstOrDefault() ?? "general_stability";

        var signalPayload = JsonSerializer.Serialize(new
        {
            level,
            category,
            confidence,
            evidenceEventIds,
            state = new
            {
                state.SleepIntegrity,
                state.MoodRisk,
                state.OverloadIndex
            },
            calmLanguage = true,
            advisoryByDefault = true,
            tone = prefs.Tone,
            muted,
            mutedUntilUtc = prefs.MutedUntilUtc
        });

        var signalEnvelope = new EventEnvelope
        {
            EventId = Guid.NewGuid().ToString(),
            UserId = userId,
            DeviceId = deviceId,
            Stream = EventStream.Nyphos,
            Type = "NyphosSignalDetected",
            Seq = null,
            TimestampUtc = null,
            SchemaVersion = 1,
            PayloadJson = signalPayload,
            ClientMsgId = $"nyphos-signal:{deviceId}:{Guid.NewGuid():N}"
        };

        var action = state.RecommendedActions.FirstOrDefault() ?? "review_routine_and_downshift";
        var safeguardPayload = JsonSerializer.Serialize(new
        {
            action,
            rationale = BuildRationale(level, category),
            severity = level,
            advisoryByDefault = true,
            tone = prefs.Tone,
            muted,
            mutedUntilUtc = prefs.MutedUntilUtc,
            delivery = muted ? "suppressed" : "active",
            muteSupportedHours = 24,
            evidenceEventIds
        });

        var safeguardEnvelope = new EventEnvelope
        {
            EventId = Guid.NewGuid().ToString(),
            UserId = userId,
            DeviceId = deviceId,
            Stream = EventStream.Nyphos,
            Type = "SafeguardRecommended",
            Seq = null,
            TimestampUtc = null,
            SchemaVersion = 1,
            PayloadJson = safeguardPayload,
            ClientMsgId = $"nyphos-safeguard:{deviceId}:{Guid.NewGuid():N}"
        };

        var signalResult = await eventStore.AppendToStreamAsync(EventStream.Nyphos, [signalEnvelope], cancellationToken);
        var safeguardResult = await eventStore.AppendToStreamAsync(EventStream.Nyphos, [safeguardEnvelope], cancellationToken);

        return (signalResult.Items.FirstOrDefault()?.Seq, safeguardResult.Items.FirstOrDefault()?.Seq);
    }

    private static string MapLevel(string state)
    {
        return state.ToLowerInvariant() switch
        {
            "green" or "stable" => "Stable",
            "yellow" or "concern" => "Concern",
            "orange" or "elevated" => "Elevated",
            "red" or "critical" => "Critical",
            _ => "Concern"
        };
    }

    private static double CalculateConfidence(NyphosState state)
    {
        var score = (state.MoodRisk + state.OverloadIndex + (100 - state.SleepIntegrity)) / 300.0;
        return Math.Round(Math.Clamp(score, 0.35, 0.95), 2);
    }

    private static string BuildRationale(string level, string category)
    {
        return level switch
        {
            "Stable" => $"Current patterns look stable. Continue gentle routines; category: {category}.",
            "Concern" => $"Mild instability detected. A calm check-in is recommended; category: {category}.",
            "Elevated" => $"Elevated strain detected. Consider downshift protocol and reduced stimulation; category: {category}.",
            "Critical" => $"Critical strain detected. Use crisis/downshift supports now and prioritize safety; category: {category}.",
            _ => $"Advisory check recommended; category: {category}."
        };
    }
}
