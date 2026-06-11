using MaIN.Docs.Api.Models;
using MaIN.Docs.Api.Services;
using MaIN.Domain.Entities;
using MaIN.Domain.Models;
using Microsoft.AspNetCore.RateLimiting;

namespace MaIN.Docs.Api.Endpoints;

public static class ChatEndpoints
{
    // Matches the frontend's per-call tool token estimate (toolTokenEstimate in home.ts) so the
    // capacity tracker's running total lines up with the "~X tool tokens · ~Y response" badges shown in the UI.
    private const int ToolCallTokenEstimate = 540;

    private static int TotalTokenUsage(AgentResult result) =>
        result.EstimatedTokens + result.ToolsUsed.Sum(t => t.Calls) * ToolCallTokenEstimate;

    public static void MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/chat/complete", HandleChat)
           .RequireRateLimiting("chat");

        app.MapPost("/api/confirm/review", async () =>
        {
            if (!PrTools.HasPendingReview)
                return Results.BadRequest("No pending review to submit.");
            var url = await PrTools.SubmitPendingReview();
            return Results.Ok(new { url });
        }).RequireRateLimiting("chat");

        app.MapPost("/api/confirm/code-change", async () =>
        {
            if (!PrTools.HasPendingCodeChange)
                return Results.BadRequest("No pending code change to push.");
            var pushed = await PrTools.ExecuteAllPendingCodeChanges();
            return Results.Ok(new { pushed = true, filesChanged = pushed.Count });
        }).RequireRateLimiting("chat");

        app.MapPost("/api/confirm/pr", async () =>
        {
            if (!PrTools.HasPendingPr)
                return Results.BadRequest("No pending pull request to create.");
            var url = await PrTools.ExecutePendingPr();
            return Results.Ok(new { url });
        }).RequireRateLimiting("chat");

        app.MapPost("/api/ensemble/design", async (
            EnsembleDesignRequest req,
            DocsAgentOrchestrator orchestrator,
            CapacityService capacityService,
            CancellationToken ct) =>
        {
            var messages = new[]
            {
                new Message { Role = "User", Content = req.Message, Type = MessageType.NotSet, Time = DateTime.UtcNow }
            };
            try
            {
                var result = await orchestrator.ProcessEnsembleDesignAsync(messages, ct);
                capacityService.RecordTokenUsage(TotalTokenUsage(result));
                return Results.Ok(new ChatResponse(result.Content, result.ToolsUsed, result.EstimatedTokens,
                    null, null, result.IssueProposed, result.IssueUrl, result.PlanProposed,
                    Capacity: capacityService.GetCapacityLevel(),
                    CapacityDetails: capacityService.GetStatus()));
            }
            catch (TimeoutException ex)
            {
                return Results.Problem(title: "Agent busy", detail: ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        }).RequireRateLimiting("chat");

        app.MapPost("/api/ensemble/code", async (
            EnsembleCodeRequest req,
            DocsAgentOrchestrator orchestrator,
            CapacityService capacityService,
            CancellationToken ct) =>
        {
            try
            {
                var (result, branchName, filesChanged) = await orchestrator.ProcessEnsembleCodeAsync(
                    req.OriginalMessage, req.DesignContent, ct);
                capacityService.RecordTokenUsage(TotalTokenUsage(result));
                return Results.Ok(new EnsembleCodeResponse(
                    result.Content, result.ToolsUsed, result.EstimatedTokens,
                    branchName, filesChanged, result.CodeChangeProposed, result.PrProposed,
                    capacityService.GetCapacityLevel(), capacityService.GetStatus()));
            }
            catch (TimeoutException ex)
            {
                return Results.Problem(title: "Agent busy", detail: ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        }).RequireRateLimiting("chat");

        app.MapPost("/api/ensemble/review", async (
            EnsembleReviewRequest req,
            DocsAgentOrchestrator orchestrator,
            CapacityService capacityService,
            CancellationToken ct) =>
        {
            try
            {
                var result = await orchestrator.ProcessEnsembleReviewAsync(
                    req.OriginalMessage, req.DesignContent, req.CodeContent, req.BranchName, ct);
                capacityService.RecordTokenUsage(TotalTokenUsage(result));
                return Results.Ok(new ChatResponse(
                    result.Content, result.ToolsUsed, result.EstimatedTokens,
                    null, null, null, null, null,
                    result.ReviewProposed, result.CodeChangeProposed, result.PrProposed, result.PrUrl,
                    Capacity: capacityService.GetCapacityLevel(),
                    CapacityDetails: capacityService.GetStatus()));
            }
            catch (TimeoutException ex)
            {
                return Results.Problem(title: "Agent busy", detail: ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        }).RequireRateLimiting("chat");
    }

    private static async Task<IResult> HandleChat(
        ChatRequest request,
        DocsAgentOrchestrator orchestrator,
        CapacityService capacityService,
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
            var result = await orchestrator.ProcessAsync(request.AgentId, messages, ct);
            capacityService.RecordTokenUsage(TotalTokenUsage(result));
            return Results.Ok(new ChatResponse(result.Content, result.ToolsUsed, result.EstimatedTokens, result.ArtifactUrl, result.ArtifactProposed, result.IssueProposed, result.IssueUrl, result.PlanProposed, result.ReviewProposed, result.CodeChangeProposed, result.PrProposed, result.PrUrl, result.ReviewPosted, result.DocsRead,
                Capacity: capacityService.GetCapacityLevel(),
                CapacityDetails: capacityService.GetStatus()));
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

        if (request.DocsAlreadyRead is { Count: > 0 } docsAlreadyRead)
        {
            var memoryMessage = new Message
            {
                Role = "User",
                Content = $"[Conversation memory] You already read these doc files earlier in this " +
                          $"conversation: {string.Join(", ", docsAlreadyRead)}. Do not call list_docs, " +
                          "search_md_files, or read_md_file for these files again unless you need different " +
                          "sections, or the new request needs a file not in this list.",
                Type = MessageType.NotSet,
                Time = DateTime.UtcNow
            };
            return history.Append(memoryMessage).Append(userMessage);
        }

        return history.Append(userMessage);
    }
}
