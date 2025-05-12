# 💬 Gemini Chat Example

The **GeminiChatExample** demonstrates how to integrate Gemini's 2.0-flash model into the framework with minimal setup, showcasing how easy it is to leverage Gemini's capabilities for interactive chat.

### 📝 Code Example

```csharp
public async Task Start()
{
    GeminiExample.Setup(); //We need to provide Gemini API key

    Console.WriteLine("(Gemini) ChatExample is running!");

    await AIHub.Chat()
        .WithModel("gemini-2.0-flash")
        .WithMessage("Is the killer whale the smartest animal?")
        .CompleteAsync(interactive: true);
}
```

## 🔹 How It Works
1. **Set up Gemini API** → `GeminiExample.Setup()` (API key is required)
2. **Initialize a chat session** → `AIHub.Chat()`
3. **Choose a model** → `.WithModel("gemini-2.0-flash")`
4. **Send a message** → `.WithMessage("Is the killer whale the smartest animal?")`
5. **Run the chat** → `.CompleteAsync(interactive: true);`

This example demonstrates how effortlessly Gemini's Gemini's 2.0-flash model can be integrated into the framework, enabling seamless interactions with just a few lines of code. The simplicity of setup and ease of use makes it ideal for developers looking to integrate powerful AI capabilities with minimal configuration.