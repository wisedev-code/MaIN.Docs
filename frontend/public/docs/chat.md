# Chat

The `ChatContext` is the primary way to interact with LLMs in MaIN.NET. It provides a fluent builder API for constructing and executing chat completions.

## Basic Usage

```csharp
var result = await AIHub.Chat()
    .WithModel("meta-llama/llama-4-scout-17b-16e-instruct")
    .WithMessage("Explain async/await in C#")
    .CompleteAsync();

Console.WriteLine(result.Message.Content);
```

## Streaming Tokens

Pass a `changeOfValue` callback to receive tokens as they arrive:

```csharp
await AIHub.Chat()
    .WithModel("claude-sonnet-4-6")
    .WithMessage("Write a haiku about .NET")
    .CompleteAsync(changeOfValue: async token => {
        Console.Write(token.Text);
    });
```

The `LLMTokenValue` has a `Type` property (`Message`, `Reason`, `ToolCall`, `Special`) for differentiating reasoning tokens from response content.

## Multi-turn Conversations

```csharp
var chat = new Chat {
    ModelId = "gpt-4o",
    Messages = [
        new Message { Role = "user",      Content = "Hello!" },
        new Message { Role = "assistant", Content = "Hi! How can I help?" },
        new Message { Role = "user",      Content = "Tell me about MaIN.NET" },
    ]
};

var result = await AIHub.Chat()
    .WithChat(chat)
    .CompleteAsync();
```

## Cancellation

```csharp
var cts = new CancellationTokenSource();

await AIHub.Chat()
    .WithMessage("Write a very long essay...")
    .CompleteAsync(
        changeOfValue: async token => Console.Write(token.Text),
        cancellationToken: cts.Token
    );

// Cancel from another thread:
cts.Cancel();
```
