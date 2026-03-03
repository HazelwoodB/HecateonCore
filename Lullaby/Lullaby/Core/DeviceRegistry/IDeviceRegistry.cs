namespace Hecateon.Core.DeviceRegistry;

using Hecateon.Core.Models;
using Hecateon.Core.EventStore;

/// <summary>
/// Represents a trusted device in the Hecateon ecosystem.
/// Device trust is module-scoped (can trust for Nyphos but not Prometheon).
/// </summary>
public record TrustedDevice
{
    public string DeviceId { get; init; } = null!;
    public string? DisplayName { get; init; }
    public bool IsApproved { get; init; }
    public string[] Scopes { get; init; } = []; // nyphos:read, nyphos:write, hecateon:admin, etc.
    public DateTime EnrolledUtc { get; init; } = DateTime.UtcNow;
    public DateTime? ApprovedUtc { get; init; }
    public DateTime LastSeenUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Device registry interface.
/// Manages trusted device enrollment, approval, and scope assignment.
/// </summary>
public interface IDeviceRegistry
{
    /// <summary>
    /// Enroll a new device (creates pending approval state).
    /// </summary>
    Task<TrustedDevice> EnrollAsync(string deviceId, string? displayName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Approve a device and assign scopes.
    /// </summary>
    Task<TrustedDevice?> ApproveAsync(string deviceId, string[] scopes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke a device's trust.
    /// </summary>
    Task<bool> RevokeAsync(string deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a device by ID.
    /// </summary>
    Task<TrustedDevice?> GetDeviceAsync(string deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if device is approved.
    /// </summary>
    Task<bool> IsApprovedAsync(string deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if device has a specific scope.
    /// </summary>
    Task<bool> HasScopeAsync(string deviceId, string scope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all devices.
    /// </summary>
    Task<IEnumerable<TrustedDevice>> GetAllDevicesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory implementation for MVP.
/// </summary>
public class InMemoryDeviceRegistry : IDeviceRegistry
{
    private readonly Dictionary<string, TrustedDevice> _devices = new();
    private readonly IEventStore _eventStore;
    private readonly object _lock = new();

    public InMemoryDeviceRegistry(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task<TrustedDevice> EnrollAsync(string deviceId, string? displayName, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_devices.TryGetValue(deviceId, out var existing))
            {
                return existing;
            }

            var device = new TrustedDevice
            {
                DeviceId = deviceId,
                DisplayName = displayName ?? deviceId,
                IsApproved = false
            };

            _devices[deviceId] = device;

            _ = _eventStore.AppendAsync(new DeviceEnrolledEvent
            {
                Module = "hecateon",
                EventType = nameof(DeviceEnrolledEvent),
                DeviceId = deviceId,
                DisplayName = displayName
            }, cancellationToken);

            return device;
        }
    }

    public async Task<TrustedDevice?> ApproveAsync(string deviceId, string[] scopes, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (!_devices.TryGetValue(deviceId, out var device))
                return null;

            var approved = device with
            {
                IsApproved = true,
                Scopes = scopes,
                ApprovedUtc = DateTime.UtcNow
            };

            _devices[deviceId] = approved;

            _ = _eventStore.AppendAsync(new DeviceApprovedEvent
            {
                Module = "hecateon",
                EventType = nameof(DeviceApprovedEvent),
                DeviceId = deviceId,
                Scopes = scopes
            }, cancellationToken);

            return approved;
        }
    }

    public async Task<bool> RevokeAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (!_devices.Remove(deviceId))
                return false;

            _ = _eventStore.AppendAsync(new DeviceRevokedEvent
            {
                Module = "hecateon",
                EventType = nameof(DeviceRevokedEvent),
                DeviceId = deviceId
            }, cancellationToken);

            return true;
        }
    }

    public Task<TrustedDevice?> GetDeviceAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _devices.TryGetValue(deviceId, out var device);
            return Task.FromResult(device);
        }
    }

    public async Task<bool> IsApprovedAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        var device = await GetDeviceAsync(deviceId, cancellationToken);
        return device?.IsApproved ?? false;
    }

    public async Task<bool> HasScopeAsync(string deviceId, string scope, CancellationToken cancellationToken = default)
    {
        var device = await GetDeviceAsync(deviceId, cancellationToken);
        return device?.Scopes.Contains(scope) ?? false;
    }

    public Task<IEnumerable<TrustedDevice>> GetAllDevicesAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_devices.Values.AsEnumerable());
        }
    }
}
