# MaIN.NET Repository Structure

This document describes the repository layout, the Examples project organization, and the correct patterns for adding new examples. Read this before designing any changes to the codebase.

---

## Repository Layout

```
MaIN.NET/
├── src/
│   ├── MaIN.Core/          # Fluent API — AIHub, ChatContext, AgentContext, FlowContext
│   ├── MaIN.Services/      # LLM backends, skill system, step processors, data sources (ChatService, StepProcessor, LLMService, OpenAiCompatibleService, AnthropicService etc etc)
│   ├── MaIN.Domain/        # Domain models: Agent, Chat, Message, Skill, config classes
│   └── MaIN.Infrastructure/# Persistence (SQLite, MongoDB, filesystem)
├── Examples/
│   ├── Examples/           # ← THE main interactive example runner (console app - most of existing examples are there)
│   └── Examples.SimpleConsole/ # Minimal starter template
├── Tests/
│   ├── MaIN.Core.UnitTests/
│   ├── MaIN.Core.IntegrationTests/
│   └── MaIN.Core.E2ETests/
└── docs/
```

**Rule:** New examples always go in `Examples/Examples/` — never create a separate project.

---

## Examples Project Structure

```
Examples/Examples/
├── Examples.csproj         # References MaIN.Core, MaIN.Services
├── IExample.cs             # interface IExample { Task Start(); }
├── Program.cs              # DI setup + interactive menu
├── Chat/                   # All Chat examples
│   ├── ChatExample.cs              (local LLM)
│   ├── ChatExampleOpenAi.cs
│   ├── ChatExampleGemini.cs
│   ├── ChatExampleOllama.cs
│   ├── ChatExampleAnthropic.cs
│   ├── ChatExampleGroqCloud.cs
│   ├── ChatExampleXai.cs
│   ├── ChatExampleVertex.cs
│   ├── ChatExampleToolsSimple.cs
│   ├── ChatExampleToolsSimpleLocalLLM.cs
│   ├── ChatWithFilesExample.cs
│   ├── ChatWithVisionExample.cs
│   ├── ChatWithImageGenExample.cs
│   ├── ChatWithReasoningExample.cs
│   ├── ChatWithReasoningDeepSeekExample.cs
│   ├── ChatWithTextToSpeechExample.cs
│   ├── ChatFromExistingExample.cs
│   ├── ChatWithCustomModelIdExample.cs
│   ├── ChatCustomGrammarExample.cs
│   └── Mcp/
│       ├── McpExample.cs
│       └── McpAgentsExample.cs
├── Agents/                 # All Agent examples
│   ├── AgentExample.cs             (basic agent, local LLM)
│   ├── AgentConversationExample.cs
│   ├── AgentExampleTools.cs
│   ├── AgentsTalkingToEachOther.cs
│   ├── AgentsWithRedirectExample.cs
│   ├── AgentsWithRedirectImageExample.cs
│   ├── AgentWithApiDataSourceExample.cs
│   ├── AgentWithBecomeExample.cs
│   ├── AgentWithKnowledgeFileExample.cs
│   ├── AgentWithKnowledgeWebExample.cs
│   ├── AgentWithWebDataSourceOpenAiExample.cs
│   ├── MultiBackendAgentsWithRedirectExample.cs
│   ├── DocsAgentExample.cs
│   ├── Skills/
│   │   ├── AgentWithFileSkillExample.cs
│   │   ├── AgentWithFolderSkillExample.cs
│   │   ├── AgentWithCustomCodeSkillExample.cs
│   │   ├── AgentWithAllSkillsExample.cs
│   │   └── AgentWithMcpFileWriterSkillExample.cs
│   └── Flows/
│       ├── AgentsComposedAsFlowExample.cs
│       └── AgentsFlowLoadedExample.cs
├── Utils/
│   ├── OpenAiExampleSetup.cs   # OpenAiExample.Setup()
│   ├── GeminiExampleSetup.cs   # GeminiExample.Setup()
│   ├── OllamaExample.cs        # OllamaExample.Setup()
│   ├── Tools.cs                # NoteTools helpers for tool-call examples
│   └── DocsTools.cs
├── skills/                 # .md and folder-based skills loaded at startup
│   ├── file-journalist.md
│   ├── code-review/SKILL.md
│   └── funfact-writer/SKILL.md
├── Files/
│   └── Knowledge/          # RAG source files (people.md, organization.md, events.md)
└── appsettings.json
```

---

## Adding a New Example — Exact Steps

### Step 1: Create the example class

Place it in the appropriate subfolder:
- `Examples/Examples/Chat/MyNewExample.cs` — for chat-based examples
- `Examples/Examples/Agents/MyNewExample.cs` — for agent examples
- `Examples/Examples/Agents/Skills/MyNewExample.cs` — for skill examples

