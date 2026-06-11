# ChatContext

`ChatContext` is the fluent builder for single-turn and multi-turn LLM completions. Access it via `AIHub.Chat()`.

## Builder Chain

```
AIHub.Chat()
  → WithModel(modelId)           ← IChatBuilderEntryPoint
    → WithMessage(content)       ← IChatMessageBuilder
      → [configuration]          ← IChatConfigurationBuilder
        → CompleteAsync()
```

## Minimal Example

```csharp
var result = await AIHub.Chat()
    .WithModel(Models.Local.Gemma2_2b)
    .EnsureModelDownloaded()
    .WithMessage("Where do hedgehogs go at night?")
    .CompleteAsync(interactive: true);
```

---

## IChatBuilderEntryPoint

Entry methods available immediately after `AIHub.Chat()`.

---

### `WithModel`

```csharp
IChatMessageBuilder WithModel(string modelId)
```

Sets the model for this completion. Must be called before adding messages.

| Parameter | Type | Description |
|---|---|---|
| `modelId` | `string` | Model identifier. Use `Models.*` constants (`Models.Local.Gemma2_2b`, `Models.OpenAi.Gpt4o`, etc.) or a plain string. Throws `ModelNotFoundException` if the ID is not in the registry. |

---

### `FromExisting`

```csharp
Task<IChatConfigurationBuilder> FromExisting(string chatId)
```

Loads a previously persisted chat by ID, restoring all messages. Skips the `WithModel` / `WithMessage` steps since the model and history are already stored.

| Parameter | Type | Description |
|---|---|---|
| `chatId` | `string` | ID from a previous `GetChatId()` call on another `ChatContext`. |

---

## IChatMessageBuilder

Methods available after `WithModel`.

---

### `EnsureModelDownloaded`

```csharp
IChatMessageBuilder EnsureModelDownloaded()
```

Flags the context to download the model file before `CompleteAsync` executes if the file is not already present on disk. No-op for cloud backends. Useful in first-run console apps where you want a single fluent chain without a separate download step.

---

### `WithMessage` (text)

```csharp
IChatConfigurationBuilder WithMessage(string content)
```

Appends a user-role message to the conversation.

| Parameter | Type | Description |
|---|---|---|
| `content` | `string` | The user's message text. Added with a UTC timestamp and `Role = "user"`. |

---

### `WithMessage` (vision)

```csharp
IChatConfigurationBuilder WithMessage(string content, byte[] image)
```

Appends a user-role message that includes an inline image. The model must support vision (e.g. `Llava16Mistral_7b`, `Gpt4o`, `Gemini2_5Flash`).

| Parameter | Type | Description |
|---|---|---|
| `content` | `string` | The user's message text (question or instruction about the image). |
| `image` | `byte[]` | Raw image bytes. PNG and JPEG are supported. |

---

### `WithMessages`

```csharp
IChatConfigurationBuilder WithMessages(IEnumerable<Message> messages)
```

Bulk-appends a list of messages to the conversation. Useful when importing an existing chat history.

| Parameter | Type | Description |
|---|---|---|
| `messages` | `IEnumerable<Message>` | Messages to append. Each `Message` requires `Role` (`"user"` / `"assistant"` / `"system"`), `Content` (string), and `Type` (`MessageType` enum). |

---

## IChatConfigurationBuilder

Optional configuration methods available after `WithMessage`. All return `IChatConfigurationBuilder` so they chain freely.

---

### `WithSystemPrompt`

```csharp
IChatConfigurationBuilder WithSystemPrompt(string systemPrompt)
```

Inserts a system-role message at index 0 of the message list, before all user and assistant messages.

| Parameter | Type | Description |
|---|---|---|
| `systemPrompt` | `string` | The system instruction. Sets persona, task framing, or output constraints for the model. |

---

### `WithInferenceParams`

```csharp
IChatConfigurationBuilder WithInferenceParams(IBackendInferenceParams inferenceParams)
```

Attaches provider-specific generation parameters. Use the class that matches the active backend.

| Parameter | Type | Description |
|---|---|---|
| `inferenceParams` | `IBackendInferenceParams` | Provider-specific params object. See table below. |

