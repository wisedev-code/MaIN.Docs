# ModelContext

`ModelContext` manages the model registry, local file downloads, and the in-memory model cache. Access it via `AIHub.Model()`.

## Entry Point

```csharp
var ctx = AIHub.Model();
```

---

## Method Reference

---

### `GetAll`

```csharp
IEnumerable<AIModel> GetAll()
```

Returns all models currently registered in the `ModelRegistry` — both local GGUF models and cloud model descriptors.

---

### `GetAllLocal`

```csharp
IEnumerable<LocalModel> GetAllLocal()
```

Returns only local GGUF model descriptors. Equivalent to filtering `GetAll()` to `LocalModel` instances.

---

### `GetModel`

```csharp
AIModel GetModel(string modelId)
```

Fetches a model descriptor from the registry by its ID.

| Parameter | Type | Description |
|---|---|---|
| `modelId` | `string` | Model identifier. Use `Models.*` constants or a plain string. Throws `ModelNotFoundException` if the ID is not registered. |

**Returns** `AIModel` with properties: `Id` (string), `Name` (string), `Description` (string).

---

### `GetEmbeddingModel`

```csharp
AIModel GetEmbeddingModel()
```

Returns the `Mxbai-Embed-Large` model descriptor, which is the default embedding model used for RAG/knowledge indexing. No parameters.

---

### `Exists`

```csharp
bool Exists(string modelId)
```

Checks whether the model's GGUF file is present on disk. Does **not** check the registry — only the file system.

| Parameter | Type | Description |
|---|---|---|
| `modelId` | `string` | Model ID to look up. The file path is resolved from `MaINSettings.ModelsPath` (or `MaIN_ModelsPath` env var). |

**Returns** `true` if the file exists, `false` otherwise.

---

### `EnsureDownloadedAsync` (by ID)

```csharp
Task<IModelContext> EnsureDownloadedAsync(string modelId, CancellationToken cancellationToken = default)
Task<IModelContext> EnsureDownloadedAsync(string modelId, IProgress<DownloadProgress>? progress, CancellationToken cancellationToken = default)
```

Downloads the model file only if it is not already on disk. Thread-safe: uses a per-model-ID async keyed lock so concurrent calls for the same model do not double-download.

| Parameter | Type | Default | Description |
|---|---|---|---|
| `modelId` | `string` | required | Model to ensure is downloaded. |
| `progress` | `IProgress<DownloadProgress>?` | `null` | Optional progress reporter. `DownloadProgress` exposes `FileName`, `Percentage` (double 0–100), `Speed` (string, e.g. `"4.2 MB/s"`), `Eta` (string). |
| `cancellationToken` | `CancellationToken` | `default` | Cancels the download. A partial file is left on disk and will be resumed on the next call (HTTP range requests). |

**Returns** `IModelContext` (same instance) — chains with further calls.

---

### `EnsureDownloadedAsync` (generic)

```csharp
Task<IModelContext> EnsureDownloadedAsync<TModel>(CancellationToken cancellationToken = default)
    where TModel : LocalModel, new()
```

Generic overload. Creates an instance of `TModel` and resolves its ID automatically — no need to pass a string.

| Type parameter | Constraint | Description |
|---|---|---|
| `TModel` | `LocalModel, new()` | Any concrete `LocalModel` subclass with a default constructor, e.g. `Gemma2_2b`. |

| Parameter | Type | Default | Description |
|---|---|---|---|
| `cancellationToken` | `CancellationToken` | `default` | Cancels the download. |

---

### `DownloadAsync` (by ID)

```csharp
Task<IModelContext> DownloadAsync(string modelId, CancellationToken cancellationToken = default)
Task<IModelContext> DownloadAsync(string modelId, IProgress<DownloadProgress>? progress, CancellationToken cancellationToken = default)
```

Force-downloads the model file regardless of whether it already exists on disk. Use to update a model or recover from a corrupted file.

| Parameter | Type | Default | Description |
|---|---|---|---|
| `modelId` | `string` | required | Model to download. |
| `progress` | `IProgress<DownloadProgress>?` | `null` | Progress reporter (same shape as `EnsureDownloadedAsync`). |
| `cancellationToken` | `CancellationToken` | `default` | Cancels the download. |

**Returns** `IModelContext` (same instance).

---

### `LoadToCache` / `LoadToCacheAsync`

```csharp
IModelContext LoadToCache(LocalModel model)
Task<IModelContext> LoadToCacheAsync(LocalModel model)
```

Loads a model into the in-memory token cache without running a completion. Use during application startup to eliminate the first-inference cold-start delay.

