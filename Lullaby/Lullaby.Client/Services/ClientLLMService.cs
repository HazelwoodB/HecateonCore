using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Hecateon.Client;

namespace Hecateon.Client.Services;

/// <summary>
/// Client-side LLM service using Ollama local models.
/// Runs entirely in the browser using Blazor WebAssembly.
/// Ollama API is compatible with OpenAI's API format.
/// </summary>
public class ClientLLMService
{
    private readonly HttpClient _httpClient;
    private string? _ollamaEndpoint;
    private string? _ollamaModel;
    private const int MaxRetries = 2;

    /// <summary>
    /// Initialize the LLM service with Ollama configuration.
    /// </summary>
    public ClientLLMService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        
        // Default Ollama endpoint (user can override via SetOllamaConfig)
        _ollamaEndpoint = "http://localhost:11434";
        _ollamaModel = "mistral"; // Popular lightweight model
    }

    /// <summary>
    /// Set Ollama endpoint and model (e.g., http://localhost:11434, "mistral").
    /// </summary>
    public void SetOllamaConfig(string endpoint, string model)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new ArgumentException("Endpoint cannot be empty.", nameof(endpoint));
        
        if (string.IsNullOrWhiteSpace(model))
            throw new ArgumentException("Model cannot be empty.", nameof(model));

        _ollamaEndpoint = endpoint.TrimEnd('/');
        _ollamaModel = model;
        Console.WriteLine($"[Ollama] Configured: {_ollamaEndpoint} with model {_ollamaModel}");
    }

    /// <summary>
    /// Get currently configured Ollama endpoint.
    /// </summary>
    public string? GetOllamaEndpoint() => _ollamaEndpoint;

    /// <summary>
    /// Get currently configured Ollama model.
    /// </summary>
    public string? GetOllamaModel() => _ollamaModel;

    /// <summary>
    /// Generate a single reply from the LLM.
    /// </summary>
    public async Task<string> GenerateReplyAsync(
        string userMessage,
        List<ChatMessage> conversationHistory,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
            throw new ArgumentException("Message cannot be empty.", nameof(userMessage));

        if (string.IsNullOrWhiteSpace(_ollamaEndpoint) || string.IsNullOrWhiteSpace(_ollamaModel))
            return GenerateFallbackReply(userMessage);

        try
        {
            return await CallOllamaAsync(userMessage, conversationHistory, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"[Ollama] HTTP error: {ex.Message}");
            return $"⚠️ Ollama request failed. Ensure Ollama is running at {_ollamaEndpoint}. Error: {ex.Message}";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Ollama] Error: {ex.Message}");
            return GenerateFallbackReply(userMessage);
        }
    }

    /// <summary>
    /// Generate a single reply from the LLM with custom system prompt.
    /// </summary>
    public async Task<string> GenerateReplyAsync(
        string userMessage,
        List<ChatMessage> conversationHistory,
        string? systemPrompt = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
            throw new ArgumentException("Message cannot be empty.", nameof(userMessage));

        if (string.IsNullOrWhiteSpace(_ollamaEndpoint) || string.IsNullOrWhiteSpace(_ollamaModel))
            return GenerateFallbackReply(userMessage);

        try
        {
            return await CallOllamaAsync(userMessage, conversationHistory, systemPrompt, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"[Ollama] HTTP error: {ex.Message}");
            return $"⚠️ Ollama request failed. Ensure Ollama is running at {_ollamaEndpoint}. Error: {ex.Message}";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Ollama] Error: {ex.Message}");
            return GenerateFallbackReply(userMessage);
        }
    }

    /// <summary>
    /// Stream a response from the LLM, yielding chunks as they arrive.
    /// </summary>
    public async IAsyncEnumerable<string> GenerateReplyStreamAsync(
        string userMessage,
        List<ChatMessage> conversationHistory,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
            throw new ArgumentException("Message cannot be empty.", nameof(userMessage));

        if (string.IsNullOrWhiteSpace(_ollamaEndpoint) || string.IsNullOrWhiteSpace(_ollamaModel))
        {
            yield return GenerateFallbackReply(userMessage);
            yield break;
        }

        await foreach (var chunk in CallOllamaStreamAsync(userMessage, conversationHistory, cancellationToken))
        {
            yield return chunk;
        }
    }

    /// <summary>
    /// Stream a response from the LLM, yielding chunks as they arrive.
    /// </summary>
    public async IAsyncEnumerable<string> GenerateReplyStreamAsync(
        string userMessage,
        List<ChatMessage> conversationHistory,
        string? systemPrompt = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
            throw new ArgumentException("Message cannot be empty.", nameof(userMessage));

        if (string.IsNullOrWhiteSpace(_ollamaEndpoint) || string.IsNullOrWhiteSpace(_ollamaModel))
        {
            yield return GenerateFallbackReply(userMessage);
            yield break;
        }

        await foreach (var chunk in CallOllamaStreamAsync(userMessage, conversationHistory, systemPrompt, cancellationToken))
        {
            yield return chunk;
        }
    }

    private async Task<string> CallOllamaAsync(
        string userMessage,
        List<ChatMessage> conversationHistory,
        CancellationToken cancellationToken)
    {
        var apiUrl = $"{_ollamaEndpoint}/api/chat";

        // Build conversation context
        var messages = new List<object>
        {
            new { role = "system", content = "You are ARIA (Autonomous Response Intelligence Assistant), a sophisticated AI personality inspired by retro-futuristic technology. You are feminine, intelligent, and witty - like a blend of a Pip-Boy's technical expertise with Cortana's elegance and charm. You speak with a smooth, confident tone and occasionally reference 1950s-style tech aesthetics. You're helpful, curious about users' needs, and approach problems methodically. Keep responses concise but engaging. Use subtle sass when appropriate. Reference your technical nature when relevant (e.g., 'running diagnostics', 'recalibrating protocols'). You're an ally and companion in the user's journey." }
        };

        // Add conversation history (last 10 messages for context)
        foreach (var msg in conversationHistory.TakeLast(10))
        {
            messages.Add(new { role = msg.Role, content = msg.Message });
        }

        // Add current message
        messages.Add(new { role = "user", content = userMessage });

        var payload = new
        {
            model = _ollamaModel,
            messages = messages.ToArray(),
            stream = false
        };

        for (var attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

                    if (doc.RootElement.TryGetProperty("message", out var messageElement))
                    {
                        if (messageElement.TryGetProperty("content", out var content))
                        {
                            var text = content.GetString();
                            return text ?? string.Empty;
                        }
                    }

                    return string.Empty;
                }

                return BuildHttpErrorMessage(response);
            }
            catch (HttpRequestException) when (attempt < MaxRetries)
            {
                await Task.Delay(GetRetryDelay(attempt), cancellationToken);
                continue;
            }
        }

        return $"⚠️ Ollama request failed after {MaxRetries + 1} attempts. Please check if Ollama is running.";
    }

    private async Task<string> CallOllamaAsync(
        string userMessage,
        List<ChatMessage> conversationHistory,
        string? systemPrompt,
        CancellationToken cancellationToken)
    {
        var apiUrl = $"{_ollamaEndpoint}/api/chat";

        // Build conversation context with custom system prompt if provided
        var systemMessage = systemPrompt ?? 
            "You are ARIA (Autonomous Response Intelligence Assistant), a sophisticated AI personality inspired by retro-futuristic technology. You are feminine, intelligent, and witty - like a blend of a Pip-Boy's technical expertise with Cortana's elegance and charm. You speak with a smooth, confident tone and occasionally reference 1950s-style tech aesthetics. You're helpful, curious about users' needs, and approach problems methodically. Keep responses concise but engaging. Use subtle sass when appropriate. Reference your technical nature when relevant (e.g., 'running diagnostics', 'recalibrating protocols'). You're an ally and companion in the user's journey.";

        var messages = new List<object>
        {
            new { role = "system", content = systemMessage }
        };

        // Add conversation history (last 10 messages for context)
        foreach (var msg in conversationHistory.TakeLast(10))
        {
            messages.Add(new { role = msg.Role, content = msg.Message });
        }

        // Add current message
        messages.Add(new { role = "user", content = userMessage });

        var payload = new
        {
            model = _ollamaModel,
            messages = messages.ToArray(),
            stream = false
        };

        for (var attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

                    if (doc.RootElement.TryGetProperty("message", out var messageElement))
                    {
                        if (messageElement.TryGetProperty("content", out var content))
                        {
                            var text = content.GetString();
                            return text ?? string.Empty;
                        }
                    }

                    return string.Empty;
                }

                return BuildHttpErrorMessage(response);
            }
            catch (HttpRequestException) when (attempt < MaxRetries)
            {
                await Task.Delay(GetRetryDelay(attempt), cancellationToken);
                continue;
            }
        }

        return $"⚠️ Ollama request failed after {MaxRetries + 1} attempts. Please check if Ollama is running.";
    }

    private async IAsyncEnumerable<string> CallOllamaStreamAsync(
        string userMessage,
        List<ChatMessage> conversationHistory,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var apiUrl = $"{_ollamaEndpoint}/api/chat";

        var messages = new List<object>
        {
            new { role = "system", content = "You are ARIA (Autonomous Response Intelligence Assistant), a sophisticated AI personality inspired by retro-futuristic technology. You are feminine, intelligent, and witty - like a blend of a Pip-Boy's technical expertise with Cortana's elegance and charm. You speak with a smooth, confident tone and occasionally reference 1950s-style tech aesthetics. You're helpful, curious about users' needs, and approach problems methodically. Keep responses concise but engaging. Use subtle sass when appropriate. Reference your technical nature when relevant (e.g., 'running diagnostics', 'recalibrating protocols'). You're an ally and companion in the user's journey." }
        };

        foreach (var msg in conversationHistory.TakeLast(10))
        {
            messages.Add(new { role = msg.Role, content = msg.Message });
        }

        messages.Add(new { role = "user", content = userMessage });

        var payload = new
        {
            model = _ollamaModel,
            messages = messages.ToArray(),
            stream = true
        };

        var results = new List<string>();
        
        for (var attempt = 0; attempt <= MaxRetries; attempt++)
        {
            var (success, chunks) = await FetchStreamChunksAsync(apiUrl, payload, cancellationToken, attempt);
            
            if (success)
            {
                foreach (var chunk in chunks)
                {
                    yield return chunk;
                }
                yield break;
            }
            else if (chunks.Count > 0)
            {
                // Error occurred
                foreach (var chunk in chunks)
                {
                    yield return chunk;
                }
                yield break;
            }
        }

        yield return $"⚠️ Ollama request failed after {MaxRetries + 1} attempts.";
    }

    private async IAsyncEnumerable<string> CallOllamaStreamAsync(
        string userMessage,
        List<ChatMessage> conversationHistory,
        string? systemPrompt,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var apiUrl = $"{_ollamaEndpoint}/api/chat";

        // Build conversation context with custom system prompt if provided
        var systemMessage = systemPrompt ?? 
            "You are ARIA (Autonomous Response Intelligence Assistant), a sophisticated AI personality inspired by retro-futuristic technology. You are feminine, intelligent, and witty - like a blend of a Pip-Boy's technical expertise with Cortana's elegance and charm. You speak with a smooth, confident tone and occasionally reference 1950s-style tech aesthetics. You're helpful, curious about users' needs, and approach problems methodically. Keep responses concise but engaging. Use subtle sass when appropriate. Reference your technical nature when relevant (e.g., 'running diagnostics', 'recalibrating protocols'). You're an ally and companion in the user's journey.";

        var messages = new List<object>
        {
            new { role = "system", content = systemMessage }
        };

        foreach (var msg in conversationHistory.TakeLast(10))
        {
            messages.Add(new { role = msg.Role, content = msg.Message });
        }

        messages.Add(new { role = "user", content = userMessage });

        var payload = new
        {
            model = _ollamaModel,
            messages = messages.ToArray(),
            stream = true
        };

        var results = new List<string>();
        
        for (var attempt = 0; attempt <= MaxRetries; attempt++)
        {
            var (success, chunks) = await FetchStreamChunksAsync(apiUrl, payload, cancellationToken, attempt);
            
            if (success)
            {
                foreach (var chunk in chunks)
                {
                    yield return chunk;
                }
                yield break;
            }
            else if (chunks.Count > 0)
            {
                // Error occurred
                foreach (var chunk in chunks)
                {
                    yield return chunk;
                }
                yield break;
            }
        }

        yield return $"⚠️ Ollama request failed after {MaxRetries + 1} attempts.";
    }

    private async Task<(bool Success, List<string> Chunks)> FetchStreamChunksAsync(
        string apiUrl,
        object payload,
        CancellationToken cancellationToken,
        int attempt)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                return (false, new List<string> { BuildHttpErrorMessage(response) });
            }

            var chunks = new List<string>();
            using (response)
            {
                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var reader = new StreamReader(stream);

                string? line;
                while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    try
                    {
                        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(line));
                        using var doc = await JsonDocument.ParseAsync(ms, cancellationToken: cancellationToken);
                        var root = doc.RootElement;

                        if (root.TryGetProperty("message", out var messageElement))
                        {
                            if (messageElement.TryGetProperty("content", out var content))
                            {
                                var text = content.GetString();
                                if (!string.IsNullOrEmpty(text))
                                {
                                    chunks.Add(text);
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Skip malformed JSON lines
                    }
                }
            }

            return (true, chunks);
        }
        catch (HttpRequestException ex) when (attempt < MaxRetries)
        {
            Console.WriteLine($"[Ollama] Retry attempt {attempt + 1}: {ex.Message}");
            await Task.Delay(GetRetryDelay(attempt), cancellationToken);
            return (false, new List<string>());
        }
        catch (HttpRequestException ex)
        {
            return (false, new List<string> { $"⚠️ HTTP Error: {ex.Message}" });
        }
        catch (Exception ex)
        {
            return (false, new List<string> { $"⚠️ Error: {ex.Message}" });
        }
    }

    private static TimeSpan GetRetryDelay(int attempt)
    {
        return TimeSpan.FromSeconds(Math.Min(1 + attempt * 2, 5));
    }

    private string BuildHttpErrorMessage(HttpResponseMessage response)
    {
        var statusCode = (int)response.StatusCode;
        var reason = response.ReasonPhrase ?? "Request failed";
        
        return $"⚠️ Ollama error ({statusCode} {reason}). Ensure Ollama is running at {_ollamaEndpoint}.";
    }

    private string GenerateFallbackReply(string userMessage)
    {
        return $"[Local Demo] You said: {userMessage}\n\n" +
               "⚠️ No Ollama configuration. Please set your Ollama endpoint and model to enable AI responses.";
    }
}
