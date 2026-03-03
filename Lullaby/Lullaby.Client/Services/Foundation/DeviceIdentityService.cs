using Microsoft.JSInterop;

namespace Lullaby.Client.Services.Foundation;

/// <summary>
/// Maintains a stable local device identity used for trusted-device and sync audit foundations.
/// </summary>
public class DeviceIdentityService
{
    private const string DeviceIdentityKey = "device_identity";
    private readonly StorageService _storageService;
    private DeviceIdentity? _identity;

    public DeviceIdentityService(StorageService storageService)
    {
        _storageService = storageService;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _identity = await _storageService.LoadAsync<DeviceIdentity>(DeviceIdentityKey, cancellationToken);
        if (_identity is not null)
        {
            return;
        }

        _identity = new DeviceIdentity
        {
            DeviceId = Guid.NewGuid().ToString("N"),
            DeviceName = Environment.MachineName,
            Platform = Environment.OSVersion.Platform.ToString(),
            CreatedAtUtc = DateTime.UtcNow
        };

        await _storageService.SaveAsync(DeviceIdentityKey, _identity, cancellationToken);
    }

    public async Task<string> GetDeviceIdAsync(CancellationToken cancellationToken = default)
    {
        if (_identity is null)
        {
            await InitializeAsync(cancellationToken);
        }

        return _identity?.DeviceId ?? "unknown-device";
    }

    public DeviceIdentity? GetIdentity() => _identity;
}

public class DeviceIdentity
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
