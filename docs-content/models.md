# Models Reference

All supported models are available as compile-time constants in the `Models` namespace. They can also be passed as plain strings (e.g. `.WithModel("gemma3:4b")`).

## Local (Self backend — GGUF via LLamaSharp)

`BackendType.Self` runs GGUF files locally using LLamaSharp, which is **built into MaIN.NET** — no extra NuGet package needed. Set `ModelsPath` to the directory containing your `.gguf` files.

| Constant | Description |
|---|---|
| `Models.Local.Gemma2_2b` | Google Gemma 2 2B — fast, small, general purpose |
| `Models.Local.Gemma3_4b` | Google Gemma 3 4B — stronger reasoning, vision capable |
| `Models.Local.Llama3_2_3b` | Meta Llama 3.2 3B — compact instruction model |
| `Models.Local.Llama3_1_8b` | Meta Llama 3.1 8B — stronger reasoning, larger context |
| `Models.Local.DeepSeekR1_1_5b` | DeepSeek-R1 1.5B — reasoning/CoT model |
| `Models.Local.Qwen2_5_0_5b` | Qwen 2.5 0.5B — ultra-small, edge deployment |
| `Models.Local.Llava16Mistral_7b` | LLaVA 1.6 Mistral 7B — vision model (image input) |
| `Models.Local.Flux1Shnell` | FLUX.1 Schnell — local image generation |
| `Models.Local.Kokoro82m` | Kokoro 82M — local text-to-speech |

**Adding a custom GGUF model** (not in the built-in catalogue):

```csharp
ModelRegistry.Register(new GenericLocalModel(
    fileName: "Qwen2.5-7B-Instruct-Q4_K_M.gguf",
    name: "Qwen 2.5 7B",
    id: "qwen2.5-7b",
    SystemMessage: null
));

await AIHub.Chat()
    .WithModel("qwen2.5-7b")
    .EnsureModelDownloaded()
    .WithMessage("Hello!")
    .CompleteAsync(interactive: true);
```

**Key facts about Self backend:**
- LLamaSharp is already a dependency of `MaIN.NET` — do NOT add it separately
- Models are downloaded to `ModelsPath` (config key) or the `MaIN_ModelsPath` env variable
- `EnsureModelDownloaded()` auto-downloads before first use; `EnsureDownloadedAsync` supports progress reporting
- `LocalInferenceParams` controls temperature, grammar, top-p, max tokens for local models
- Context size is set via `WithMemoryParams(new MemoryParams { ContextSize = 4096 })`

---

## OpenAI

`BackendType.OpenAi` — set `OpenAiKey` in config.

| Constant | Model |
|---|---|
| `Models.OpenAi.Gpt4o` | GPT-4o |
| `Models.OpenAi.Gpt4oMini` | GPT-4o Mini |
| `Models.OpenAi.Gpt5` | GPT-5 |
| `Models.OpenAi.Gpt5Nano` | GPT-5 Nano |
| `Models.OpenAi.DallE3` | DALL·E 3 (image generation) |

---

## Anthropic

`BackendType.Anthropic` — set `AnthropicKey` in config.

| Constant | Model |
|---|---|
| `Models.Anthropic.ClaudeSonnet4_6` | Claude Sonnet 4.6 |
| `Models.Anthropic.ClaudeOpus4_7` | Claude Opus 4.7 |
| `Models.Anthropic.ClaudeHaiku4_5` | Claude Haiku 4.5 |

---

## Gemini (Google AI)

`BackendType.Gemini` — set `GeminiKey` in config.

| Constant | Model |
|---|---|
| `Models.Gemini.Gemini2_5Pro` | Gemini 2.5 Pro |
| `Models.Gemini.Gemini2_5Flash` | Gemini 2.5 Flash |
| `Models.Gemini.Gemini2_0Flash` | Gemini 2.0 Flash |
| `Models.Gemini.Imagen4_0` | Imagen 4.0 (image generation) |

---

## GroqCloud

`BackendType.GroqCloud` — set `GroqCloudKey` in config.

| Constant | Model |
|---|---|
| `Models.Groq.Llama4Scout17b` | Llama 4 Scout 17B |
| `Models.Groq.Llama3_3_70b` | Llama 3.3 70B |
| `Models.Groq.GptOss20b` | GPT OSS 20B |

---

## DeepSeek

`BackendType.DeepSeek` — set `DeepSeekKey` in config.

| Constant | Model |
|---|---|
| `Models.DeepSeek.Chat` | DeepSeek Chat |
| `Models.DeepSeek.Reasoner` | DeepSeek Reasoner (R1) |

---

## xAI

`BackendType.Xai` — set `XaiKey` in config.

| Constant | Model |
|---|---|
| `Models.Xai.Grok3Beta` | Grok 3 Beta |
| `Models.Xai.Grok4_20Reasoning` | Grok 4 (20-step reasoning) |

---

## Ollama

`BackendType.Ollama` — set `OllamaKey` to the Ollama server URL (e.g. `http://localhost:11434`). Use Ollama Cloud for hosted inference.

| Constant | Model |
|---|---|
| `Models.Ollama.Gemma3_4b` | Gemma 3 4B via Ollama |
| `Models.Ollama.Llama4` | Llama 4 via Ollama |

Plain strings work too: `.WithModel("qwen2.5:7b")` uses whatever tag is pulled in Ollama.

---

## Vertex AI (Google Cloud)

`BackendType.Vertex` — requires service account credentials configured in the environment.

| Constant | Model |
|---|---|
| `Models.Vertex.Gemini2_5Pro` | Gemini 2.5 Pro via Vertex |
| `Models.Vertex.Gemini2_5Flash` | Gemini 2.5 Flash via Vertex |
| `Models.Vertex.Veo2_0` | Veo 2.0 (video generation) |
| `Models.Vertex.Imagen4_0` | Imagen 4.0 (image generation) |

---

## Vision-capable models

Models that accept image input via `.WithMessage(text, imageBytes)` or `Message.Images`:

- `Models.Local.Gemma3_4b`
- `Models.Local.Llava16Mistral_7b`
- `Models.OpenAi.Gpt4o`, `Models.OpenAi.Gpt5`
- `Models.Anthropic.*` (all)
- `Models.Gemini.*` (all)
- `Models.Xai.Grok3Beta`, `Models.Xai.Grok4_20Reasoning`

---

## Inference params by backend

| Backend | Params class | Key fields |
|---|---|---|
| `Self` | `LocalInferenceParams` | `Temperature`, `TopP`, `MaxTokens`, `Grammar` |
| `OpenAi` | `OpenAiInferenceParams` | `ResponseFormat`, `AdditionalParams` |
| `Vertex` | `VertexInferenceParams` | `Location` (GCP region) |
| Others | none required | pass nothing, provider defaults apply |
