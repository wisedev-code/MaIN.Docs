# App Template — ASP.NET Core Minimal API

A minimal, complete ASP.NET Core Web API exposing a single `/chat` endpoint
backed by MaIN.NET. Use this as the exact reference when a user asks for an
"API", "web service", "HTTP endpoint", or "backend" project.

This is a non-interactive process — there is no console to prompt the user at
runtime. Instead, **configuration is supplied via `appsettings.json` /
environment variables**, and the app fails fast at startup with a clear,
actionable error message if a required cloud API key or model name is
missing. Never hardcode an API key.

---

## Files

### File: ChatApi/ChatApi.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="MaIN.NET" Version="*" />
  </ItemGroup>
</Project>
```

### File: ChatApi/appsettings.json

```json
{
  "MaIN": {
    "BackendType": "OpenAi",
    "OpenAiKey": "",
    "GeminiKey": "",
    "AnthropicKey": "",
    "OllamaKey": ""
  },
  "App": {
    "DefaultModel": ""
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

> Leave `OpenAiKey` / `GeminiKey` / etc. empty in source control. Supply real
> values via environment variables (e.g. `MaIN__OpenAiKey=sk-...`) or a local,
> git-ignored `appsettings.Development.json`.

### File: ChatApi/Program.cs

```csharp
using MaIN.Core;
using MaIN.Core.Hub;
using MaIN.Domain.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMaIN(builder.Configuration);

var maINSection = builder.Configuration.GetSection("MaIN");
var backendType = maINSection["BackendType"] ?? "Self";
var defaultModel = builder.Configuration["App:DefaultModel"];

switch (backendType)
{
    case "OpenAi":
        Require(maINSection["OpenAiKey"], "MaIN:OpenAiKey", "MaIN__OpenAiKey");
        defaultModel ??= Models.OpenAi.Gpt4oMini;
        break;

    case "Gemini":
        Require(maINSection["GeminiKey"], "MaIN:GeminiKey", "MaIN__GeminiKey");
        defaultModel ??= Models.Gemini.Gemini2_5Flash;
        break;

    case "Anthropic":
        Require(maINSection["AnthropicKey"], "MaIN:AnthropicKey", "MaIN__AnthropicKey");
        defaultModel ??= Models.Anthropic.ClaudeHaiku4_5;
        break;

    case "Ollama":
        Require(maINSection["OllamaKey"], "MaIN:OllamaKey", "MaIN__OllamaKey");
        Require(defaultModel, "App:DefaultModel", "App__DefaultModel");
        break;

    case "Self":
        defaultModel ??= Models.Local.Llama3_2_3b;
        break;

    default:
        throw new InvalidOperationException(
            $"Unsupported MaIN:BackendType '{backendType}'. " +
            "Use one of: Self, OpenAi, Gemini, Anthropic, Ollama.");
}

var app = builder.Build();
app.Services.UseMaIN();

app.MapPost("/chat", async (ChatRequest req) =>
{
    var result = await AIHub.Chat()
        .WithModel(defaultModel!)
        .EnsureModelDownloaded()
        .WithMessage(req.Message)
        .CompleteAsync();

    return Results.Ok(new ChatResponse(result.Message.Content));
});

app.Run();

static void Require(string? value, string configKey, string envVar)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException(
            $"Missing required configuration '{configKey}'. Set it in " +
            $"appsettings.json or via the environment variable '{envVar}'.");
    }
}

record ChatRequest(string Message);
record ChatResponse(string Reply);
```

---

## Why this shape

- **`AddMaIN(builder.Configuration)`** before `builder.Build()`, then
  **`app.Services.UseMaIN()`** right after — this is the required ASP.NET
  Core bootstrap order from `getting-started.md`. All `AIHub.*` calls are only
  safe after `UseMaIN()` has run.
- **The `switch` on `backendType` runs before `builder.Build()`** so the
  process exits immediately with a descriptive `InvalidOperationException` if
  required config is missing — this is the "prompt for config" mechanism for
  a non-interactive API: fail fast with the exact `appsettings.json` key (and
  `__`-separated environment variable form) the operator must set, instead of
  a cryptic error on the first request.
- **`App:DefaultModel`** is a small custom config section (not part of
  MaIN.NET) for the model ID/name. For `Ollama`, this must be a free-form
  model name like `gemma3:4b`; for cloud backends it falls back to a sensible
  `Models.*` constant if not set.
- **`EnsureModelDownloaded()`** is safe to call unconditionally — it only does
  work for `BackendType.Self`.
- The endpoint uses `AIHub.Chat()` for a stateless, single-turn completion.
  `result.Message.Content` is the reply text.

## Customizing

- For multi-turn conversations, persist `chat.GetChatId()` (e.g., per
  authenticated user) and reload with `AIHub.Chat().FromExisting(chatId)` —
  see `chat.md`.
- To give the endpoint tools or RAG knowledge, switch from `AIHub.Chat()` to
  `AIHub.Agent()...CreateAsync()` and call `agent.ProcessAsync(req.Message)` —
  see `agents.md`.
- To add more backends, copy a `case` branch and use the corresponding
  `Models.*`/`*Key` pair from `getting-started.md`'s backend table.
