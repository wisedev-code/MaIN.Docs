# 🚀 Basic Tutorial

MaIN is a minimalist AI orchestration library designed to get you chatting/designing/.. with LLMs in .NET with minimal setup. Let's get started in under 5 minutes!

## Installation

```bash
dotnet add package MaIN.Core
```

## Basic Setup

### 1. Initialize MaIN

Start with these essential service registrations:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Minimal required configuration
builder.Services.AddMaIN();
var app = builder.Build();
app.UseMaIN();
```

### 2. Configuration (Optional)

MaIN works out-of-the-box with sensible defaults. Model path is being set in env first time when you use our CLI. For custom settings: 
1. Create `appsettings.json`:
```json
{
  "MaIN": {
    "ModelsPath": "C:\\MAIN.Models",
  }
}
```

## Your First AI Chat Api!

Here's a complete minimal example of api:

```csharp
using MaIN.Core;

var app = WebApplication.CreateBuilder(args)
    .Services
    .AddMaIN()
    .BuildApplication();

app.UseMaIN();

app.MapGet("/chat", async () => 
{
    return await AIHub.Chat()
        .WithMessage("Why do owls make good pets?")
        .CompleteAsync();
});

app.Run();
```

## Interactive Example

You can also provide interactive param, that will allow to provide answers token by token

```csharp
public class ChatExample : IExample
{
    public async Task Start()
    {
        var context = AIHub.Chat().WithModel("gemma2:2b");
        
        await context
            .WithMessage("Where do hedgehogs go at night?")
            .CompleteAsync(interactive: true);
    }
}
```

Key features used in example:
- `WithMessage()` - Add conversation messages
- `WithModel()` - Choose different LLMs
- `CompleteAsync()` - Stream responses live with `interactive: true`


## Simple Console example
If you dont need webapi builder, you can also initialize main anywhere in system with that approach:

```csharp
MaINBootstrapper.Initialize();

await AIHub.Chat()
        .WithModel("gemma2:2b")
        .WithMessage("Hello, World!")
        .CompleteAsync(interactive: true);
```

## CLI Configuration Helper

First time user? Run our [CLI](#/doc/cli)

## Next Steps

Explore more advanced scenarios:
- [ ] Agent workflows
- [ ] Multi-modal interactions
- [ ] File attachments
- [ ] API data integrations

Check our examples folder for ready-to-run scenarios!
```

> 💡 **Keep it simple!** MaIN requires just 2 method calls to start chatting: - and we also working on enabling it without DI container
> 1. `AddMaIN()` - Service registration
> 2. `UseMaIN()` - Middleware initialization
```