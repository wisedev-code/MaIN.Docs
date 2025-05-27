# 🔗 MCP Agent Example

This example demonstrates how to create agents that utilize MCP (Model Context Protocol) to interact with external services and tools. In this case, the example shows how to integrate with GitHub's MCP server to fetch repository information and process it through multiple connected agents.

## 🚀 Quick Start

This example sets up two interconnected agents: one that connects to GitHub via MCP to fetch repository data, and another that provides opinions and analysis about the retrieved information. The agents work together in a pipeline to deliver comprehensive responses.

### 📝 Code Example

```csharp
public class McpAgentsExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("McpClientExample is running!");

        AIHub.Extensions.DisableLLamaLogs();
        
        var contextSecond = await AIHub.Agent()
            .WithModel("qwq:7b")
            .WithInitialPrompt("Your main role is to provide opinions about facts that you are given in a conversation.")
            .CreateAsync(interactiveResponse: true);
        
        var context = await AIHub.Agent()
            .WithBackend(BackendType.OpenAi)
            .WithMcpConfig(new Mcp
            {
                Name = "GitHub",
                Arguments = ["run", "-i", "--rm", "-e", "GITHUB_PERSONAL_ACCESS_TOKEN", "ghcr.io/github/github-mcp-server"],
                EnvironmentVariables = new Dictionary<string, string>()
                {
                    {"GITHUB_PERSONAL_ACCESS_TOKEN", "<YOUR_GITHUB_TOKEN>"}
                },
                Command = "docker",
                Model = "gpt-4o-mini"
            })
            .WithModel("gpt-4o-mini")
            .WithSteps(StepBuilder.Instance
                .Mcp()
                .Redirect(agentId: contextSecond.GetAgentId())
                .Build())
            .CreateAsync();
        
        await context.ProcessAsync("What are recently added features in https://github.com/wisedev-code/MaIN.NET (based on recently closed issues)", translate: true);
    }
}
```

## 🔹 How It Works

1. **Disable Logging** → `AIHub.Extensions.DisableLLamaLogs()` disables LLaMA logging for cleaner output during execution.

2. **Create Analysis Agent** → The second agent (`contextSecond`) is created with the "qwq:7b" model and configured to provide opinions and analysis about facts presented to it.

3. **Configure MCP Integration** → The primary agent is configured with:
   - **Backend**: OpenAI backend for processing
   - **MCP Configuration**: GitHub MCP server running in Docker with authentication
   - **Model**: GPT-4o-mini for processing GitHub data
   - **Environment Variables**: GitHub Personal Access Token for API authentication

4. **Set Up Agent Pipeline** → Using `StepBuilder`, the primary agent is configured to:
   - Use MCP to fetch data from GitHub
   - Redirect the processed information to the analysis agent

5. **Process Query** → The system processes a query about recently added features in a GitHub repository, fetching data via MCP and analyzing it through the connected agents.

## 🔧 Features

- **MCP Integration**: Seamlessly connects to external services (GitHub) using the Model Context Protocol
- **Docker-Based MCP Server**: Utilizes GitHub's official MCP server running in a Docker container
- **Multi-Agent Pipeline**: Combines data fetching and analysis through interconnected agents
- **Authentication Support**: Securely handles API tokens through environment variables
- **Flexible Backend Selection**: Uses OpenAI backend for enhanced processing capabilities
- **Step-Based Processing**: Implements a structured workflow using StepBuilder for complex operations

## 🛠️ Configuration Details

### MCP Configuration

The example uses a comprehensive MCP configuration:

```csharp
new Mcp
{
    Name = "GitHub",                    // Descriptive name for the MCP service
    Arguments = [...],                  // Docker run command arguments
    EnvironmentVariables = {...},       // Secure token management
    Command = "docker",                 // Docker as the execution command
    Model = "gpt-4o-mini"              // AI model for processing
}
```

### Agent Pipeline

The pipeline structure:
1. **Primary Agent** → Fetches GitHub data via MCP
2. **MCP Step** → Processes external service integration  
3. **Redirect Step** → Forwards data to analysis agent
4. **Analysis Agent** → Provides opinions and insights

## 📋 Prerequisites

- Docker installed and running
- Valid GitHub Personal Access Token
- Access to OpenAI API (for GPT-4o-mini model)
- MaIN.NET framework properly configured

## 🎯 Use Cases

- **Repository Analysis**: Analyze GitHub repositories for recent changes and features
- **Code Review Assistance**: Get insights about code changes and development trends
- **Project Management**: Track feature development and issue resolution
- **Development Insights**: Understand project evolution and contribution patterns