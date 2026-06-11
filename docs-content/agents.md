# AgentContext

`AgentContext` is a stateful AI agent with a persistent chat session, tool use, knowledge retrieval, and a configurable execution pipeline. Access it via `AIHub.Agent()`.

## Two-Phase Pattern

```
Phase 1 — Configure + Create:
  AIHub.Agent()
    → WithModel(modelId)
      → WithInitialPrompt(prompt)
        → [configuration]
          → Create() / CreateAsync()   →  IAgentContextExecutor

Phase 2 — Process:
  executor.ProcessAsync(message)       →  ChatResult
```

## Minimal Example

```csharp
var agent = AIHub.Agent()
    .WithModel(Models.Local.Llama3_2_3b)
    .WithInitialPrompt("You are an NPC advisor in a Game of Thrones RPG.")
    .Create();

var result = await agent.ProcessAsync("Where is the Iron Throne located?");
Console.WriteLine(result.Message.Content);
```

---

## IAgentBuilderEntryPoint

---

### `WithModel`

```csharp
IAgentConfigurationBuilder WithModel(string modelId)
```

Sets the model for this agent. Must be called first.

| Parameter | Type | Description |
|---|---|---|
| `modelId` | `string` | Model identifier. Use `Models.*` constants or a plain string. Throws if the ID is not in the registry. |

---

### `FromExisting`

```csharp
Task<IAgentContextExecutor> FromExisting(string agentId)
```

Loads a previously persisted agent, restoring its configuration, behaviours, chat history, and knowledge base (if any).

| Parameter | Type | Description |
|---|---|---|
| `agentId` | `string` | ID from a previous `GetAgentId()` call. |

---

## IAgentConfigurationBuilder

All methods return `IAgentConfigurationBuilder` and chain freely.

---

### `WithInitialPrompt`

```csharp
IAgentConfigurationBuilder WithInitialPrompt(string prompt)
```

Sets the agent's system instruction, stored as `AgentConfig.Instruction`. This is the primary way to define the agent's persona and task framing.

| Parameter | Type | Description |
|---|---|---|
| `prompt` | `string` | The system instruction. Inserted as the first system-role message on every `ProcessAsync` call. |

---

### `WithId`

```csharp
IAgentConfigurationBuilder WithId(string id)
```

Assigns a fixed ID to the agent before `Create()`. Required when another agent's step pipeline will `Redirect` to this agent by ID.

| Parameter | Type | Description |
|---|---|---|
| `id` | `string` | Any unique string. Typically a `Guid.NewGuid().ToString()` captured before creating the target agent. |

---

### `WithName`

```csharp
IAgentConfigurationBuilder WithName(string name)
```

| Parameter | Type | Description |
|---|---|---|
| `name` | `string` | Human-readable display name. Used in flow serialization and logs. |

---

### `WithOrder`

```csharp
IAgentConfigurationBuilder WithOrder(int order)
```

| Parameter | Type | Description |
|---|---|---|
| `order` | `int` | Execution priority within a `FlowContext`. Lower values execute first. |

---

### `EnsureModelDownloaded`

```csharp
IAgentConfigurationBuilder EnsureModelDownloaded()
```

Downloads the model file before `Create()` if it is not already on disk. No parameters. No-op for cloud backends.

---

### `DisableCache`

```csharp
IAgentConfigurationBuilder DisableCache()
```

Bypasses the model token cache for all `ProcessAsync` calls on this agent. No parameters.

---

### `WithBehaviour`

```csharp
IAgentConfigurationBuilder WithBehaviour(string name, string instruction)
```

Registers a named behaviour and immediately sets it as the agent's active behaviour. Activated at runtime via a `BECOME` step in the pipeline.

| Parameter | Type | Description |
|---|---|---|
| `name` | `string` | Identifier used in `StepBuilder.Instance.Become("name")`. Also the key in the `Agent.Behaviours` dictionary. |
| `instruction` | `string` | Full system prompt that **replaces** the initial prompt when this behaviour is activated. |

