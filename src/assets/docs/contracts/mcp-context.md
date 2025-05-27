# `McpContext` Contract

The `McpContext` class is designed to manage and interact with MCP (Model Context Protocol) configurations in the MaIN (Modular Artificial Intelligence Network) framework. It provides methods for configuring MCP settings, selecting backends, and processing prompts through the MCP service. This class enables seamless integration with external MCP servers and services.

The `McpContext` works with the `Mcp` configuration entity which defines the connection parameters, command arguments, model settings, and environment variables required for MCP server interactions.

This document provides an overview of the key methods within the `McpContext` class and the `Mcp` configuration structure.

---

## **Mcp Configuration Entity**

The `Mcp` class defines the configuration structure required for MCP server connections and operations:

```csharp
public class Mcp 
{
    public required string Name { get; init; }
    public required List<string> Arguments { get; init; }
    public required string Command { get; init; }
    public required string Model { get; init; }
    public Dictionary<string, string> Properties { get; set; } = [];
    public BackendType? Backend { get; set; }
    public Dictionary<string, string> EnvironmentVariables { get; set; } = [];
    
    public static Mcp NotSet => new Mcp()
    {
        Arguments = [],
        Command = string.Empty,
        Model = string.Empty,
        Properties = new Dictionary<string, string>(),
        Name = string.Empty,
        Backend = BackendType.Self
    };
}
```

**Properties**:
- `Name`: A descriptive name for the MCP configuration
- `Arguments`: Command-line arguments to pass to the MCP server
- `Command`: The executable command to start the MCP server
- `Model`: The AI model identifier to use for processing
- `Properties`: Additional key-value properties for configuration
- `Backend`: The backend type for processing (optional)
- `EnvironmentVariables`: Environment variables to set for the MCP server process

**Static Property**:
- `NotSet`: A default empty configuration used for initialization

---

### **Methods:**

## **WithConfig(Mcp mcpConfig)**

**Purpose**:  
Sets the MCP configuration for the context. This configuration defines the connection parameters and settings required to interact with MCP servers.

**Usage**:

```csharp
mcpContext.WithConfig(myMcpConfig);
```

**Parameters**:  
- `mcpConfig`: The `Mcp` configuration object containing server connection details and settings.

**Returns**:  
- The `McpContext` instance to enable method chaining.

---

## **WithBackend(BackendType backendType)**

**Purpose**:  
Specifies the backend type to be used for MCP operations. This allows you to select different backend implementations based on your requirements.

**Usage**:

```csharp
mcpContext.WithBackend(BackendType.OpenAI);
```

**Parameters**:  
- `backendType`: The `BackendType` enum value specifying which backend implementation to use.

**Returns**:  
- The `McpContext` instance to enable method chaining.

**Note**:  
This method requires that a configuration has already been set using `WithConfig()`. If no configuration is present, this will throw an exception.

---

## **PromptAsync(string prompt)**

**Purpose**:  
Asynchronously processes a prompt through the configured MCP service, sending the prompt to the MCP server and returning the processed result.

**Usage**:

```csharp
var result = await mcpContext.PromptAsync("What is the weather today?");
```

**Parameters**:  
- `prompt`: The text prompt to be processed by the MCP service.

**Returns**:  
- A `McpResult` object containing the processed response from the MCP server.

**Exceptions**:  
- `InvalidOperationException`: Thrown when no MCP configuration has been set using `WithConfig()`.

**Note**:  
This method requires that a valid MCP configuration has been established before calling. Ensure `WithConfig()` has been called with a valid configuration object.

---

### **Usage Examples:**

**Usage Examples:**

**Creating an MCP Configuration**:

```csharp
var mcpConfig = new Mcp
{
    Name = "WeatherService",
    Command = "python",
    Arguments = ["weather_mcp_server.py", "--port", "8080"],
    Model = "gpt-4",
    Properties = { ["timeout"] = "30", ["retries"] = "3" },
    Backend = BackendType.OpenAI,
    EnvironmentVariables = { ["API_KEY"] = "your-api-key", ["LOG_LEVEL"] = "INFO" }
};
```

**Basic Configuration and Prompt Processing**:

```csharp
// Configure MCP context with custom configuration
mcpContext
    .WithConfig(mcpConfig)
    .WithBackend(BackendType.Claude);

// Process a prompt
var result = await mcpContext.PromptAsync("Analyze this data set");
```

**Using Default Configuration**:

```csharp
// Start with default configuration and modify as needed
mcpContext
    .WithConfig(Mcp.NotSet)
    .WithBackend(BackendType.Self);
```

**Chain Configuration Methods**:

```csharp
// Method chaining for clean configuration
var result = await mcpContext
    .WithConfig(serverConfig)
    .WithBackend(BackendType.OpenAI)
    .PromptAsync("Generate a summary report");
```

---

### **Important Notes:**

- **Configuration Requirement**: The `McpContext` must be configured with a valid `Mcp` configuration object before processing prompts. Attempting to call `PromptAsync()` without a configuration will result in an `InvalidOperationException`.

- **MCP Configuration Structure**: The `Mcp` entity provides comprehensive configuration options including server command execution, environment variables, and custom properties to support various MCP server implementations.

- **Default Configuration**: The `Mcp.NotSet` static property provides a safe default configuration that can be used as a starting point, with `BackendType.Self` as the default backend.

- **Environment Variables**: The configuration supports setting environment variables that will be available to the MCP server process, enabling secure API key management and runtime configuration.

- **Command Arguments**: The `Arguments` list allows passing command-line parameters to the MCP server executable, providing flexibility in server startup configuration.

- **Backend Selection**: The `WithBackend()` method allows you to specify which backend implementation to use for processing prompts, providing flexibility in choosing the appropriate AI model or service.

- **Method Chaining**: All configuration methods return the `McpContext` instance, enabling fluent method chaining for clean and readable code.

- **Asynchronous Operations**: Prompt processing is asynchronous to ensure non-blocking operations when communicating with external MCP servers.

---

### Summary:

The `McpContext` class provides a streamlined interface for interacting with MCP (Model Context Protocol) services within the MaIN framework. Working together with the comprehensive `Mcp` configuration entity, it enables detailed server configuration including command execution, environment variables, and custom properties. The class facilitates integration with external AI services and servers through configuration management, backend selection, and prompt processing capabilities. The fluent API design ensures easy configuration and usage, while the asynchronous processing capabilities support scalable and responsive applications. The rich configuration options support various MCP server implementations and deployment scenarios.