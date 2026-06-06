namespace MaIN.Docs.Api.Models;

public record HistoryMessage(string Role, string Content);
public record ChatRequest(string AgentId, string Message, List<HistoryMessage> History);
public record ChatResponse(string Text);
