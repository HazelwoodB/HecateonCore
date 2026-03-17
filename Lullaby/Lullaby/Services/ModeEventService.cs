using System.Text.Json;
using Hecateon.Core.EventStore;
using Hecateon.Data;
using Hecateon.Data.Models;
using Hecateon.Events;
using Microsoft.EntityFrameworkCore;

namespace Hecateon.Services;

public sealed record ModeTagResult(
    string UserId,
    string DeviceId,
    string CurrentMode,
    string? PreviousMode,
    bool ShiftDetected,
    long? ModeSelfTaggedSeq,
    long? ModeShiftDetectedSeq,
    DateTimeOffset UpdatedUtc);

public interface IModeEventService
{
    Task<ModeTagResult> TagModeAsync(string userId, string deviceId, string mode, CancellationToken cancellationToken = default);
    Task<ModeStateRecord?> GetCurrentModeAsync(string userId, string deviceId, CancellationToken cancellationToken = default);
}

public sealed class ModeEventService(ChatDbContext dbContext, IEventStore eventStore) : IModeEventService
{
    private static readonly HashSet<string> AllowedModes = ["Child", "Magician", "Operator"];

    public async Task<ModeTagResult> TagModeAsync(string userId, string deviceId, string mode, CancellationToken cancellationToken = default)
    {
        if (!AllowedModes.Contains(mode))
        {
            throw new ArgumentException("Mode must be one of Child, Magician, Operator.", nameof(mode));
        }

        var now = DateTimeOffset.UtcNow;
        var state = await dbContext.ModeStates.FirstOrDefaultAsync(x => x.UserId == userId && x.DeviceId == deviceId, cancellationToken);
        var previousMode = state?.CurrentMode;
        var shiftDetected = !string.IsNullOrWhiteSpace(previousMode) && !string.Equals(previousMode, mode, StringComparison.Ordinal);

        var selfTagPayload = JsonSerializer.Serialize(new
        {
            mode,
            taggedUtc = now,
            source = "user",
            schemaVersion = 1
        });

        var selfTagEnvelope = new EventEnvelope
        {
            EventId = Guid.NewGuid().ToString(),
            UserId = userId,
            DeviceId = deviceId,
            Stream = EventStream.System,
            Type = "ModeSelfTagged",
            Seq = null,
            TimestampUtc = null,
            SchemaVersion = 1,
            PayloadJson = selfTagPayload,
            ClientMsgId = $"mode-self:{userId}:{deviceId}:{Guid.NewGuid():N}"
        };

        var selfTagResult = await eventStore.AppendToStreamAsync(EventStream.System, [selfTagEnvelope], cancellationToken);
        var selfTagSeq = selfTagResult.Items.FirstOrDefault()?.Seq;
        var selfTagEventId = selfTagResult.Items.FirstOrDefault()?.EventId;

        long? shiftSeq = null;
        if (shiftDetected)
        {
            var shiftPayload = JsonSerializer.Serialize(new
            {
                from = previousMode,
                to = mode,
                confidence = 1.0,
                evidenceEventIds = selfTagEventId is null ? Array.Empty<string>() : new[] { selfTagEventId },
                detectedUtc = now,
                source = "user_tag_transition",
                schemaVersion = 1
            });

            var shiftEnvelope = new EventEnvelope
            {
                EventId = Guid.NewGuid().ToString(),
                UserId = userId,
                DeviceId = deviceId,
                Stream = EventStream.System,
                Type = "ModeShiftDetected",
                Seq = null,
                TimestampUtc = null,
                SchemaVersion = 1,
                PayloadJson = shiftPayload,
                ClientMsgId = $"mode-shift:{userId}:{deviceId}:{Guid.NewGuid():N}"
            };

            var shiftResult = await eventStore.AppendToStreamAsync(EventStream.System, [shiftEnvelope], cancellationToken);
            shiftSeq = shiftResult.Items.FirstOrDefault()?.Seq;
        }

        if (state is null)
        {
            dbContext.ModeStates.Add(new ModeStateRecord
            {
                UserId = userId,
                DeviceId = deviceId,
                CurrentMode = mode,
                PreviousMode = previousMode,
                LastConfidence = 1.0,
                LastSource = "user",
                LastEvidenceEventIdsJson = selfTagEventId is null ? "[]" : JsonSerializer.Serialize(new[] { selfTagEventId }),
                UpdatedUtc = now
            });
        }
        else
        {
            state.PreviousMode = previousMode;
            state.CurrentMode = mode;
            state.LastConfidence = 1.0;
            state.LastSource = "user";
            state.LastEvidenceEventIdsJson = selfTagEventId is null ? "[]" : JsonSerializer.Serialize(new[] { selfTagEventId });
            state.UpdatedUtc = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new ModeTagResult(userId, deviceId, mode, previousMode, shiftDetected, selfTagSeq, shiftSeq, now);
    }

    public Task<ModeStateRecord?> GetCurrentModeAsync(string userId, string deviceId, CancellationToken cancellationToken = default)
    {
        return dbContext.ModeStates
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.DeviceId == deviceId, cancellationToken);
    }
}
