using Hecateon.Client.Services.Foundation;

namespace Hecateon.Client.Services;

public class OperatorPanelPreferencesService
{
    private const string StorageKey = "operator_panel_preferences";

    private readonly StorageService _storageService;

    public OperatorPanelPreferencesService(StorageService storageService)
    {
        _storageService = storageService;
    }

    public async Task<OperatorPanelPreferences> LoadAsync(CancellationToken cancellationToken = default)
    {
        var loaded = await _storageService.LoadAsync<OperatorPanelPreferences>(StorageKey, cancellationToken);
        return Normalize(loaded ?? OperatorPanelPreferences.CreateDefault());
    }

    public async Task SaveAsync(OperatorPanelPreferences preferences, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(preferences);
        await _storageService.SaveAsync(StorageKey, normalized, cancellationToken);
    }

    public async Task<OperatorPanelPreferences> ResetAsync(CancellationToken cancellationToken = default)
    {
        await _storageService.DeleteAsync(StorageKey, cancellationToken);
        var defaults = OperatorPanelPreferences.CreateDefault();
        await _storageService.SaveAsync(StorageKey, defaults, cancellationToken);
        return defaults;
    }

    private static OperatorPanelPreferences Normalize(OperatorPanelPreferences preferences)
    {
        preferences.StreamFilter ??= string.Empty;
        preferences.ImportFileName ??= string.Empty;

        preferences.Limit = Math.Clamp(preferences.Limit, 1, 500);
        preferences.AutoRefreshSeconds = Math.Clamp(preferences.AutoRefreshSeconds, 5, 300);
        preferences.ExportRecentEvents = Math.Clamp(preferences.ExportRecentEvents, 1, 500);

        if (!preferences.AutoRefreshEnabled)
        {
            preferences.AutoRefreshPaused = false;
        }

        return preferences;
    }
}

public class OperatorPanelPreferences
{
    public string StreamFilter { get; set; } = string.Empty;
    public int Limit { get; set; } = 100;
    public bool AutoRefreshEnabled { get; set; }
    public int AutoRefreshSeconds { get; set; } = 30;
    public bool AutoRefreshPaused { get; set; }
    public int ExportRecentEvents { get; set; } = 50;
    public string ImportFileName { get; set; } = string.Empty;
    public bool ImportApply { get; set; }
    public string? LastExportFileName { get; set; }
    public string? LastExportRelativePath { get; set; }

    public static OperatorPanelPreferences CreateDefault() => new();
}
