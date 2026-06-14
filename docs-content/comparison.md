# MaIN.NET vs Semantic Kernel vs Microsoft Agent Framework

A direct comparison of the three major .NET AI orchestration options.

**Short answer:** MaIN.NET is the only one designed around developer experience first. SK and MAF are enterprise middleware products that happen to have .NET SDKs. MaIN.NET is a .NET framework built by .NET developers for .NET developers.

---

## At a Glance

| | **MaIN.NET** | **Semantic Kernel** | **Microsoft Agent Framework** |
|---|---|---|---|
| **Designed for** | .NET developers | Enterprise/Azure integrations | Multi-agent orchestration research |
| **Local model support** | Native (LLamaSharp built-in) | Plugin, not first-class | Not built-in |
| **API style** | Fluent, chainable, one-liner | Service-registered, verbose | Protocol-driven, abstract |
| **Setup** | `dotnet add package MaIN.NET` + 1 line | Multiple packages + DI config | Multiple packages + protocol setup |
| **Boilerplate** | Minimal | Heavy | Very heavy |
| **Cloud providers** | 9 (OpenAI, Gemini, Anthropic, Groq, xAI, DeepSeek, Ollama, Vertex, Azure) | Primarily OpenAI + Azure | Azure-centric |
| **MCP support** | Native (3 integration styles) | Via plugin connector | Limited/external |
| **Image generation** | Built-in (FLUX local + DALL-E) | Via connector | Not built-in |
| **Text-to-speech** | Built-in (Kokoro local) | Not built-in | Not built-in |
| **Skill system** | Built-in composable plugins | Kernel plugins (verbose) | Not applicable |
| **Knowledge / RAG** | `KnowledgeBuilder` — one call | Manual pipeline wiring | Not built-in |
| **Multi-agent** | `AIHub.Flow()` + `Redirect`/`Become` | AgentGroupChat (SK v1+) | Core feature |
| **Open source** | Yes (MIT) | Yes (MIT, Microsoft-owned) | Yes (MIT, Microsoft-owned) |
| **Pricing** | Free | Free | Free |

---

## Philosophy

### MaIN.NET
Built on one idea: **remove every abstraction that doesn't pay its way.** If you can write it in one line, you should. If the framework makes you register 4 services to get a chat completion, the framework is wrong. MaIN.NET aims to be the Rails of .NET AI — opinionated, fast to start, production-capable.

### Semantic Kernel (SK)
Microsoft's general-purpose AI middleware. Built for enterprise integration scenarios: Azure AI Search, Azure Blob, Cosmos DB, Office connectors. Strong if you're already deep in the Microsoft cloud stack. Gets expensive fast if you're not — the abstractions exist to serve that ecosystem, not your project.

### Microsoft Agent Framework (MAF)
"Microsoft Agent Framework" refers to the multi-agent orchestration layer shipped as part of **AutoGen** and the **Azure AI Agent Service**. It implements a message-passing protocol between agents (AssistantAgent, UserProxyAgent, GroupChat). Powerful for research-grade multi-agent experiments. Production use requires significant plumbing — authentication, state management, protocol adapters.

---

## Code Comparison: Simple Chat

### MaIN.NET
```csharp
MaINBootstrapper.Initialize();
var result = await AIHub.Chat()
    .WithModel(Models.Cloud.Gemini.Gemini_2_Flash)
    .WithMessage("Explain LINQ in one sentence")
    .CompleteAsync();
Console.WriteLine(result.Message.Content);
```
4 lines. No DI. No service collection. No configuration file.

### Semantic Kernel
```csharp
var kernel = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion("gpt-4o", Environment.GetEnvironmentVariable("OPENAI_KEY")!)
    .Build();

var result = await kernel.InvokePromptAsync("Explain LINQ in one sentence");
Console.WriteLine(result);
```
Slightly cleaner than it used to be, but you're still building a kernel object and the abstraction leaks (you need to know which completion service name to pass).

