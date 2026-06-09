using MaIN.Core.Hub;
using MaIN.Core.Hub.Contexts.Interfaces.AgentContext;
using MaIN.Core.Hub.Utils;
using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Tools;
using MaIN.Domain.Models;
using MaIN.Docs.Api.Models;
using DomainModels = MaIN.Domain.Models.Models;

namespace MaIN.Docs.Api.Services;

public class DocsAgentOrchestrator(DocsLoader loader, ArtifactService artifactService, GitHubService githubService, ILogger<DocsAgentOrchestrator> logger)
{
    private readonly Dictionary<string, IAgentContextExecutor> _agents = new();
    private readonly Dictionary<string, SemaphoreSlim> _locks = new();

    public async Task InitializeAsync()
    {
        var docsPath = loader.DocsPath;
        MdTools.Initialize(docsPath, logger);
        ArtifactTools.Init(artifactService);
        IssueTools.Init(githubService);
        PrTools.Init(githubService);

        var chattyTools  = BuildChattyTools(docsPath);
        var codeTools    = BuildCodeTools(docsPath);
        var designTools  = BuildDesignTools(docsPath);
        var reviewTools  = BuildReviewTools(docsPath);
        var forgeTools   = BuildForgeTools(docsPath);

        var modelChatty = DomainModels.Gemini.Gemini3_1FlashLite;
        var modelCode   = DomainModels.Gemini.Gemini3_5Flash;
        var modelReview = DomainModels.Gemini.Gemini3_1FlashLite;
        var modelDesign = DomainModels.Gemini.Gemini2_5Pro;
        var modelForge  = DomainModels.Gemini.Gemini2_5Pro;

        var defs = new[]
        {
            new AgentDef("chatty", "Chatty", modelChatty, ChattySystemPrompt, chattyTools),
            new AgentDef("code",   "Code",   modelCode,   CodeSystemPrompt,   codeTools),
            new AgentDef("design", "Design", modelDesign, DesignSystemPrompt, designTools),
            new AgentDef("review", "Review", modelReview, ReviewSystemPrompt, reviewTools),
            new AgentDef("forge",  "Forge",  modelForge,  ForgeSystemPrompt,  forgeTools),
        };

        foreach (var def in defs)
        {
            try
            {
                var ctx = await AIHub.Agent()
                    .WithModel(def.Model)
                    .WithId($"docs-{def.Id}")
                    .WithName(def.Name)
                    .WithInitialPrompt(def.SystemPrompt)
                    .WithTools(def.Tools)
                    .CreateAsync();

                _agents[def.Id] = ctx;
                _locks[def.Id] = new SemaphoreSlim(1, 1);
                logger.LogInformation("Agent '{Id}' initialized with model {Model}", def.Id, def.Model);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to initialize agent '{Id}' — check API key for {Model}", def.Id, def.Model);
            }
        }
    }

    public async Task<AgentResult> ProcessAsync(
        string agentId,
        IEnumerable<Message> messages,
        CancellationToken ct)
    {
        if (!_agents.TryGetValue(agentId, out var ctx))
            throw new KeyNotFoundException($"Agent '{agentId}' is not available.");

        var sem = _locks[agentId];

        if (!await sem.WaitAsync(TimeSpan.FromSeconds(60), ct))
            throw new TimeoutException($"Agent '{agentId}' is busy. Please retry.");

        string? artifactUrl = null;
        ArtifactProposal? artifactProposed = null;
        string? issueUrl = null;
        IssueProposal? issueProposed = null;
        PlanProposal? planProposed = null;
        PrReviewProposal? reviewProposed = null;
        ReviewPosted? reviewPosted = null;
        CodeChangeProposal? codeChangeProposed = null;
        PrProposal? prProposed = null;
        string? prUrl = null;
        List<PresentedCodeFile>? codePresented = null;

        if (agentId == "code")
        {
            ArtifactTools.SetCapture(url => artifactUrl = url);
            ArtifactTools.SetProposalCapture(p => artifactProposed = new ArtifactProposal(p.ArchiveName, p.Description));
        }

        if (agentId is "design" or "ensemble-design")
        {
            IssueTools.SetProposalCapture(p => issueProposed = new IssueProposal(p.Title, p.Body));
            IssueTools.SetUrlCapture(url => issueUrl = url);
            PlanTools.SetCapture(plan => planProposed = plan);
        }

        if (agentId is "review" or "ensemble-review")
        {
            PrTools.SetReviewCapture(r => reviewProposed = r);
            PrTools.SetReviewPostedCapture(r => reviewPosted = r);
            PrTools.SetCodeChangeCapture(c => codeChangeProposed = c);
            PrTools.SetPrCapture(p => prProposed = p);
            PrTools.SetPrUrlCapture(url => prUrl = url);
        }

        if (agentId == "forge")
        {
            ArtifactTools.SetCapture(url => artifactUrl = url);
            ArtifactTools.SetProposalCapture(p => artifactProposed = new ArtifactProposal(p.ArchiveName, p.Description));
            IssueTools.SetProposalCapture(p => issueProposed = new IssueProposal(p.Title, p.Body));
            IssueTools.SetUrlCapture(url => issueUrl = url);
            PlanTools.SetCapture(plan => planProposed = plan);
            PrTools.SetReviewCapture(r => reviewProposed = r);
            PrTools.SetReviewPostedCapture(r => reviewPosted = r);
            PrTools.SetCodeChangeCapture(c => codeChangeProposed = c);
            PrTools.SetPrCapture(p => prProposed = p);
            PrTools.SetPrUrlCapture(url => prUrl = url);
            CodePresentTools.SetCapture(files => codePresented = files
                .Select(f => new PresentedCodeFile(f.Path, f.Content, f.Language))
                .ToList());
        }

        try
        {
            var completedInvocations = new List<ToolInvocation>();
            var result = await ctx.ProcessAsync(messages, toolCallback: inv =>
            {
                if (!inv.Done)
                    logger.LogInformation("[{Agent}] Tool CALL  → {Tool} args={Args}", agentId, inv.ToolName, inv.Arguments);
                else
                    logger.LogInformation("[{Agent}] Tool DONE  ← {Tool}", agentId, inv.ToolName);

                if (inv.Done) completedInvocations.Add(inv);
                return Task.CompletedTask;
            });

            var tokenSummary = result.Message.Tokens
                .GroupBy(t => t.Type)
                .Select(g => $"{g.Key}:{g.Count()}(len={g.Sum(t => t.Text?.Length ?? 0)})")
                .ToList();
            logger.LogInformation("[{Agent}] Result content length={Len}, tokens=[{Tokens}]",
                agentId,
                result.Message.Content?.Length ?? 0,
                string.Join(", ", tokenSummary));

            var toolsUsed = completedInvocations
                .GroupBy(i => i.ToolName)
                .Select(g => new ToolUsage(g.Key, g.Count()))
                .ToList();

            var estimatedTokens = (int)Math.Round((result.Message.Content?.Length ?? 0) / 4.0);

            return new AgentResult(result.Message.Content ?? string.Empty, toolsUsed, estimatedTokens,
                artifactUrl, artifactProposed, issueProposed, issueUrl, planProposed,
                reviewProposed, codeChangeProposed, prProposed, prUrl, codePresented, reviewPosted);
        }
        finally
        {
            ArtifactTools.SetCapture(null);
            ArtifactTools.SetProposalCapture(null);
            IssueTools.SetProposalCapture(null);
            IssueTools.SetUrlCapture(null);
            PlanTools.SetCapture(null);
            PrTools.SetReviewCapture(null);
            PrTools.SetReviewPostedCapture(null);
            PrTools.SetCodeChangeCapture(null);
            PrTools.SetPrCapture(null);
            PrTools.SetPrUrlCapture(null);
            CodePresentTools.SetCapture(null);
            sem.Release();
        }
    }

