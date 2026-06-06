using MaIN.Docs.Api.Models;
using MaIN.Docs.Api.Services;
using MaIN.Domain.Entities;
using MaIN.Domain.Models;
using Microsoft.AspNetCore.RateLimiting;

namespace MaIN.Docs.Api.Endpoints;

public static class ChatEndpoints
{
    public static void MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/chat/complete", HandleChat)
           .RequireRateLimiting("chat");
    }

    private static async Task<IResult> HandleChat(
        ChatRequest request,
        DocsAgentOrchestrator orchestrator,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.AgentId) || string.IsNullOrWhiteSpace(request.Message))
            return Results.BadRequest("agentId and message are required.");

        if (!orchestrator.IsAvailable(request.AgentId))
            return Results.Problem(
                title: "Agent unavailable",
                detail: $"Agent '{request.AgentId}' is not configured. Check the API key for its backend.",
                statusCode: StatusCodes.Status503ServiceUnavailable);

        try
        {
            var messages = BuildMessages(request);
            var text = await orchestrator.ProcessAsync(request.AgentId, messages, ct);
            return Results.Ok(new ChatResponse(text));
        }
        catch (TimeoutException ex)
        {
            return Results.Problem(title: "Agent busy", detail: ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    private static IEnumerable<Message> BuildMessages(ChatRequest request)
    {
        var history = request.History.Select(h => new Message
        {
            Role = h.Role.Equals("user", StringComparison.OrdinalIgnoreCase) ? "User" : "Assistant",
            Content = h.Content,
            Type = MessageType.NotSet,
            Time = DateTime.UtcNow
        });

        var userMessage = new Message
        {
            Role = "User",
            Content = request.Message,
            Type = MessageType.NotSet,
            Time = DateTime.UtcNow
        };

        return history.Append(userMessage);
    }
}
