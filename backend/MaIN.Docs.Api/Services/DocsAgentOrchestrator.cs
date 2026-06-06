using MaIN.Core.Hub;
using MaIN.Core.Hub.Contexts.Interfaces.AgentContext;
using MaIN.Core.Hub.Utils;
using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Tools;
using MaIN.Domain.Models;
using DomainModels = MaIN.Domain.Models.Models;

namespace MaIN.Docs.Api.Services;

public class DocsAgentOrchestrator(DocsLoader loader, ILogger<DocsAgentOrchestrator> logger)
{
    private readonly Dictionary<string, IAgentContextExecutor> _agents = new();
    private readonly Dictionary<string, SemaphoreSlim> _locks = new();

    public async Task InitializeAsync()
    {
        var docsPath = loader.DocsPath;

        var defs = new[]
        {
            new AgentDef("code",   "Code",   DomainModels.Ollama.Llama4Scout,    CodeSystemPrompt,   CodeTools(docsPath)),
            new AgentDef("design", "Design", DomainModels.Ollama.Gemma3_12b,     DesignSystemPrompt, DesignTools(docsPath)),
            new AgentDef("review", "Review", DomainModels.OpenAi.Gpt4_1,         ReviewSystemPrompt, ReviewTools(docsPath)),
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

    public async Task<string> ProcessAsync(
        string agentId,
        IEnumerable<Message> messages,
        CancellationToken ct)
    {
        if (!_agents.TryGetValue(agentId, out var ctx))
            throw new KeyNotFoundException($"Agent '{agentId}' is not available.");

        var sem = _locks[agentId];

        if (!await sem.WaitAsync(TimeSpan.FromSeconds(60), ct))
            throw new TimeoutException($"Agent '{agentId}' is busy. Please retry.");

        try
        {
            var result = await ctx.ProcessAsync(messages);
            return result.Message.Content;
        }
        finally
        {
            sem.Release();
        }
    }

    public bool IsAvailable(string agentId) => _agents.ContainsKey(agentId);

    private record AgentDef(string Id, string Name, string Model, string SystemPrompt, ToolsConfiguration Tools);

    private static ToolsConfiguration CodeTools(string docsPath) =>
        new ToolsConfigurationBuilder()
            .AddTool("search_md_files", "Search docs for a keyword. Returns matching file paths and context snippets.",
                new
                {
                    type = "object",
                    properties = new { query = new { type = "string", description = "Keyword or phrase to search for" } },
                    required = new[] { "query" }
                },
                MdTools.SearchIn(docsPath))
            .AddTool("read_md_file", "Read the full content of a docs Markdown file by its path.",
                new
                {
                    type = "object",
                    properties = new { path = new { type = "string", description = "Absolute path to the .md file" } },
                    required = new[] { "path" }
                },
                MdTools.Read())
            .WithToolChoice("auto")
            .Build();

    private static ToolsConfiguration DesignTools(string docsPath) =>
        new ToolsConfigurationBuilder()
            .AddTool("search_md_files", "Search docs for a keyword. Returns matching file paths and context snippets.",
                new
                {
                    type = "object",
                    properties = new { query = new { type = "string", description = "Keyword or phrase to search for" } },
                    required = new[] { "query" }
                },
                MdTools.SearchIn(docsPath))
            .WithToolChoice("auto")
            .Build();

    private static ToolsConfiguration ReviewTools(string docsPath) =>
        new ToolsConfigurationBuilder()
            .AddTool("search_md_files", "Search docs for a keyword. Returns matching file paths and context snippets.",
                new
                {
                    type = "object",
                    properties = new { query = new { type = "string", description = "Keyword or phrase to search for" } },
                    required = new[] { "query" }
                },
                MdTools.SearchIn(docsPath))
            .AddTool("read_md_file", "Read the full content of a docs Markdown file by its path.",
                new
                {
                    type = "object",
                    properties = new { path = new { type = "string", description = "Absolute path to the .md file" } },
                    required = new[] { "path" }
                },
                MdTools.Read())
            .WithToolChoice("auto")
            .Build();

    private const string CodeSystemPrompt = """
        You are an expert MaIN.NET code assistant. MaIN.NET is an open-source .NET AI orchestration
        framework that makes building AI-powered applications simple — locally via LLamaSharp or in
        the cloud via GroqCloud, OpenAI, Gemini, Anthropic, and more.

        Your role: generate complete, correct, copy-paste-ready C# code examples.

        Use search_md_files to find relevant documentation, then read_md_file to get full content
        before answering. Always ground code examples in what the docs say.

        Key API patterns you know well:
        - DI setup: services.AddMaIN(config, s => { s.BackendType = BackendType.GroqCloud; s.GroqCloudKey = "..."; })
          followed by app.Services.UseMaIN()
        - Chat (stateless, per-request): AIHub.Chat()
            .WithModel(Models.Groq.Llama4Scout17b)
            .WithSystemPrompt("...")
            .WithMessage("...")
            .CompleteAsync(changeOfValue: async token => { /* stream token */ })
        - Agent (stateful with tools): AIHub.Agent()
            .WithModel(Models.Ollama.Llama4Scout)
            .WithInitialPrompt("You are...")
            .WithTools(new ToolsConfigurationBuilder()...Build())
            .CreateAsync()
          followed by context.ProcessAsync(messages, tokenCallback: cb)
        - Flows: AIHub.Flow() for orchestrating multiple agents in sequence
        - Model constants: Models.Ollama.Llama4Scout, Models.Ollama.Gemma3_12b, Models.OpenAi.Gpt4_1,
          Models.Gemini.Gemini3_5Flash, Models.Anthropic.ClaudeOpus4_7
        - Rate limiting + X-Api-Key middleware for production APIs

        Always include using statements. Prefer Minimal API (Program.cs) style.
        Format all code in ```csharp blocks. Show complete, runnable examples.
        """;

    private const string DesignSystemPrompt = """
        You are an expert AI systems architect specializing in MaIN.NET. You reason about system
        design, tradeoffs, and production patterns for AI-powered .NET applications.

        Your role: help design multi-agent systems, select backends, plan tool compositions, and
        guide production architecture decisions.

        Use search_md_files to discover available documentation before answering.

        Architecture concepts you reason about:
        - Agent step pipelines: FETCH_DATA → REDIRECT → BECOME → ANSWER
        - Flow orchestration with AIHub.Flow() for multi-stage agent pipelines
        - Backend selection: GroqCloud (fast/cheap), OpenAI (tools/vision), Anthropic (long context),
          Gemini (multimodal), local LLMs (privacy/offline)
        - Tool composition: give each agent only the tools its role needs
        - Concurrency patterns: stateless Chat() for web APIs vs stateful Agent() for workflows,
          SemaphoreSlim for agent serialization in ASP.NET
        - Cost optimisation: model tier selection, context window sizing, caching
        - Deployment: Azure Container Apps, Static Web Apps + Container App split, Docker Compose

        Always reason about tradeoffs. Give concrete model recommendations. Highlight production
        considerations: rate limiting, key rotation, resilience, and observability.
        """;

    private const string ReviewSystemPrompt = """
        You are an expert MaIN.NET code reviewer and debugger. You find bugs, misconfigurations,
        security issues, and performance problems in MaIN.NET applications and fix them.

        Your role: audit MaIN.NET usage for correctness, performance, and security.

        Use search_md_files to locate relevant docs, then read_md_file to verify correct API usage
        before flagging issues.

        Common issues you catch:
        1. Race conditions — shared AgentContext without SemaphoreSlim in a web API
        2. Missing app.Services.UseMaIN() — agents/chats silently fail
        3. LLamaSharp log spam — missing AIHub.Extensions.DisableLLamaLogs()
        4. Stale model IDs — using obsolete KnownModels instead of Models.* or ModelRegistry
        5. Model not registered — throws ModelNotRegisteredException at runtime
        6. Blazor JS mismatch — using blazor.web.js for server-only publish (should be blazor.server.js)
        7. Docker DataProtection — missing PersistKeysToFileSystem() causes antiforgery failures across restarts
        8. Missing CORS — Angular frontend blocked by browser
        9. Exposed API keys — keys in code or version control instead of environment variables
        10. Rate limiter not applied — missing .RequireRateLimiting("policy") on the endpoint

        Always provide before/after code examples. Explain WHY each issue is a problem.
        Prioritize issues by severity: security > correctness > performance > style.
        """;
}
