# App Template — Console Chat

A minimal, complete console chat application built on MaIN.NET. Use this as the
exact reference when a user asks for a "console app", "CLI", "command line
chat bot", or doesn't specify a project kind.

The app starts with a small interactive setup wizard so the user picks a model
backend and supplies any required configuration (API key, Ollama URL) at
runtime — **never hardcode an API key**.

---

## Files

### File: ConsoleChat/ConsoleChat.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="MaIN.NET" Version="*" />
  </ItemGroup>
</Project>
```

### File: ConsoleChat/Program.cs

```csharp
using MaIN.Core;
using MaIN.Core.Hub;
using MaIN.Domain.Configuration;
using MaIN.Domain.Models;

Console.WriteLine("=== MaIN.NET Console Chat ===");
Console.WriteLine();
Console.WriteLine("Choose a model backend:");
Console.WriteLine("  1) Local (offline, downloads a small model on first run)");
Console.WriteLine("  2) OpenAI");
Console.WriteLine("  3) Gemini");
Console.WriteLine("  4) Anthropic");
Console.WriteLine("  5) Ollama");
Console.Write("> ");

var choice = Console.ReadLine();
string modelId;

switch (choice)
{
    case "2":
        Console.Write("Enter your OpenAI API key: ");
        var openAiKey = Console.ReadLine() ?? "";
        MaINBootstrapper.Initialize(configureSettings: o =>
        {
            o.BackendType = BackendType.OpenAi;
            o.OpenAiKey = openAiKey;
        });
        modelId = Models.OpenAi.Gpt4oMini;
        break;

    case "3":
        Console.Write("Enter your Gemini API key: ");
        var geminiKey = Console.ReadLine() ?? "";
        MaINBootstrapper.Initialize(configureSettings: o =>
        {
            o.BackendType = BackendType.Gemini;
            o.GeminiKey = geminiKey;
        });
        modelId = Models.Gemini.Gemini2_5Flash;
        break;

    case "4":
        Console.Write("Enter your Anthropic API key: ");
        var anthropicKey = Console.ReadLine() ?? "";
        MaINBootstrapper.Initialize(configureSettings: o =>
        {
            o.BackendType = BackendType.Anthropic;
            o.AnthropicKey = anthropicKey;
        });
        modelId = Models.Anthropic.ClaudeHaiku4_5;
        break;

    case "5":
        Console.Write("Ollama server URL [http://localhost:11434]: ");
        var ollamaUrl = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(ollamaUrl)) ollamaUrl = "http://localhost:11434";

        Console.Write("Ollama model name [gemma3:4b]: ");
        var ollamaModel = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(ollamaModel)) ollamaModel = "gemma3:4b";

        MaINBootstrapper.Initialize(configureSettings: o =>
        {
            o.BackendType = BackendType.Ollama;
            o.OllamaKey = ollamaUrl;
        });
        modelId = ollamaModel;
        break;

    default:
        MaINBootstrapper.Initialize();
        modelId = Models.Local.Llama3_2_3b;
        break;
}

Console.WriteLine();
Console.WriteLine("Setting up agent (this may download a model on first run)...");
Console.WriteLine();

var agent = await AIHub.Agent()
    .WithModel(modelId)
    .WithInitialPrompt("You are a friendly, helpful assistant.")
    .EnsureModelDownloaded()
    .CreateAsync(interactiveResponse: true);

Console.WriteLine("Ready! Type a message and press Enter (type 'exit' to quit).");
Console.WriteLine();

while (true)
{
    Console.Write("> ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input) || input.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;

    await agent.ProcessAsync(input);
    Console.WriteLine();
    Console.WriteLine();
}
```

---

## Compile rules

- **Never add `using MaIN.Domain.Entities;`** — `Message` and `MessageType` are
  internal server-side types not exported by the `MaIN.NET` NuGet package. Using
  them causes CS0246/CS0103. The `agent.ProcessAsync(string)` overload takes a
  plain string — no `Message` object needed.

## Why this shape

- **`MaINBootstrapper.Initialize(configureSettings: ...)`** is called exactly
  once, inside the chosen branch, before any `AIHub.*` call — this matches the
  framework's bootstrap contract.
- **`EnsureModelDownloaded()`** is safe to call unconditionally: it downloads
  local GGUF files when `BackendType.Self` is active and is a no-op for every
  cloud/Ollama backend.
- **`CreateAsync(interactiveResponse: true)`** makes every later
  `agent.ProcessAsync(input)` call stream the response straight to
  `Console.Write` — no manual token-callback wiring needed for a console UI.
- The Ollama branch treats `OllamaKey` as the **server URL** (per
  `models.md`), and the model name is a free-form string (e.g. `gemma3:4b`),
  not a `Models.*` constant.

## Customizing

- To give the agent tools, RAG knowledge, or a multi-step pipeline, see
  `agents.md` (`WithTools`, `WithKnowledge`, `WithSteps`).
- To add more backends to the menu (GroqCloud, DeepSeek, xAI, Vertex), copy
  the OpenAI branch and swap `BackendType.OpenAi` / `OpenAiKey` for the
  corresponding `BackendType.*` / `*Key` pair from `getting-started.md`'s
  backend table.
