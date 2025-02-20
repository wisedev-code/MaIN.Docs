# `AgentContext` Contract

The `AgentContext` class is a key component within the MaIN (Modular Artificial Intelligence Network) ecosystem, designed to manage and configure individual AI agents. It offers methods for creating, modifying, and interacting with agents. Through this class, developers can set up agents with specific behaviors, source information, prompts, models, and more. The `AgentContext` also allows for processing messages, managing agent states, and interacting with chat sessions.

This document outlines the functionality of each method in the `AgentContext` class.

---

### **Methods:**

## **WithId(string id)**

**Purpose**:  
Assigns a unique identifier to the agent.

**Usage**:

```csharp
agentContext.WithId("custom-agent-id");
```

**Parameters**:  
- `id`: The unique identifier for the agent.

**Returns**:  
- The `AgentContext` instance to enable method chaining.

---

## **GetAgentId()**

**Purpose**:  
Retrieves the unique identifier for the agent.

**Usage**:

```csharp
string agentId = agentContext.GetAgentId();
```

**Returns**:  
- A string representing the agent's unique identifier.

---

## **GetAgent()**

**Purpose**:  
Fetches the current agent instance.

**Usage**:

```csharp
Agent currentAgent = agentContext.GetAgent();
```

**Returns**:  
- The `Agent` object containing all the properties and data for the current agent.

---

## **WithOrder(int order)**

**Purpose**:  
Sets the order of the agent. This can be used in scenarios where agents need to be prioritized or sequenced.

**Usage**:

```csharp
agentContext.WithOrder(1);
```

**Parameters**:  
- `order`: The order value to assign to the agent.

**Returns**:  
- The `AgentContext` instance to enable method chaining.

---

## **WithSource(IAgentSource source, AgentSourceType type)**

**Purpose**:  
Sets the source of the agent’s context, including information related to the agent's source and its type.

**Usage**:

```csharp
agentContext.WithSource(mySource, AgentSourceType.External);
```

**Parameters**:  
- `source`: The source instance providing the agent’s context.
- `type`: The source type, such as `External` or `Internal`.

**Returns**:  
- The `AgentContext` instance to enable method chaining.

---

## **WithName(string name)**

**Purpose**:  
Assigns a custom name to the agent.

**Usage**:

```csharp
agentContext.WithName("MyCustomAgent");
```

**Parameters**:  
- `name`: The custom name for the agent.

**Returns**:  
- The `AgentContext` instance to enable method chaining.

---

## **WithModel(string model)**

**Purpose**:  
Sets the AI model for the agent to use during its interactions.

**Usage**:

```csharp
agentContext.WithModel("llama3.2:3b");
```

**Parameters**:  
- `model`: The name or identifier of the AI model to be used.

**Returns**:  
- The `AgentContext` instance to enable method chaining.

---

## **WithCustomModel(string model, string path)**

**Purpose**:  
Specifies a custom model along with its file path for use by the agent. This allows using locally stored models in addition to predefined ones.

**Usage**:

```csharp
agentContext.WithCustomModel("my-custom-model", "./models/myModel");
```

**Parameters**:  
- `model`: The name of the custom model.
- `path`: The path to the custom model’s file.

**Returns**:  
- The `AgentContext` instance to enable method chaining.

---

## **WithInitialPrompt(string prompt)**

**Purpose**:  
Sets the initial prompt for the agent. This prompt serves as an instruction or context that guides the agent's behavior during its execution.

**Usage**:

```csharp
agentContext.WithInitialPrompt("Hello, I'm here to assist you.");
```

**Parameters**:  
- `prompt`: The initial prompt or instruction for the agent.

**Returns**:  
- The `AgentContext` instance to enable method chaining.

---

## **WithSteps(List<string> steps)**

**Purpose**:  
Configures the steps that the agent will follow during its interaction. Each step is a task or action that the agent will execute sequentially.

**Usage**:

```csharp
agentContext.WithSteps(new List<string> { "ANSWER", "PROCESS" });
```

**Parameters**:  
- `steps`: A list of strings representing the steps the agent should follow.

**Returns**:  
- The `AgentContext` instance to enable method chaining.

---

## **WithBehaviour(string name, string instruction)**

**Purpose**:  
Defines a behavior for the agent, specifying an action or task the agent should perform based on the provided name and instruction.

**Usage**:

```csharp
agentContext.WithBehaviour("Assist", "Assist with general inquiries.");
```

