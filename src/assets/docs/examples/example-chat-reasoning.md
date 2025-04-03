# 💬 Chat with Reasoning Example

The **ChatWithReasoningExample** demonstrates a chat interaction with AI, encouraging deeper reasoning in responses.

### 📝 Code Example

```csharp
public class ChatWithReasoningExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("ChatWithReasoningExample is running!");

        await AIHub.Chat()
            .WithModel("deepseekR1:1.5b")
            .WithMessage("Think about greatest poet of all time")
            .CompleteAsync(interactive: true);
    }
}
```

## 🔹 How It Works
1. **Start a chat session** → `AIHub.Chat()`
2. **Select a reasoning-focused model** → `.WithModel("deepseekR1:1.5b")`
3. **Provide a thought-provoking message** → `.WithMessage("Think about greatest poet of all time")`
4. **Execute the conversation** → `.CompleteAsync(interactive: true);`

This example prompts the AI (reasoning model) to engage in deeper reasoning about the topic.

