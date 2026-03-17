namespace Hecateon.Models.Api.Endpoints;

using Hecateon.Events;

public record EnrollDeviceRequest(string DeviceId, string? DisplayName);
public record ApproveDeviceRequest(string DeviceId, string[] Scopes);
public record RevokeDeviceRequest(string DeviceId);

public record LogSleepRequest(DateTime SleepStart, DateTime SleepEnd, int? QualityScore, string[]? Interruptions);
public record LogMoodRequest(int? EnergyLevel, int? MoodScore, string? MoodLabel, string? Notes);


public record StreamAppendRequest(IReadOnlyList<EventEnvelope> Events);

public record SyncPushRequest(string Stream, int? MaxItems);
public record SyncPullRequest(string Stream, int? Limit);

public record ModeTagRequest(string Mode, string? UserId);

public record NyphosToneRequest(string Tone, string? UserId);
public record NyphosMuteRequest(int? Hours, string? UserId);