---

### `WithSteps`

```csharp
IAgentConfigurationBuilder WithSteps(List<string> steps)
```

Defines the agent's execution pipeline. Steps run in order on every `ProcessAsync` call.

| Parameter | Type | Description |
|---|---|---|
| `steps` | `List<string>` | Ordered step identifiers. Always build via `StepBuilder.Instance` — do not pass raw strings. |

Common step chains:

| Chain | Behaviour |
|---|---|
| `StepBuilder.Instance.Answer().Build()` | Answer the user message directly |
| `StepBuilder.Instance.FetchData().Build()` | Pull from the data source, then stop |
| `StepBuilder.Instance.FetchData().Answer().Build()` | Pull data, then answer |
| `StepBuilder.Instance.FetchData().Become("name").Answer().Build()` | Pull data, switch persona, answer |
| `StepBuilder.Instance.AnswerUseKnowledge().Build()` | RAG retrieval + answer |
| `StepBuilder.Instance.Answer().Redirect(agentId).Build()` | Answer, then hand off to another agent |
| `StepBuilder.Instance.Mcp().Redirect(agentId).Build()` | Execute MCP tools, then hand off |

---

### `WithSource`

```csharp
IAgentConfigurationBuilder WithSource(IAgentSource source, AgentSourceType type)
```

Attaches an external data source fetched during a `FETCH_DATA` step.

| Parameter | Type | Description |
|---|---|---|
| `source` | `IAgentSource` | Source configuration object. Must match the `type` argument (see table below). |
| `type` | `AgentSourceType` | Enum value indicating the source kind. |

| `AgentSourceType` | Source class | Key properties |
|---|---|---|
| `API` | `AgentApiSourceDetails` | `Url`, `Method`, `ResponseType`, `ChunkLimit` |
| `File` | `AgentFileSourceDetails` | `Files` (list of paths) |
| `Web` | `AgentWebSourceDetails` | `Url` |
| `SQL` | `AgentSqlSourceDetails` | connection and query properties |
| `NoSQL` | `AgentNoSqlSourceDetails` | collection and filter properties |
| `Text` | `AgentTextSourceDetails` | `Content` (raw string) |

---

### `WithTools`

```csharp
IAgentConfigurationBuilder WithTools(ToolsConfiguration toolsConfiguration)
```

Attaches function-calling tools available during `ProcessAsync`.

| Parameter | Type | Description |
|---|---|---|
| `toolsConfiguration` | `ToolsConfiguration` | Built via `ToolsConfigurationBuilder`. Use `.AddTool(name, description, execute)` or the generic `.AddTool<TArgs>(name, description, schema, execute)` overload. Finish with `.WithToolChoice("auto")` and `.Build()`. |

> **COMPILE RULE — required `using` statements:**
> `ToolsConfigurationBuilder` lives in `MaIN.Core.Hub.Utils`; `ToolsConfiguration` (and
> `ToolDefinition`) live in `MaIN.Domain.Entities.Tools`. Add both:
> ```csharp
> using MaIN.Core.Hub.Utils;
> using MaIN.Domain.Entities.Tools;
> ```
> Omitting these causes `CS0246: The type or namespace name 'ToolsConfigurationBuilder'
> could not be found`.

---

### `WithInferenceParams`

```csharp
IAgentConfigurationBuilder WithInferenceParams(IBackendInferenceParams inferenceParams)
```

| Parameter | Type | Description |
|---|---|---|
| `inferenceParams` | `IBackendInferenceParams` | Provider-specific params. Match the active backend: `LocalInferenceParams` (Self), `OpenAiInferenceParams` (OpenAi), `VertexInferenceParams` (Vertex). |

---

### `WithMemoryParams`

```csharp
IAgentConfigurationBuilder WithMemoryParams(MemoryParams memoryParams)
```

