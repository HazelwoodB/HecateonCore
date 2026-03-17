namespace Hecateon.Events;

public readonly record struct IdempotencyKey(string UserId, string DeviceId, string ClientMsgId);
