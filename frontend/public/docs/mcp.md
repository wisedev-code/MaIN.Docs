# McpContext

The Model Context Protocol (MCP) connects agents to external tool and resource servers — file systems, GitHub, search engines, databases — over a standard stdio interface. Servers are launched as child processes (`npx`, `docker`, or any executable).

MaIN.NET exposes three integration points:

| Use case | Entry point |
|---|---|
| One-shot MCP prompt | `AIHub.Mcp().WithConfig(…).PromptAsync(…)` |
| Agent executes MCP as a pipeline step | `agent.WithMcpConfig(…)` + `StepBuilder.Instance.Mcp()` |
| Agent uses MCP server as a RAG knowledge source | `KnowledgeBuilder.Instance.AddMcp(…)` |

---

## The `Mcp` Configuration Object

`Mcp` is shared across all three integration points. It describes how to launch and communicate with one MCP server.

| Field | Type | Required | Description |
|---|---|---|---|
| `Name` | `string` | yes | Logical identifier for this server. Used in logs and routing. |
| `Command` | `string` | yes | Executable to launch: `"npx"`, `"docker"`, `"uvx"`, `"python"`, etc. |
| `Arguments` | `string[]` | yes | CLI arguments passed to `Command`. For `npx`: `["-y", "@modelcontextprotocol/server-everything"]`. |
| `Model` | `string` | yes | Model ID used by this MCP server for reasoning. Can differ from the agent's model. |
| `Backend` | `BackendType?` | no | Overrides the backend for `Model`. Inferred from the agent if not set. |
| `EnvironmentVariables` | `Dictionary<string, string>` | no | Environment variables injected into the server process (API keys, tokens, paths). |

---

## McpContext — Direct One-Shot Prompt

`AIHub.Mcp()` is the entry point for standalone MCP calls not tied to an agent.

---

### `WithBackend`

```csharp
McpContext WithBackend(BackendType backendType)
```

Sets the LLM backend that will handle the MCP reasoning.

| Parameter | Type | Description |
|---|---|---|
| `backendType` | `BackendType` | The backend enum value: `BackendType.OpenAi`, `BackendType.Gemini`, `BackendType.Anthropic`, etc. Must match the `Model` set in `WithConfig`. |

---

### `WithConfig`

```csharp
McpContext WithConfig(Mcp config)
```

Configures the MCP server to use for this context.

| Parameter | Type | Description |
|---|---|---|
| `config` | `Mcp` | A fully populated `Mcp` object (see fields above). |

---

### `PromptAsync`

```csharp
Task<ChatResult> PromptAsync(string prompt)
```

Sends a single prompt to the model through the configured MCP server and returns the response.

| Parameter | Type | Description |
|---|---|---|
| `prompt` | `string` | The user's question or instruction. The model uses MCP tool calls to gather data before answering. |

**Returns** `ChatResult` with the same shape as `ChatContext.CompleteAsync`.

**Example:**

```csharp
var result = await AIHub.Mcp()
    .WithBackend(BackendType.OpenAi)
    .WithConfig(new Mcp
    {
        Name = "McpEverythingDemo",
        Command = "npx",
        Arguments = ["-y", "@modelcontextprotocol/server-everything"],
        Model = Models.OpenAi.Gpt4oMini
    })
    .PromptAsync("Provide info about resource 21 and 37. Explain how you retrieved it.");

Console.WriteLine(result.Message.Content);
```

---

## MCP in Agents — Pipeline Step

Attach an MCP server to an agent and add a `Mcp()` step. The agent calls the server's tools during the step, then continues through the rest of the pipeline.

### `WithMcpConfig` (on `IAgentConfigurationBuilder`)

```csharp
IAgentConfigurationBuilder WithMcpConfig(Mcp mcpConfig)
```

| Parameter | Type | Description |
|---|---|---|
| `mcpConfig` | `Mcp` | MCP server configuration. `Backend` is optional — inferred from the agent's model if not set. |

