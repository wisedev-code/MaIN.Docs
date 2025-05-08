# `ChatContext` Contract

`ChatContext` is an essential class in MaIN (Modular Artificial Intelligence Network) for managing and interacting with chat sessions. This class allows you to set up and maintain an interactive chat flow, configure models, add user/system messages, and manage related files. It provides functionality for completing chat sessions and managing their state.

This document outlines each method available in `ChatContext` with its purpose and usage.

---

### **Methods:**

## **WithModel(string model)**

**Purpose**:  
Sets the AI model to be used for the current chat session. This determines how the AI will respond to messages based on the selected model.

**Usage**:

```csharp
chatContext.WithModel("llama3.2:3b");
```

**Parameters**:  
- `model`: The name of the AI model to be used.

---

## **WithCustomModel(string model, string path)**

**Purpose**:  
Allows you to specify a custom model along with its file path. This is useful when you have a locally stored model that you wish to use for the current session.

**Usage**:

```csharp
chatContext.WithCustomModel("custom-model", "./path/to/model");
```

**Parameters**:  
- `model`: The name of the custom model.
- `path`: The file path where the model is located.

---

## **WithMessage(string content)**

**Purpose**:  
Adds a user message to the chat. This method captures the message content and assigns the "User" role to it. It also timestamps the message for proper ordering.

**Usage**:

```csharp
chatContext.WithMessage("What is the weather today?");
```

**Parameters**:  
- `content`: The message content that you wish to send.

---

## **WithSystemPrompt(string systemPrompt)**

**Purpose**:  
Inserts a system message at the beginning of the chat. System messages are typically used for setting the context or providing instructions to the AI.

**Usage**:

```csharp
chatContext.WithSystemPrompt("You are a helpful assistant.");
```

**Parameters**:  
- `systemPrompt`: The system prompt content that provides instructions to the AI.

---

## **WithFiles(List<FileInfo> files, preProcess = false)**

**Purpose**:  
Attaches files to the most recent message in the chat. Files are associated with the last message to provide additional context or media for the AI to process.

**Usage**:

```csharp
var files = new List<FileInfo> { new FileInfo("./documents/file.pdf") };
chatContext.WithFiles(files);
```

**Parameters**:  
- `files`: A list of `FileInfo` objects representing the files to attach.
- `preProcess`: Include preprocessing of document, that can consume more time and resources, but can also greatly improve quality of inference

---

## **WithFiles(List<FileStream> fileStreams, bool preProcess = false)**

**Purpose**:  
Attaches files to the most recent message in the chat. Files are associated with the last message to provide additional context or media for the AI to process.

**Usage**:

```csharp
 FileStream fs = new FileStream(
                    "./documents/file.pdf",
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);

chatContext.WithFiles([fs], preProcess: true);
```

**Parameters**:  
- `files`: A list of `FileStream` objects representing the files to attach.
- `preProcess`: Include preprocessing of document, that can consume more time and resources, but can also greatly improve quality of inference


---

## **WithFiles(List<string> filePaths, preProcess = false)**

**Purpose**:  
Attaches a list of files to the most recent message in the chat by specifying their file paths. This method is an alternative to using `FileInfo`.

**Usage**:

```csharp
chatContext.WithFiles(new List<string> { "./documents/file.pdf", "./images/photo.png" });
```

**Parameters**:  
- `filePaths`: A list of file paths to attach to the most recent message.
- `preProcess`: Include preprocessing of document, that can consume more time and resources, but can also greatly improve quality of inference


---


## **WithBackend(BackendType backendType)**

**Purpose**:  
Defines backend that will be used for model inference

**Usage**:

```csharp
chatContext.WithBackend(BackendType.OpenAi)
```

**Parameters**:  
- `backendType`: An enum that defines which Ai backend to use, Default uses .Self (LLamaSharp backend), ATM available options are: OpenAi, Self

---

## **DisableCache()**

**Purpose**:  
Each time we run inference we need to load model into memory, this takes time and memory. This method allows us to save some more of GPU/RAM resources with cost of time, because model weights are no longer cached

**Usage**:

```csharp
chatContext.DisableCache()
```

**Parameters**:  
(nothing to see here ;p)

---

## **EnableVisual()**

