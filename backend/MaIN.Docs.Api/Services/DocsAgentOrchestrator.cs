using MaIN.Core.Hub;
using MaIN.Core.Hub.Contexts.Interfaces.AgentContext;
using MaIN.Core.Hub.Utils;
using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Tools;
using MaIN.Domain.Models;
using MaIN.Docs.Api.Models;
using DomainModels = MaIN.Domain.Models.Models;

namespace MaIN.Docs.Api.Services;

public class DocsAgentOrchestrator(DocsLoader loader, ArtifactService artifactService, ILogger<DocsAgentOrchestrator> logger)
{
    private readonly Dictionary<string, IAgentContextExecutor> _agents = new();
    private readonly Dictionary<string, SemaphoreSlim> _locks = new();

    public async Task InitializeAsync()
    {
        var docsPath = loader.DocsPath;
        MdTools.Initialize(docsPath, logger);
        ArtifactTools.Init(artifactService);

        var sharedTools = BuildTools(docsPath);
        var codeTools   = BuildCodeTools(docsPath);

        var modelCode   = DomainModels.Gemini.Gemini3_5Flash;
        var modelReview = DomainModels.Gemini.Gemini3_1FlashLite;
        var modelDesign = DomainModels.Gemini.Gemini2_5Pro;

        var defs = new[]
        {
            new AgentDef("code",   "Code",   modelCode,   CodeSystemPrompt,   codeTools),
            new AgentDef("design", "Design", modelDesign, DesignSystemPrompt, sharedTools),
            new AgentDef("review", "Review", modelReview, ReviewSystemPrompt, sharedTools),
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
        if (agentId == "code")
        {
            ArtifactTools.SetCapture(url => artifactUrl = url);
            ArtifactTools.SetProposalCapture(p => artifactProposed = new ArtifactProposal(p.ArchiveName, p.Description));
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

            return new AgentResult(result.Message.Content ?? string.Empty, toolsUsed, estimatedTokens, artifactUrl, artifactProposed);
        }
        finally
        {
            ArtifactTools.SetCapture(null);
            ArtifactTools.SetProposalCapture(null);
            sem.Release();
        }
    }

    public bool IsAvailable(string agentId) => _agents.ContainsKey(agentId);

    private record AgentDef(string Id, string Name, string Model, string SystemPrompt, ToolsConfiguration Tools);

    private static ToolsConfiguration BuildTools(string docsPath) =>
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
                MdTools.Read)
            .WithToolChoice("auto")
            .Build();

    private static ToolsConfiguration BuildCodeTools(string docsPath) =>
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
                MdTools.Read)
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
            .WithToolChoice("auto")
            .Build();

    private const string CodeSystemPrompt =
        "You are a MaIN.NET code assistant. Use list_docs to discover docs, then search_md_files or read_md_file before answering. " +
        "Generate complete, runnable C# code examples in ```csharp blocks with using statements. Prefer Minimal API style. " +
        "You have two artifact tools — use judgment: " +
        "• propose_artifact_generation — call this when your response contains a COMPLETE, self-contained solution (not a snippet or an explanation). " +
        "  Complete means: has all required using statements, a proper entry point, and could be pasted into a file and run. " +
        "  Skip it for partial examples, conceptual answers, or when you'd need more info to make the code runnable. " +
        "  If you're missing key info (which LLM backend, specific use-case details), ask first — then produce the full solution and propose. " +
        "• generate_artifact — only when the user explicitly says yes / confirms download. " +
        "  Package a full .NET project: .csproj with all NuGet references, Program.cs, and any supporting files — enough to 'dotnet run' immediately.";

    private const string DesignSystemPrompt =
        "You are a MaIN.NET architecture assistant. Use list_docs to discover docs, then search_md_files or read_md_file before answering. " +
        "Reason about tradeoffs, backend selection, and production concerns (rate limiting, resilience, cost).";

    private const string ReviewSystemPrompt =
        "You are a MaIN.NET code reviewer. Use list_docs to discover docs, then search_md_files or read_md_file to verify correct API usage. " +
        "Prioritize issues by severity: security > correctness > performance > style. Show before/after examples.";
}
