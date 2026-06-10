namespace MaIN.Docs.Api.Models;

public record HistoryMessage(string Role, string Content);
public record ChatRequest(string AgentId, string Message, List<HistoryMessage> History, List<string>? DocsAlreadyRead = null);
public record ToolUsage(string Name, int Calls);
public record ArtifactProposal(string ArchiveName, string Description, string Kind);
public record IssueProposal(string Title, string Body);
public record PlanStep(string Title, string Description, string? CodeSnippet = null, string? Language = null);
public record PlanProposal(string Title, string Context, List<PlanStep> Steps);
public record PrReviewProposal(int PrNumber, string Verdict, string Summary, int CommentCount);
public record CodeChangeProposal(string Branch, string FilePath, string CommitMessage, string Rationale, string Preview);
public record PrProposal(string Title, string Body, string HeadBranch, string BaseBranch);
public record ReviewPosted(int PrNumber, string Verdict, string Summary, int CommentCount, string Url);
public record AgentResult(string Content, List<ToolUsage> ToolsUsed, int EstimatedTokens,
    string? ArtifactUrl = null, ArtifactProposal? ArtifactProposed = null,
    IssueProposal? IssueProposed = null, string? IssueUrl = null,
    PlanProposal? PlanProposed = null,
    PrReviewProposal? ReviewProposed = null, CodeChangeProposal? CodeChangeProposed = null,
    PrProposal? PrProposed = null, string? PrUrl = null,
    ReviewPosted? ReviewPosted = null,
    List<string>? DocsRead = null);
public record ChatResponse(string Text, List<ToolUsage> ToolsUsed, int EstimatedTokens,
    string? ArtifactUrl = null, ArtifactProposal? ArtifactProposed = null,
    IssueProposal? IssueProposed = null, string? IssueUrl = null,
    PlanProposal? PlanProposed = null,
    PrReviewProposal? ReviewProposed = null, CodeChangeProposal? CodeChangeProposed = null,
    PrProposal? PrProposed = null, string? PrUrl = null,
    ReviewPosted? ReviewPosted = null,
    List<string>? DocsRead = null);
public record EnsembleDesignRequest(string Message, List<HistoryMessage> History);
public record EnsembleCodeRequest(string OriginalMessage, string DesignContent);
public record EnsembleReviewRequest(string OriginalMessage, string DesignContent, string CodeContent, string BranchName);
public record EnsembleCodeResponse(string Text, List<ToolUsage> ToolsUsed, int EstimatedTokens,
    string BranchName, int FilesChanged,
    CodeChangeProposal? CodeChangeProposed = null, PrProposal? PrProposed = null);
