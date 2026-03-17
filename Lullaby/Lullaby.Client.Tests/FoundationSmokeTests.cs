using Hecateon.Client.Services.Foundation;

namespace Hecateon.Client.Tests;

public class FoundationSmokeTests
{
    [Fact]
    public async Task StorageService_SaveThenLoad_RoundTrips()
    {
        var storage = new StorageService();
        var data = new DemoPayload { Name = "alpha", Count = 3 };

        await storage.SaveAsync("smoke_payload", data);
        var loaded = await storage.LoadAsync<DemoPayload>("smoke_payload");

        Assert.NotNull(loaded);
        Assert.Equal("alpha", loaded!.Name);
        Assert.Equal(3, loaded.Count);
    }

    [Fact]
    public async Task DeviceIdentityService_Initializes_AndReturnsStableDeviceId()
    {
        var storage = new StorageService();
        var device = new DeviceIdentityService(storage);

        await device.InitializeAsync();
        var id1 = await device.GetDeviceIdAsync();
        var id2 = await device.GetDeviceIdAsync();

        Assert.False(string.IsNullOrWhiteSpace(id1));
        Assert.Equal(id1, id2);
    }

    private sealed class DemoPayload
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
