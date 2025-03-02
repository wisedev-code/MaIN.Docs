# 💬 OpenAI Chat Example

The **OpenAIChatExample** demonstrates how to integrate OpenAI's GPT-4 model into the framework with minimal setup, showcasing how easy it is to leverage OpenAI's capabilities for interactive chat.

### 📝 Code Example

```csharp
public async Task Start()
{
    OpenAiExample.Setup(); // We need to provide OpenAI API key

    Console.WriteLine("(OpenAi) ChatExample is running!"); 
    
    await AIHub.Chat()
        .WithModel("gpt-4o-mini")
        .WithMessage("What do you consider to be the greatest invention in history?")
        .CompleteAsync(interactive: true);
}
```

## 🔹 How It Works
1. **Set up OpenAI API** → `OpenAiExample.Setup()` (API key is required)
2. **Initialize a chat session** → `AIHub.Chat()`
3. **Choose a model** → `.WithModel("gpt-4o-mini")`
4. **Send a message** → `.WithMessage("What do you consider to be the greatest invention in history?")`
5. **Run the chat** → `.CompleteAsync(interactive: true);`

This example demonstrates how effortlessly OpenAI's GPT-4 model can be integrated into the framework, enabling seamless interactions with just a few lines of code. The simplicity of setup and ease of use makes it ideal for developers looking to integrate powerful AI capabilities with minimal configuration.