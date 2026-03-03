using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Hecateon.Client;
using Hecateon.Client.Services.Foundation;

namespace Hecateon.Client.Services;

/// <summary>
/// Manages chat operations entirely on the client side.
/// Uses server only for persistence and cross-client sync.
/// </summary>
public class ClientChatManager
{
    private readonly ClientLLMService _llmService;
    private readonly ClientSentimentService _sentimentService;
    private readonly HttpClient _httpClient;
    private readonly StorageService _storageService;
    private readonly DeviceIdentityService _deviceIdentityService;

    private List<ChatMessage> _conversationContext = [];
    private readonly ConcurrentDictionary<string, ChatMessage> _messageCache = new();
    private readonly List<ChatMessage> _pendingSyncQueue = [];
    private readonly SemaphoreSlim _syncLock = new(1, 1);
    private const int MaxContextMessages = 20;
    private const int MaxPendingQueueMessages = 500;
    private const string PendingSyncStorageKey = "pending_sync_messages";

    public bool IsServerAvailable { get; private set; } = true;
    public int PendingSyncCount => _pendingSyncQueue.Count;

    public ClientChatManager(
        ClientLLMService llmService,
        ClientSentimentService sentimentService,
        HttpClient httpClient,
        StorageService storageService,
        DeviceIdentityService deviceIdentityService)
    {
        _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
        _sentimentService = sentimentService ?? throw new ArgumentNullException(nameof(sentimentService));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _deviceIdentityService = deviceIdentityService ?? throw new ArgumentNullException(nameof(deviceIdentityService));
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _deviceIdentityService.InitializeAsync(cancellationToken);

        var pending = await _storageService.LoadAsync<List<ChatMessage>>(PendingSyncStorageKey, cancellationToken) ?? [];
        _pendingSyncQueue.Clear();
        _pendingSyncQueue.AddRange(pending.Where(x => x != null));

        await SyncPendingMessagesAsync(cancellationToken);
    }

    /// <summary>
    /// Process a user message and generate an AI response.
    /// Runs entirely on client; optionally syncs to server.
    /// </summary>
    public async Task<ClientChatResponse> ProcessMessageAsync(
        string userMessage,
        bool syncToServer = true,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
            throw new ArgumentException("Message cannot be empty.", nameof(userMessage));

        // Analyze user sentiment
        var sentimentResult = _sentimentService.AnalyzeSentiment(userMessage);

        // Create user message
        var userMsg = new ChatMessage
        {
            Id = Guid.NewGuid(),
            Role = "user",
            Message = userMessage,
            Timestamp = DateTime.UtcNow,
            Sentiment = sentimentResult.Label,
            Score = sentimentResult.Score
        };

        // Add to local context
        _conversationContext.Add(userMsg);
        _messageCache.TryAdd(userMsg.Id.ToString(), userMsg);
        MaintainContextWindow();

        // Optional: Sync to server
        if (syncToServer)
        {
            await SyncMessageToServerAsync(userMsg, cancellationToken);
        }

        // Generate AI response
        var aiResponse = await _llmService.GenerateReplyAsync(
            userMessage,
            _conversationContext,
            cancellationToken);

        // Create assistant message
        var assistantMsg = new ChatMessage
        {
            Id = Guid.NewGuid(),
            Role = "assistant",
            Message = aiResponse,
            Timestamp = DateTime.UtcNow,
            Sentiment = null,
            Score = null
        };

        // Add to local context
        _conversationContext.Add(assistantMsg);
        _messageCache.TryAdd(assistantMsg.Id.ToString(), assistantMsg);
        MaintainContextWindow();

        // Optional: Sync to server
        if (syncToServer)
        {
            await SyncMessageToServerAsync(assistantMsg, cancellationToken);
        }

        return new ClientChatResponse
        {
            UserMessage = userMsg,
            AssistantMessage = assistantMsg,
            Sentiment = sentimentResult.Label,
            Score = sentimentResult.Score
        };
    }

    /// <summary>
    /// Process message with streaming response and optional custom system prompt.
    /// </summary>
    public async IAsyncEnumerable<string> ProcessMessageStreamAsync(
        string userMessage,
        string? systemPrompt = null,
        bool syncToServer = true,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
            throw new ArgumentException("Message cannot be empty.", nameof(userMessage));

        // Analyze user sentiment
        var sentimentResult = _sentimentService.AnalyzeSentiment(userMessage);

        // Create and store user message
        var userMsg = new ChatMessage
        {
            Id = Guid.NewGuid(),
            Role = "user",
            Message = userMessage,
            Timestamp = DateTime.UtcNow,
            Sentiment = sentimentResult.Label,
            Score = sentimentResult.Score
        };

        _conversationContext.Add(userMsg);
        _messageCache.TryAdd(userMsg.Id.ToString(), userMsg);
        MaintainContextWindow();

        if (syncToServer)
        {
            await SyncMessageToServerAsync(userMsg, cancellationToken);
        }

        // Stream AI response with custom system prompt
        var fullResponse = new StringBuilder();
        var assistantMsgId = Guid.NewGuid();

        await foreach (var chunk in _llmService.GenerateReplyStreamAsync(
            userMessage,
            _conversationContext,
            systemPrompt,
            cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            fullResponse.Append(chunk);
            yield return chunk;
        }

        // Store complete assistant message
        var assistantMsg = new ChatMessage
        {
            Id = assistantMsgId,
            Role = "assistant",
            Message = fullResponse.ToString(),
            Timestamp = DateTime.UtcNow,
            Sentiment = null,
            Score = null
        };

        _conversationContext.Add(assistantMsg);
        _messageCache.TryAdd(assistantMsg.Id.ToString(), assistantMsg);
        MaintainContextWindow();

        if (syncToServer)
        {
            await SyncMessageToServerAsync(assistantMsg, cancellationToken);
        }
    }

    /// <summary>
    /// Load conversation history from server.
    /// </summary>
    public async Task LoadHistoryFromServerAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            var deviceId = await _deviceIdentityService.GetDeviceIdAsync(cancellationToken);
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/history");
            request.Headers.TryAddWithoutValidation("X-Device-Id", deviceId);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var messages = JsonSerializer.Deserialize<List<ChatMessage>>(
                    json,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web));

