# 💬 Chat Example

The **ChatExample** demonstrates a simple chat interaction using an AI model.
This example runs a chat session, sends a message to the model, and returns a response.

### 📝 Code Example

```csharp
public class ChatExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("ChatExample is running!");
        
        var context = AIHub.Chat().WithModel("gemma2:2b");
        
        await context
            .WithMessage("Where does the hedgehog go at night?")
            .CompleteAsync(interactive: true);
    }
}
```

## 🔹 How It Works
1. **Initialize a chat session** → `AIHub.Chat()`
2. **Choose a model** → `.WithModel("gemma2:2b")`
3. **Send a message** → `.WithMessage("Where does the hedgehog go at night?")`
4. **Run the chat** → `.CompleteAsync(interactive: true);`

This allows for a quick and straightforward AI-powered conversation.

