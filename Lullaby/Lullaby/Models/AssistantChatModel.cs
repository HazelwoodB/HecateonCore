using Hecateon.Services;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hecateon.Models;

/// <summary>
/// Comprehensive assistant chat model that orchestrates all LLM and ML capabilities.
/// Provides ChatGPT-level functionality including context management, sentiment analysis,
/// streaming responses, and conversation history.
/// </summary>
public class AssistantChatModel
{
    private readonly LLMAssistantService _llmService;
    private readonly SimpleSentimentModel _sentimentModel;
    private readonly ChatLogService _chatLogService;
    private readonly Hecateon.Client.Services.Foundation.ConversationalNaturalnessEngine _naturalnessEngine;
    private readonly Hecateon.Client.Services.Foundation.PreferenceManager _preferenceManager;

    private List<ChatMessage> _conversationContext = [];
    private const int MaxContextMessages = 20; // Keep last N messages for context

    public AssistantChatModel(
        LLMAssistantService llmService,
        SimpleSentimentModel sentimentModel,
        ChatLogService chatLogService,
        Hecateon.Client.Services.Foundation.ConversationalNaturalnessEngine naturalnessEngine,
        Hecateon.Client.Services.Foundation.PreferenceManager preferenceManager)
    {
        _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
        _sentimentModel = sentimentModel ?? throw new ArgumentNullException(nameof(sentimentModel));
        _chatLogService = chatLogService ?? throw new ArgumentNullException(nameof(chatLogService));
        _naturalnessEngine = naturalnessEngine ?? throw new ArgumentNullException(nameof(naturalnessEngine));
        _preferenceManager = preferenceManager ?? throw new ArgumentNullException(nameof(preferenceManager));
    }

    /// <summary>
    /// Initializes the conversation context from stored history.
    /// </summary>
    public async Task InitializeContextAsync(int contextLimit = MaxContextMessages)
    {
        var history = await _chatLogService.GetHistoryAsync(contextLimit);
        _conversationContext = history.ToList();
    }

    /// <summary>
    /// Initializes the conversation context from stored history (synchronous).
    /// </summary>
    public void InitializeContext(int contextLimit = MaxContextMessages)
    {
        var history = _chatLogService.GetHistory(contextLimit);
        _conversationContext = history.ToList();
    }

    /// <summary>
    /// Processes a user message and generates an assistant response.
    /// Includes sentiment analysis and context management.
    /// </summary>
    public async Task<ChatResponse> ProcessUserMessageAsync(
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
            throw new ArgumentException("Message cannot be empty.", nameof(userMessage));

        // Create and log user message
        var userMsg = CreateUserMessage(userMessage);
        await _chatLogService.AddMessageAsync(userMsg);
        _conversationContext.Add(userMsg);
        MaintainContextWindow();

        // === 1. Observation: Fetch system state ===
        var nyphosEngine = (Hecateon.Services.NyphosRiskEngine?)AppServices.GetService(typeof(Hecateon.Services.NyphosRiskEngine));
        var prometheonEngine = (Hecateon.Modules.Prometheon.Services.IPrometheronEngine?)AppServices.GetService(typeof(Hecateon.Modules.Prometheon.Services.IPrometheronEngine));
        var nyphosAssessment = nyphosEngine != null ? await nyphosEngine.CalculateRiskStateAsync(7, cancellationToken) : null;
        var prometheonState = prometheonEngine != null ? await prometheonEngine.GetCurrentStateAsync() : null;

        // === 2. Interpretation: Build explainable summary ===
        string observation = nyphosAssessment != null
            ? $"System state: {nyphosAssessment.CurrentState} (Risk {nyphosAssessment.RiskScore}/100). Key factors: {string.Join(", ", nyphosAssessment.ContributingFactors.Select(f => f.Factor))}."
            : "System state: unavailable.";
        string interpretation = nyphosAssessment != null
            ? nyphosAssessment.StateExplanation ?? "No interpretation available."
            : "No interpretation available.";

        // === 3. Recommendation: Strategic action ===
        string recommendation = nyphosAssessment != null && nyphosAssessment.RecommendedActions.Any()
            ? $"Recommended action: {nyphosAssessment.RecommendedActions[0]}"
            : "No specific recommendation.";

        // === 4. Deterministic options ===
        string options = nyphosAssessment != null && nyphosAssessment.RecommendedActions.Count > 1
            ? $"Other options: {string.Join(" | ", nyphosAssessment.RecommendedActions.Skip(1))}"
            : "No additional options.";

        // Escalate tone if risk is high
        string mode = nyphosAssessment != null ? nyphosAssessment.CurrentState.ToString() : "Unknown";
        string tone = mode switch
        {
            "Red" => "[Crisis mode: Direct, urgent, safety-first.]",
            "Orange" => "[Protective mode: Firm, stabilizing, proactive.]",
            "Yellow" => "[Analytical mode: Cautious, attentive, advisory.]",
            "Green" => "[Advisory mode: Calm, supportive, precise.]",
            _ => "[Default mode: Calm, precise.]"
        };

        // Compose structured context for LLM
        var structuredPrompt = $@"{tone}
Observation: {observation}
Interpretation: {interpretation}
Recommendation: {recommendation}
Deterministic options: {options}

User message: {userMessage}
";

        // Use narrative-fitting system prompt
        var systemPrompt = _naturalnessEngine.GenerateHumanLikeSystemPrompt(null, null);

        // Generate assistant response with structured context
        var reply = await _llmService.GenerateReplyAsync(structuredPrompt, cancellationToken)
            .ConfigureAwait(false);

        // Post-process with ConversationalNaturalnessEngine
        var humanizedReply = _naturalnessEngine.HumanizeResponse(reply, null);

        // Create and log assistant message
        var assistantMsg = CreateAssistantMessage(humanizedReply);
        await _chatLogService.AddMessageAsync(assistantMsg);
        _conversationContext.Add(assistantMsg);
        MaintainContextWindow();

        return new ChatResponse
        {
            Reply = humanizedReply,
            Sentiment = userMsg.Sentiment ?? string.Empty,
            Score = userMsg.Score ?? 0
        };
    }

