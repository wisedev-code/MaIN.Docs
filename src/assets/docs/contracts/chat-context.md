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

## **WithFiles(List<FileInfo> files)**

**Purpose**:  
Attaches files to the most recent message in the chat. Files are associated with the last message to provide additional context or media for the AI to process.

**Usage**:

```csharp
var files = new List<FileInfo> { new FileInfo("./documents/file.pdf") };
chatContext.WithFiles(files);
```

**Parameters**:  
- `files`: A list of `FileInfo` objects representing the files to attach.

---

## **WithFiles(List<string> filePaths)**

**Purpose**:  
Attaches a list of files to the most recent message in the chat by specifying their file paths. This method is an alternative to using `FileInfo`.

**Usage**:

```csharp
chatContext.WithFiles(new List<string> { "./documents/file.pdf", "./images/photo.png" });
```

**Parameters**:  
- `filePaths`: A list of file paths to attach to the most recent message.

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

### Summary:
`ChatContext` provides a simple yet powerful interface for managing AI-driven chat sessions. It allows you to configure AI models, manage chat messages, attach files, and retrieve or delete chat sessions as needed. This class offers a flexible, modular approach to integrating chat functionalities into your applications, with support for both system-generated and user-generated content.

The addition of the `WithInferenceParams` method further enhances the ability to control how chat responses are generated. By adjusting inference-related parameters, developers can fine-tune the chat's behavior to suit various conversational contexts or use cases.