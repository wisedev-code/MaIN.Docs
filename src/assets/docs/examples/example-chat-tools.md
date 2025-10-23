# 💬 OpenAI Chat Example with Tools

The **OpenAIChatWithToolsExample** showcases how to integrate OpenAI’s GPT-5 model with *tool calling* capabilities — allowing the model to access and execute custom tools for enhanced functionality.

### 📝 Code Example

```csharp
public async Task Start()
{
    OpenAiExample.Setup(); // We need to provide OpenAI API key

    Console.WriteLine("(OpenAi) ChatExample with tools is running!"); 
    
    await AIHub.Chat()
        .WithModel("gpt-5-nano")
        .WithMessage("What time is it right now?")
        .WithTools(new ToolsConfigurationBuilder()
            .AddTool(
                name: "get_current_time",
                description: "Get the current date and time",
                execute: Tools.GetCurrentTime)
            .WithToolChoice("auto")
            .Build())
        .CompleteAsync(interactive: true);
}
```

## 🔹 How It Works

1. **Set up OpenAI API** → `OpenAiExample.Setup()` (requires your API key)
2. **Start a chat session** → `AIHub.Chat()`
3. **Select a model** → `.WithModel("gpt-5-nano")`
4. **Send a message** → `.WithMessage("What time is it right now?")`
5. **Configure tools** → `.WithTools(...)` adds custom tool definitions the model can use

   * Here, the `get_current_time` tool allows GPT-5 to retrieve the current system time.
6. **Execute interactively** → `.CompleteAsync(interactive: true)` runs the chat with real-time interaction.

This example demonstrates how easy it is to empower GPT-5 with **external tool integration**, enabling the model to **perform real actions**—such as fetching live data—beyond text generation. With minimal setup, developers can build dynamic, intelligent systems that blend AI reasoning with practical functionality.
