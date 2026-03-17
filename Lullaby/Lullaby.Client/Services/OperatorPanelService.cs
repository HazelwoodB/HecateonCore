using System.Net.Http.Json;
using System.Text.Json;
using Hecateon.Client.Services.Foundation;

namespace Hecateon.Client.Services;

public class OperatorPanelService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly DeviceIdentityService _deviceIdentityService;

    public OperatorPanelService(HttpClient httpClient, DeviceIdentityService deviceIdentityService)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _deviceIdentityService = deviceIdentityService ?? throw new ArgumentNullException(nameof(deviceIdentityService));
    }

    public async Task<OperatorPanelResponse?> GetPanelAsync(
        string? stream = null,
        int? limit = null,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(stream, limit);
        using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, $"/api/operator/panel{query}", userId, cancellationToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<OperatorPanelResponse>(JsonOptions, cancellationToken);
    }

    public async Task<OperatorRunbookResult> ExecuteRunbookActionAsync(
        OperatorRunbookAction action,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        if (action is null)
        {
            return new OperatorRunbookResult(false, "Runbook action is required.");
        }

        if (!action.SafeAnytime)
        {
            return new OperatorRunbookResult(false, $"Action '{action.Id}' is not marked safe-anytime.");
        }

        HttpMethod method;
        try
        {
            method = new HttpMethod(action.Method);
        }
        catch
        {
            return new OperatorRunbookResult(false, $"Unsupported HTTP method '{action.Method}'.");
        }

        var query = BuildActionDefaults(action);
        using var request = await CreateAuthorizedRequestAsync(method, $"{action.Path}{query}", userId, cancellationToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new OperatorRunbookResult(false, $"Action '{action.Id}' failed with {(int)response.StatusCode}: {TrimMessage(body)}");
        }

        return new OperatorRunbookResult(true, $"Action '{action.Id}' completed: {TrimMessage(body)}");
    }

    public async Task<OperatorRunbookResult> ExecuteSnapshotImportAsync(
        string fileName,
        bool apply,
        bool confirmed,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        if (!confirmed)
        {
            return new OperatorRunbookResult(false, "Snapshot import requires explicit confirmation.");
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            return new OperatorRunbookResult(false, "Snapshot file name is required.");
        }

        var uri = $"/api/operator/snapshot/import?fileName={Uri.EscapeDataString(fileName)}&apply={apply.ToString().ToLowerInvariant()}";
        using var request = await CreateAuthorizedRequestAsync(HttpMethod.Post, uri, userId, cancellationToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new OperatorRunbookResult(false, $"Snapshot import failed with {(int)response.StatusCode}: {TrimMessage(body)}");
        }

        return new OperatorRunbookResult(true, $"Snapshot import completed: {TrimMessage(body)}");
    }

    public async Task<OperatorSnapshotExportResult> ExecuteSnapshotExportAsync(
        int recentEventsPerStream,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        var safeRecentEvents = recentEventsPerStream <= 0
            ? 50
            : Math.Clamp(recentEventsPerStream, 1, 500);

        var uri = $"/api/operator/snapshot/export?recentEventsPerStream={safeRecentEvents}";
        using var request = await CreateAuthorizedRequestAsync(HttpMethod.Post, uri, userId, cancellationToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new OperatorSnapshotExportResult(false, $"Snapshot export failed with {(int)response.StatusCode}: {TrimMessage(body)}");
        }

        string? fileName = null;
        string? relativePath = null;

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (root.TryGetProperty("fileName", out var fileNameEl) && fileNameEl.ValueKind == JsonValueKind.String)
            {
                fileName = fileNameEl.GetString();
            }

            if (root.TryGetProperty("relativePath", out var pathEl) && pathEl.ValueKind == JsonValueKind.String)
            {
                relativePath = pathEl.GetString();
            }
        }
        catch
        {
            // Keep success response even if parsing fails; body still indicates success.
        }

        var message = string.IsNullOrWhiteSpace(fileName)
            ? "Snapshot export completed."
            : $"Snapshot export completed: {fileName}";

        return new OperatorSnapshotExportResult(true, message, fileName, relativePath);
    }

    private async Task<HttpRequestMessage> CreateAuthorizedRequestAsync(
        HttpMethod method,
        string uri,
        string? userId,
        CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(method, uri);

        var deviceId = await _deviceIdentityService.GetDeviceIdAsync(cancellationToken);
        request.Headers.TryAddWithoutValidation("X-Device-Id", deviceId);

        if (!string.IsNullOrWhiteSpace(userId))
        {
            request.Headers.TryAddWithoutValidation("X-User-Id", userId);
        }

        return request;
    }

    private static string BuildActionDefaults(OperatorRunbookAction action)
    {
        var queryParts = new List<string>(2);

        foreach (var key in action.Query)
        {
            if (key.Equals("batchSize", StringComparison.OrdinalIgnoreCase))
            {
                queryParts.Add("batchSize=200");
            }
            else if (key.Equals("recentEventsPerStream", StringComparison.OrdinalIgnoreCase))
            {
                queryParts.Add("recentEventsPerStream=50");
            }
        }

        return queryParts.Count == 0
            ? string.Empty
            : $"?{string.Join("&", queryParts)}";
    }

    private static string TrimMessage(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return "OK";
        }

        const int maxLength = 200;
        return input.Length <= maxLength
            ? input
            : $"{input[..maxLength]}...";
    }

    private static string BuildQuery(string? stream, int? limit)
    {
        var queryParts = new List<string>(2);

        if (!string.IsNullOrWhiteSpace(stream))
        {
            queryParts.Add($"stream={Uri.EscapeDataString(stream)}");
        }

        if (limit is > 0)
        {
            queryParts.Add($"limit={limit.Value}");
        }

        return queryParts.Count == 0
            ? string.Empty
            : $"?{string.Join("&", queryParts)}";
    }
}

public record OperatorRunbookResult(bool Success, string Message);
public record OperatorSnapshotExportResult(bool Success, string Message, string? FileName = null, string? RelativePath = null);