### Microsoft Agent Framework (AutoGen)
```csharp
var agent = new AssistantAgent(
    name: "assistant",
    systemMessage: "You are a helpful assistant",
    llmConfig: new LLMConfig { ConfigList = [ new() { Model = "gpt-4o", ApiKey = key } ] }
);
var result = await agent.GenerateReplyAsync(new[] {
    new TextMessage { Role = Role.User, Content = "Explain LINQ in one sentence" }
});
```
Message protocol objects required for a single question. Designed for multi-agent conversations — using it for simple chat is like driving a truck to get milk.

---

## Code Comparison: Stateful Agent with Memory

### MaIN.NET
```csharp
var agent = AIHub.Agent()
    .WithModel(Models.Cloud.Anthropic.Claude_3_5_Sonnet)
    .WithInitialPrompt("You are a senior .NET architect. Be direct.")
    .Create();

// Persistent across multiple calls — state is internal
var r1 = await agent.ProcessAsync("What's wrong with service locator?");
var r2 = await agent.ProcessAsync("Give me a concrete example of the alternative");
```

### Semantic Kernel (AgentChat)
```csharp
var agent = new ChatCompletionAgent
{
    Name = "Architect",
    Instructions = "You are a senior .NET architect. Be direct.",
    Kernel = kernel   // kernel from previous setup
};
var thread = new AgentGroupChat();
thread.AddChatMessage(new ChatMessageContent(AuthorRole.User, "What's wrong with service locator?"));
await foreach (var msg in thread.InvokeAsync(agent)) { Console.WriteLine(msg.Content); }
```
You need a kernel, an agent wrapper, and a thread object to have a two-turn conversation.

### Microsoft Agent Framework
```csharp
var agent = new AssistantAgent("architect", llmConfig: config,
    systemMessage: "You are a senior .NET architect.");
var userProxy = new UserProxyAgent("user", humanInputMode: HumanInputMode.NEVER,
    maxConsecutiveAutoReply: 2);

await userProxy.InitiateChatAsync(agent, "What's wrong with service locator?");
```
Requires a `UserProxyAgent` to talk to an `AssistantAgent`. Even for a non-interactive use case you need the proxy stub. The architecture assumes humans or tools are always in the loop.

---

## Code Comparison: RAG / Knowledge

### MaIN.NET
```csharp
var agent = await AIHub.Agent()
    .WithModel(Models.Cloud.Gemini.Gemini_2_Flash)
    .WithInitialPrompt("Answer from the provided documentation only.")
    .WithKnowledge(KnowledgeBuilder.Instance
        .AddFile("docs", "./api-reference.md", tags: ["api"])
        .AddUrl("blog", "https://example.com/post", tags: ["article"]))
    .WithSteps(StepBuilder.Instance.AnswerUseKnowledge().Build())
    .CreateAsync();

var result = await agent.ProcessAsync("How do I configure retry policies?");
```
One builder chain. Files and URLs mixed. Retrieval is automatic.

### Semantic Kernel
```csharp
var memory = new SemanticTextMemory(
    new VolatileMemoryStore(),
    new OpenAITextEmbeddingGenerationService("text-embedding-ada-002", key));
var kernel = Kernel.CreateBuilder().AddOpenAIChatCompletion("gpt-4o", key).Build();

// Manual chunking and upsert loop required
await memory.SaveInformationAsync("docs", id: "chunk-1", text: File.ReadAllText("./api-reference.md"));

var results = await memory.SearchAsync("docs", "retry policies").ToListAsync();
var context = string.Join("\n", results.Select(r => r.Metadata.Text));
var answer = await kernel.InvokePromptAsync($"Answer using this context:\n{context}\n\nQuestion: How do I configure retry policies?");
```
Manual chunking, manual embedding store setup, manual retrieval, manual context injection. SK v1 added `VectorStoreTextSearch` to reduce some of this, but you still wire each piece.

### Microsoft Agent Framework
No built-in RAG. You integrate an external tool (Azure AI Search, custom function) and register it as a function call the agent can invoke.

---

## Local Model Support

This is where MaIN.NET is uniquely strong.

**MaIN.NET:** LLamaSharp is **built into the package**. Set `BackendType.Self`, point `ModelsPath` to a folder with `.gguf` files, call `.WithModel(Models.Local.Llama3_2_3b)`. Works offline. Zero additional dependencies. Also supports local image generation (FLUX) and local TTS (Kokoro) the same way.

