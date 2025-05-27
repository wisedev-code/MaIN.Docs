# 🔗 MCP Example

The **MCP Example** demonstrates a simple MCP (Model Context Protocol) interaction using an AI model with external tool integration. This example runs an MCP session, connects to an external MCP server, sends a prompt, and returns a response with data retrieved from external resources.

### 📝 Code Example

```csharp
public class McpExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("McpClientExample is running!");
        OpenAiExample.Setup();

        var result = await AIHub.Mcp()
            .WithBackend(BackendType.OpenAi)
            .WithConfig(
            new Mcp
            {
                Name = "McpEverythingDemo",
                Arguments = ["-y", "@modelcontextprotocol/server-everything"],
                Command = "npx",
                Model = "gpt-4o-mini"
            })
            .PromptAsync("Provide me information about resource 21 and 37. Also explain how you get this data");
        
        Console.WriteLine(result.Message.Content);
    }
}
```

## 🔹 How It Works

1. **Initialize MCP session** → `AIHub.Mcp()`
2. **Choose backend** → `.WithBackend(BackendType.OpenAi)`
3. **Configure MCP server** → `.WithConfig(new Mcp { ... })`
4. **Send prompt** → `.PromptAsync("Provide me information about resource 21 and 37...")`

This allows for AI-powered interactions with external tools and data sources through the Model Context Protocol, enabling the AI to access and process information from connected services.