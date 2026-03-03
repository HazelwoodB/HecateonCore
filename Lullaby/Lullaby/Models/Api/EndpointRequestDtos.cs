namespace Lullaby.Models.Api.Endpoints;

public record EnrollDeviceRequest(string DeviceId, string? DisplayName);
public record ApproveDeviceRequest(string DeviceId, string[] Scopes);
public record RevokeDeviceRequest(string DeviceId);

public record LogSleepRequest(DateTime SleepStart, DateTime SleepEnd, int? QualityScore, string[]? Interruptions);
public record LogMoodRequest(int? EnergyLevel, int? MoodScore, string? MoodLabel, string? Notes);

public record LegacyHealthLogRequest(int Mood, double? Sleep, DateTime Timestamp);
