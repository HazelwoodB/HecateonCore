using System.Collections.Concurrent;
using System.Text.Json;
using Hecateon.Models;

namespace Hecateon.Services;

/// <summary>
/// File-backed trusted device registry with explicit approve/revoke controls.
/// </summary>
public class TrustedDeviceRegistryService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly string _registryPath;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ConcurrentDictionary<string, TrustedDeviceRecord> _devices = new(StringComparer.OrdinalIgnoreCase);

    public TrustedDeviceRegistryService(IWebHostEnvironment environment)
    {
        var dataDirectory = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDirectory);
        _registryPath = Path.Combine(dataDirectory, "trusted-devices.json");

        LoadFromDisk();
    }

    public IReadOnlyList<TrustedDeviceRecord> GetAllDevices()
    {
        return _devices.Values
            .OrderByDescending(x => x.LastSeenUtc)
            .ToList();
    }

    public async Task<TrustedDeviceRecord> EnrollOrUpdatePendingAsync(string deviceId, string? displayName, CancellationToken cancellationToken = default)
    {
        var record = _devices.AddOrUpdate(
            deviceId,
            _ => new TrustedDeviceRecord
            {
                DeviceId = deviceId,
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? deviceId : displayName,
                IsApproved = false,
                Scopes = ["read-write"],
                CreatedUtc = DateTime.UtcNow,
                LastSeenUtc = DateTime.UtcNow
            },
            (_, existing) =>
            {
                existing.LastSeenUtc = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(displayName))
                {
                    existing.DisplayName = displayName;
                }

                return existing;
            });

        await PersistAsync(cancellationToken);
        return record;
    }

    public bool IsApproved(string deviceId)
    {
        return _devices.TryGetValue(deviceId, out var record) && record.IsApproved;
    }

    public async Task<bool> ApproveAsync(string deviceId, string[]? scopes = null, CancellationToken cancellationToken = default)
    {
        if (!_devices.TryGetValue(deviceId, out var record))
        {
            return false;
        }

        record.IsApproved = true;
        record.LastSeenUtc = DateTime.UtcNow;
        record.Scopes = scopes is { Length: > 0 } ? scopes : ["read-write"];

        await PersistAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RevokeAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        if (!_devices.TryGetValue(deviceId, out var record))
        {
            return false;
        }

        record.IsApproved = false;
        record.LastSeenUtc = DateTime.UtcNow;
        await PersistAsync(cancellationToken);
        return true;
    }

    private void LoadFromDisk()
    {
        if (!File.Exists(_registryPath))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(_registryPath);
            var records = JsonSerializer.Deserialize<List<TrustedDeviceRecord>>(json, JsonOptions) ?? [];
            foreach (var record in records)
            {
                if (!string.IsNullOrWhiteSpace(record.DeviceId))
                {
                    _devices[record.DeviceId] = record;
                }
            }
        }
        catch
        {
        }
    }

    private async Task PersistAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var json = JsonSerializer.Serialize(_devices.Values.ToList(), JsonOptions);
            await File.WriteAllTextAsync(_registryPath, json, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }
}

public class TrustedDeviceRecord
{
    public string DeviceId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public string[] Scopes { get; set; } = [];
    public DateTime CreatedUtc { get; set; }
    public DateTime LastSeenUtc { get; set; }
}
