# Agent with Knowledge MCP Example

## Overview

This example demonstrates the most advanced knowledge integration using Model Context Protocol (MCP) servers. The agent can access multiple external services including web search, filesystem operations, and GitHub integration, each powered by different AI models and backends for optimal performance.

## Code Example

```csharp
using MaIN.Core.Hub;
using MaIN.Core.Hub.Utils;
using MaIN.Domain.Configuration;

namespace Examples.Mcp;

public class AgentWithKnowledgeMcpExample : IExample
{
    public async Task Start()
    {
        //Note: to run this example that uses 3 different AI providers. You have to assign api keys for those providers in ENV variables or in appsettings
        //Note: to run this example, you should do 'gh auth login' to give octocode mcp server access to github CLI
        Console.WriteLine("Agent with knowledge base example MCP sources");
        AIHub.Extensions.DisableLLamaLogs();
        
        var context = await AIHub.Agent()
            .WithBackend(BackendType.OpenAi)
            .WithModel("gpt-4o-mini")
            .WithKnowledge(KnowledgeBuilder.Instance
                .AddMcp(new MaIN.Domain.Entities.Mcp
                {
                    Name = "ExaDeepSearch",
                    Arguments = ["-y", "exa-mcp-server"],
                    Command = "npx",
                    EnvironmentVariables = {{"EXA_API_KEY","<raw_key>"}},
                    Backend = BackendType.Gemini,
                    Model = "gemini-2.0-flash"
                }, ["search", "browser", "web access", "research"])
                .AddMcp(new MaIN.Domain.Entities.Mcp
                {
                    Name = "FileSystem",
                    Command = "npx",
                    Arguments = ["-y",
                        "@modelcontextprotocol/server-filesystem",
                        "C:\\Users\\stach\\Desktop",  //Align paths to fit your system
                        "C:\\WiseDev"], //Align paths to fit your system
                    Backend = BackendType.GroqCloud,
                    Model = "openai/gpt-oss-20b"
                }, ["filesystem", "file operations", "read write", "disk search"])
                .AddMcp(new MaIN.Domain.Entities.Mcp
                {
                    Name = "Octocode",
                    Command = "npx",
                    Arguments = ["octocode-mcp"],
                    Backend = BackendType.OpenAi,
                    Model = "gpt-5-nano"
                }, ["code", "github", "repository", "packages", "npm"]))
            .WithSteps(StepBuilder.Instance
                .AnswerUseKnowledge()
                .Build())
            .CreateAsync();
            
        Console.WriteLine("Agent ready! Type 'exit' to quit.\n");
        
        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write("You: ");
            Console.ResetColor();
            
            var input = Console.ReadLine();
            
            if (input?.ToLower() == "exit") break;
            if (string.IsNullOrWhiteSpace(input)) continue;
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Agent: ");
            
            var result = await context.ProcessAsync(input);
            Console.WriteLine(result.Message.Content);
            Console.ResetColor();
            Console.WriteLine();
        }
    }
}
```

## Key Components

### Multi-Backend Architecture
This example showcases using different AI backends for different MCP servers:
- **Primary Agent**: OpenAI GPT-4o-mini for general conversation
- **Web Search**: Gemini 2.0 Flash for search operations
- **Filesystem**: GroqCloud for file operations
- **GitHub**: OpenAI GPT-5-nano for code-related tasks

### Prerequisites

#### API Keys Required
You must set up API keys for three different AI providers:
- **OpenAI**: For primary agent and GitHub operations
- **Google Gemini**: For web search functionality
- **GroqCloud**: For filesystem operations
- **Exa API**: For enhanced web search capabilities

#### GitHub CLI Setup
```bash
gh auth login
```
This gives the Octocode MCP server access to GitHub CLI for repository operations.

## MCP Server Configurations

### 1. ExaDeepSearch MCP Server
```csharp
.AddMcp(new MaIN.Domain.Entities.Mcp
{
    Name = "ExaDeepSearch",
    Arguments = ["-y", "exa-mcp-server"],
    Command = "npx",
    EnvironmentVariables = {{"EXA_API_KEY","<raw_key>"}},
    Backend = BackendType.Gemini,
    Model = "gemini-2.0-flash"
}, ["search", "browser", "web access", "research"])
```

**Purpose**: Advanced web search and research capabilities
- **Command**: Runs via npm package manager
- **Environment**: Requires Exa API key for enhanced search
- **Backend**: Uses Gemini 2.0 Flash for optimal search result processing
- **Tags**: Triggers on search-related queries

### 2. FileSystem MCP Server
```csharp
.AddMcp(new MaIN.Domain.Entities.Mcp
{
    Name = "FileSystem",
    Command = "npx",
    Arguments = ["-y",
        "@modelcontextprotocol/server-filesystem",
        "C:\\Users\\stach\\Desktop",  //Align paths to fit your system
        "C:\\WiseDev"], //Align paths to fit your system
    Backend = BackendType.GroqCloud,
    Model = "openai/gpt-oss-20b"
}, ["filesystem", "file operations", "read write", "disk search"])
```

