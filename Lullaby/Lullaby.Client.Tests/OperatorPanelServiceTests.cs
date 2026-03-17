using System.Net;
using System.Net.Http;
using System.Text;
using Hecateon.Client;
using Hecateon.Client.Services;
using Hecateon.Client.Services.Foundation;

namespace Hecateon.Client.Tests;

public class OperatorPanelServiceTests
{
    [Fact]
    public async Task GetPanelAsync_SendsDeviceHeader_AndParsesResponse()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            capturedRequest = request;
            const string body = """
{
  "status": {
    "identity": { "userId": "u1", "deviceId": "d1" },
    "sync": { "queueSize": 1, "dueQueueSize": 0 },
    "mode": { "current": "normal" },
    "nyphos": { "state": "Green" }
  },
  "events": [],
  "projectionHealth": { "graphLastAppliedSeq": 12, "prometheonLastProcessedSeq": 10, "streamHeads": { "chat": 99 } },
  "runbookActions": [],
  "correlationId": "abc"
}
""";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        });

        var service = await CreateServiceAsync(handler);
        var result = await service.GetPanelAsync(limit: 20);

        Assert.NotNull(result);
        Assert.Equal("abc", result!.CorrelationId);
        Assert.Equal(12, result.ProjectionHealth?.GraphLastAppliedSeq);
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest!.Method);
        Assert.Contains("/api/operator/panel?limit=20", capturedRequest.RequestUri?.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.True(capturedRequest.Headers.Contains("X-Device-Id"));
    }

    [Fact]
    public async Task ExecuteRunbookActionAsync_RejectsUnsafeAction_WithoutHttpCall()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            });

        var service = await CreateServiceAsync(handler);
        var action = new OperatorRunbookAction
        {
            Id = "snapshot-import",
            Method = "POST",
            Path = "/api/operator/snapshot/import",
            SafeAnytime = false,
            Query = ["fileName", "apply"]
        };

        var result = await service.ExecuteRunbookActionAsync(action);

        Assert.False(result.Success);
        Assert.Contains("not marked safe-anytime", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task ExecuteSnapshotImportAsync_RequiresConfirmation()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            });

        var service = await CreateServiceAsync(handler);
        var result = await service.ExecuteSnapshotImportAsync("snapshot.json", apply: true, confirmed: false);

        Assert.False(result.Success);
        Assert.Contains("requires explicit confirmation", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task ExecuteSnapshotExportAsync_ReturnsParsedFileInfo()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            capturedRequest = request;
            const string body = """
{
  "action": "snapshot-export",
  "fileName": "hecateon-snapshot-20260305-120000.json",
  "relativePath": "App_Data/snapshots/hecateon-snapshot-20260305-120000.json"
}
""";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        });

        var service = await CreateServiceAsync(handler);
        var result = await service.ExecuteSnapshotExportAsync(75);

        Assert.True(result.Success);
        Assert.Equal("hecateon-snapshot-20260305-120000.json", result.FileName);
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest!.Method);
        Assert.Contains("recentEventsPerStream=75", capturedRequest.RequestUri?.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.True(capturedRequest.Headers.Contains("X-Device-Id"));
    }

    private static async Task<OperatorPanelService> CreateServiceAsync(StubHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost")
        };

        var storageService = new StorageService();
        var deviceIdentityService = new DeviceIdentityService(storageService);
        await deviceIdentityService.InitializeAsync();

        return new OperatorPanelService(httpClient, deviceIdentityService);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
        public int CallCount { get; private set; }

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(_responder(request));
        }
    }
}
