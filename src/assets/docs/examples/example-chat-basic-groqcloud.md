# 💬 GroqCloud Chat Example

The **GroqCloudChatExample** demonstrates how to integrate GroqCloud's LLaMA 3 model into the framework with minimal setup, showcasing its performance and simplicity for interactive chat.

### 📝 Code Example

```csharp
public async Task Start()
{
    GroqCloudExample.Setup(); //We need to provide GroqCloud API key
    Console.WriteLine("(GroqCloud) ChatExample is running!");

    await AIHub.Chat()
        .WithModel("llama3-8b-8192")
        .WithMessage("Which color do people like the most?")
        .CompleteAsync(interactive: true);
}
```

## 🔹 How It Works
1. **Set up GroqCloud API** → `GroqCloudExample.Setup()` (API key is required)
2. **Initialize a chat session** → `AIHub.Chat()`
3. **Choose a model** → `.WithModel("llama3-8b-8192")`
4. **Send a message** → `.WithMessage("Which color do people like the most?")`
5. **Run the chat** → `.CompleteAsync(interactive: true);`

This example demonstrates how effortlessly GroqCloud's LLAMA 3 model can be integrated into the framework, enabling seamless interactions with just a few lines of code. The simplicity of setup and ease of use makes it ideal for developers looking to integrate powerful AI capabilities with minimal configuration.