                if (messages != null)
                {
                    _conversationContext = messages.TakeLast(limit).ToList();
                    foreach (var msg in _conversationContext)
                    {
                        _messageCache.TryAdd(msg.Id.ToString(), msg);
                    }
                }

                IsServerAvailable = true;
                await SyncPendingMessagesAsync(cancellationToken);
            }
            else
            {
                IsServerAvailable = false;
            }
        }
        catch (Exception ex)
        {
            IsServerAvailable = false;
            Console.WriteLine($"[ClientChat] Error loading history: {ex.Message}");
        }
    }

    /// <summary>
    /// Clear local conversation context.
    /// </summary>
    public void ClearContext()
    {
        _conversationContext.Clear();
        _messageCache.Clear();
    }

    /// <summary>
    /// Get current conversation context.
    /// </summary>
    public IReadOnlyList<ChatMessage> GetContext()
    {
        return _conversationContext.AsReadOnly();
    }

    /// <summary>
    /// Sync a message to the server for persistence.
    /// </summary>
    private async Task SyncMessageToServerAsync(ChatMessage message, CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(message);
            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            var deviceId = await _deviceIdentityService.GetDeviceIdAsync(cancellationToken);
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/messages/sync")
            {
                Content = content
            };
            request.Headers.TryAddWithoutValidation("X-Device-Id", deviceId);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                IsServerAvailable = false;
                await EnqueuePendingMessageAsync(message, cancellationToken);
                Console.WriteLine($"[ClientChat] Failed to sync message: {response.StatusCode}");
                return;
            }

            IsServerAvailable = true;
            await SyncPendingMessagesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            IsServerAvailable = false;
            await EnqueuePendingMessageAsync(message, cancellationToken);
            Console.WriteLine($"[ClientChat] Error syncing message: {ex.Message}");
        }
    }

    private async Task EnqueuePendingMessageAsync(ChatMessage message, CancellationToken cancellationToken)
    {
        if (_pendingSyncQueue.Any(m => m.Id == message.Id))
        {
            return;
        }

        _pendingSyncQueue.Add(message);

        if (_pendingSyncQueue.Count > MaxPendingQueueMessages)
        {
            _pendingSyncQueue.RemoveRange(0, _pendingSyncQueue.Count - MaxPendingQueueMessages);
        }

        await SavePendingQueueAsync(cancellationToken);
    }

    private async Task SyncPendingMessagesAsync(CancellationToken cancellationToken)
    {
        if (_pendingSyncQueue.Count == 0)
        {
            return;
        }

        await _syncLock.WaitAsync(cancellationToken);
        try
        {
            if (_pendingSyncQueue.Count == 0)
            {
                return;
            }

            var payload = _pendingSyncQueue
                .GroupBy(m => m.Id)
                .Select(g => g.First())
                .ToList();

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var deviceId = await _deviceIdentityService.GetDeviceIdAsync(cancellationToken);
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/messages/sync-batch")
            {
                Content = content
            };
            request.Headers.TryAddWithoutValidation("X-Device-Id", deviceId);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                IsServerAvailable = false;
                return;
            }

            IsServerAvailable = true;
            _pendingSyncQueue.Clear();
            await SavePendingQueueAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            IsServerAvailable = false;
            Console.WriteLine($"[ClientChat] Error syncing pending queue: {ex.Message}");
        }
        finally
        {
            _syncLock.Release();
        }
    }

    private async Task SavePendingQueueAsync(CancellationToken cancellationToken)
    {
        await _storageService.SaveAsync(PendingSyncStorageKey, _pendingSyncQueue.ToList(), cancellationToken);
    }

    /// <summary>
    /// Maintain conversation context window size.
    /// </summary>
    private void MaintainContextWindow()
    {
        if (_conversationContext.Count > MaxContextMessages)
        {
            var toRemove = _conversationContext.Count - MaxContextMessages;
            _conversationContext.RemoveRange(0, toRemove);
        }
    }
}

/// <summary>
/// Response from processing a message.
/// </summary>
public class ClientChatResponse
{
    public ChatMessage UserMessage { get; set; } = null!;
    public ChatMessage AssistantMessage { get; set; } = null!;
    public string Sentiment { get; set; } = string.Empty;
    public float Score { get; set; }
}