**Purpose**: Local filesystem access and operations
- **Scope**: Limited to specified directories for security
- **Backend**: GroqCloud for efficient file processing
- **Path Configuration**: Customizable to your system paths
- **Tags**: Activates for file-related operations

### 3. Octocode MCP Server
```csharp
.AddMcp(new MaIN.Domain.Entities.Mcp
{
    Name = "Octocode",
    Command = "npx",
    Arguments = ["octocode-mcp"],
    Backend = BackendType.OpenAi,
    Model = "gpt-5-nano"
}, ["code", "github", "repository", "packages", "npm"])
```

**Purpose**: GitHub repository and package management
- **Integration**: Requires GitHub CLI authentication
- **Backend**: OpenAI for code understanding and generation
- **Capabilities**: Repository analysis, package management, code operations
- **Tags**: Responds to development-related queries

## Interactive Console Interface

The example includes a full interactive console:

```csharp
Console.WriteLine("Agent ready! Type 'exit' to quit.\n");

while (true)
{
    Console.ForegroundColor = ConsoleColor.Blue;
    Console.Write("You: ");
    Console.ResetColor();
    
    var input = Console.ReadLine();
    
    if (input?.ToLower() == "exit") break;
    if (string.IsNullOrWhiteSpace(input)) continue;
    
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("Agent: ");
    
    var result = await context.ProcessAsync(input);
    Console.WriteLine(result.Message.Content);
    Console.ResetColor();
    Console.WriteLine();
}
```

## Example Usage Scenarios

### Web Research Queries
- **"Find the latest information about .NET 9 features"**
- **"Research current best practices for microservices architecture"**
- **"What are the trending JavaScript frameworks in 2025?"**

These queries trigger the ExaDeepSearch MCP server using Gemini 2.0 Flash.

### Filesystem Operations
- **"List all .cs files in my project directory"**
- **"Read the contents of README.md in my desktop folder"**
- **"Create a new file with project documentation"**

These queries activate the FileSystem MCP server using GroqCloud.

### GitHub/Code Operations
- **"Show me the recent commits in my repository"**
- **"What npm packages are outdated in my project?"**
- **"Create a new branch for the feature I'm working on"**

These queries utilize the Octocode MCP server with OpenAI.

## Advanced Features

### Intelligent Backend Selection
Each MCP server is configured with the optimal AI backend:
- **Gemini**: Excellent for search and research tasks
- **GroqCloud**: Fast and efficient for file operations
- **OpenAI**: Superior for code understanding and generation

### Environment Configuration
```csharp
EnvironmentVariables = {{"EXA_API_KEY","your-api-key-here"}}
```
Supports custom environment variables for MCP server configuration.

### Security Considerations
- **Filesystem Access**: Limited to specified directories
- **GitHub Access**: Requires explicit authentication
- **API Keys**: Stored securely in environment variables

## Setup Instructions

### 1. Install Required Packages
```bash
npm install -g exa-mcp-server
npm install -g @modelcontextprotocol/server-filesystem
npm install -g octocode-mcp
```

### 2. Configure API Keys
Set environment variables or appsettings.json:
```json
{
  "OpenAI": {
    "ApiKey": "your-openai-key"
  },
  "Gemini": {
    "ApiKey": "your-gemini-key"
  },
  "GroqCloud": {
    "ApiKey": "your-groq-key"
  }
}
```

### 3. GitHub Authentication
```bash
gh auth login
```

### 4. Customize File Paths
Update the filesystem paths in the code to match your system:
```csharp
Arguments = ["-y",
    "@modelcontextprotocol/server-filesystem",
    "/path/to/your/directory1",
    "/path/to/your/directory2"]
```

## Limitations and Considerations

### Current MCP Limitations
- **Single Server Response**: Cannot combine responses from multiple MCP servers in one request
- **Sequential Processing**: MCP calls are processed individually
- **Error Handling**: Network or server failures affect functionality

### Performance Considerations
- **Network Latency**: MCP servers require network calls
- **API Rate Limits**: Multiple AI providers have different rate limits
- **Resource Usage**: Multiple backend models consume more resources

## Best Practices

### Tag Strategy
- Use specific, non-overlapping tags to avoid conflicts
- Include both broad and specific terms for flexible matching
- Consider user's natural language patterns

### Error Handling
```csharp
// Consider implementing timeout handling
// Add retry logic for failed MCP calls
// Provide fallback responses when MCP servers are unavailable
```

### Cost Optimization
- Choose appropriate models for each task type
- Monitor API usage across different providers
- Consider caching frequently accessed information

This MCP example demonstrates the most sophisticated knowledge integration available in MaIN.NET, enabling agents to access external tools and services while intelligently routing different types of queries to the most appropriate AI backends for optimal results.