| Parameter | Type | Description |
|---|---|---|
| `memoryParams` | `MemoryParams` | Context window config. Key property: `ContextSize` (int, token limit). Example: `new MemoryParams { ContextSize = 4096 }`. |

---

### `WithKnowledge` (delegate)

```csharp
IAgentConfigurationBuilder WithKnowledge(Func<KnowledgeBuilder, KnowledgeBuilder> knowledgeConfig)
```

Configures a RAG knowledge base inline. Knowledge is persisted to disk by default and reloaded on `FromExisting`.

| Parameter | Type | Description |
|---|---|---|
| `knowledgeConfig` | `Func<KnowledgeBuilder, KnowledgeBuilder>` | Receives a `KnowledgeBuilder` and returns it after adding sources via `.AddFile(...)`, `.AddUrl(...)`, or `.AddMcp(...)`. |

**`KnowledgeBuilder` methods:**

| Method | Parameters | Description |
|---|---|---|
| `AddFile(string name, string path, string[]? tags)` | `name`: label; `path`: file path; `tags`: routing hints for retrieval | Adds a local file (PDF, Markdown, image) as a knowledge source |
| `AddUrl(string name, string url, string[]? tags)` | `name`: label; `url`: page URL; `tags`: routing hints | Crawls the URL and indexes its content |
| `AddMcp(Mcp config, string[] tags)` | `config`: MCP server; `tags`: routing hints | Uses an MCP server as a retrieval source |

---

### `WithKnowledge` (builder)

```csharp
IAgentConfigurationBuilder WithKnowledge(KnowledgeBuilder knowledge)
```

| Parameter | Type | Description |
|---|---|---|
| `knowledge` | `KnowledgeBuilder` | A pre-configured `KnowledgeBuilder` instance. Call `KnowledgeBuilder.Instance.AddFile(...)...` before passing. |

---

### `WithKnowledge` (object)

```csharp
IAgentConfigurationBuilder WithKnowledge(Knowledge knowledge)
```

| Parameter | Type | Description |
|---|---|---|
| `knowledge` | `Knowledge` | A pre-built `Knowledge` object, e.g. obtained from a previous agent via `GetKnowledge()`. |

---

### `WithInMemoryKnowledge`

```csharp
IAgentConfigurationBuilder WithInMemoryKnowledge(Func<KnowledgeBuilder, KnowledgeBuilder> knowledgeConfig)
```

Same as `WithKnowledge(Func<...>)` but the knowledge is **not persisted to disk**. Rebuilt from scratch on each run. Use for volatile or session-specific data.

| Parameter | Type | Description |
|---|---|---|
| `knowledgeConfig` | `Func<KnowledgeBuilder, KnowledgeBuilder>` | Same as the persistent overload. |

---

### `WithMcpConfig`

```csharp
IAgentConfigurationBuilder WithMcpConfig(Mcp mcpConfig)
```

Attaches an MCP server that the agent can call via a `MCP` step. See McpContext docs for all `Mcp` fields.

| Parameter | Type | Description |
|---|---|---|
| `mcpConfig` | `Mcp` | MCP server config: `Name`, `Command`, `Arguments`, `Model`, `Backend`, `EnvironmentVariables`. If `Backend` is not set, it is inferred from the agent's own model. |

---

### `WithSkill`

```csharp
IAgentConfigurationBuilder WithSkill(string skillName)
IAgentConfigurationBuilder WithSkill(AgentSkill skill)
```

Queues a skill to be applied during `CreateAsync`. Skills inject steps, tools, and instruction fragments into the agent.

| Overload | Parameter | Description |
|---|---|---|
| `WithSkill(string)` | `skillName: string` | Name of a registered skill: built-in (`"web-search"`, `"journalist"`, etc.), folder-based (from `SkillsDirectory`), or DI-registered. |
| `WithSkill(AgentSkill)` | `skill: AgentSkill` | Inline skill definition. Use for one-off skills without DI registration. |

---

### `WithSkills`

```csharp
IAgentConfigurationBuilder WithSkills(params string[] skillNames)
```

