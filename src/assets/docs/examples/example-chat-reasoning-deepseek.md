# 💬 DeepSeek Chat with Reasoning Example

The **ChatWithReasoningDeepSeekExample** demonstrates an interactive chat session using the DeepSeek Reasoner model, focusing on eliciting thoughtful and reasoned responses from the AI.

```csharp
public async Task Start()
{
    DeepSeekExample.Setup(); //We need to provide DeepSeek API key
    Console.WriteLine("(DeepSeek) ChatExample with reasoning is running!");

    await AIHub.Chat()
        .WithModel("deepseek-reasoner") // a model that supports reasoning
        .WithMessage("What chill pc game do you recommend?")
        .CompleteAsync(interactive: true);
}
```

## 🔹 How It Works

1. **Set up DeepSeek API** → `DeepSeekExample.Setup()` (API key is required)
2. **Initialize a chat session** → `AIHub.Chat()`
3. **Choose a model** → `.WithModel("deepseek-reasoner")`
4. **Send a message** → `.WithMessage("What chill pc game do you recommend?")`
5. **Run the chat** → `.CompleteAsync(interactive: true);`

This example showcases how to leverage the DeepSeek Reasoner model to engage in a conversational exchange where the AI is prompted to provide recommendations based on reasoning, making it ideal for scenarios requiring more elaborate and thought-out responses.

## 💡 Basic Chat without Reasoning

If you don't require the advanced reasoning capabilities, you can simply select a DeepSeek model that isn't designed for reasoning ("`deepseek-chat`"). The integration process remains just as straightforward.
