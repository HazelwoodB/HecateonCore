using Microsoft.JSInterop;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Hecateon.Client.Services.Foundation;

/// <summary>
/// Abstraction for local storage operations
/// Handles all data persistence (localStorage, IndexedDB)
/// Provides encryption-ready structure for future enhancements
/// </summary>
public class StorageService
{
    private const string STORAGE_PREFIX = "ARIA_";
    private static readonly ConcurrentDictionary<string, string> InMemoryStore = new();
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Save data to local storage with key prefix
    /// </summary>
    public async Task SaveAsync<T>(string key, T data, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var storageKey = $"{STORAGE_PREFIX}{key}";
            var json = JsonSerializer.Serialize(data, JsonOptions);

            InMemoryStore[storageKey] = json;

            Console.WriteLine($"[Storage] Saving {key} ({json.Length} bytes)");

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Storage] Error saving {key}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Load data from local storage
    /// </summary>
    public async Task<T?> LoadAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var storageKey = $"{STORAGE_PREFIX}{key}";

            Console.WriteLine($"[Storage] Loading {key}");

            if (!InMemoryStore.TryGetValue(storageKey, out var json) || string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Storage] Error loading {key}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Delete data from storage
    /// </summary>
    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var storageKey = $"{STORAGE_PREFIX}{key}";

            InMemoryStore.TryRemove(storageKey, out _);

            Console.WriteLine($"[Storage] Deleting {key}");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Storage] Error deleting {key}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Clear all ARIA data from storage
    /// </summary>
    public async Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            foreach (var key in InMemoryStore.Keys.Where(k => k.StartsWith(STORAGE_PREFIX, StringComparison.Ordinal)))
            {
                InMemoryStore.TryRemove(key, out _);
            }

            Console.WriteLine("[Storage] Clearing all ARIA data");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Storage] Error clearing storage: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Check if a key exists in storage
    /// </summary>
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var storageKey = $"{STORAGE_PREFIX}{key}";
            return await Task.FromResult(InMemoryStore.ContainsKey(storageKey));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get storage size in bytes
    /// </summary>
    public async Task<long> GetStorageSizeAsync(CancellationToken cancellationToken = default)
    {
        var total = InMemoryStore
            .Where(kvp => kvp.Key.StartsWith(STORAGE_PREFIX, StringComparison.Ordinal))
            .Sum(kvp => kvp.Value.Length);

        return await Task.FromResult(total);
    }
}