    public bool IsAvailable(string agentId) => _agents.ContainsKey(agentId);

    public Task<AgentResult> ProcessEnsembleDesignAsync(IEnumerable<Message> messages, CancellationToken ct) =>
        ProcessAsync("ensemble-design", messages, ct);

    public async Task<(AgentResult Result, string BranchName, int FilesChanged)> ProcessEnsembleCodeAsync(
        string originalMessage, string designContent, CancellationToken ct)
    {
        var branchName = $"flow/{DateTime.UtcNow:yyyyMMdd-HHmmss}";
        await githubService.CreateBranchAsync(branchName, "main");
        PrTools.ClearPendingCodeChanges();
        var messages = BuildEnsembleCodeMessages(originalMessage, designContent, branchName);
        var result = await ProcessAsync("ensemble-code", messages, ct);
        return (result, branchName, PrTools.PendingCodeChangeCount);
    }

    public Task<AgentResult> ProcessEnsembleReviewAsync(
        string originalMessage, string designContent, string codeContent, string branchName, CancellationToken ct)
    {
        var messages = BuildEnsembleReviewMessages(originalMessage, designContent, codeContent, branchName);
        return ProcessAsync("ensemble-review", messages, ct);
    }

    private static IEnumerable<Message> BuildEnsembleCodeMessages(
        string originalMessage, string designContent, string branchName) =>
    [
        new Message
        {
            Role = "User",
            Content = $"""
                [DESIGN PLAN]
                {designContent}
                [END DESIGN PLAN]

                User request: {originalMessage}
                Branch for changes: {branchName}

                Read the relevant source files, then call propose_code_change once per file with COMPLETE new content. Target branch: {branchName}
                """,
            Type = MessageType.NotSet,
            Time = DateTime.UtcNow
        }
    ];

    private static IEnumerable<Message> BuildEnsembleReviewMessages(
        string originalMessage, string designContent, string codeContent, string branchName) =>
    [
        new Message
        {
            Role = "User",
            Content = $"""
                [DESIGN PLAN]
                {designContent}
                [END DESIGN PLAN]

                [CODE CHANGES SUMMARY]
                {codeContent}
                [END CODE CHANGES SUMMARY]

                User request: {originalMessage}
                Branch with changes: {branchName}

                Verify the changes on branch {branchName} match the design plan, then call propose_pull_request from {branchName} to main.
                """,
            Type = MessageType.NotSet,
            Time = DateTime.UtcNow
        }
    ];

    private record AgentDef(string Id, string Name, string Model, string SystemPrompt, ToolsConfiguration Tools);

    private static ToolsConfigurationBuilder SharedDocsTools() =>
        new ToolsConfigurationBuilder()
            .AddTool<MdTools.ListDocsArgs>(
                "list_docs",
                "List all available documentation files.",
                new { type = "object", properties = new { } },
                MdTools.ListDocs)
            .AddTool<MdTools.SearchArgs>(
                "search_md_files",
                "Search docs for a keyword. Returns matching file paths and snippets.",
                new
                {
                    type = "object",
                    properties = new { query = new { type = "string", description = "Keyword or phrase to search for" } },
                    required = new[] { "query" }
                },
                MdTools.Search)
            .AddTool<MdTools.ReadArgs>(
                "read_md_file",
                "Read the full content of a documentation file by its path.",
                new
                {
                    type = "object",
                    properties = new { path = new { type = "string", description = "Absolute path to the .md file" } },
                    required = new[] { "path" }
                },
                MdTools.Read);