| Backend | Class |
|---|---|
| `Self` (local GGUF) | `LocalInferenceParams` — `Grammar`, `Temperature`, `TopP`, `MaxTokens`, etc. |
| `OpenAi` | `OpenAiInferenceParams` — `ResponseFormat`, `AdditionalParams` dictionary |
| `Vertex` | `VertexInferenceParams` — `Location` (GCP region string) |

---

### `WithTools`

```csharp
IChatConfigurationBuilder WithTools(ToolsConfiguration toolsConfiguration)
```

Attaches function-calling tools that the model may invoke during completion.

| Parameter | Type | Description |
|---|---|---|
| `toolsConfiguration` | `ToolsConfiguration` | Built via `ToolsConfigurationBuilder`. Use `.AddTool(name, description, execute)` or the generic `.AddTool<TArgs>(...)` overload, then `.WithToolChoice("auto")` and `.Build()`. |

> **COMPILE RULE — required `using` statements:**
> `ToolsConfigurationBuilder` lives in `MaIN.Core.Hub.Utils`; `ToolsConfiguration` (and
> `ToolDefinition`) live in `MaIN.Domain.Entities.Tools`. Add both:
> ```csharp
> using MaIN.Core.Hub.Utils;
> using MaIN.Domain.Entities.Tools;
> ```
> Omitting these causes `CS0246: The type or namespace name 'ToolsConfigurationBuilder'
> could not be found`.

---

### `WithMemoryParams`

```csharp
IChatConfigurationBuilder WithMemoryParams(MemoryParams memoryParams)
```

Configures context window settings for the completion.

| Parameter | Type | Description |
|---|---|---|
| `memoryParams` | `MemoryParams` | Context window configuration. Key property: `ContextSize` (int) — maximum token window size. |

---

### `Speak`

```csharp
IChatConfigurationBuilder Speak(TextToSpeechParams speechParams)
```

Generates audio from the response text. The audio bytes are returned in `ChatResult.Message.Speech`.

| Parameter | Type | Description |
|---|---|---|
| `speechParams` | `TextToSpeechParams` | TTS configuration. Contains: TTS model (e.g. `new Kokoro_82m()`), voice (from `VoiceService.GetVoice("af_heart")`), and `autoPlay` (bool — plays audio immediately if `true`). |

---

### `WithFiles`

```csharp
IChatConfigurationBuilder WithFiles(List<string> file, bool preProcess = false)
IChatConfigurationBuilder WithFiles(List<FileStream> file, bool preProcess = false)
IChatConfigurationBuilder WithFiles(List<FileInfo> file, bool preProcess = false)
```

Attaches files to the message. Supported formats: PDF, Markdown, plain text, images (OCR via Tesseract).

| Parameter | Type | Default | Description |
|---|---|---|---|
| `file` | `List<string>` / `List<FileStream>` / `List<FileInfo>` | required | The files to attach. Paths can be absolute or relative to the working directory. |
| `preProcess` | `bool` | `false` | When `true`, files are chunked and embedded into the context window before completion. When `false`, raw file content is passed directly to the model. Use `true` for large documents, `false` for short files where you want exact verbatim content. |

---

### `DisableCache`

```csharp
IChatConfigurationBuilder DisableCache()
```

Bypasses the model token cache for this completion. No parameters. Useful when file content changes between calls and the cached KV state would return stale results.

---

### `CompleteAsync`

```csharp
Task<ChatResult> CompleteAsync(
    bool translate = false,
    bool interactive = false,
    Func<LLMTokenValue?, Task>? changeOfValue = null,
    CancellationToken cancellationToken = default)
```

Executes the completion and returns the model's response.

| Parameter | Type | Default | Description |
|---|---|---|---|
| `translate` | `bool` | `false` | Auto-translates the response text to the host system's locale. |
| `interactive` | `bool` | `false` | Streams each token to `Console.Write` as it arrives. Equivalent to passing a `Console.Write` `changeOfValue` callback. |
| `changeOfValue` | `Func<LLMTokenValue?, Task>?` | `null` | Async callback invoked for every token. `LLMTokenValue` exposes: `Text` (string) and `Type` (`Message` / `Reason` / `ToolCall` / `Special`). Filter on `Type == Message` to skip reasoning tokens. |
| `cancellationToken` | `CancellationToken` | `default` | Cancels the in-flight HTTP request. Already-received tokens are present in the partial `ChatResult` that is returned. |

**Returns** `Task<ChatResult>`.

