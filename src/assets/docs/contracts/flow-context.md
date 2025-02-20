# `FlowContext` Contract

The `FlowContext` class is designed to manage and interact with AI agent flows in the MaIN (Modular Artificial Intelligence Network) framework. It provides methods for creating, modifying, saving, loading, and processing flows of agents. Flows allow multiple agents to be linked together and interact in a sequential or collaborative manner.

This document provides an overview of the key methods within the `FlowContext` class.

---

### **Methods:**

## **WithId(string id)**

**Purpose**:  
Assigns a unique identifier to the flow.

**Usage**:

```csharp
flowContext.WithId("custom-flow-id");
```

**Parameters**:  
- `id`: The unique identifier for the flow.

**Returns**:  
- The `FlowContext` instance to enable method chaining.

---

## **WithName(string name)**

**Purpose**:  
Assigns a custom name to the flow.

**Usage**:

```csharp
flowContext.WithName("MyFlow");
```

**Parameters**:  
- `name`: The custom name for the flow.

**Returns**:  
- The `FlowContext` instance to enable method chaining.

---

## **WithDescription(string description)**

**Purpose**:  
Sets a description for the flow.

**Usage**:

```csharp
flowContext.WithDescription("This flow manages agents for customer support.");
```

**Parameters**:  
- `description`: A brief description of the flow's purpose.

**Returns**:  
- The `FlowContext` instance to enable method chaining.

---

## **Save(string path)**

**Purpose**:  
Saves the current flow and its associated agents to a zip archive at the specified path. This method also includes a text file for the flow description.

**Usage**:

```csharp
flowContext.Save("/path/to/save/flow.zip");
```

**Parameters**:  
- `path`: The file path where the flow and its agents should be saved.

**Returns**:  
- The `FlowContext` instance to enable method chaining.

---

## **Load(string path)**

**Purpose**:  
Loads an existing flow from a zip archive located at the specified path. This archive should contain the flow description and agent files in JSON format.

**Usage**:

```csharp
flowContext.Load("/path/to/flow.zip");
```

**Parameters**:  
- `path`: The file path where the flow archive is stored.

**Returns**:  
- The `FlowContext` instance to enable method chaining.

---

## **AddAgent(Agent agent)**

**Purpose**:  
Adds an agent to the flow. This allows you to dynamically update the flow with new agents.

**Usage**:

```csharp
flowContext.AddAgent(myAgent);
```

**Parameters**:  
- `agent`: The `Agent` to be added to the flow.

**Returns**:  
- The `FlowContext` instance to enable method chaining.

---

## **ProcessAsync(Chat chat, bool translate = false)**

**Purpose**:  
Processes a chat through the first agent in the flow, generating a response based on the chat’s messages and the agent’s context.

**Usage**:

```csharp
var result = await flowContext.ProcessAsync(chat);
```

**Parameters**:  
- `chat`: The `Chat` object to process.
- `translate`: A flag indicating whether the response should be translated.

**Returns**:  
- A `ChatResult` object containing the processed message and other related information.

---

## **ProcessAsync(string message, bool translate = false)**

**Purpose**:  
Processes a user-provided message through the first agent in the flow, generating a response based on the agent’s context.

**Usage**:

```csharp
var result = await flowContext.ProcessAsync("Hello, agent!");
```

**Parameters**:  
- `message`: The message to be processed by the agent.
- `translate`: A flag indicating whether the response should be translated.

**Returns**:  
- A `ChatResult` object containing the processed message and other related information.

---

## **ProcessAsync(Message message, bool translate = false)**

**Purpose**:  
Processes a message object through the first agent in the flow, generating a response based on the agent's context and message data.

**Usage**:

```csharp
var result = await flowContext.ProcessAsync(new Message { Content = "Hello, agent!" });
```

**Parameters**:  
- `message`: The `Message` object to be processed.
- `translate`: A flag indicating whether the response should be translated.

**Returns**:  
- A `ChatResult` object containing the processed message and other related information.

---

## **AddAgents(IEnumerable<Agent> agents)**

**Purpose**:  
Adds a collection of agents to the flow. This method enables the batch addition of multiple agents at once.

**Usage**:

```csharp
flowContext.AddAgents(new List<Agent> { agent1, agent2 });
```

**Parameters**:  
- `agents`: A collection of `Agent` objects to be added to the flow.

**Returns**:  
- The `FlowContext` instance to enable method chaining.

---

## **CreateAsync()**

**Purpose**:  
Asynchronously creates the agent flow in the system by invoking the associated flow service.

**Usage**:

```csharp
await flowContext.CreateAsync();
```

**Returns**:  
- An `AgentFlow` object representing the created flow.

---

## **Delete()**

**Purpose**:  
Deletes the current flow from the system. This method ensures that any previously created flow is removed.

**Usage**:

```csharp
await flowContext.Delete();
```

**Returns**:  
- No return value.

---

## **GetCurrentFlow()**

**Purpose**:  
Fetches the current flow from the system. The flow is retrieved by its ID if it exists.

**Usage**:

```csharp
AgentFlow currentFlow = await flowContext.GetCurrentFlow();
```

**Returns**:  
- The `AgentFlow` object representing the current flow.

---

## **GetAllFlows()**

**Purpose**:  
Fetches all agent flows available in the system.

**Usage**:

```csharp
List<AgentFlow> allFlows = await flowContext.GetAllFlows();
```

**Returns**:  
- A list of `AgentFlow` objects representing all agent flows in the system.

---

## **FromExisting(string flowId)**

**Purpose**:  
Fetches an existing flow by its ID and reinitializes the `FlowContext` instance with the flow data.

**Usage**:

```csharp
FlowContext existingFlowContext = await flowContext.FromExisting("existing-flow-id");
```

**Parameters**:  
- `flowId`: The ID of the flow to be fetched.

**Returns**:  
- A new `FlowContext` instance initialized with the existing flow.

---

### Summary:

The `FlowContext` class provides robust capabilities for managing agent flows, allowing developers to create, modify, and process interactions within a collection of agents. By enabling agent additions, saving and loading flows, and processing chats or messages, this class facilitates complex agent workflows, providing a highly flexible and extensible system for AI-based interaction in the MaIN framework.