Queues multiple named skills at once.

| Parameter | Type | Description |
|---|---|---|
| `skillNames` | `params string[]` | Names of skills to attach. Applied in the order provided. |

---

### `WithAllSkills`

```csharp
IAgentConfigurationBuilder WithAllSkills()
```

Queues all registered skills, excluding Replace-type and built-in provider skills. No parameters.

---

### `Create` / `CreateAsync`

```csharp
IAgentContextExecutor Create(bool flow = false, bool interactiveResponse = false)
Task<IAgentContextExecutor> CreateAsync(bool flow = false, bool interactiveResponse = false)
```

Finalises the agent, applies pending skills, and returns an executor ready to receive messages.

| Parameter | Type | Default | Description |
|---|---|---|---|
| `flow` | `bool` | `false` | Set to `true` when this agent will be added to a `FlowContext`. Affects internal chat session management (the session is shared across the flow). |
| `interactiveResponse` | `bool` | `false` | If `true`, each `ProcessAsync` call streams tokens to `Console.Write` as they arrive, in addition to returning the complete `ChatResult`. |

Use `CreateAsync` when the agent has pending skill uploads (e.g. cloud provider native skills) or when `EnsureModelDownloaded` is set.

---

## IAgentContextExecutor — ProcessAsync

All overloads return `Task<ChatResult>`.

---

### `ProcessAsync` (string)

```csharp
Task<ChatResult> ProcessAsync(
    string message,
    bool translate = false,
    Func<LLMTokenValue, Task>? tokenCallback = null,
    Func<ToolInvocation, Task>? toolCallback = null)
```

The most common overload. Appends the message to the agent's chat history and runs the step pipeline.

| Parameter | Type | Default | Description |
|---|---|---|---|
| `message` | `string` | required | The user's input text. |
| `translate` | `bool` | `false` | Auto-translates the response to the host system's locale. |
| `tokenCallback` | `Func<LLMTokenValue, Task>?` | `null` | Called for each streamed token. `LLMTokenValue` has `Text` (string) and `Type` (`Message` / `Reason` / `ToolCall` / `Special`). |
| `toolCallback` | `Func<ToolInvocation, Task>?` | `null` | Called whenever the agent invokes a tool. `ToolInvocation` has `Name` (string), `Arguments` (JSON string), and `Result` (object). |

---

### `ProcessAsync` (Message)

```csharp
Task<ChatResult> ProcessAsync(
    Message message,
    bool translate = false,
    Func<LLMTokenValue, Task>? tokenCallback = null,
    Func<ToolInvocation, Task>? toolCallback = null)
```

Use when the input carries files, images, or custom properties.

| Parameter | Type | Default | Description |
|---|---|---|---|
| `message` | `Message` | required | Structured message. Key properties: `Content` (string), `Role` (string), `Type` (MessageType), `Files` (`List<FileInfo>?`) for document attachments, `Images` (`List<byte[]>?`) for vision. |
| `translate` | `bool` | `false` | Auto-translate response. |
| `tokenCallback` | `Func<LLMTokenValue, Task>?` | `null` | Token stream callback (same as string overload). |
| `toolCallback` | `Func<ToolInvocation, Task>?` | `null` | Tool invocation callback (same as string overload). |

---

### `ProcessAsync` (IEnumerable\<Message\>)

```csharp
Task<ChatResult> ProcessAsync(
    IEnumerable<Message> messages,
    bool translate = false,
    Func<LLMTokenValue, Task>? tokenCallback = null,
    Func<ToolInvocation, Task>? toolCallback = null)
```

Appends multiple messages to the agent's history before running the pipeline. Useful for bulk-importing conversation context.

| Parameter | Type | Default | Description |
|---|---|---|---|
| `messages` | `IEnumerable<Message>` | required | Sequence of messages appended in order to the agent's chat history. |
| `translate` / `tokenCallback` / `toolCallback` | — | `false` / `null` / `null` | Same as string overload. |

---