```csharp
using MaIN.Core.Hub;
using MaIN.Domain.Models;

namespace Examples.Agents;   // or Examples.Chat, Examples.Agents.Skills, etc.

public class MyNewExample : IExample
{
    public async Task Start()
    {
        // Setup backend if not using local LLM:
        // GeminiExample.Setup();   // for Gemini
        // OpenAiExample.Setup();   // for OpenAI
        // OllamaExample.Setup();   // for Ollama

        Console.WriteLine("Description of what this example demonstrates");

        var context = await AIHub.Agent()
            .WithModel(Models.Local.Llama3_2_3b)     // or Models.Gemini.Gemini3_5Flash etc.
            .WithInitialPrompt("System prompt here")
            .CreateAsync();

        var result = await context.ProcessAsync("User message here");
        Console.WriteLine(result.Message.Content);
    }
}
```

### Step 2: Register in Program.cs (TWO places)

**In `RegisterExamples()`** — add a transient DI registration:
```csharp
services.AddTransient<MyNewExample>();
```

**In `ExampleRegistry.GetAvailableExamples()`** — add to the menu list:
```csharp
("■ My New Example Description", serviceProvider.GetRequiredService<MyNewExample>()),
```

Both changes are in `Examples/Examples/Program.cs`.

---

## Key Namespaces

| Namespace | What goes there |
|-----------|----------------|
| `Examples.Chat` | All `ChatContext`-based examples |
| `Examples.Chat.Mcp` | MCP + chat examples |
| `Examples.Agents` | All `AgentContext`-based examples |
| `Examples.Agents.Skills` | Skill-focused agent examples |
| `Examples.Agents.Flows` | Flow / multi-agent pipeline examples |
| `Examples.Mcp` | Standalone MCP client examples |
| `Examples.Utils` | Shared helpers (backend setup, tool functions) |

---

## Core API Reference

### AIHub (MaIN.Core.Hub)

Static entry point for everything:

```csharp
AIHub.Chat()    // → ChatContext
AIHub.Agent()   // → AgentContext (IAgentContextExecutor after Create/CreateAsync)
AIHub.Flow()    // → FlowContext
AIHub.Model()   // → ModelContext (download local models)
AIHub.Mcp()     // → McpContext
```

### ChatContext — key methods

```csharp
AIHub.Chat()
    .WithModel(string modelId)
    .WithMessage(string message)
    .WithMessages(List<Message> history)
    .WithSystemPrompt(string prompt)
    .WithFiles(List<string> filePaths)
    .WithTools(ToolsConfiguration tools)
    .WithInferenceParams(IBackendInferenceParams p)
    .WithMemoryParams(MemoryParams p)
    .EnsureModelDownloaded()
    .CompleteAsync()            // → ChatResult
    .FromExisting(string chatId)
```

### AgentContext — key methods

```csharp
// Configure:
AIHub.Agent()
    .WithModel(string modelId)
    .WithInitialPrompt(string systemPrompt)
    .WithName(string name)
    .WithId(string id)
    .WithDescription(string desc)
    .WithSteps(List<string> steps)            // pipeline steps
    .WithBehaviour(string name, string instruction)
    .WithKnowledge(KnowledgeBuilder knowledge) // RAG
    .WithInMemoryKnowledge(KnowledgeBuilder k) // transient RAG (no persist)
    .WithSource(IAgentSource src, AgentSourceType type)
    .WithSkill(string name)
    .WithSkills(List<AgentSkill> skills)
    .WithAllSkills()
    .WithTools(ToolsConfiguration tools)
    .WithMcpConfig(Mcp mcp)
    .WithInferenceParams(IBackendInferenceParams p)
    .WithMemoryParams(MemoryParams p)
    .WithOrder(int order)                     // execution order in flows
    .EnsureModelDownloaded()
    .DisableCache()
    .Create()          // sync — returns IAgentContextExecutor
    .CreateAsync()     // async — returns IAgentContextExecutor

// Execute (IAgentContextExecutor):
await context.ProcessAsync(string message)
await context.ProcessAsync(Chat chat)
await context.ProcessAsync(Message msg)
await context.ProcessAsync(IEnumerable<Message> messages)
// All support optional token streaming callback: Func<LLMTokenValue, Task>?
// And tool invocation callback: Func<ToolInvocation, Task>?

// Query:
context.GetAgent()
context.GetAgentId()
context.GetChat()
context.GetAllAgents()
context.GetAgentById(string id)
context.FromExisting(string agentId)  // load persisted agent
context.RestartChat()
context.Delete()
context.Exists()
```

### StepBuilder — pipeline steps

```csharp
StepBuilder.Instance
    .Answer()                           // plain LLM response
    .AnswerUseMemory()                  // with short-term memory
    .AnswerUseKnowledge()               // with RAG lookup
    .AnswerUseKnowledgeWithTags(tags)   // RAG filtered by tags
    .AnswerUseKnowledgeAndMemory()
    .Become("BehaviourName")            // switch to a named behaviour
    .FetchData()                        // pull from IAgentSource
    .Mcp()                              // MCP step
    .Redirect("agentId")               // hand off to another agent
    .Build()                            // → List<string>
```

### KnowledgeBuilder — RAG