    /// <summary>
    /// Streams a response from the assistant, yielding chunks as they arrive.
    /// Useful for real-time UI updates.
    /// </summary>
    public async IAsyncEnumerable<string> ProcessUserMessageStreamAsync(
        string userMessage,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
            throw new ArgumentException("Message cannot be empty.", nameof(userMessage));

        // Create and log user message
        var userMsg = CreateUserMessage(userMessage);
        await _chatLogService.AddMessageAsync(userMsg);
        _conversationContext.Add(userMsg);
        MaintainContextWindow();

        // Stream assistant response
        var fullResponse = new System.Text.StringBuilder();
        await foreach (var chunk in _llmService.GenerateReplyStreamAsync(userMessage, cancellationToken)
            .ConfigureAwait(false))
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            fullResponse.Append(chunk);
            yield return chunk;
        }

        // Log complete assistant message
        var assistantMsg = CreateAssistantMessage(fullResponse.ToString());
        await _chatLogService.AddMessageAsync(assistantMsg);
        _conversationContext.Add(assistantMsg);
        MaintainContextWindow();
    }

    /// <summary>
    /// Gets the current conversation history from context.
    /// </summary>
    public IReadOnlyList<ChatMessage> GetConversationHistory()
    {
        return _conversationContext.AsReadOnly();
    }

    /// <summary>
    /// Loads historical conversation for resume/continuation.
    /// </summary>
    public async Task LoadConversationHistoryAsync(int limit = MaxContextMessages)
    {
        await InitializeContextAsync(limit);
    }

    /// <summary>
    /// Loads historical conversation for resume/continuation (synchronous).
    /// </summary>
    public void LoadConversationHistory(int limit = MaxContextMessages)
    {
        InitializeContext(limit);
    }

    /// <summary>
    /// Clears the in-memory conversation context.
    /// Note: Does not clear persistent chat logs.
    /// </summary>
    public void ClearContext()
    {
        _conversationContext.Clear();
    }

    /// <summary>
    /// Gets full conversation history from persistent storage.
    /// </summary>
    public IEnumerable<ChatMessage> GetFullHistory(int limit = 200)
    {
        return _chatLogService.GetHistory(limit);
    }

    /// <summary>
    /// Analyzes sentiment of a given text.
    /// </summary>
    public SentimentAnalysis AnalyzeSentiment(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text cannot be empty.", nameof(text));

        var sentiment = _sentimentModel.Predict(text);
        return new SentimentAnalysis
        {
            Label = sentiment.Label,
            Score = sentiment.Score,
            IsPositive = sentiment.Label == "Positive"
        };
    }

    /// <summary>
    /// Gets conversation statistics.
    /// </summary>
    public ConversationStats GetConversationStats()
    {
        var history = _conversationContext;
        var userMessages = history.Where(m => m.Role == "user").ToList();
        var assistantMessages = history.Where(m => m.Role == "assistant").ToList();

        var avgUserSentiment = userMessages
            .Where(m => m.Score.HasValue)
            .Select(m => (double)m.Score!.Value)
            .DefaultIfEmpty(0.0)
            .Average();

        var positiveUserMessages = userMessages
            .Count(m => m.Sentiment == "Positive");

        return new ConversationStats(history.Count, userMessages.Count, assistantMessages.Count, float.IsNaN((float)avgUserSentiment) ? 0f : (float)avgUserSentiment, positiveUserMessages, history.Count > 0
                ? (history.Last().Timestamp - history.First().Timestamp)
                : TimeSpan.Zero);
    }

    // Private helper methods

    private ChatMessage CreateUserMessage(string message)
    {
        var sentiment = _sentimentModel.Predict(message);
        return new ChatMessage
        {
            Role = "user",
            Message = message,
            Timestamp = DateTime.UtcNow,
            Sentiment = sentiment.Label,
            Score = sentiment.Score
        };
    }

    private ChatMessage CreateAssistantMessage(string message)
    {
        return new ChatMessage
        {
            Role = "assistant",
            Message = message,
            Timestamp = DateTime.UtcNow
        };
    }

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
/// Represents sentiment analysis results.
/// </summary>
public class SentimentAnalysis
{
    public string Label { get; set; } = string.Empty;
    public float Score { get; set; }
    public bool IsPositive { get; set; }
}

/// <summary>
/// Represents conversation statistics.
/// </summary>
/// <param name="TotalMessages"></param>
/// <param name="UserMessageCount"></param>
/// <param name="AssistantMessageCount"></param>
/// <param name="AverageUserSentiment"></param>
/// <param name="PositiveUserMessageCount"></param>
/// <param name="SessionDuration"></param>
public record ConversationStats(int TotalMessages, int UserMessageCount, int AssistantMessageCount, float AverageUserSentiment, int PositiveUserMessageCount, TimeSpan SessionDuration);
