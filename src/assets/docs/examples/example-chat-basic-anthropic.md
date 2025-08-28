# 💬 Anthropic Chat Example

The **AnthropicChatExample** demonstrates how to integrate Anthropic's claude-sonnet-4 model into the framework with minimal setup, showcasing how easy it is to leverage Anthropic's capabilities for interactive chat.

### 📝 Code Example

```csharp
public async Task Start()
{
    AnthropicExample.Setup(); //We need to provide Anthropic API key
    Console.WriteLine("(Anthropic) ChatExample is running!");

    await AIHub.Chat()
        .WithModel("claude-sonnet-4-20250514")
        .WithMessage("Write a haiku about programming on Monday morning.")
        .CompleteAsync(interactive: true);
}
```

## 🔹 How It Works
1. **Set up Anthropic API** → `AnthropicExample.Setup()` (API key is required)
2. **Initialize a chat session** → `AIHub.Chat()`
3. **Choose a model** → `.WithModel("claude-sonnet-4-20250514")`
4. **Send a message** → `.WithMessage("Write a haiku about programming on Monday morning.")`
5. **Run the chat** → `.CompleteAsync(interactive: true);`

This example shows how Anthropic's claude-sonnet-4 model can be effortlessly integrated into the framework. With just a few lines of code, developers can enable powerful AI capabilities, thanks to the model's simple setup and ease of use. This makes it an ideal solution for anyone looking to add advanced AI features with minimal effort.