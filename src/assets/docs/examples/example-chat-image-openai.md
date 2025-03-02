# 🎨 OpenAI Image Generation with Chat Example

The **OpenAI Image Generation Chat Example** showcases how to seamlessly combine chat and image generation features using OpenAI's capabilities within the same framework. This example demonstrates how easy it is to generate an image from a text prompt in a chat interaction.

### 📝 Code Example

```csharp
public async Task Start()
{
    Console.WriteLine("ChatExample with image gen is running! (OpenAi)");
    OpenAiExample.Setup(); // We need to provide OpenAi API key
    
    var result = await AIHub.Chat()
        .EnableVisual() // Enable image generation
        .WithMessage("Generate rock style cow playing guitar")
        .CompleteAsync();
    
    ImagePreview.ShowImage(result.Message.Images); // Show the generated image
}
```

## 🔹 How It Works
1. **Set up OpenAI API** → `OpenAiExample.Setup()` (API key is required)
2. **Initialize a chat session** → `AIHub.Chat()`
3. **Enable image generation** → `.EnableVisual()`
4. **Send a message with a text prompt** → `.WithMessage("Generate rock style cow playing guitar")`
5. **Generate the image alongside the chat** → `.CompleteAsync()`
6. **Display the generated image** → `ImagePreview.ShowImage(result.Message.Images)`

This example highlights the effortless integration of image generation alongside interactive chat, allowing you to create images based on natural language prompts while engaging in a conversation. By enabling visual capabilities in just a couple of method calls, OpenAI's image generation becomes a natural extension of your chat-based interactions, making it simple and intuitive to generate images directly from the chat interface.