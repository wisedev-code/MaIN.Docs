# Getting Started with MaIN.NET

MaIN.NET is an open-source .NET AI orchestration framework. Run models locally via LLamaSharp or connect to cloud providers — OpenAI, Anthropic, Gemini, GroqCloud, xAI, DeepSeek, Ollama, and Vertex.

## Installation

```bash
dotnet add package MaIN.NET
```

## Bootstrap

### Console App

The fastest way to get started is a single call that sets up everything:

```csharp
using MaIN.Core;
using MaIN.Core.Hub;

MaINBootstrapper.Initialize();
```

To target a cloud provider instead of a local model, pass a configuration lambda:

```csharp
MaINBootstrapper.Initialize(configureSettings: options =>
{
    options.BackendType = BackendType.OpenAi;
    options.OpenAiKey = Environment.GetEnvironmentVariable("OPENAI_KEY")!;
});
```

### ASP.NET Core App

```csharp
using MaIN.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMaIN(builder.Configuration);
// optional: load skill bundles from a directory
builder.Services.AddSkillsFromDirectory("./skills");

var app = builder.Build();
app.Services.UseMaIN();
AIHub.Extensions.DisableLLamaLogs(); // suppress LLamaSharp noise if not using local models

app.Run();
```

`UseMaIN()` warms the skill cache and initialises the `AIHub` static facade. All subsequent `AIHub.*` calls are safe after this point.

## Configuration

Settings live under the `MaIN` section in `appsettings.json`:

```json
{
  "MaIN": {
    "BackendType": "OpenAi",
    "OpenAiKey": "",
    "GeminiKey": "",
    "AnthropicKey": "",
    "GroqCloudKey": "",
    "DeepSeekKey": "",
    "XaiKey": "",
    "OllamaKey": "",
    "ModelsPath": "./models",
    "ImageGenUrl": "http://localhost:5003",
    "SkillsDirectory": "./skills"
  }
}
```

| Setting | Purpose |
|---|---|
| `BackendType` | Active LLM provider (see table below) |
| `ModelsPath` | Directory where local GGUF files are stored |
| `ImageGenUrl` | URL of a local image generation service |
| `SkillsDirectory` | Directory scanned for folder-based skills |
| `*Key` | API key for the corresponding cloud provider |

## Supported Backends

| Backend | `BackendType` value | Key setting |
|---|---|---|
| Local GGUF (LLamaSharp) | `Self` | none |
| OpenAI | `OpenAi` | `OpenAiKey` |
| Google Gemini | `Gemini` | `GeminiKey` |
| Anthropic | `Anthropic` | `AnthropicKey` |
| GroqCloud | `GroqCloud` | `GroqCloudKey` |
| DeepSeek | `DeepSeek` | `DeepSeekKey` |
| xAI | `Xai` | `XaiKey` |
| Ollama | `Ollama` | `OllamaKey` |
| Google Vertex AI | `Vertex` | service account credentials |

## First Chat

```csharp
using MaIN.Core;
using MaIN.Core.Hub;
using MaIN.Domain.Models;

MaINBootstrapper.Initialize();

await AIHub.Chat()
    .WithModel(Models.Local.Gemma2_2b)
    .EnsureModelDownloaded()           // auto-download the model if not present
    .WithMessage("Where do hedgehogs go at night?")
    .CompleteAsync(interactive: true); // stream tokens to console
```

For a cloud model, swap the model constant and remove `EnsureModelDownloaded()`:

```csharp
MaINBootstrapper.Initialize(configureSettings: o =>
{
    o.BackendType = BackendType.Anthropic;
    o.AnthropicKey = "<key>";
});

await AIHub.Chat()
    .WithModel(Models.Anthropic.ClaudeSonnet4_6)
    .WithMessage("Write a haiku about programming on Monday morning.")
    .CompleteAsync(interactive: true);
```

## AIHub Entry Points

`AIHub` is the static facade for all MaIN.NET functionality:

| Method | Returns | Purpose |
|---|---|---|
| `AIHub.Chat()` | `ChatContext` | Single-turn and multi-turn completions |
| `AIHub.Agent()` | `AgentContext` | Stateful agents with tools, knowledge, skills |
| `AIHub.Flow()` | `FlowContext` | Multi-agent orchestration pipelines |
| `AIHub.Model()` | `ModelContext` | Model registry, download, and cache management |
| `AIHub.Mcp()` | `McpContext` | Model Context Protocol integration |

## Model Constants

All supported models are available as compile-time constants in `Models.*`:

```csharp
// Local GGUF models
Models.Local.Gemma2_2b
Models.Local.Llama3_2_3b
Models.Local.Gemma3_4b

// Cloud models
Models.OpenAi.Gpt4o
Models.OpenAi.Gpt5
Models.Anthropic.ClaudeSonnet4_6
Models.Gemini.Gemini2_5Flash
Models.Groq.Llama4Scout17b
Models.Ollama.Gemma3_4b
```

Models can also be supplied as plain strings: `.WithModel("gemma3:4b")`.