Pair with `StepBuilder.Instance.Mcp()` in `WithSteps`:

| Step chain | Effect |
|---|---|
| `StepBuilder.Instance.Mcp().Build()` | Execute MCP tools and return the result |
| `StepBuilder.Instance.Mcp().Redirect(agentId).Build()` | Execute MCP tools, then pass result to another agent |
| `StepBuilder.Instance.Mcp().Answer().Build()` | Execute MCP tools, then answer the user |

**Example:**

```csharp
var agent = await AIHub.Agent()
    .WithModel(Models.OpenAi.Gpt4oMini)
    .WithMcpConfig(new Mcp
    {
        Name = "GitHub",
        Command = "docker",
        Arguments = [
            "run", "-i", "--rm",
            "-e", "GITHUB_PERSONAL_ACCESS_TOKEN",
            "ghcr.io/github/github-mcp-server"
        ],
        EnvironmentVariables = new() { { "GITHUB_PERSONAL_ACCESS_TOKEN", "<token>" } },
        Model = Models.OpenAi.Gpt4oMini
    })
    .WithSteps(StepBuilder.Instance.Mcp().Redirect(agentId: reviewerAgent.GetAgentId()).Build())
    .CreateAsync();

await agent.ProcessAsync("What are the recently added features in MaIN.NET?");
```

---

## MCP as Knowledge Source

Use `KnowledgeBuilder.Instance.AddMcp` to treat an MCP server as a retrieval source in a RAG agent. Tags route queries to the correct source when multiple sources are combined.

### `KnowledgeBuilder.AddMcp`

```csharp
KnowledgeBuilder AddMcp(Mcp config, string[] tags)
```

| Parameter | Type | Description |
|---|---|---|
| `config` | `Mcp` | MCP server config. `Backend` and `Model` can differ from the parent agent — each knowledge source can use its own reasoning model. |
| `tags` | `string[]` | Semantic labels used by the retrieval system to route queries to this source. Choose terms that appear in the questions the source is expected to answer. |

**Example:**

```csharp
var agent = await AIHub.Agent()
    .WithModel(Models.OpenAi.Gpt4_1Mini)
    .WithKnowledge(KnowledgeBuilder.Instance
        .AddMcp(new Mcp
        {
            Name = "ExaDeepSearch",
            Command = "npx",
            Arguments = ["-y", "exa-mcp-server"],
            EnvironmentVariables = { { "EXA_API_KEY", "<key>" } },
            Backend = BackendType.Gemini,
            Model = Models.Gemini.Gemini2_0Flash
        }, ["search", "browser", "web access", "research"])
        .AddMcp(new Mcp
        {
            Name = "FileSystem",
            Command = "npx",
            Arguments = ["-y", "@modelcontextprotocol/server-filesystem", "C:\\Projects"],
            Backend = BackendType.GroqCloud,
            Model = Models.Groq.GptOss20b
        }, ["filesystem", "files", "read", "write", "disk"]))
    .WithSteps(StepBuilder.Instance.AnswerUseKnowledge().Build())
    .CreateAsync();

var result = await agent.ProcessAsync("Find the latest design docs in my projects folder.");
```

---

## Choosing the Right Integration

| Scenario | Use |
|---|---|
| Ad-hoc query to one MCP server, no persistent agent | `AIHub.Mcp().WithConfig(…).PromptAsync(…)` |
| Agent needs to actively call MCP tools as part of its pipeline | `WithMcpConfig(…)` + `StepBuilder.Mcp()` |
| Agent needs to retrieve information from MCP servers as background context | `KnowledgeBuilder.AddMcp(…)` inside `WithKnowledge` |
| Mixed: some MCP sources for retrieval, one for active tool execution | Combine `WithMcpConfig` for active + `AddMcp` in `WithKnowledge` for retrieval |

A single agent can use all three integration styles simultaneously, each pointing to different servers with different models.
