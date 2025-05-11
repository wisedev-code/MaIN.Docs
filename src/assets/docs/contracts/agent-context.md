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

## **WithBackend(BackendType backendType)**

**Purpose**:  
Defines backend that will be used for model inference

**Usage**:

```csharp
agentContext.WithBackend(BackendType.OpenAi)
```

**Parameters**:  
- `backendType`: An enum that defines which Ai backend to use, Default uses .Self (LLamaSharp backend), ATM available options are: OpenAi, Gemini, Self

**Returns**:  
- The `AgentContext` instance to enable method chaining.

---

## **DisableCache()**

**Purpose**:  
Each time we run inference we need to load model into memory, this takes time and memory. This method allows us to save some more of GPU/RAM resources with cost of time, because model weights are no longer cached

**Usage**:

```csharp
agentContext.DisableCache()
```

**Parameters**:  
(nothing to see here ;p)

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
## **WithInferenceParams(InferenceParams inferenceParams)**

**Purpose**:  
Sets the inference parameters for the chat session, allowing you to customize how the AI processes and generates responses based on specific parameters. Inference parameters can influence various aspects of the chat, such as response length, temperature, and other model-specific settings.

# InferenceParams Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Temperature` | float | 0.8 | Controls randomness in generation. Higher values (e.g., 1.0) make output more random, lower values (e.g., 0.2) make it more focused and deterministic. |
| `ContextSize` | int | 1024 | Maximum number of tokens that can be processed in a single inference operation. Defines the "memory" window for the model. |
| `GpuLayerCount` | int | 30 | Number of model layers to run on GPU (vs CPU). Higher values use more GPU memory but improve performance. |
| `SeqMax` | uint | 1 | Maximum number of sequences to generate. |
| `BatchSize` | uint | 512 | Number of tokens processed simultaneously during inference. Higher values can improve throughput. |
| `UBatchSize` | uint | 512 | Update batch size - number of tokens processed in a single update operation. |
| `Embeddings` | bool | false | Whether to return token embeddings alongside generated text. |
| `TypeK` | int | 0 | Type of key tensors to use in attention mechanism. 0 typically indicates default type. |
| `TypeV` | int | 0 | Type of value tensors to use in attention mechanism. 0 typically indicates default type. |
| `TokensKeep` | int | - | Number of tokens to retain from previous context when continuing generation. |
| `MaxTokens` | int | -1 | Maximum number of tokens to generate. -1 typically means no specific limit beyond context size. |
| `TopK` | int | 40 | Limits token selection to the K most likely next tokens. Helps control output quality. |
| `TopP` | float | 0.9 | Nucleus sampling threshold. Selects from smallest set of tokens whose cumulative probability exceeds P. |

**Usage**:

```csharp
InferenceParams inferenceParams = new InferenceParams
{
    Temperature = 0.7f,
    MaxTokens = 200
};
agentContext.WithInferenceParams(inferenceParams);
```

**Parameters**:  
- `inferenceParams`: An `InferenceParams` object that holds the parameters for inference, such as `Temperature`, `MaxTokens`, `TopP`, etc. These parameters control the generation behavior of the agent.

**Returns**:  
- The `AgentContext` instance to enable method chaining. This allows further configurations or operations to be applied to the same agent context.

---

## WithMemoryParams(MemoryParams memoryParams)

**Purpose**:
Sets the memory parameters for the chat session, allowing you to customize how the AI accesses and utilizes its memory for generating responses. Memory parameters influence aspects such as context size, memory search depth, and token allocation for responses.

**# MemoryParams Properties**
| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ContextSize` | int | 2048 | Maximum number of tokens that can be processed in the memory context window. Defines how much "memory" the model can access. |
| `GpuLayerCount` | int | 30 | Number of memory-related model layers to run on GPU (vs CPU). Higher values use more GPU memory but improve performance. |
| `MaxMatchesCount` | int | 5 | Maximum number of memory matches to retrieve when generating a response. Controls how many relevant memories are considered. |
| `FrequencyPenalty` | float | 1.0 | Reduces the likelihood of repetition in responses. Higher values discourage repeating the same content. |
| `Temperature` | float | 0.6 | Controls randomness in memory-based generation. Higher values produce more diverse responses, lower values more focused ones. |
| `AnswerTokens` | int | 500 | Maximum number of tokens reserved for the response. If the model supports 5000 tokens and AnswerTokens is 500, the prompt (including question and grounding information) will be limited to 4500 tokens. |

**Usage**:
```csharp
MemoryParams memoryParams = new MemoryParams
{
    ContextSize = 4096,
    MaxMatchesCount = 10,
    AnswerTokens = 800
};
agentContext.WithMemoryParams(memoryParams);
```

**Parameters**:
- `memoryParams`: A `MemoryParams` object that holds the parameters for memory management, such as `ContextSize`, `MaxMatchesCount`, `AnswerTokens`, etc. These parameters control how agent utilizes memory for response generation.

**Returns**:
- The `AgentContext` instance to enable method chaining. This allows further configurations or operations to be applied to the same agent context.

---

### Summary:

`AgentContext` is a powerful tool for managing AI agents within the MaIN framework. It provides flexibility in setting agent attributes, handling messages, managing chats, and interacting with agent services. With its rich set of methods, it supports creating agents, defining behaviors, processing user inputs, and ensuring that agents can seamlessly fit into various AI workflows.

The addition of the `WithInferenceParams` method further enhances the ability to control how agents generate responses by adjusting inference-related parameters, allowing developers to fine-tune agent behavior to fit specific use cases.