---

## IChatActions

Available at any point in the builder chain.

| Method | Returns | Description |
|---|---|---|
| `GetChatId()` | `string` | The internal UUID of the current chat. Pass to `FromExisting` in a later session. |
| `GetCurrentChat()` | `Task<Chat>` | Fetches the full `Chat` entity including all messages and configuration. |
| `GetAllChats()` | `Task<List<Chat>>` | Returns all persisted chats from the repository. |
| `DeleteChat()` | `Task` | Permanently deletes the current chat from the repository. |
| `GetChatHistory()` | `List<MessageShort>` | Returns a lightweight summary of all messages (role, truncated content, timestamp). |

---

## ChatResult

```csharp
public class ChatResult
{
    public string Model { get; init; }        // model ID used for this completion
    public DateTime CreatedAt { get; set; }   // UTC timestamp of the response
    public Message Message { get; init; }     // the model's response message
    public bool Done { get; init; }           // true when the completion finished normally
}
```

`Message` properties:

| Property | Type | Description |
|---|---|---|
| `Content` | `string` | The text of the model's response. |
| `Role` | `string` | Always `"assistant"` for completion responses. |
| `Type` | `MessageType` | `LocalLLM`, `CloudLLM`, `Image`, `Speech`, etc. |
| `Images` | `List<byte[]>?` | Image bytes when the model generated an image. |
| `Speech` | `byte[]?` | Audio bytes when `Speak(...)` was used. |
| `Tokens` | `List<LLMTokenValue>` | All streamed tokens; available even without a callback. |

---

## Examples

### Streaming

```csharp
await AIHub.Chat()
    .WithModel(Models.Groq.Llama4Scout17b)
    .WithMessage("Write a short poem about the ocean")
    .CompleteAsync(changeOfValue: async token =>
    {
        if (token?.Type == LLMTokenType.Message)
            Console.Write(token.Text);
    });
```

### Multi-turn

```csharp
var chat = AIHub.Chat().WithModel(Models.Local.Qwen2_5_0_5b);

await chat.WithMessage("What do you think about math theories?").CompleteAsync();
await chat.WithMessage("And about physics?").CompleteAsync();

// Persist and reload
var id = chat.GetChatId();
var restored = await AIHub.Chat().FromExisting(id);
var history = restored.GetChatHistory();
```

### File Attachments

```csharp
var result = await AIHub.Chat()
    .WithModel(Models.Local.Gemma3_4b)
    .WithMessage("What is the difference between their work?")
    .WithFiles(["./Nicolaus_Copernicus.pdf", "./Galileo_Galilei.pdf"])
    .DisableCache()
    .CompleteAsync();
```

### Vision

```csharp
var image = await File.ReadAllBytesAsync("./screenshot.jpg");

await AIHub.Chat()
    .WithModel(Models.Local.Llava16Mistral_7b)
    .WithMessage("What can you see in this image?", image)
    .CompleteAsync(interactive: true);
```

### Structured Output

```csharp
await AIHub.Chat()
    .WithModel(Models.Gemini.Gemini2_5Flash)
    .WithMessage("Generate a random person")
    .WithInferenceParams(new LocalInferenceParams
    {
        Grammar = new Grammar("""
            { "type": "object", "properties": { "name": {"type":"string"}, "age": {"type":"integer"} }, "required": ["name","age"] }
            """, GrammarFormat.JSONSchema)
    })
    .CompleteAsync(interactive: true);
```

### Function Calling

```csharp
await AIHub.Chat()
    .WithModel(Models.OpenAi.Gpt4oMini)
    .WithMessage("What time is it right now?")
    .WithTools(new ToolsConfigurationBuilder()
        .AddTool(
            name: "get_current_time",
            description: "Returns the current date and time",
            execute: () => (object)DateTime.Now.ToString("O"))
        .WithToolChoice("auto")
        .Build())
    .CompleteAsync(interactive: true);
```

### Cancellation

```csharp
var cts = new CancellationTokenSource();

await AIHub.Chat()
    .WithModel(Models.Anthropic.ClaudeSonnet4_6)
    .WithMessage("Write a very long essay...")
    .CompleteAsync(
        changeOfValue: async token => Console.Write(token?.Text),
        cancellationToken: cts.Token);

cts.Cancel(); // from another thread or task
```