```csharp
KnowledgeBuilder.Instance
    .ForAgent(agent)
    .AddFile("label", "/path/to/file", new[] { "tag1" })
    .AddUrl("label", "https://...", new[] { "tag" })
    .AddText("label", "raw text content", new[] { "tag" })
    .AddMcp(mcpConfig, new[] { "tag" })
    .WithPersistence(true)
    .Build()
```

### ToolsConfigurationBuilder — function tools

```csharp
using MaIN.Core.Hub.Utils;       // ToolsConfigurationBuilder
using MaIN.Domain.Entities.Tools; // ToolsConfiguration, ToolDefinition

new ToolsConfigurationBuilder()
    .AddTool<TArgs>(
        "tool_name",
        "Description for the LLM",
        new { type = "object", properties = new { ... }, required = new[] { "field" } },
        MyToolClass.MyMethod)
    .WithToolChoice("auto")   // "auto" | "required" | "none"
    .WithMaxIterations(10)
    .Build()
```

> **COMPILE RULE:** forgetting `using MaIN.Core.Hub.Utils;` causes
> `CS0246: The type or namespace name 'ToolsConfigurationBuilder' could not be found`.

---

## Model Constants (MaIN.Domain.Models)

```csharp
// Local (llama.cpp / GGUF):
Models.Local.Llama3_2_3b
Models.Local.Llama3_2_1b
Models.Local.Mistral7b
Models.Local.Phi3Mini
// ... many more

// OpenAI:
Models.OpenAi.Gpt4o
Models.OpenAi.Gpt4oMini
Models.OpenAi.O1
Models.OpenAi.O3Mini

// Gemini:
Models.Gemini.Gemini3_5Flash
Models.Gemini.Gemini3_1ProPreview
Models.Gemini.Flash2_0

// Anthropic:
Models.Anthropic.Claude3_5Sonnet
Models.Anthropic.Claude3_5Haiku

// Ollama — use string model name directly, e.g. "llama3.2"
// DeepSeek, GroqCloud, xAI, Vertex — use their respective Models.* constants
```

---

## Backend Configuration

The Examples project initializes MaIN via `services.AddMaIN(configuration)` + `serviceProvider.UseMaIN()` in `Program.cs`. Individual examples that need a non-default backend call a setup helper at the top of `Start()`:

```csharp
// Switch to OpenAI for this example:
OpenAiExample.Setup();    // defined in Utils/OpenAiExampleSetup.cs

// Switch to Gemini:
GeminiExample.Setup();    // defined in Utils/GeminiExampleSetup.cs

// Switch to Ollama:
OllamaExample.Setup();    // defined in Utils/OllamaExample.cs
```

These helpers call `MaINBootstrapper.Initialize(options => { options.BackendType = ...; options.XxxKey = ...; })`.

For the local (default) backend no setup call is needed — it uses the `appsettings.json` defaults.

---

## Skills System

Skills are composable behaviour modules that can be attached to agents. Three forms:

**1. Single-file skill** (`skills/my-skill.md`):
```yaml
---
name: my-skill
description: What it does
version: 1.0.0
steps: [BECOME+RoleName, ANSWER]
placement: before       # before | after | replace
priority: 100
behaviours:
  RoleName: "System prompt fragment..."
tags: [category]
---
Optional extra instruction appended to system prompt.
```

**2. Folder skill** (`skills/my-skill/SKILL.md`) — same frontmatter, plus supporting files referenced via `includes:`. The folder is bundled into the skill.

**3. C# code skill** — implement `IAgentSkillProvider`, registered in DI as `services.AddSingleton<IAgentSkillProvider, MySkill>()`. Allows injecting C# function tools, data sources, and MCP configs into an agent via the skill system.

Skills in the `./skills/` directory are auto-loaded at startup via `services.AddSkillsFromDirectory("./skills")`. Agents opt into skills with `.WithSkill("name")`, `.WithSkills(list)`, or `.WithAllSkills()`.

---

## Flow / Multi-Agent Pipelines

Agents can be chained into a flow two ways:

**1. Via `REDIRECT` steps** (runtime routing):
```csharp
var agentA = await AIHub.Agent()
    .WithModel(Models.Gemini.Gemini3_5Flash)
    .WithSteps(StepBuilder.Instance.Answer().Redirect("agent-b").Build())
    .CreateAsync();

var agentB = await AIHub.Agent()
    .WithId("agent-b")
    .WithModel(Models.Local.Llama3_2_3b)
    .WithSteps(StepBuilder.Instance.Answer().Build())
    .CreateAsync();
```

**2. Via `FlowContext`** (serializable pipelines):
```csharp
var flow = await AIHub.Flow()
    .WithName("My Pipeline")
    .AddAgent(agentA.GetAgent())
    .AddAgent(agentB.GetAgent())
    .Save("./my-flow.zip");

// Load and run later:
var loaded = await AIHub.Flow().Load("./my-flow.zip");
var result = await loaded.ProcessAsync("input message");
```

Flow examples are in `Examples/Examples/Agents/Flows/`.
