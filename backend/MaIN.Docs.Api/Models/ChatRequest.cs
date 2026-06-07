namespace MaIN.Docs.Api.Models;

public record HistoryMessage(string Role, string Content);
public record ChatRequest(string AgentId, string Message, List<HistoryMessage> History);
public record ToolUsage(string Name, int Calls);
public record ArtifactProposal(string ArchiveName, string Description);
public record AgentResult(string Content, List<ToolUsage> ToolsUsed, int EstimatedTokens, string? ArtifactUrl = null, ArtifactProposal? ArtifactProposed = null);
public record ChatResponse(string Text, List<ToolUsage> ToolsUsed, int EstimatedTokens, string? ArtifactUrl = null, ArtifactProposal? ArtifactProposed = null);
