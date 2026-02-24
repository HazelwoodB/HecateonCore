using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AutoPC.Services;

public class LLMAssistantService(IHttpClientFactory httpFactory)
{
    private readonly HttpClient _http = httpFactory.CreateClient("llm");
    private readonly string? _openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    private readonly string? _openAiHost = Environment.GetEnvironmentVariable("OPENAI_API_HOST");
    private readonly string? _openAiModel = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-3.5-turbo";
    private readonly string? _azureKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");
    private readonly string? _azureEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
    private readonly string? _azureDeployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT");

    private async IAsyncEnumerable<string> CallAzureOpenAiStreamAsync(string userMessage, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var endpoint = _azureEndpoint!.TrimEnd('/');
        var url = $"{endpoint}/openai/deployments/{_azureDeployment}/chat/completions?api-version=2023-10-01-preview";

        var payload = new
        {
            messages = new[]
            {
                new { role = "system", content = "You are a helpful assistant." },
                new { role = "user", content = userMessage }
            },
            max_tokens = 512,
            stream = true
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Add("api-key", _azureKey);
        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        res.EnsureSuccessStatusCode();

        using var stream = await res.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream);

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line == null)
                break;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (line.StartsWith("data: "))
                line = line["data: ".Length..];

            if (line.Trim() == "[DONE]")
                yield break;

            string? toYield = null;
            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;
                if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var delta = choices[0].GetProperty("delta");
                    if (delta.TryGetProperty("content", out var contentEl))
                    {
                        var content = contentEl.GetString();
                        if (!string.IsNullOrEmpty(content))
                        {
                            toYield = content;
                        }
                    }
                }
            }
            catch
            {
                toYield = line + "\n";
            }

            if (!string.IsNullOrEmpty(toYield))
                yield return toYield;
        }
    }

    private async IAsyncEnumerable<string> CallOpenAiStreamAsync(string userMessage, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var apiHost = _openAiHost ?? "https://api.openai.com";
        var url = apiHost.TrimEnd('/') + "/v1/chat/completions";

        var payload = new
        {
            model = _openAiModel,
            messages = new[]
            {
                new { role = "system", content = "You are a helpful assistant." },
                new { role = "user", content = userMessage }
            },
            max_tokens = 512,
            stream = true
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openAiKey);
        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        res.EnsureSuccessStatusCode();

        using var stream = await res.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream);

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line == null)
                break;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            // OpenAI streams lines that start with "data: " and end with [DONE]
            if (line.StartsWith("data: "))
                line = line["data: ".Length..];

            if (line.Trim() == "[DONE]")
                yield break;

            string? toYield = null;
            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;
                if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var delta = choices[0].GetProperty("delta");
                    if (delta.TryGetProperty("content", out var contentEl))
                    {
                        var content = contentEl.GetString();
                        if (!string.IsNullOrEmpty(content))
                        {
                            toYield = content;
                        }
                    }
                }
            }
            catch
            {
                // ignore parse errors and yield raw line
                toYield = line + "\n";
            }

            if (!string.IsNullOrEmpty(toYield))
                yield return toYield;
        }
    }

    public async Task<string> GenerateReplyAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_openAiKey))
        {
            return await CallOpenAiAsync(userMessage, cancellationToken).ConfigureAwait(false);
        }

        if (!string.IsNullOrWhiteSpace(_azureKey) && !string.IsNullOrWhiteSpace(_azureEndpoint) && !string.IsNullOrWhiteSpace(_azureDeployment))
        {
            return await CallAzureOpenAiAsync(userMessage, cancellationToken).ConfigureAwait(false);
        }

        // Fallback: simple echo if no keys provided
        return $"[Local assistant] You said: {userMessage}";
    }

    // Streaming variant: yields incremental chunks as produced by the LLM (or a single fallback chunk)
    public async IAsyncEnumerable<string> GenerateReplyStreamAsync(string userMessage, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_openAiKey))
        {
            await foreach (var part in CallOpenAiStreamAsync(userMessage, cancellationToken))
                yield return part;
            yield break;
        }

        if (!string.IsNullOrWhiteSpace(_azureKey) && !string.IsNullOrWhiteSpace(_azureEndpoint) && !string.IsNullOrWhiteSpace(_azureDeployment))
        {
            await foreach (var part in CallAzureOpenAiStreamAsync(userMessage, cancellationToken))
                yield return part;
            yield break;
        }

        // Fallback single chunk
        yield return $"[Local assistant] You said: {userMessage}";
    }

    private async Task<string> CallOpenAiAsync(string userMessage, CancellationToken cancellationToken)
    {
        var apiHost = _openAiHost ?? "https://api.openai.com";
        var url = apiHost.TrimEnd('/') + "/v1/chat/completions";

        var payload = new
        {
            model = _openAiModel,
            messages = new[]
            {
                new { role = "system", content = "You are a helpful assistant." },
                new { role = "user", content = userMessage }
            },
            max_tokens = 512
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openAiKey);
        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var res = await _http.SendAsync(req, cancellationToken).ConfigureAwait(false);
        res.EnsureSuccessStatusCode();

        using var stream = await res.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
        {
            var message = choices[0].GetProperty("message").GetProperty("content").GetString();
            return message ?? string.Empty;
        }

        return string.Empty;
    }

    private async Task<string> CallAzureOpenAiAsync(string userMessage, CancellationToken cancellationToken)
    {
        // Azure OpenAI chat completion endpoint format
        // POST {endpoint}/openai/deployments/{deployment}/chat/completions?api-version=2023-10-01-preview
        var endpoint = _azureEndpoint!.TrimEnd('/');
        var url = $"{endpoint}/openai/deployments/{_azureDeployment}/chat/completions?api-version=2023-10-01-preview";

        var payload = new
        {
            messages = new[]
            {
                new { role = "system", content = "You are a helpful assistant." },
                new { role = "user", content = userMessage }
            },
            max_tokens = 512
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Add("api-key", _azureKey);
        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var res = await _http.SendAsync(req, cancellationToken).ConfigureAwait(false);
        res.EnsureSuccessStatusCode();

        using var stream = await res.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
        {
            var message = choices[0].GetProperty("message").GetProperty("content").GetString();
            return message ?? string.Empty;
        }

        return string.Empty;
    }
}
