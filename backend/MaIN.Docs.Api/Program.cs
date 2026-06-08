using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using MaIN.Core;
using MaIN.Core.Hub;
using MaIN.Docs.Api.Endpoints;
using MaIN.Docs.Api.Middleware;
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
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy
            .WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                ?? ["http://localhost:4200"])
            .AllowAnyHeader()
            .AllowAnyMethod());
});

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

var app = builder.Build();

app.UseCors();
app.UseMiddleware<ApiKeyMiddleware>();
app.UseRateLimiter();

app.Services.UseMaIN();

var orchestrator = app.Services.GetRequiredService<DocsAgentOrchestrator>();
await orchestrator.InitializeAsync();

app.MapChatEndpoints();

app.Run();