### `ProcessAsync` (Chat)

```csharp
Task<ChatResult> ProcessAsync(Chat chat, bool translate = false)
```

Replaces the agent's current chat session entirely with the provided `Chat` object.

| Parameter | Type | Default | Description |
|---|---|---|---|
| `chat` | `Chat` | required | A full `Chat` entity. Its `Messages` list becomes the agent's new conversation context. The agent's model is overridden by `chat.ModelId` if set. |
| `translate` | `bool` | `false` | Auto-translate response. |

---

## IAgentActions

| Method | Returns | Description |
|---|---|---|
| `GetAgentId()` | `string` | The agent's ID. Pass to another agent's `Redirect` step or to `FromExisting`. |
| `GetAgent()` | `Agent` | The full `Agent` entity (model, config, behaviours, skills). |
| `GetKnowledge()` | `Knowledge?` | The agent's knowledge base, or `null` if none was configured. |
| `GetChat()` | `Task<Chat>` | The agent's current chat session, including all messages. |
| `RestartChat()` | `Task<Chat>` | Clears the chat history and starts a fresh session, keeping the agent's configuration. |
| `GetAllAgents()` | `Task<List<Agent>>` | Returns all persisted agents from the repository. |
| `GetAgentById(string id)` | `Task<Agent?>` | Fetches a specific agent by ID, or `null` if not found. |
| `Delete()` | `Task` | Permanently deletes the agent and its chat history. |
| `Exists()` | `Task<bool>` | Returns `true` if the agent is persisted in the repository. |

---

## Examples

### Streaming callbacks

```csharp
var result = await agent.ProcessAsync(
    "Summarise the quarterly report",
    tokenCallback: async token =>
    {
        if (token.Type == LLMTokenType.Message)
            Console.Write(token.Text);
    },
    toolCallback: async invocation =>
    {
        Console.WriteLine($"[tool] {invocation.Name}({invocation.Arguments})");
    });
```

### Structured message with file attachment

```csharp
var result = await agent.ProcessAsync(new Message
{
    Content = "Prepare a short image description about Copernicus.",
    Role = "user",
    Type = MessageType.LocalLLM,
    Files = [new FileInfo { Name = "Copernicus", Extension = "pdf", Path = "./Files/Copernicus.pdf" }]
});
```

### Agent-to-agent redirect

```csharp
var targetId = Guid.NewGuid().ToString();

var rapper = AIHub.Agent()
    .WithModel(Models.Local.Gemma2_2b)
    .WithId(targetId)
    .WithInitialPrompt("Transform the given poem into rap bars.")
    .Create(interactiveResponse: true);

var poet = AIHub.Agent()
    .WithModel(Models.Local.Llama3_2_3b)
    .WithInitialPrompt("You are a refined English poet.")
    .WithSteps(StepBuilder.Instance.Answer().Redirect(agentId: targetId).Build())
    .Create();

await poet.ProcessAsync("Write a poem about the distant future.");
```

### Knowledge base (RAG)

```csharp
var agent = await AIHub.Agent()
    .WithModel(Models.Local.Gemma3_4b)
    .WithInitialPrompt("You are a helpful company assistant for TechVibe Solutions.")
    .WithKnowledge(KnowledgeBuilder.Instance
        .AddFile("people", "./people.md", tags: ["workers", "employees"])
        .AddFile("org", "./org.md", tags: ["company structure"]))
    .WithSteps(StepBuilder.Instance.AnswerUseKnowledge().Build())
    .CreateAsync();

var result = await agent.ProcessAsync("Where can I find printer paper?");
```

### Interactive loop

```csharp
var agent = await AIHub.Agent()
    .WithModel(Models.Ollama.Gemma3_4b)
    .WithInitialPrompt("You are a helpful assistant.")
    .CreateAsync(interactiveResponse: true);

while (true)
{
    var input = Console.ReadLine();
    if (input is "exit") break;
    await agent.ProcessAsync(input!);
}
```
