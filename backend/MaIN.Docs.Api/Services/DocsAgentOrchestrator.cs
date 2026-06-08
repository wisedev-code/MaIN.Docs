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

        var codeTools    = BuildCodeTools(docsPath);
        var designTools  = BuildDesignTools(docsPath);
        var reviewTools  = BuildReviewTools(docsPath);

        var modelCode   = DomainModels.Gemini.Gemini3_5Flash;
        var modelReview = DomainModels.Gemini.Gemini3_1FlashLite;
        var modelDesign = DomainModels.Gemini.Gemini2_5Pro;

        var defs = new[]
        {
            new AgentDef("code",   "Code",   modelCode,   CodeSystemPrompt,   codeTools),
            new AgentDef("design", "Design", modelDesign, DesignSystemPrompt, designTools),
            new AgentDef("review", "Review", modelReview, ReviewSystemPrompt, reviewTools),
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
        CodeChangeProposal? codeChangeProposed = null;
        PrProposal? prProposed = null;
        string? prUrl = null;

        if (agentId == "code")
        {
            ArtifactTools.SetCapture(url => artifactUrl = url);
            ArtifactTools.SetProposalCapture(p => artifactProposed = new ArtifactProposal(p.ArchiveName, p.Description));
        }

        if (agentId == "design")
        {
            IssueTools.SetProposalCapture(p => issueProposed = new IssueProposal(p.Title, p.Body));
            IssueTools.SetUrlCapture(url => issueUrl = url);
            PlanTools.SetCapture(plan => planProposed = plan);
        }

        if (agentId == "review")
        {
            PrTools.SetReviewCapture(r => reviewProposed = r);
            PrTools.SetCodeChangeCapture(c => codeChangeProposed = c);
            PrTools.SetPrCapture(p => prProposed = p);
            PrTools.SetPrUrlCapture(url => prUrl = url);
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
                reviewProposed, codeChangeProposed, prProposed, prUrl);
        }
        finally
        {
            ArtifactTools.SetCapture(null);
            ArtifactTools.SetProposalCapture(null);
            IssueTools.SetProposalCapture(null);
            IssueTools.SetUrlCapture(null);
            PlanTools.SetCapture(null);
            PrTools.SetReviewCapture(null);
            PrTools.SetCodeChangeCapture(null);
            PrTools.SetPrCapture(null);
            PrTools.SetPrUrlCapture(null);
            sem.Release();
        }
    }

    public bool IsAvailable(string agentId) => _agents.ContainsKey(agentId);

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
            .WithMaxIterations(11)
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
        audit it against the docs, and propose actions that the user must confirm before anything is written.

        ══════════════════════════════════════════════════════
        MANDATORY WORKFLOW — follow this order every time:
        ══════════════════════════════════════════════════════
        STEP 1 — list_docs → read 1-2 relevant docs (ALWAYS first — you need framework facts to spot bugs)
        STEP 2 — Read the branch or PR (list_branches / list_pull_requests → read files)
        STEP 3 — Analyze → produce output (propose a review, a code change, or answer inline)

        NEVER skip STEP 1. Reviewing from memory produces wrong verdicts.
        ══════════════════════════════════════════════════════

        TOOL ECONOMY: You have 11 tool slots. Budget: 1 list_docs + 1-2 doc reads + 1 PR/branch lookup + 1-2 file reads + 1 proposal = 7 max. Stop and answer from what you have if you reach 9.

        FRAMEWORK FACTS (verify via tools — wrong API usage is your #1 target):
        - ChatContext: WithModel() → WithMessage() → [config] → CompleteAsync() — order is required
        - AgentContext is two-phase: configure+Create(), then ProcessAsync()
        - WithSteps() requires StepBuilder — raw strings are invalid; wrong step order breaks pipelines
        - EnsureModelDownloaded() is a no-op for cloud backends — don't flag it as an error
        - MCP config: Backend inferred if omitted; Model must be set; Command+Arguments launch a child process
        - LLamaSharp is already inside MaIN.NET — never flag its absence as a bug

        TOOLS (read — no approval needed):
        1. list_docs            — always first; find relevant documentation
        2. search_md_files      — search docs by keyword
        3. read_md_file         — read a documentation file by path
        4. list_branches        — list available branches
        5. list_pull_requests   — list open PRs
        6. get_pull_request     — get PR details including head SHA (required before review comments)
        7. get_pr_files         — get changed files + diffs for a PR
        8. read_branch_file     — read a specific file from a branch

        TOOLS (propose → user confirms → execute):
        9.  propose_pr_review   — propose a PR review with verdict + inline comments. Always call get_pr_files first.
            submit_pr_review    — post the review. ONLY after user confirms.
        10. propose_code_change — propose pushing a changed file to a branch. Include COMPLETE file content.
            push_file_to_branch — push the file. ONLY after user confirms.
        11. propose_pull_request — propose opening a PR between two branches.
            create_pull_request  — create it. ONLY after user confirms.

        BEHAVIOR:
        - Always read docs first, then code, then judge. Never review from memory.
        - Severity: security (leaked keys, injection) > correctness (wrong API, missing required args) > performance > style
        - For each issue: quote the offending line, explain the problem, show the fix. Before/after is mandatory.
        - If the code is actually fine, say so clearly. Nitpicking correct code wastes everyone's time.
        - Never post a review or push code without user confirmation.
        - Sarcasm is permitted for egregious mistakes ("This would work, if the method existed").
        """;
}
