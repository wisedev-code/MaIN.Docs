# 📂 Chat with Image Generation Example

This example demonstrates how to enhance chat interactions by generating images based on specific text prompts.
This example sends a prompt to the AI model to generate an image based on the description and displays the resulting image.

### 📝 Code Example

```csharp
public class ChatWithImageGenExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("ChatExample with image gen is running!");
        
        var result = await AIHub.Chat()
            .EnableVisual()
            .WithMessage("Generate cyberpunk godzilla cat warrior")
            .CompleteAsync();
        
        ImagePreviewer.ShowImage(result.Message.Images);
    }
}
```

## 🔹 How It Works
1. **Enable image generation** → `.EnableVisual()`
2. **Send text prompt** → `.WithMessage("Generate cyberpunk godzilla cat warrior")`
3. **Generate response** → `.CompleteAsync()`
4. **Display image** → `ImagePreviewer.ShowImage(result.Message.Images)`

## ⚠️ Requirements
To run this example, the **ImageGen API** needs to be running. You can start the API via the command line interface (mcli).

