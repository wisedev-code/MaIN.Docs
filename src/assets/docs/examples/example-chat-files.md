# 📂 Chat with Files Example

This example demonstrates how to enhance chat interactions by providing external documents as context.
This example loads PDF files into memory and asks the model to analyze their content.

### 📝 Code Example

```csharp
public class ChatWithFilesExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("ChatExample with files is running!");

        List<string> files = ["./Files/Nicolaus_Copernicus.pdf", "./Files/Galileo_Galilei.pdf"];
        
        var result = await AIHub.Chat()
            .WithModel("gemma2:2b")
            .WithMessage("You have 2 documents in memory. What's the difference between Galileo and Copernicus' work? Give an answer based on the documents.")
            .WithFiles(files)
            .CompleteAsync();
        
        Console.WriteLine(result.Message.Content);
    }
}
```

## 🔹 How It Works
1. **Specify files** → `List<string> files = ["./Files/Nicolaus_Copernicus.pdf", "./Files/Galileo_Galilei.pdf"]`
2. **Initialize chat session** → `AIHub.Chat()`
3. **Choose a model** → `.WithModel("gemma2:2b")`
4. **Attach documents** → `.WithFiles(files)`
5. **Ask a question** → `.WithMessage("...")`
6. **Retrieve response** → `.CompleteAsync();`