**Purpose**:  
Enables the visual output for the current chat session. This flag allows the AI to generate and return visual content, such as images or charts, as part of its response.

**Usage**:

```csharp
chatContext.EnableVisual();
```

**No parameters**.

---

## **GetChatId()**

**Purpose**:  
Returns the unique identifier of the current chat session. This is useful for tracking or managing specific chats within a broader system.

**Usage**:

```csharp
string chatId = chatContext.GetChatId();
```

**Returns**:  
- A string representing the chat's unique identifier.

---

## **CompleteAsync(bool translate = false, bool interactive = false)**

**Purpose**:  
Completes the chat session by generating a response based on the messages so far. This method interacts with the underlying chat service to process the chat and generate a result.

**Usage**:

```csharp
var result = await chatContext.CompleteAsync();
```

**Parameters**:  
- `translate`: A flag indicating whether the response should be translated. Default is `false`.
- `interactive`: A flag indicating whether the chat session should be interactive. Default is `false`.

**Returns**:  
- A `ChatResult` object containing the result of the completed chat session.

---

## **GetCurrentChat()**

**Purpose**:  
Retrieves the current chat session by its ID. This method is useful when you need to access the ongoing chat session and inspect its data.

**Usage**:

```csharp
Chat currentChat = await chatContext.GetCurrentChat();
```

**Returns**:  
- A `Chat` object representing the current chat session.

**Throws**:  
- `InvalidOperationException` if the chat has not been created yet.

---

## **GetAllChats()**

**Purpose**:  
Fetches all available chat sessions stored in the system. This can be used to list past chat sessions.

**Usage**:

```csharp
List<Chat> allChats = await chatContext.GetAllChats();
```

**Returns**:  
- A list of `Chat` objects representing all chat sessions.

---

## **DeleteChat()**

**Purpose**:  
Deletes the current chat session. This is useful for cleanup or when you no longer need the chat data.

**Usage**:

```csharp
await chatContext.DeleteChat();
```

**Throws**:  
- `InvalidOperationException` if the chat has not been created yet.

---

## **FromExisting(string chatId)**

**Purpose**:  
Creates a new `ChatContext` from an existing chat session identified by its `chatId`. This allows you to resume or interact with an already existing chat.

**Usage**:

```csharp
ChatContext resumedChatContext = await chatContext.FromExisting("existing-chat-id");
```

**Parameters**:  
- `chatId`: The unique identifier of the chat you want to resume.

**Returns**:  
- A new `ChatContext` object that represents the existing chat.

---

## **GetChatHistory()**

**Purpose**:  
Retrieves a simplified list of message summaries from the chat history. This is useful for viewing a short overview of the conversation without the full message details.

**Usage**:

```csharp
List<MessageShort> chatHistory = chatContext.GetChatHistory();
```

**Returns**:  
- A list of `MessageShort` objects, each containing the content, role (user/system), and timestamp of a message.

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
chatContext.WithInferenceParams(inferenceParams);
```

**Parameters**:  
- `inferenceParams`: An `InferenceParams` object that holds the parameters for inference, such as `Temperature`, `MaxTokens`, `TopP`, etc. These parameters control the generation behavior of the chat.

**Returns**:  
- The `ChatContext` instance to enable method chaining. This allows further configurations or operations to be applied to the same chat context.

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
chatContext.WithMemoryParams(memoryParams);
```

**Parameters**:
- `memoryParams`: A `MemoryParams` object that holds the parameters for memory management, such as `ContextSize`, `MaxMatchesCount`, `AnswerTokens`, etc. These parameters control how the chat utilizes memory for response generation.

**Returns**:
- The `ChatContext` instance to enable method chaining. This allows further configurations or operations to be applied to the same chat context.

---

### Summary:
`ChatContext` provides a simple yet powerful interface for managing AI-driven chat sessions. It allows you to configure AI models, manage chat messages, attach files, and retrieve or delete chat sessions as needed. This class offers a flexible, modular approach to integrating chat functionalities into your applications, with support for both system-generated and user-generated content.

The addition of the `WithInferenceParams` method further enhances the ability to control how chat responses are generated. By adjusting inference-related parameters, developers can fine-tune the chat's behavior to suit various conversational contexts or use cases.