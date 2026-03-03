using Lullaby.Client.Services;
using Lullaby.Client.Services.Foundation;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Configure HttpClient with the server base address
// The BaseAddress will be automatically set to the host environment's base address
// which in a hosted Blazor WASM app is the server URL
builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress),
    Timeout = TimeSpan.FromSeconds(30)
});

// Register foundation services
builder.Services.AddScoped<StorageService>();
builder.Services.AddScoped<DeviceIdentityService>();
builder.Services.AddScoped<UserProfileService>();
builder.Services.AddScoped<PreferenceManager>();
builder.Services.AddScoped<FeedbackCollector>();

// Register advanced intelligence services
builder.Services.AddScoped<EmotionRecognitionService>();
builder.Services.AddScoped<PersonalityEngine>();
builder.Services.AddScoped<ConversationalMemoryService>();
builder.Services.AddScoped<RetroThemeService>();
builder.Services.AddScoped<ConversationalNaturalnessEngine>(); // Turing test enhancement

// Register chat services
builder.Services.AddScoped<ClientLLMService>();
builder.Services.AddScoped<ClientSentimentService>();
builder.Services.AddScoped<ClientChatManager>();

await builder.Build().RunAsync();
