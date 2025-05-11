# 📂 Gemini Chat with Files Example

This example demonstrates how to enhance Gemini chat interactions by providing external documents as context.
This example loads PDF files into memory and asks the model to analyze their content.

### 📝 Code Example

```csharp
public class ChatWithFilesExampleGemini : IExample
{
    public async Task Start()
    {
        Console.WriteLine("(Gemini) ChatExample is running!");
        GeminiExample.Setup(); //We need to provide Gemini API key

        List<string> files = ["./Files/Nicolaus_Copernicus.pdf", "./Files/Galileo_Galilei.pdf"];

        var result = await AIHub.Chat()
            .WithModel("gemini-2.0-flash")
            .WithMessage("You have 2 documents in memory. Whats the difference of work between Galileo and Copernicus?. Give answer based on the documents.")
            .WithFiles(files)
            .CompleteAsync(interactive: true);

        Console.WriteLine(result.Message.Content);
        Console.ReadKey();
    }
}
```

## 🔹 How It Works
1. **Set up Gemini API** → `GeminiExample.Setup()` (API key is required)
2. **Specify files** → `List<string> files = ["./Files/Nicolaus_Copernicus.pdf", "./Files/Galileo_Galilei.pdf"]`
3. **Initialize chat session** → `AIHub.Chat()`
4. **Choose a model** → `.WithModel("gemini-2.0-flash")`
5. **Ask a question** → `.WithMessage("...")`
6. **Attach documents** → `.WithFiles(files)`
7. **Retrieve response** → `.CompleteAsync();`

This example demonstrates the powerful capability of enhancing Gemini chat interactions by providing external documents as context. By utilizing the .WithFiles() method, you can effortlessly enable the model to analyze and respond based on the content of your attached documents, expanding the scope of your conversational AI beyond its pre-trained knowledge and allowing for informed discussions directly from your provided files.