using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using MaIN.Core;
using MaIN.Core.Hub;
using MaIN.Docs.Api.Endpoints;
using MaIN.Docs.Api.Extensions;
using MaIN.Docs.Api.Middleware;
using MaIN.Docs.Api.Models;
using MaIN.Docs.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMaIN(builder.Configuration);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddSlidingWindowLimiter("chat", limiter =>
    {
        limiter.PermitLimit = builder.Configuration.GetValue("RateLimit:RequestsPerMinute", 20);
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.SegmentsPerWindow = 4;
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit = 5;
    });
    options.AddSlidingWindowLimiter("session", limiter =>
    {
        limiter.PermitLimit = builder.Configuration.GetValue("RateLimit:SessionRequestsPerMinute", 10);
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.SegmentsPerWindow = 4;
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit = 2;
    });
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy
            .WithOrigins(builder.Configuration.GetAllowedOrigins())
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.Configure<CapacitySettings>(builder.Configuration.GetSection("Capacity"));
builder.Services.Configure<ModelSettings>(builder.Configuration.GetSection("Models"));
builder.Services.AddSingleton<CapacityStateStore>();
builder.Services.AddSingleton<CapacityService>();
builder.Services.AddHostedService<CapacityPersistenceService>();

builder.Services.AddSingleton<DocsLoader>();
builder.Services.AddSingleton<ArtifactService>();
builder.Services.AddHttpClient<GitHubService>((sp, client) =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri("https://api.github.com");
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", cfg["GITHUB_TOKEN"]);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("MaIN.Docs/1.0");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
});
builder.Services.AddSingleton<DocsAgentOrchestrator>();
builder.Services.AddHostedService<IssueCleanupService>();

builder.Services.AddSingleton<SessionTokenService>();
builder.Services.AddHttpClient<TurnstileService>(client =>
{
    client.BaseAddress = new Uri("https://challenges.cloudflare.com/");
});

var sessionSecret = builder.Configuration["SessionSecret"];
if (!builder.Environment.IsDevelopment() && string.IsNullOrEmpty(sessionSecret))
{
    throw new InvalidOperationException(
        "SessionSecret must be configured (set the SESSION_SECRET environment variable) in non-Development environments.");
}

var app = builder.Build();

app.UseCors();
app.UseMiddleware<SessionTokenMiddleware>();
app.UseRateLimiter();

app.Services.UseMaIN();

var capacityService = app.Services.GetRequiredService<CapacityService>();
await capacityService.LoadStateAsync();

var orchestrator = app.Services.GetRequiredService<DocsAgentOrchestrator>();
await orchestrator.InitializeAsync();

app.MapChatEndpoints();
app.MapSessionEndpoints();

app.Run();