| Parameter | Type | Description |
|---|---|---|
| `model` | `LocalModel` | The model descriptor to load. Retrieve from `GetModel(id)` or instantiate directly (e.g. `new Gemma2_2b()`). |

---

## Custom Model Registration

Use `ModelRegistry` to register models that are not in the built-in catalogue.

### `ModelRegistry.Register`

```csharp
ModelRegistry.Register(LocalModel model)
```

Adds a new model descriptor. Throws if the ID is already registered.

| Parameter | Type | Description |
|---|---|---|
| `model` | `LocalModel` | A `LocalModel` subclass, typically `GenericLocalModel`. |

### `ModelRegistry.RegisterOrReplace`

```csharp
ModelRegistry.RegisterOrReplace(LocalModel model)
```

Adds or overwrites an existing registration. Use when overriding a built-in entry (e.g. specialised image-generation models that share an ID).

**`GenericLocalModel` constructor:**

| Parameter | Type | Description |
|---|---|---|
| `fileName` | `string` | The GGUF file name (e.g. `"Gemma2-2b.gguf"`). Used to resolve the file path under `ModelsPath`. |
| `name` | `string` | Human-readable display name. |
| `id` | `string` | Registry key used in `.WithModel(id)` calls. |
| `SystemMessage` | `string?` | Optional system prompt injected at the start of every chat. |

**Example:**

```csharp
ModelRegistry.Register(new GenericLocalModel(
    fileName: "Gemma2-2b.gguf",
    name: "Writing Assistant",
    id: "writing-assistant",
    SystemMessage: "You are a creative writing assistant."
));

await AIHub.Chat()
    .WithModel("writing-assistant")
    .EnsureModelDownloaded()
    .WithMessage("Write an opening line for a mystery story.")
    .CompleteAsync(interactive: true);
```

---

## Model Constants

Compile-time constants for all supported models, organised by namespace:

| Namespace | Examples |
|---|---|
| `Models.Local` | `Gemma2_2b`, `Gemma3_4b`, `Llama3_2_3b`, `Llama3_1_8b`, `DeepSeekR1_1_5b`, `Qwen2_5_0_5b`, `Flux1Shnell` (image), `Kokoro82m` (TTS) |
| `Models.OpenAi` | `Gpt4o`, `Gpt4oMini`, `Gpt5`, `Gpt5Nano`, `DallE3` (image) |
| `Models.Anthropic` | `ClaudeSonnet4_6`, `ClaudeOpus4_7`, `ClaudeHaiku4_5` |
| `Models.Gemini` | `Gemini2_5Pro`, `Gemini2_5Flash`, `Gemini2_0Flash`, `Imagen4_0` (image) |
| `Models.Groq` | `Llama4Scout17b`, `Llama3_3_70b`, `GptOss20b` |
| `Models.DeepSeek` | `Chat`, `Reasoner` |
| `Models.Xai` | `Grok3Beta`, `Grok4_20Reasoning` |
| `Models.Ollama` | `Gemma3_4b`, `Llama4` |
| `Models.Vertex` | `Gemini2_5Pro`, `Gemini2_5Flash`, `Veo2_0`, `Imagen4_0` |

Models can also be referenced as plain strings (e.g. `"gemma3:4b"`, `"llama3.2:3b"`).

---

## Download Behaviour

| Behaviour | Detail |
|---|---|
| Thread safety | Per-model-ID async keyed lock prevents concurrent duplicate downloads |
| Resume | Interrupted downloads resume via HTTP `Content-Range` requests |
| Retry | Up to 5 attempts with exponential backoff on network errors |
| Stall detection | If no bytes arrive for 30 seconds the attempt is aborted and retried |
| HTTP timeout | 30 minutes per request (accommodates multi-GB model files) |
| Storage path | `MaINSettings.ModelsPath` → falls back to `MaIN_ModelsPath` environment variable |

---

## Examples

```csharp
var ctx = AIHub.Model();

// Check before download
bool exists = ctx.Exists(Models.Local.Gemma2_2b);

// Download only if needed
await ctx.EnsureDownloadedAsync(Models.Local.Gemma2_2b);

// Download with progress
var progress = new Progress<DownloadProgress>(p =>
    Console.WriteLine($"{p.FileName}: {p.Percentage:F1}%  {p.Speed}  ETA {p.Eta}"));
await ctx.EnsureDownloadedAsync(Models.Local.Llama3_2_3b, progress);

// Generic overload — no string ID needed
await ctx.EnsureDownloadedAsync<Gemma2_2b>();

// Force re-download
await ctx.DownloadAsync(Models.Local.Gemma3_4b);

// Registry lookup
var model = ctx.GetModel(Models.Local.Gemma3_4b);
Console.WriteLine($"{model.Name} — {model.Id}");
```
