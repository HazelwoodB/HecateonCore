using Hecateon.Data;
using Hecateon.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Hecateon.Services;

public sealed record NyphosPreferenceSnapshot(string UserId, string DeviceId, string Tone, DateTimeOffset? MutedUntilUtc, DateTimeOffset UpdatedUtc)
{
    public bool IsMuted => MutedUntilUtc.HasValue && MutedUntilUtc.Value > DateTimeOffset.UtcNow;
}

public interface INyphosPreferenceService
{
    Task<NyphosPreferenceSnapshot> GetOrDefaultAsync(string userId, string deviceId, CancellationToken cancellationToken = default);
    Task<NyphosPreferenceSnapshot> SetToneAsync(string userId, string deviceId, string tone, CancellationToken cancellationToken = default);
    Task<NyphosPreferenceSnapshot> SetMuteAsync(string userId, string deviceId, int hours, CancellationToken cancellationToken = default);
    Task<NyphosPreferenceSnapshot> ClearMuteAsync(string userId, string deviceId, CancellationToken cancellationToken = default);
}

public sealed class NyphosPreferenceService(ChatDbContext dbContext) : INyphosPreferenceService
{
    private static readonly HashSet<string> AllowedTones = ["Gentle", "Neutral", "Direct"];

    public async Task<NyphosPreferenceSnapshot> GetOrDefaultAsync(string userId, string deviceId, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.NyphosPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.DeviceId == deviceId, cancellationToken);

        if (record is null)
        {
            return new NyphosPreferenceSnapshot(userId, deviceId, "Gentle", null, DateTimeOffset.UtcNow);
        }

        return new NyphosPreferenceSnapshot(record.UserId, record.DeviceId, record.Tone, record.MutedUntilUtc, record.UpdatedUtc);
    }

    public async Task<NyphosPreferenceSnapshot> SetToneAsync(string userId, string deviceId, string tone, CancellationToken cancellationToken = default)
    {
        if (!AllowedTones.Contains(tone))
        {
            throw new ArgumentException("Tone must be one of Gentle, Neutral, Direct.", nameof(tone));
        }

        var record = await GetOrCreateMutableAsync(userId, deviceId, cancellationToken);
        record.Tone = tone;
        record.UpdatedUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new NyphosPreferenceSnapshot(record.UserId, record.DeviceId, record.Tone, record.MutedUntilUtc, record.UpdatedUtc);
    }

    public async Task<NyphosPreferenceSnapshot> SetMuteAsync(string userId, string deviceId, int hours, CancellationToken cancellationToken = default)
    {
        var safeHours = Math.Clamp(hours, 1, 24);
        var record = await GetOrCreateMutableAsync(userId, deviceId, cancellationToken);
        record.MutedUntilUtc = DateTimeOffset.UtcNow.AddHours(safeHours);
        record.UpdatedUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new NyphosPreferenceSnapshot(record.UserId, record.DeviceId, record.Tone, record.MutedUntilUtc, record.UpdatedUtc);
    }

    public async Task<NyphosPreferenceSnapshot> ClearMuteAsync(string userId, string deviceId, CancellationToken cancellationToken = default)
    {
        var record = await GetOrCreateMutableAsync(userId, deviceId, cancellationToken);
        record.MutedUntilUtc = null;
        record.UpdatedUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new NyphosPreferenceSnapshot(record.UserId, record.DeviceId, record.Tone, record.MutedUntilUtc, record.UpdatedUtc);
    }

    private async Task<NyphosPreferenceRecord> GetOrCreateMutableAsync(string userId, string deviceId, CancellationToken cancellationToken)
    {
        var record = await dbContext.NyphosPreferences
            .FirstOrDefaultAsync(x => x.UserId == userId && x.DeviceId == deviceId, cancellationToken);

        if (record is not null)
        {
            return record;
        }

        record = new NyphosPreferenceRecord
        {
            UserId = userId,
            DeviceId = deviceId,
            Tone = "Gentle",
            MutedUntilUtc = null,
            UpdatedUtc = DateTimeOffset.UtcNow
        };

        dbContext.NyphosPreferences.Add(record);
        return record;
    }
}
