using Hecateon.Client.Services;
using Hecateon.Client.Services.Foundation;

namespace Hecateon.Client.Tests;

public class OperatorPanelPreferencesServiceTests
{
    [Fact]
    public async Task LoadAsync_WhenEmpty_ReturnsDefaults()
    {
        var service = CreateService();

        var prefs = await service.LoadAsync();

        Assert.Equal(string.Empty, prefs.StreamFilter);
        Assert.Equal(100, prefs.Limit);
        Assert.False(prefs.AutoRefreshEnabled);
        Assert.False(prefs.AutoRefreshPaused);
        Assert.Equal(30, prefs.AutoRefreshSeconds);
        Assert.Equal(50, prefs.ExportRecentEvents);
        Assert.Equal(string.Empty, prefs.ImportFileName);
    }

    [Fact]
    public async Task SaveAsync_NormalizesOutOfRangeValues_AndLoadReturnsNormalized()
    {
        var service = CreateService();

        var input = new OperatorPanelPreferences
        {
            StreamFilter = null!,
            Limit = -5,
            AutoRefreshEnabled = false,
            AutoRefreshPaused = true,
            AutoRefreshSeconds = 1000,
            ExportRecentEvents = 0,
            ImportFileName = null!
        };

        await service.SaveAsync(input);
        var loaded = await service.LoadAsync();

        Assert.Equal(string.Empty, loaded.StreamFilter);
        Assert.Equal(1, loaded.Limit);
        Assert.False(loaded.AutoRefreshEnabled);
        Assert.False(loaded.AutoRefreshPaused);
        Assert.Equal(300, loaded.AutoRefreshSeconds);
        Assert.Equal(1, loaded.ExportRecentEvents);
        Assert.Equal(string.Empty, loaded.ImportFileName);
    }

    [Fact]
    public async Task ResetAsync_ClearsPreferences_ToDefaults()
    {
        var service = CreateService();
        await service.SaveAsync(new OperatorPanelPreferences
        {
            StreamFilter = "chat",
            Limit = 250,
            AutoRefreshEnabled = true,
            AutoRefreshPaused = true,
            AutoRefreshSeconds = 45,
            ExportRecentEvents = 77,
            ImportFileName = "snapshot.json",
            ImportApply = true,
            LastExportFileName = "last.json"
        });

        var reset = await service.ResetAsync();
        var loaded = await service.LoadAsync();

        Assert.Equal(string.Empty, reset.StreamFilter);
        Assert.Equal(100, reset.Limit);
        Assert.False(reset.AutoRefreshEnabled);
        Assert.False(reset.AutoRefreshPaused);
        Assert.Equal(30, reset.AutoRefreshSeconds);
        Assert.Equal(50, reset.ExportRecentEvents);
        Assert.Equal(string.Empty, loaded.StreamFilter);
        Assert.Equal(100, loaded.Limit);
        Assert.False(loaded.ImportApply);
        Assert.Null(loaded.LastExportFileName);
    }

    private static OperatorPanelPreferencesService CreateService()
    {
        return new OperatorPanelPreferencesService(new StorageService());
    }
}
