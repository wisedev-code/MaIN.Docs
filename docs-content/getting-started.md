# Getting Started with MaIN.NET

MaIN.NET is an open-source .NET AI orchestration framework that makes it easy to build AI-powered applications — run models locally via LLamaSharp or connect to cloud providers such as GroqCloud, OpenAI, Gemini, Anthropic, xAI, and Ollama.

## Installation

Install the NuGet package:

```bash
dotnet add package MaIN.Core
```

## Bootstrap

Register MaIN in your `Program.cs` and call `UseMaIN()` after building the host:

```csharp
using MaIN.Core;
using MaIN.Domain.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMaIN(builder.Configuration, settings =>
{
    settings.BackendType = BackendType.GroqCloud;
    settings.GroqCloudKey = Environment.GetEnvironmentVariable("GROQ_KEY")!;
});

var app = builder.Build();
app.Services.UseMaIN();
AIHub.Extensions.DisableLLamaLogs();

app.Run();
```

Configure keys in `appsettings.json` under the `MaIN` section:

```json
{
  "MaIN": {
    "GroqCloudKey": "",
    "OpenAiKey": "",
    "GeminiKey": "",
    "AnthropicKey": ""
  }
}
```

## Simple Chat

```csharp
using MaIN.Core.Hub;
using MaIN.Domain.Models;

await AIHub.Chat()
    .WithModel(Models.Groq.Llama4Scout17b)
    .WithSystemPrompt("You are a helpful assistant.")
    .WithMessage("What is MaIN.NET?")
    .CompleteAsync(changeOfValue: async token =>
    {
        Console.Write(token.Text);
    });
```

## Supported Backends

| Backend | Enum value | Key setting |
|---|---|---|
| Local LLM (LLamaSharp) | `BackendType.Self` | none |
| GroqCloud | `BackendType.GroqCloud` | `GroqCloudKey` |
| OpenAI | `BackendType.OpenAi` | `OpenAiKey` |
| Gemini | `BackendType.Gemini` | `GeminiKey` |
| Anthropic | `BackendType.Anthropic` | `AnthropicKey` |
| xAI | `BackendType.Xai` | `XaiKey` |
| Ollama | `BackendType.Ollama` | `OllamaKey` |
| DeepSeek | `BackendType.DeepSeek` | `DeepSeekKey` |