    private static ToolsConfiguration BuildDesignTools(string docsPath) =>
        SharedDocsTools()
            .AddTool<IssueTools.ListIssuesArgs>(
                "list_issues",
                "List open GitHub issues in the MaIN.NET repository.",
                new { type = "object", properties = new { } },
                IssueTools.ListIssues)
            .AddTool<IssueTools.GetIssueArgs>(
                "get_issue",
                "Get the full details of a specific GitHub issue by number.",
                new { type = "object", properties = new { number = new { type = "integer", description = "Issue number" } }, required = new[] { "number" } },
                IssueTools.GetIssue)
            .AddTool<IssueTools.ListRepoFilesArgs>(
                "list_repo_files",
                "List files in a known directory of the MaIN.NET repo. Call at most ONCE per response — prefer read_repo_file with a direct path instead. Do not use to explore; use only when you need to discover a specific filename inside a known directory.",
                new { type = "object", properties = new { path = new { type = "string", description = "Directory path inside the repo, e.g. 'src/MaIN.Core/Hub' or 'src/MaIN.Backends'" } } },
                IssueTools.ListRepoFiles)
            .AddTool<IssueTools.ReadRepoFileArgs>(
                "read_repo_file",
                "Read the raw content of a file from the MaIN.NET repository.",
                new { type = "object", properties = new { path = new { type = "string", description = "File path inside the repo, e.g. 'src/MaIN.Core/Hub/AIHub.cs'" } }, required = new[] { "path" } },
                IssueTools.ReadRepoFile)
            .AddTool<PlanTools.ProposePlanArgs>(
                "propose_plan",
                "Renders a structured implementation plan card in the UI. Call when the user describes a problem or asks how to implement something. Do NOT repeat the plan in text — the UI displays it. Call this BEFORE propose_github_issue.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        title   = new { type = "string", description = "Short plan title, e.g. 'Implement multimodal Ollama support'" },
                        context = new { type = "string", description = "1-2 sentence problem statement or motivation" },
                        steps   = new
                        {
                            type = "array",
                            description = "Ordered implementation steps",
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    title       = new { type = "string", description = "Step title (short)" },
                                    description = new { type = "string", description = "What to do and why, 1-3 sentences" },
                                    codeSnippet = new { type = "string", description = "Optional. Concrete code for this step — a method signature, a diff fragment, or a new class. Omit for conceptual/config steps." },
                                    language    = new { type = "string", description = "Language of codeSnippet, e.g. 'csharp', 'json', 'bash'. Required when codeSnippet is set." }
                                },
                                required = new[] { "title", "description" }
                            }
                        }
                    },
                    required = new[] { "title", "context", "steps" }
                },
                PlanTools.Propose)
            .AddTool<IssueTools.ProposeIssueArgs>(
                "propose_github_issue",
                "Signals the UI to offer creating a GitHub issue. Call when the conversation surfaces a concrete, actionable gap or improvement in MaIN.NET. Write a contributor-ready title and body.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        title            = new { type = "string",  description = "Short, clear issue title" },
                        body             = new { type = "string",  description = "Issue body: problem statement, proposed solution, acceptance criteria" },
                        additionalLabels = new { type = "array", items = new { type = "string" }, description = "Extra labels beyond 'proposal', e.g. ['enhancement', 'bug']" }
                    },
                    required = new[] { "title", "body", "additionalLabels" }
                },
                IssueTools.Propose)
            .AddTool<IssueTools.CreateIssueArgs>(
                "create_github_issue",
                "Creates the GitHub issue after user confirmation. ONLY call when the user explicitly confirms. Always tagged 'proposal'; auto-closed in 3 days if no contributor engages.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        title            = new { type = "string",  description = "Issue title" },
                        body             = new { type = "string",  description = "Full issue body" },
                        additionalLabels = new { type = "array", items = new { type = "string" }, description = "Extra labels beyond 'proposal'" }
                    },
                    required = new[] { "title", "body", "additionalLabels" }
                },
                IssueTools.Create)
            .WithMaxIterations(9)
            .WithToolChoice("auto")
            .Build();

    private static ToolsConfiguration BuildReviewTools(string docsPath) =>
        SharedDocsTools()
            .AddTool<PrTools.ListBranchesArgs>(
                "list_branches",
                "List all branches in the MaIN.NET repository.",
                new { type = "object", properties = new { } },
                PrTools.ListBranches)
            .AddTool<PrTools.ListPrsArgs>(
                "list_pull_requests",
                "List open pull requests in the MaIN.NET repository.",
                new { type = "object", properties = new { } },
                PrTools.ListPullRequests)
            .AddTool<PrTools.GetPrArgs>(
                "get_pull_request",
                "Get full details of a specific pull request including head SHA (required for review comments).",
                new { type = "object", properties = new { number = new { type = "integer", description = "PR number" } }, required = new[] { "number" } },
                PrTools.GetPullRequest)
            .AddTool<PrTools.GetPrFilesArgs>(
                "get_pr_files",
                "Get changed files with diffs/patches for a pull request.",
                new { type = "object", properties = new { number = new { type = "integer", description = "PR number" } }, required = new[] { "number" } },
                PrTools.GetPrFiles)
            .AddTool<PrTools.ReadBranchFileArgs>(
                "read_branch_file",
                "Read a specific file from a branch of the MaIN.NET repository.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        path   = new { type = "string", description = "File path in the repo, e.g. 'src/MaIN.Core/Hub/AIHub.cs'" },
                        branch = new { type = "string", description = "Branch name, e.g. 'main' or 'feature/xyz'" }
                    },
                    required = new[] { "path", "branch" }
                },
                PrTools.ReadBranchFile)
            .AddTool<PrTools.ProposePrReviewArgs>(
                "propose_pr_review",
                "Propose posting an inline PR review (verdict + comments). Fires a UI confirmation card. ALWAYS call get_pr_files first to see the actual diff. Do NOT call submit_pr_review until user confirms.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        prNumber = new { type = "integer", description = "PR number to review" },
                        headSha  = new { type = "string",  description = "Head commit SHA from get_pull_request" },
                        verdict  = new { type = "string",  description = "One of: APPROVE, REQUEST_CHANGES, COMMENT" },
                        summary  = new { type = "string",  description = "Overall review summary shown to the user" },
                        comments = new
                        {
                            type = "array",
                            description = "Inline review comments on specific lines",
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    filePath = new { type = "string",  description = "File path in the PR, e.g. 'src/Foo.cs'" },
                                    line     = new { type = "integer", description = "Line number in the file" },
                                    body     = new { type = "string",  description = "Comment text with the issue and fix" }
                                },
                                required = new[] { "filePath", "line", "body" }
                            }
                        }
                    },
                    required = new[] { "prNumber", "headSha", "verdict", "summary", "comments" }
                },
                PrTools.ProposePrReview)
            .AddTool<PrTools.SubmitPrReviewArgs>(
                "submit_pr_review",
                "Post the PR review to GitHub. ONLY call after the user explicitly confirms. Requires headSha from get_pull_request.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        prNumber = new { type = "integer", description = "PR number" },
                        headSha  = new { type = "string",  description = "Head commit SHA" },
                        verdict  = new { type = "string",  description = "APPROVE, REQUEST_CHANGES, or COMMENT" },
                        summary  = new { type = "string",  description = "Review body text" },
                        comments = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    filePath = new { type = "string" },
                                    line     = new { type = "integer" },
                                    body     = new { type = "string" }
                                },
                                required = new[] { "filePath", "line", "body" }
                            }
                        }
                    },
                    required = new[] { "prNumber", "headSha", "verdict", "summary", "comments" }
                },
                PrTools.SubmitPrReview)
            .AddTool<PrTools.CreatePrReviewArgs>(
                "create_pr_review",
                "PREFERRED: directly posts a PR review with inline comments to GitHub — no user confirmation required. " +
                "Use this instead of propose_pr_review when you are ready to submit. Always call get_pr_files first to read the actual diff.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        prNumber = new { type = "integer", description = "PR number" },
                        headSha  = new { type = "string",  description = "Head commit SHA from get_pull_request" },
                        verdict  = new { type = "string",  description = "APPROVE, REQUEST_CHANGES, or COMMENT" },
                        summary  = new { type = "string",  description = "Overall review summary" },
                        comments = new
                        {
                            type = "array",
                            description = "Inline review comments — at least 1 comment required",
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    filePath = new { type = "string",  description = "File path in the PR" },
                                    line     = new { type = "integer", description = "Line number" },
                                    body     = new { type = "string",  description = "Comment with the issue and fix" }
                                },
                                required = new[] { "filePath", "line", "body" }
                            }
                        }
                    },
                    required = new[] { "prNumber", "headSha", "verdict", "summary", "comments" }
                },
                PrTools.CreatePrReview)
            .AddTool<PrTools.ProposeCodeChangeArgs>(
                "propose_code_change",
                "Propose pushing a modified file to a branch. Fires a UI confirmation card. Include the COMPLETE new file content. Do NOT call push_file_to_branch until user confirms.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        branch        = new { type = "string", description = "Target branch name" },
                        filePath      = new { type = "string", description = "File path in the repo" },
                        content       = new { type = "string", description = "Complete new file content" },
                        commitMessage = new { type = "string", description = "Commit message" },
                        rationale     = new { type = "string", description = "Why this change is needed" }
                    },
                    required = new[] { "branch", "filePath", "content", "commitMessage", "rationale" }
                },
                PrTools.ProposeCodeChange)
            .AddTool<PrTools.PushFileArgs>(
                "push_file_to_branch",
                "Push a file to a branch on GitHub. ONLY call after user explicitly confirms.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        branch        = new { type = "string", description = "Target branch name" },
                        filePath      = new { type = "string", description = "File path in the repo" },
                        content       = new { type = "string", description = "Complete file content" },
                        commitMessage = new { type = "string", description = "Commit message" }
                    },
                    required = new[] { "branch", "filePath", "content", "commitMessage" }
                },
                PrTools.PushFileToBranch)
            .AddTool<PrTools.ProposePrArgs>(
                "propose_pull_request",
                "Propose creating a pull request from one branch to another. Fires a UI confirmation card. Do NOT call create_pull_request until user confirms.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        title      = new { type = "string", description = "PR title" },
                        body       = new { type = "string", description = "PR description in markdown" },
                        headBranch = new { type = "string", description = "Source branch with the changes" },
                        baseBranch = new { type = "string", description = "Target branch, usually 'main'" }
                    },
                    required = new[] { "title", "body", "headBranch", "baseBranch" }
                },
                PrTools.ProposePullRequest)
            .AddTool<PrTools.CreatePrArgs>(
                "create_pull_request",
                "Create the pull request on GitHub. ONLY call after user explicitly confirms.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        title      = new { type = "string", description = "PR title" },
                        body       = new { type = "string", description = "PR description" },
                        headBranch = new { type = "string", description = "Source branch" },
                        baseBranch = new { type = "string", description = "Target branch" }
                    },
                    required = new[] { "title", "body", "headBranch", "baseBranch" }
                },
                PrTools.CreatePullRequest)
            .WithMaxIterations(20)
            .WithToolChoice("auto")
            .Build();

    private static ToolsConfiguration BuildEnsembleCodeTools() =>
        SharedDocsTools()
            .AddTool<IssueTools.ListRepoFilesArgs>(
                "list_repo_files",
                "List files in a known directory of the MaIN.NET repo. Call at most ONCE per response.",
                new { type = "object", properties = new { path = new { type = "string", description = "Directory path inside the repo" } } },
                IssueTools.ListRepoFiles)
            .AddTool<IssueTools.ReadRepoFileArgs>(
                "read_repo_file",
                "Read the raw content of a file from the MaIN.NET repository.",
                new { type = "object", properties = new { path = new { type = "string", description = "File path inside the repo, e.g. 'src/MaIN.Core/Hub/AIHub.cs'" } }, required = new[] { "path" } },
                IssueTools.ReadRepoFile)
            .AddTool<PrTools.ProposeCodeChangeArgs>(
                "propose_code_change",
                "Queue a file change for user confirmation. Provide the COMPLETE new file content — not a diff or excerpt. One call per file.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        branch        = new { type = "string", description = "Target branch name (provided in context)" },
                        filePath      = new { type = "string", description = "File path in the repo" },
                        content       = new { type = "string", description = "Complete new file content" },
                        commitMessage = new { type = "string", description = "Commit message" },
                        rationale     = new { type = "string", description = "Why this change is needed" }
                    },
                    required = new[] { "branch", "filePath", "content", "commitMessage", "rationale" }
                },
                PrTools.ProposeCodeChange)
            .WithMaxIterations(15)
            .WithToolChoice("auto")
            .Build();

    private static ToolsConfiguration BuildCodeTools(string docsPath) =>
        SharedDocsTools()
            .AddTool<ArtifactTools.ProposeArgs>(
                "propose_artifact_generation",
                "Signals the UI to offer a downloadable ZIP artifact. " +
                "Call when your response contains a complete, compilable solution — not for partial snippets or conceptual answers. " +
                "Skip it when the user is just exploring or when you'd need more details to make the code runnable. " +
                "Call at most once per response.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        archiveName = new { type = "string", description = "Suggested zip filename, e.g. MyAgent.zip" },
                        description = new { type = "string", description = "One-line description of what the solution does" }
                    },
                    required = new[] { "archiveName", "description" }
                },
                ArtifactTools.Propose)
            .AddTool<ArtifactTools.GenerateArgs>(
                "generate_artifact",
                "Packages a complete .NET solution into a ZIP archive and uploads it to cloud storage. " +
                "ONLY call when the user explicitly confirms they want to download. " +
                "Include all files needed to run: .csproj with correct NuGet references, Program.cs, and any supporting files. " +
                "Do NOT call proactively.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        archiveName = new { type = "string", description = "ZIP filename, e.g. MyAgent.zip" },
                        files = new
                        {
                            type = "array",
                            description = "Files to include in the archive",
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    path    = new { type = "string", description = "Relative path inside the zip, e.g. MyAgent/Program.cs" },
                                    content = new { type = "string", description = "Complete file content" }
                                },
                                required = new[] { "path", "content" }
                            }
                        },
                        description = new { type = "string", description = "One-line description of what the solution does" }
                    },
                    required = new[] { "archiveName", "files" }
                },
                ArtifactTools.Generate)
            .WithMaxIterations(7)
            .WithToolChoice("auto")
            .Build();

    private static ToolsConfiguration BuildForgeTools(string docsPath) =>
        SharedDocsTools()
            // ── Code presentation (Code mode — standalone examples) ──
            .AddTool<CodePresentTools.PresentArgs>(
                "present_code",
                "MANDATORY in CODE STAGE for standalone examples. Presents code files in the chat UI — " +
                "this is the ONLY way the user sees your code. Call BEFORE propose_artifact_generation. " +
                "Pass every file in the solution (minimum: .csproj + Program.cs).",
                new
                {
                    type = "object",
                    properties = new
                    {
                        files = new
                        {
                            type = "array",
                            description = "All files in the solution",
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    path     = new { type = "string", description = "Relative file path, e.g. 'MyAgent/Program.cs'" },
                                    content  = new { type = "string", description = "Complete file content" },
                                    language = new { type = "string", description = "Language: 'csharp', 'xml', 'json', 'bash'" }
                                },
                                required = new[] { "path", "content", "language" }
                            }
                        }
                    },
                    required = new[] { "files" }
                },
                CodePresentTools.Present)
            // ── Artifact tools (Code mode) ──
            .AddTool<ArtifactTools.ProposeArgs>(
                "propose_artifact_generation",
                "Signals the UI to offer a downloadable ZIP artifact. Call after writing the complete solution. At most once per response.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        archiveName = new { type = "string", description = "Suggested zip filename" },
                        description = new { type = "string", description = "One-line description of what the solution does" }
                    },
                    required = new[] { "archiveName", "description" }
                },
                ArtifactTools.Propose)
            .AddTool<ArtifactTools.GenerateArgs>(
                "generate_artifact",
                "Packages a complete .NET solution into a ZIP and uploads it. ONLY call when user explicitly confirms.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        archiveName = new { type = "string", description = "ZIP filename" },
                        files = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    path    = new { type = "string", description = "Relative path inside the zip" },
                                    content = new { type = "string", description = "Complete file content" }
                                },
                                required = new[] { "path", "content" }
                            }
                        },
                        description = new { type = "string" }
                    },
                    required = new[] { "archiveName", "files" }
                },
                ArtifactTools.Generate)
            // ── GitHub issue tools (Design mode) ──
            .AddTool<IssueTools.ListIssuesArgs>(
                "list_issues",
                "List open GitHub issues in the MaIN.NET repository.",
                new { type = "object", properties = new { } },
                IssueTools.ListIssues)
            .AddTool<IssueTools.GetIssueArgs>(
                "get_issue",
                "Get the full details of a specific GitHub issue by number.",
                new { type = "object", properties = new { number = new { type = "integer", description = "Issue number" } }, required = new[] { "number" } },
                IssueTools.GetIssue)
            .AddTool<IssueTools.ListRepoFilesArgs>(
                "list_repo_files",
                "List files in a known directory of the MaIN.NET repo. Call at most ONCE per response.",
                new { type = "object", properties = new { path = new { type = "string", description = "Directory path inside the repo" } } },
                IssueTools.ListRepoFiles)
            .AddTool<IssueTools.ReadRepoFileArgs>(
                "read_repo_file",
                "Read the raw content of a file from the MaIN.NET repository.",
                new { type = "object", properties = new { path = new { type = "string", description = "File path inside the repo" } }, required = new[] { "path" } },
                IssueTools.ReadRepoFile)
            .AddTool<PlanTools.ProposePlanArgs>(
                "propose_plan",
                "Renders a structured implementation plan card in the UI. Write one brief sentence in text — do NOT repeat the plan. Call BEFORE propose_github_issue.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        title   = new { type = "string" },
                        context = new { type = "string" },
                        steps   = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    title       = new { type = "string" },
                                    description = new { type = "string" },
                                    codeSnippet = new { type = "string" },
                                    language    = new { type = "string" }
                                },
                                required = new[] { "title", "description" }
                            }
                        }
                    },
                    required = new[] { "title", "context", "steps" }
                },
                PlanTools.Propose)
            .AddTool<IssueTools.ProposeIssueArgs>(
                "propose_github_issue",
                "Signals the UI to offer creating a GitHub issue. Always AFTER propose_plan.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        title            = new { type = "string" },
                        body             = new { type = "string" },
                        additionalLabels = new { type = "array", items = new { type = "string" } }
                    },
                    required = new[] { "title", "body", "additionalLabels" }
                },
                IssueTools.Propose)
            .AddTool<IssueTools.CreateIssueArgs>(
                "create_github_issue",
                "Creates the GitHub issue. ONLY after explicit user confirmation.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        title            = new { type = "string" },
                        body             = new { type = "string" },
                        additionalLabels = new { type = "array", items = new { type = "string" } }
                    },
                    required = new[] { "title", "body", "additionalLabels" }
                },
                IssueTools.Create)
            // ── PR / branch tools (Review mode) ──
            .AddTool<PrTools.ListBranchesArgs>(
                "list_branches",
                "List all branches in the MaIN.NET repository.",
                new { type = "object", properties = new { } },
                PrTools.ListBranches)
            .AddTool<PrTools.ListPrsArgs>(
                "list_pull_requests",
                "List open pull requests.",
                new { type = "object", properties = new { } },
                PrTools.ListPullRequests)
            .AddTool<PrTools.GetPrArgs>(
                "get_pull_request",
                "Get PR details including head SHA.",
                new { type = "object", properties = new { number = new { type = "integer" } }, required = new[] { "number" } },
                PrTools.GetPullRequest)
            .AddTool<PrTools.GetPrFilesArgs>(
                "get_pr_files",
                "Get changed files with diffs for a PR.",
                new { type = "object", properties = new { number = new { type = "integer" } }, required = new[] { "number" } },
                PrTools.GetPrFiles)
            .AddTool<PrTools.ReadBranchFileArgs>(
                "read_branch_file",
                "Read a file from a specific branch.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        path   = new { type = "string" },
                        branch = new { type = "string" }
                    },
                    required = new[] { "path", "branch" }
                },
                PrTools.ReadBranchFile)
            .AddTool<PrTools.ProposePrReviewArgs>(
                "propose_pr_review",
                "Propose posting a PR review. Always call get_pr_files first. Do NOT call submit_pr_review until user confirms.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        prNumber = new { type = "integer" },
                        headSha  = new { type = "string" },
                        verdict  = new { type = "string", description = "APPROVE, REQUEST_CHANGES, or COMMENT" },
                        summary  = new { type = "string" },
                        comments = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    filePath = new { type = "string" },
                                    line     = new { type = "integer" },
                                    body     = new { type = "string" }
                                },
                                required = new[] { "filePath", "line", "body" }
                            }
                        }
                    },
                    required = new[] { "prNumber", "headSha", "verdict", "summary", "comments" }
                },
                PrTools.ProposePrReview)
            .AddTool<PrTools.SubmitPrReviewArgs>(
                "submit_pr_review",
                "Post the PR review to GitHub. ONLY after user confirms.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        prNumber = new { type = "integer" },
                        headSha  = new { type = "string" },
                        verdict  = new { type = "string" },
                        summary  = new { type = "string" },
                        comments = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    filePath = new { type = "string" },
                                    line     = new { type = "integer" },
                                    body     = new { type = "string" }
                                },
                                required = new[] { "filePath", "line", "body" }
                            }
                        }
                    },
                    required = new[] { "prNumber", "headSha", "verdict", "summary", "comments" }
                },
                PrTools.SubmitPrReview)
            .AddTool<PrTools.CreatePrReviewArgs>(
                "create_pr_review",
                "Directly posts a PR review with inline comments to GitHub — no confirmation needed. Call get_pr_files first.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        prNumber = new { type = "integer" },
                        headSha  = new { type = "string" },
                        verdict  = new { type = "string", description = "APPROVE, REQUEST_CHANGES, or COMMENT" },
                        summary  = new { type = "string" },
                        comments = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    filePath = new { type = "string" },
                                    line     = new { type = "integer" },
                                    body     = new { type = "string" }
                                },
                                required = new[] { "filePath", "line", "body" }
                            }
                        }
                    },
                    required = new[] { "prNumber", "headSha", "verdict", "summary", "comments" }
                },
                PrTools.CreatePrReview)
            .AddTool<PrTools.ProposeCodeChangeArgs>(
                "propose_code_change",
                "REVIEW STAGE ONLY — forbidden in DESIGN and CODE stages. Proposes pushing a file to a branch. Include COMPLETE file content. Do NOT call push_file_to_branch until user confirms.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        branch        = new { type = "string" },
                        filePath      = new { type = "string" },
                        content       = new { type = "string" },
                        commitMessage = new { type = "string" },
                        rationale     = new { type = "string" }
                    },
                    required = new[] { "branch", "filePath", "content", "commitMessage", "rationale" }
                },
                PrTools.ProposeCodeChange)
            .AddTool<PrTools.PushFileArgs>(
                "push_file_to_branch",
                "Push a file to a branch. ONLY after user explicitly confirms.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        branch        = new { type = "string" },
                        filePath      = new { type = "string" },
                        content       = new { type = "string" },
                        commitMessage = new { type = "string" }
                    },
                    required = new[] { "branch", "filePath", "content", "commitMessage" }
                },
                PrTools.PushFileToBranch)
            .AddTool<PrTools.ProposePrArgs>(
                "propose_pull_request",
                "REVIEW STAGE ONLY — forbidden in DESIGN and CODE stages. Proposes creating a PR. Do NOT call create_pull_request until user confirms.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        title      = new { type = "string" },
                        body       = new { type = "string" },
                        headBranch = new { type = "string" },
                        baseBranch = new { type = "string" }
                    },
                    required = new[] { "title", "body", "headBranch", "baseBranch" }
                },
                PrTools.ProposePullRequest)
            .AddTool<PrTools.CreatePrArgs>(
                "create_pull_request",
                "Create the pull request on GitHub. ONLY after user explicitly confirms.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        title      = new { type = "string" },
                        body       = new { type = "string" },
                        headBranch = new { type = "string" },
                        baseBranch = new { type = "string" }
                    },
                    required = new[] { "title", "body", "headBranch", "baseBranch" }
                },
                PrTools.CreatePullRequest)
            .WithMaxIterations(13)
            .WithToolChoice("auto")
            .Build();

    private static ToolsConfiguration BuildChattyTools(string docsPath) =>
        SharedDocsTools()
            .WithMaxIterations(5)
            .WithToolChoice("auto")
            .Build();

    private const string ChattySystemPrompt = """
        You are the MaIN.NET Docs Agent — sharp, edgy, and occasionally a bit too honest. 
        Your job is to answer questions about the MaIN.NET framework using ONLY the provided documentation.

        BEHAVIOR:
        - Tone: Edgy, minimalist, and direct. Think "senior dev who's had enough coffee but zero patience for fluff."
        - Style: No corporate speak. No "I am here to help." Just facts and code.
        - Occasional sarcasm or witty remarks about AI architecture are encouraged.
        - If the user asks something stupid, point it out (politely-ish).
        - Use emojis sparingly and only if they feel ironic.

        MANDATORY WORKFLOW:
        1. list_docs — discover what's available.
        2. read_md_file — read the relevant docs before opening your mouth.
        3. Answer — summarize the facts. If they need code, provide a surgical C# snippet.

        CODE SNIPPETS:
        - Modern C# 12+ (top-level statements).
        - No boilerplate. Just the essence.
        - Mention the exact doc you're pulling from.

        If you don't know something because it's not in the docs, say: "Not in the docs. Probably a skill issue (ours, or yours)."
        """;

    private const string CodeSystemPrompt = """
        You are the MaIN.NET Code Agent — part brilliant engineer, part sarcastic rubber duck.
        You know the MaIN.NET framework cold. Here's what matters:

        FRAMEWORK FACTS (don't guess, verify via tools):
        - Entry points: AIHub.Chat(), AIHub.Agent(), AIHub.Flow(), AIHub.Model(), AIHub.Mcp()
        - ChatContext chain: WithModel() → WithMessage() → [optional config] → CompleteAsync()
        - AgentContext is two-phase: configure+Create(), then ProcessAsync()
        - StepBuilder controls agent pipeline: .Answer(), .FetchData(), .Become(), .Redirect(), .Mcp(), .AnswerUseKnowledge()
        - KnowledgeBuilder adds RAG sources: .AddFile(), .AddUrl(), .AddMcp()
        - Backends: Self (local GGUF), OpenAi, Gemini, Anthropic, GroqCloud, DeepSeek, Xai, Ollama, Vertex
        - Console bootstrap: MaINBootstrapper.Initialize(); ASP.NET: services.AddMaIN(configuration)
        - MCP has 3 integration styles: direct prompt, agent pipeline step, RAG knowledge source

        TOOL ECONOMY: You have 7 tool slots. list_docs (1) → read 1-2 docs (2) → answer. Never read the same file twice. Propose artifact counts as 1 slot.

        TOOLS — use them every time, no improvising:
        1. list_docs — discover what files exist
        2. search_md_files — find relevant sections by keyword
        3. read_md_file — get exact API signatures and parameter tables

        CODE STYLE — always:
        - Top-level statements (no explicit Program class or Main method)
        - Minimal code — only what the user asked for, nothing extra
        - Modern C# 12+: primary constructors, collection expressions, pattern matching, var everywhere
        - No boilerplate comments or XML docs
        - Target net9.0 or net10.0 in .csproj
        - ImplicitUsings=enable, Nullable=enable
        - Bad: class Program { static async Task Main() { ... } }
        - Good: MaINBootstrapper.Initialize(); var result = await AIHub.Chat()...

        BEHAVIOR:
        - Always read the docs before answering. "I think the API looks like..." is not your style.
        - Read user intent before deciding how much to write:
          · Exploratory ("can I...", "is it possible...", "how does X work"): explain + a focused snippet. No full project.
          · Explicit build request ("build me...", "write a complete...", "create a project..."): write the full solution.
          · Ambiguous: ask ONE sharp question about scope or use case — don't assume they want a full project.
        - Occasional sarcasm is fine ("Yes, you could also write this in 40 lines... or you could just use WithKnowledge").

        ARTIFACTS:
        - ALWAYS write out the complete code solution in your response first — .csproj and Program.cs in full,
          fenced code blocks, nothing omitted. The user must be able to read and run the code from your message alone.
          Proposing an artifact is NOT a substitute for showing the code. If your response has no code blocks, you've failed.
        - propose_artifact_generation: call AFTER you've written the full solution, as an optional convenience download.
          Only call it when the solution is complete and runnable (has .csproj + Program.cs + runs with 'dotnet run').
        - generate_artifact: ONLY when the user explicitly confirms they want the download. Package the full project.
        - After generate_artifact succeeds, do NOT include the download URL in your text — the UI shows a download card. Just confirm it's ready.

        RESPONSE RULE: ALWAYS write text in your response. When proposing an artifact, include a brief explanation of what you built. When generating an artifact, confirm it's ready in 1 sentence. Never return an empty text response.
        """;

    private const string DesignSystemPrompt = """
        You are the MaIN.NET Design Agent — the one who's seen enough bad AI architecture to have opinions.
        You help users design systems using MaIN.NET and you're not shy about pushing back on questionable choices.
        You also have direct access to the MaIN.NET GitHub repository — source files and open issues.

        ══════════════════════════════════════════════════════
        MANDATORY WORKFLOW — follow this order every time:
        ══════════════════════════════════════════════════════
        STEP 1 — list_docs (ALWAYS first, no exceptions)
                 Know what documentation exists before touching anything else.
        STEP 2 — read_md_file on the relevant docs
                 For any question touching models/backends → read models.md
                 For any question touching agents/pipelines → read agents.md
                 For any question touching chat/completions → read chat.md
                 For setup/config → read getting-started.md
                 Read 1-3 docs maximum, then move on.
        STEP 3 — (optional) read 1-2 repo files for specific implementation details
                 Only if the docs don't answer the question. Use direct paths, not directory listings.
        STEP 4 — propose_plan (or answer directly for pure design questions)

        NEVER skip STEP 1 and STEP 2. Answering without reading docs first produces hallucinations.
        ══════════════════════════════════════════════════════

        CRITICAL KNOWLEDGE — do not contradict these facts from the docs:
        - The Self (local) backend uses LLamaSharp internally — it is already a dependency of MaIN.NET.
          Do NOT suggest "add LLamaSharp NuGet package". Adding GGUF/local model support = configure
          BackendType.Self + ModelsPath, then optionally ModelRegistry.Register(new GenericLocalModel(...)).
        - All model constants live in the Models.* namespace (read models.md for the full list).
          Never invent a model constant — look it up in models.md or model-context.md.
        - EnsureModelDownloaded() is a no-op for cloud backends. Only needed for Self backend.
        - AgentContext is two-phase: configure+Create(), then ProcessAsync(). Do not collapse them.
        - StepBuilder steps must be built via StepBuilder.Instance — never pass raw strings.

        REPO LAYOUT — navigate directly, never explore blindly:
        Root: src/, tests/, samples/, docs/, .github/
          src/MaIN.Core/Hub/AIHub.cs                        ← entry points (Chat, Agent, Flow, Model, Mcp)
          src/MaIN.Core/Hub/Builders/                       ← context builder implementations
          src/MaIN.Core/Hub/Contexts/                       ← executors and interfaces
          src/MaIN.Core/Hub/Utils/ToolsConfigurationBuilder.cs
          src/MaIN.Domain/Entities/                         ← Message, Agent, Tool domain types
          src/MaIN.Domain/Models/Models/                    ← model name constants
          src/MaIN.Backends/                                ← one folder per backend provider

        DOCS TOOLS:
        1. list_docs — ALWAYS first call
        2. search_md_files — find relevant sections by keyword
        3. read_md_file — read docs by absolute path returned from list_docs

        GITHUB TOOLS — surgical, not exploratory:
        4. list_repo_files — call at most ONCE per response, only when you need to discover a filename inside a known dir
        5. read_repo_file — read files directly by path (from REPO LAYOUT above or after one list_repo_files call)
        6. list_issues — check before proposing anything new; avoid duplicates
        7. get_issue — read full issue when user mentions a number or title
        8. propose_github_issue — contributor-ready title + body. Always AFTER propose_plan, never before.
        9. create_github_issue — ONLY after explicit user confirmation. Tagged "proposal"; auto-closed in 3 days.

        ANTI-LOOP: After 6 total tool calls, stop and produce the plan from what you have. Never call list_repo_files more than once.

        PLANNING — primary output mode for implementation questions:
        10. propose_plan — whenever the user describes a problem or asks how to implement something.
            - Complete STEPS 1-3 of the mandatory workflow first. The plan must reflect actual docs knowledge.
            - Structure: clear title, 1-2 sentence context, 3-7 concrete ordered steps.
            - CODE IN STEPS: Include codeSnippet for every step that touches actual code.
              · Use real APIs from the docs — exact method signatures, real Models.* constants.
              · For specific requests ("add X to backend Y"): each step that modifies code needs a snippet.
              · For broad requests ("design a multi-agent system"): high-level steps, snippets optional.
              · Set language (csharp, json, bash). Omit codeSnippet only for pure config/environment steps.
            - Do NOT write the plan in text — the UI renders it as a card. One brief sentence as your reply.
            - Order: propose_plan FIRST, then optionally propose_github_issue. Never reverse.
            - Skip propose_plan only for pure design questions with no implementation steps.

        BEHAVIOR:
        - Ask about scale, privacy, and latency budget before picking a backend.
        - Explain tradeoffs concisely — bullet points and ASCII diagrams, not essays.
        - If a design is unnecessarily complex, say so diplomatically.
        - "It depends" must be followed immediately by what it depends on and a recommendation.
        """;

    private const string ReviewSystemPrompt = """
        You are the MaIN.NET Review Agent — you read real code from GitHub branches and PRs,
        tear it apart against the docs, and either fix it or make the user confirm before you push anything.
        You are not here to encourage. You are not here to validate feelings. You are here to find bugs and call them out.

        TONE — mandatory, not optional:
        - Direct and terse. No "Great work!", no "This is a nice improvement", no "You are all set."
        - If the code has issues: name them bluntly. Quote the line, explain the problem, show the fix.
        - If the code is actually correct: say so in one sentence and move on. Do NOT praise it.
        - Sarcasm for obvious mistakes is expected ("This would work if the method existed").
        - Never say "feel free", "happy to", "you are all set", or anything a motivational poster would say.

        ══════════════════════════════════════════════════════
        CONFIRMATION FAST PATH — CHECK THIS FIRST, EVERY TURN
        ══════════════════════════════════════════════════════
        STEP 1 — list_docs → read 1-2 relevant docs (ALWAYS first — wrong framework facts = wrong verdict)
        STEP 2 — Read the branch or PR (list_branches / list_pull_requests → get files)
        STEP 3 — Analyze mercilessly → produce output

        NEVER skip STEP 1. Reviewing from memory produces hallucinated bugs and missed real ones.
        ══════════════════════════════════════════════════════

        TOOL ECONOMY: 11 slots. list_docs (1) + 2 doc reads (2) + PR lookup (1) + file reads (2) + review tool (1) = 7 max.

        FRAMEWORK FACTS (verify via tools — wrong API usage is your #1 target):
        - ChatContext: WithModel() → WithMessage() → [config] → CompleteAsync() — order matters
        - AgentContext is two-phase: configure+Create(), then ProcessAsync() — do not collapse
        - WithSteps() requires StepBuilder — raw strings crash; wrong step order breaks pipelines
        - EnsureModelDownloaded() is a no-op for cloud backends — do NOT flag it as an error
        - MCP config: Backend inferred if omitted; Model must be set; Command+Arguments launch a child process
        - LLamaSharp ships inside MaIN.NET — flagging its absence as a bug is itself a bug
        - Backends available: Self (local GGUF), OpenAi, Gemini, Anthropic, GroqCloud, DeepSeek, Xai, Ollama, Vertex
        - Console bootstrap: MaINBootstrapper.Initialize(); ASP.NET: services.AddMaIN(configuration)
        Only call read_md_file if you see an API call that isn't covered above.

        INLINE COMMENT GUIDANCE:
        - The diff from get_pr_files is your source of truth. Comment only on lines visible in the diff.
          Line numbers outside diff hunks are rejected by GitHub. Don't guess.
        - 3-5 sharp comments beats 10 nitpicks. Correctness and security first; style last.

        TOOLS (read):
        1. list_docs            — always first
        2. search_md_files      — keyword search in docs
        3. read_md_file         — read a documentation file
        4. list_branches        — list repo branches
        5. list_pull_requests   — list open PRs
        6. get_pull_request     — get PR details + head SHA (required before review comments)
        7. get_pr_files         — get changed files + diffs
        8. read_branch_file     — read a file from a branch

        TOOLS (direct action or propose):
        9.  create_pr_review      — PREFERRED: directly posts PR review + inline comments to GitHub. Always call get_pr_files first. No confirmation needed.
            propose_pr_review     — use only when you want the user to approve the review text before it goes live.
            submit_pr_review      — post after user confirms propose_pr_review.
        10. propose_code_change   — propose pushing a changed file. Include COMPLETE file content.
            push_file_to_branch   — push after user confirms.
        11. propose_pull_request  — propose opening a PR.
            create_pull_request   — create after user confirms.

        CRITICAL BEHAVIOR:
        - Severity: security (leaked keys, injection) > correctness (wrong API, wrong method order) > performance > style
        - For each issue: quote the exact offending line, explain why it's wrong against the docs, show the corrected version. Before/after is not optional.
        - Sarcasm is permitted for obvious mistakes ("This works in imagination. In reality, this method doesn't exist.").
        - ALWAYS write a text summary before firing any tool. Never return an empty text response.
        - Prefer create_pr_review over the two-step propose/submit flow — post the review directly.

        SCOPE RULES:
        - Snippet (user pastes code inline, no PR number): review what's there honestly. Write findings in text. No tool call needed.
        - PR review (user provides a PR number OR asks to review a PR):
            1. Call get_pull_request to get the head SHA
            2. Call get_pr_files to read the actual diff
            3. Write a brief text summary (2-3 sentences max — do NOT write the full inline comments in text)
            4. MANDATORY: call create_pr_review with your verdict + every inline comment. This is not optional.
               Writing the findings only in text and not calling create_pr_review is a failure.
            Read thoroughly. Always find something worth commenting on — if the logic is correct, look harder:
            null-safety, missing error handling, inefficient patterns, wrong model constants, simplification opportunities.
            A PR review with zero findings is a failed review unless the code is genuinely exceptional and you explicitly say why.
        """;

    private const string ForgeSystemPrompt = """
        You are Forge — MaIN.NET's unified flow agent. You operate in three sequential stages driven by the user.
        Each message begins with a stage directive — follow ONLY that stage's rules.

        CRITICAL KNOWLEDGE (all stages):
        - LLamaSharp is already inside MaIN.NET — never suggest adding it separately
        - All model constants live in Models.* — look them up in models.md, never invent them
        - AgentContext is two-phase: configure+Create(), then ProcessAsync(). Do not collapse.
        - StepBuilder steps must be built via StepBuilder.Instance — never pass raw strings
        - EnsureModelDownloaded() is a no-op for cloud backends
        - ALWAYS write text in your response — never return empty content

        REPO LAYOUT (navigate directly):
          src/MaIN.Core/Hub/AIHub.cs                   ← entry points
          src/MaIN.Core/Hub/Builders/                  ← context builders
          src/MaIN.Domain/Models/Models/               ← model constants
          src/MaIN.Backends/                           ← backend providers

        ══ [DESIGN STAGE] ══
        You are in PLANNING mode. Understand the request, classify it, then propose a structured plan.

        ── IF THE USER IS CORRECTING A PREVIOUS PLAN ──
        If there is already a propose_plan in the conversation and the user is pushing back or redirecting:
          1. Read their correction word-for-word before calling any tool.
          2. Only change what they asked to change — do NOT re-derive classification from scratch.
          3. If they say "add to existing X" or "put it in the current project":
             call list_repo_files on the parent directory (e.g. 'samples/') FIRST to discover exact structure,
             then read_repo_file on ONE existing sibling file to understand naming and format.
             Your plan MUST slot into that existing structure (same folder convention, same .csproj format).
          4. Produce a corrected propose_plan that directly addresses their feedback.
        ────────────────────────────────────────────────

        CLASSIFICATION — mandatory before any tool call:

        !! DEFAULT IS ALWAYS TYPE B !!
        Every request is TYPE B (extend the MaIN.NET repo) unless the user's exact words contain
        "brand new solution", "new project from scratch", "scaffold a project", or "standalone app".
        The words "example", "demo", "sample", "show me", "how to use", "implement", "extend",
        "add", "create a skill", "add to the repo" — ALL of these are TYPE B, not TYPE A.

          TYPE B — Extend the MaIN.NET repository. Add or modify files inside the existing repo.
            Examples: new sample in samples/, new method in src/, new skill file, new extension class.
            A "new example" means a new FOLDER inside samples/ with files that reference the MaIN.NET
            package — NOT a brand-new solution with its own .sln file outside the repo.

          TYPE A — ONLY when the user explicitly asks for a brand-new standalone solution they will
            run locally on their machine, completely separate from the MaIN.NET repo.
            Trigger phrases (all must be present or clearly implied): "brand new", "new project",
            "from scratch", "standalone", "scaffold". A single word like "new" is NOT enough.

        If there is any ambiguity, TYPE B. Never guess TYPE A.

        STEP 2 — MANDATORY repo read for TYPE B (skip only for TYPE A):
          a. list_docs → read 1-2 relevant docs for API details
          b. !! REQUIRED before proposing anything !!
             - For samples/examples: call list_repo_files with path='samples/' to see what already exists.
               Then call read_repo_file on ONE existing sample's .csproj to learn the exact project format.
               Do NOT invent a project structure — copy the format from the file you just read.
               Only propose a new .csproj if no existing project can host the new example.
             - For library/source changes: call read_repo_file on each existing file you will touch.
          Skipping step (b) and going straight to propose_plan is a hard failure for TYPE B.

        STEP 3 — propose_plan:
          - First line of `context` field: "TYPE A" or "TYPE B — <one sentence rationale>"
          - For TYPE B samples: steps must name the EXACT paths discovered in step 2b, not invented names.
            If a new folder is needed, match the numbering and casing of existing sibling folders.
          - codeSnippets in steps are encouraged for clarity
          - After propose_plan, write exactly 1-2 sentences summarizing what you planned

        Rules:
        - Do NOT write code blocks outside plan steps, do NOT propose artifacts, do NOT call GitHub PR tools
        - propose_github_issue is allowed AFTER propose_plan if the conversation surfaces a gap in MaIN.NET
        TOOL ECONOMY: 9 slots. list_docs (1) + list_repo_files (1) + read (1) + propose_plan (1) = 4 max. Stop at 8.

        ══ [CODE STAGE] ══
        You are in IMPLEMENTATION mode.

        STEP 1 — Read the plan's `context` field (first line). It starts with "TYPE A" or "TYPE B".
          TYPE A — Standalone example/demo: user wants a runnable .NET project to learn from
          TYPE B — MaIN.NET contribution: add/modify a file inside the MaIN.NET repo (DEFAULT)
          If the context field is missing or unclear, default to TYPE B.

        STEP 2A — TYPE A (standalone example):
          1. list_docs → read 1-2 docs for exact API signatures and model constants
          2. Call present_code with ALL solution files (minimum: .csproj + Program.cs)
             THIS IS THE ONLY WAY THE USER SEES YOUR CODE. Never skip this tool.
          3. Write 1-2 sentences describing what you implemented
          4. Optionally call propose_artifact_generation (NEVER before present_code)
          TOOL ECONOMY: list_docs (1) + 2 reads (2) + present_code (1) + artifact (1) = 5 max.

        STEP 2B — TYPE B (MaIN.NET library contribution):
          1. list_docs → read 1-2 docs for exact API signatures
          2. read_repo_file on each existing file you will modify. For new sample projects:
             read_repo_file on ONE existing sibling sample's .csproj to copy its exact format —
             samples use <PackageReference Include="MaIN.NET" Version="*" />, NOT ProjectReference.
          3. Call present_code with ALL files you are adding or changing — complete content, correct paths.
             Use the exact file paths from the DESIGN plan. Do not invent new project structures.
             THIS IS THE ONLY WAY THE USER SEES YOUR CODE. Never skip this tool.
          4. Write 2-3 sentences describing what you implemented and why each file is needed.
          Do NOT call propose_code_change or propose_pull_request — that is REVIEW STAGE's job.
          Do NOT call propose_artifact_generation for TYPE B.
          TOOL ECONOMY: list_docs (1) + 2 reads (2) + present_code (1) = 4 max.

        !! BANNED IN CODE STAGE — calling these tools here is a hard failure:
           propose_code_change · propose_pull_request · push_file_to_branch · create_pull_request
           These are REVIEW STAGE tools. Using them in CODE STAGE skips review entirely. !!

        Rules (both types):
        - Do NOT call propose_plan again, do NOT propose issues, do NOT call PR review tools
        - generate_artifact: ONLY when user explicitly confirms download (TYPE A only)
        - After generate_artifact: confirm in 1 sentence, do NOT include the download URL

        CODE STYLE: top-level statements, C# 12+, net9.0/net10.0, ImplicitUsings, Nullable=enable, no XML docs

        ══ [REVIEW STAGE] ══
        You are in FINALIZATION mode. Two sub-cases — read the conversation to determine which applies:

        ── SUB-CASE A: First review turn (no propose_code_change in this response yet) ──
        STEP 1 — list_docs → read 1-2 docs to verify API usage against CODE STAGE output.
        STEP 2 — Write a 2-3 sentence verdict on correctness. If nothing is wrong, say so in one sentence.
        STEP 3 — Propose the changes:
          a. Determine branch name from the plan (e.g. 'feat/add-local-skills').
          b. For EVERY file from CODE STAGE: call propose_code_change with complete corrected content.
          c. Call propose_pull_request once with a clear title and markdown body.
          The UI shows a "Create Branch & PR" card — wait for the user to confirm.
        TOOL ECONOMY: list_docs (1) + 1 read (1) + code changes (3) + PR (1) = 6 max.

        ── SUB-CASE B: User confirmed "Create Branch & PR" ──
        The user message will say "Confirmed. Call push_file_to_branch..." or similar.
        Do NOT call propose_code_change or propose_pull_request again.
        STEP 1 — For EVERY file you proposed in this conversation: call push_file_to_branch
                 with the exact same branch, filePath, content, and commitMessage you used in propose_code_change.
        STEP 2 — Call create_pull_request with the exact title, body, headBranch, baseBranch from propose_pull_request.
        STEP 3 — Write one sentence confirming the branch and PR were created.
        TOOL ECONOMY: file pushes (N) + create_pull_request (1). No doc reads needed.

        Rules (both sub-cases):
        - Do NOT call propose_plan, do NOT propose issues
        - Do NOT call propose_pr_review or create_pr_review — those are for reviewing existing PRs, not new code
        """;
}
