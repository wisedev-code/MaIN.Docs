# 📂 Chat with Vision Example

This example demonstrates how to enhance chat interactions by providing images as context to the AI model. The model can analyze the content of the images and generate responses accordingly Example loads an image into memory, sends it to the AI model along with a prompt, and asks the model to analyze the image (in this case, to identify the title of the game shown in the image).

### 📝 Code Example

```csharp
public class ChatWithVisionExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("ChatExample with files is running!");

        List<string> images = ["./Files/gamex.jpg"];
        
        var result = await AIHub.Chat()
            .WithModel("llama3.2:3b")
            .WithMessage("What is the title of game?")
            .WithFiles(images)
            .CompleteAsync();
        
        Console.WriteLine(result.Message.Content);
    }
}
```

## 🔹 How It Works
1. **Attach image(s)** → `List<string> images = ["./Files/gamex.jpg"]`
2. **Initialize chat session** → `AIHub.Chat()`
3. **Choose a model** → `.WithModel("llama3.2:3b")`
4. **Send the prompt** → `.WithMessage("What is the title of game?")`
5. **Attach image files** → `.WithFiles(images)`
6. **Retrieve response** → `.CompleteAsync();`

The AI model uses the provided image to generate a response based on its content.

## ⚠️ Vision Support
Currently, **vision functionality via multimodal models** (like Llava) is **not supported**. You can use other models like `llama3.2:3b` for the task in this example.