**Parameters**:  
- `name`: The name of the behavior.
- `instruction`: The instruction associated with the behavior.

**Returns**:  
- The `AgentContext` instance to enable method chaining.

---

## **CreateAsync(bool flow = false, bool interactiveResponse = false)**

**Purpose**:  
Asynchronously creates the agent in the system. This method integrates the agent into the underlying agent service, making it ready for use.

**Usage**:

```csharp
await agentContext.CreateAsync();
```

**Parameters**:  
- `flow`: A flag indicating whether the agent should be part of an agent flow.
- `interactiveResponse`: A flag indicating whether the agent should generate interactive responses.

**Returns**:  
- The `AgentContext` instance to enable method chaining.

---

## **Create(bool flow = false, bool interactiveResponse = false)**

**Purpose**:  
Synchronously creates the agent in the system.

**Usage**:

```csharp
agentContext.Create();
```

**Parameters**:  
- `flow`: A flag indicating whether the agent should be part of an agent flow.
- `interactiveResponse`: A flag indicating whether the agent should generate interactive responses.

**Returns**:  
- The `AgentContext` instance to enable method chaining.

---

## **ProcessAsync(Chat chat, bool translate = false)**

**Purpose**:  
Processes a chat through the agent, generating a response based on the chat’s messages and the agent's context.

**Usage**:

```csharp
var result = await agentContext.ProcessAsync(chat);
```

**Parameters**:  
- `chat`: The `Chat` object to process.
- `translate`: A flag indicating whether the response should be translated.

**Returns**:  
- A `ChatResult` object containing the processed message and other related information.

---

## **ProcessAsync(string message, bool translate = false)**

**Purpose**:  
Processes a user-provided message through the agent, generating a response based on the agent’s context.

**Usage**:

```csharp
var result = await agentContext.ProcessAsync("Hello, agent!");
```

**Parameters**:  
- `message`: The message to be processed by the agent.
- `translate`: A flag indicating whether the response should be translated.

**Returns**:  
- A `ChatResult` object containing the processed message and other related information.

---

## **ProcessAsync(Message message, bool translate = false)**

**Purpose**:  
Processes a message object through the agent, generating a response based on the agent's context and message data.

**Usage**:

```csharp
var result = await agentContext.ProcessAsync(new Message { Content = "Hello, agent!" });
```

**Parameters**:  
- `message`: The `Message` object to be processed.
- `translate`: A flag indicating whether the response should be translated.

**Returns**:  
- A `ChatResult` object containing the processed message and other related information.

---

## **GetChat()**

**Purpose**:  
Retrieves the chat session associated with the current agent.

**Usage**:

```csharp
Chat agentChat = await agentContext.GetChat();
```

**Returns**:  
- A `Chat` object representing the chat session associated with the agent.

---

## **RestartChat()**

**Purpose**:  
Restarts the chat session associated with the current agent, typically resetting the conversation state.

**Usage**:

```csharp
Chat restartedChat = await agentContext.RestartChat();
```

**Returns**:  
- A `Chat` object representing the restarted chat session.

---

## **GetAllAgents()**

**Purpose**:  
Fetches all agents managed by the underlying agent service.

**Usage**:

```csharp
List<Agent> agents = await agentContext.GetAllAgents();
```

**Returns**:  
- A list of `Agent` objects representing all agents.

---

## **Delete()**

**Purpose**:  
Deletes the current agent from the system.

**Usage**:

```csharp
await agentContext.Delete();
```

---

## **Exists()**

**Purpose**:  
Checks if the current agent exists in the system.

**Usage**:

```csharp
bool agentExists = await agentContext.Exists();
```

**Returns**:  
- A boolean indicating whether the agent exists.

---

## **FromExisting(IAgentService agentService, string agentId)**

**Purpose**:  
Fetches an existing agent by its ID, allowing you to create a new `AgentContext` from an already existing agent.

**Usage**:

```csharp
AgentContext existingAgentContext = await AgentContext.FromExisting(agentService, "existing-agent-id");
```

**Parameters**:  
- `agentService`: The service to interact with the agent.
- `agentId`: The ID of the agent to fetch.

**Returns**:  
- A new `AgentContext` instance associated with the existing agent.

---

### Summary:
`AgentContext` is a powerful tool for managing AI agents within the MaIN framework. It provides flexibility in setting agent attributes, handling messages, managing chats, and interacting with agent services. With its rich set of methods, it supports creating agents, defining behaviors, processing user inputs, and ensuring that agents can seamlessly fit into various AI workflows.