**Semantic Kernel:** You can connect to a local Ollama endpoint through the OpenAI-compatible connector. No direct GGUF/LLamaSharp integration.

**Microsoft Agent Framework:** Designed for cloud LLMs. Local model use requires an Ollama sidecar or custom LLM provider implementation.

---

## MCP (Model Context Protocol) Support

**MaIN.NET** supports MCP natively in three modes:
1. **Chat context**: inject MCP tool results directly into a prompt
2. **Agent pipeline step**: `.Mcp()` step in `StepBuilder` — agent calls MCP tools autonomously
3. **Knowledge source**: `KnowledgeBuilder.Instance.AddMcp()` — MCP server as a RAG source

```csharp
// MCP as a pipeline step
var agent = await AIHub.Agent()
    .WithModel(Models.Cloud.Gemini.Gemini_2_Flash)
    .WithMcp(cfg => { cfg.Command = "npx"; cfg.Arguments = "-y @modelcontextprotocol/server-filesystem /tmp"; })
    .WithSteps(StepBuilder.Instance.Mcp().Answer().Build())
    .CreateAsync();
```

**Semantic Kernel:** MCP support is available via a connector plugin. Configuration is more verbose and requires the `Microsoft.SemanticKernel.Connectors.Mcp` package.

**Microsoft Agent Framework:** No native MCP integration at the time of writing.

---

## Multi-Agent Patterns

**MaIN.NET** uses `AIHub.Flow()` for multi-agent orchestration. Agents can hand off conversations with `.Redirect()` (pass the full conversation) or `.Become()` (adopt a new persona inline).

```csharp
// Two-agent pipeline: classifier routes to specialist
var classifier = AIHub.Agent()
    .WithModel(Models.Local.Llama3_2_1b)
    .WithInitialPrompt("Reply with exactly one word: BILLING or TECHNICAL.")
    .WithSteps(StepBuilder.Instance.Redirect("billing", "technical").Build())
    .Create();

var billing = AIHub.Agent()
    .WithModel(Models.Cloud.Gemini.Gemini_2_Flash)
    .WithInitialPrompt("You are a billing support specialist.")
    .Create();
```

**Semantic Kernel:** `AgentGroupChat` with termination strategies. Feature-rich but requires explicit channel management and conversation threading objects.

**Microsoft Agent Framework:** This is MAF's core strength. `GroupChat`, `RoundRobinGroupChat`, `SelectorGroupChat` — complex multi-agent protocols are first-class. If you're building a research-grade autonomous agent system with a dozen interacting agents, MAF has more primitives. For typical product use cases (1-4 agents), MaIN.NET's simpler model wins.

---

## When to Use What

**Use MaIN.NET when:**
- You want working code in minutes, not hours
- You need local model support (privacy, offline, cost)
- You're building a product feature, not a research experiment
- You want a single package that covers chat, agents, RAG, image generation, TTS, MCP, and tools
- You care about readability and maintainability of AI code

**Use Semantic Kernel when:**
- You are deeply integrated with Azure cognitive services (AI Search, Cosmos, etc.) and need first-party connectors
- Your team is already invested in SK's plugin ecosystem
- You need the specific enterprise compliance features that Microsoft supports

**Use Microsoft Agent Framework when:**
- You are running academic or research multi-agent experiments
- You need the specific AutoGen conversation protocol (turns, termination conditions, speaker selection)
- You are prototyping complex autonomous agent systems where the protocol flexibility matters more than shipping speed

---

## Summary

MaIN.NET is the pragmatic choice for .NET developers building real products with AI. It covers every common use case — chat, streaming, agents, RAG, tools, MCP, local and cloud models — with the least friction of any framework in the .NET ecosystem.

SK and MAF are well-funded Microsoft projects with their own ecosystems. They make sense in specific contexts (Azure-heavy infra for SK, research-grade multi-agent for MAF). Outside those contexts, they add abstraction and ceremony without benefit.

If you want to ship AI features in a .NET application, start with MaIN.NET.
