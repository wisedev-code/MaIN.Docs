# 📂 Chat from Existing Example

This example starts a chat, adds messages, and then retrieves the conversation history from an existing chat session.
This example demonstrates how to reuse a previous chat session and retrieve its history.

### 📝 Code Example

```csharp
public class ChatFromExistingExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("ChatExample with files is running!");

        var result = AIHub.Chat()
            .WithModel("qwen2.5:0.5b");
        
        await result.WithMessage("What do you think about math theories?")
            .CompleteAsync();
        
        await result.WithMessage("And about physics?")
            .CompleteAsync();

        var chatNewContext = await AIHub.Chat().FromExisting(result.GetChatId());
        var messages = chatNewContext.GetChatHistory();
        Console.WriteLine(JsonSerializer.Serialize(messages));
    }
}
```

## 🔹 How It Works
1. **Initialize a chat session** → `AIHub.Chat()`
2. **Choose a model** → `.WithModel("qwen2.5:0.5b")`
3. **Send multiple messages** → `.WithMessage("..."`).CompleteAsync();`
4. **Load an existing chat session** → `AIHub.Chat().FromExisting(result.GetChatId())`
5. **Retrieve chat history** → `.GetChatHistory()`


