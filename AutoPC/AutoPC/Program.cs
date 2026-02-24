using AutoPC.Client.Pages;
using AutoPC.Components;
using AutoPC.Models;
using AutoPC.Services;
using AutoPC.Data;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public partial class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveWebAssemblyComponents();

        // Add database context
        builder.Services.AddDbContext<ChatDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("ChatDb")));

        // Register ML model service
        builder.Services.AddSingleton<SimpleSentimentModel>();
        builder.Services.AddHttpClient("llm");
        builder.Services.AddSingleton<LLMAssistantService>();
        builder.Services.AddScoped<ChatLogService>();
        builder.Services.AddScoped<AssistantChatModel>();

        WebApplication app = builder.Build();

        // Initialize database on startup
        app.Services.InitializeDatabase();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        app.UseHttpsRedirection();

        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveWebAssemblyRenderMode()
            .AddAdditionalAssemblies(typeof(AutoPC.Client._Imports).Assembly);

        // Minimal API endpoints for chatbot and model prediction

        // Debug endpoint
        app.MapGet("/api/test", () =>
        {
            return Results.Ok(new { message = "API is reachable (client-side processing mode)", timestamp = DateTime.UtcNow });
        });

        // Legacy endpoints kept for compatibility but note they're not used in client-side mode
        app.MapPost("/api/chat", async ([FromServices] AssistantChatModel model, [FromBody] ChatRequest req) =>
        {
            if (req is null || string.IsNullOrWhiteSpace(req.Message))
            {
                return Results.BadRequest(new { error = "Message is required." });
            }

            try
            {
                var response = await model.ProcessUserMessageAsync(req.Message).ConfigureAwait(false);
                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                return Results.StatusCode(500);
            }
        });

        // NEW: Get all messages (for cross-client sync)
        app.MapGet("/api/history", ([FromServices] ChatLogService chatLogService) =>
        {
            try
            {
                var history = chatLogService.GetHistory(200);
                return Results.Ok(history);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error in /api/history: {ex}");
                return Results.StatusCode(500);
            }
        });

        // NEW: Sync single message to server
        app.MapPost("/api/messages/sync", ([FromServices] ChatLogService chatLogService, [FromBody] ChatMessage message) =>
        {
            if (message is null)
            {
                return Results.BadRequest(new { error = "Message is required." });
            }

            try
            {
                chatLogService.AddMessage(message);
                Console.WriteLine($"[API] Message synced: {message.Role} - {message.Message.Substring(0, Math.Min(50, message.Message.Length))}");
                return Results.Ok(new { id = message.Id, synced = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error in /api/messages/sync: {ex}");
                return Results.StatusCode(500);
            }
        });

        // NEW: Sync batch of messages
        app.MapPost("/api/messages/sync-batch", ([FromServices] ChatLogService chatLogService, [FromBody] List<ChatMessage> messages) =>
        {
            if (messages is null || messages.Count == 0)
            {
                return Results.BadRequest(new { error = "Messages are required." });
            }

            try
            {
                foreach (var msg in messages)
                {
                    chatLogService.AddMessage(msg);
                }
                Console.WriteLine($"[API] Batch sync: {messages.Count} messages");
                return Results.Ok(new { synced = messages.Count });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error in /api/messages/sync-batch: {ex}");
                return Results.StatusCode(500);
            }
        });

        // NEW: Get messages for specific user or conversation
        app.MapGet("/api/messages", ([FromServices] ChatLogService chatLogService, [FromQuery] int limit = 100) =>
        {
            try
            {
                var messages = chatLogService.GetHistory(limit);
                return Results.Ok(messages);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error in /api/messages: {ex}");
                return Results.StatusCode(500);
            }
        });

        // NEW: Get single message by ID
        app.MapGet("/api/messages/{id:guid}", ([FromServices] ChatLogService chatLogService, Guid id) =>
        {
            try
            {
                var message = chatLogService.GetHistory(500).FirstOrDefault(m => m.Id == id);
                if (message is null)
                    return Results.NotFound();
                return Results.Ok(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error in /api/messages/{{id}}: {ex}");
                return Results.StatusCode(500);
            }
        });

        // Legacy streaming endpoint (not used in new architecture)
        app.MapPost("/api/chat/stream", async (HttpContext http, [FromServices] AssistantChatModel model, [FromBody] ChatRequest req) =>
        {
            if (req is null || string.IsNullOrWhiteSpace(req.Message))
            {
                http.Response.StatusCode = 400;
                await http.Response.WriteAsync("Message is required.");
                return;
            }

            http.Response.ContentType = "text/plain; charset=utf-8";
            http.Response.Headers.CacheControl = "no-cache";

            try
            {
                await foreach (var chunk in model.ProcessUserMessageStreamAsync(req.Message, http.RequestAborted))
                {
                    if (http.RequestAborted.IsCancellationRequested)
                        break;

                    var bytes = System.Text.Encoding.UTF8.GetBytes(chunk);
                    await http.Response.Body.WriteAsync(bytes, http.RequestAborted).ConfigureAwait(false);
                    await http.Response.Body.FlushAsync(http.RequestAborted).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // client cancelled
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error in /api/chat/stream: {ex}");
            }
        });

        // Legacy sentiment endpoint
        app.MapPost("/api/predict", ([FromServices] AssistantChatModel model, [FromBody] ChatRequest req) =>
        {
            if (req is null || string.IsNullOrWhiteSpace(req.Message))
            {
                return Results.BadRequest(new { error = "Message is required." });
            }

            try
            {
                var sentiment = model.AnalyzeSentiment(req.Message);
                return Results.Ok(sentiment);
            }
            catch (Exception ex)
            {
                return Results.StatusCode(500);
            }
        });

        app.MapGet("/api/stats", ([FromServices] AssistantChatModel model) =>
        {
            var stats = model.GetConversationStats();
            return Results.Ok(stats);
        });

        app.MapPost("/api/chat/init-context", ([FromServices] AssistantChatModel model) =>
        {
            model.LoadConversationHistory();
            return Results.Ok(new { message = "Context loaded successfully." });
        });

        app.MapPost("/api/chat/clear-context", ([FromServices] AssistantChatModel model) =>
        {
            model.ClearContext();
            return Results.Ok(new { message = "Context cleared." });
        });

        app.Run();
    }
}