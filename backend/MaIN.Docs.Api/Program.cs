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
builder.Services.AddSingleton<DocsAgentOrchestrator>();

var app = builder.Build();

app.UseCors();
app.UseMiddleware<ApiKeyMiddleware>();
app.UseRateLimiter();

app.Services.UseMaIN();
AIHub.Extensions.DisableLLamaLogs();

var orchestrator = app.Services.GetRequiredService<DocsAgentOrchestrator>();
await orchestrator.InitializeAsync();

app.MapChatEndpoints();

app.Run();
