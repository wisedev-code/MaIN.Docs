# Docker — MaIN.NET InferPage

MaIN.NET ships a ready-to-run Docker image called **InferPage** (`ghcr.io/wisedev-code/main-inferpage`). It is a Blazor Server web app that wraps MaIN.NET and exposes a browser-based chat UI on **port 5555**. Zero code required — pull, run, chat.

InferPage is genuinely multiplatform. The same repo produces four image tags targeting different hardware:

| Tag | Base image | Ideal for |
|---|---|---|
| `:cuda` | `nvidia/cuda:12.9.1-runtime-ubuntu24.04` | Windows or Linux with an NVIDIA GPU |
| `:cpu` / `:latest` | `mcr.microsoft.com/dotnet/aspnet:10.0` | Mac (Apple Silicon) or CPU-only Linux |
| `:ollama` | `mcr.microsoft.com/dotnet/aspnet:10.0` | Any OS — delegates to an external Ollama instance |
| `:ollama-bundled` | `mcr.microsoft.com/dotnet/aspnet:10.0` | Any OS — Ollama binary bundled inside the image |

## Key runtime details

- **Port**: `5555` (set via `ASPNETCORE_URLS=http://+:5555`)
- **Models volume**: `/app/Models` — mount a host directory here to persist GGUF models across restarts
- **Data Protection volume**: `/app/DataProtection-Keys`
- **Default backend**: `Self` (local LLamaSharp inference). Override with `MaIN__BackendType` env var.

## Environment variables

| Variable | Default | Purpose |
|---|---|---|
| `MaIN__ModelsPath` | `/app/Models` | Path where GGUF files are stored |
| `MaIN__BackendType` | `0` | Active backend: 0=Self, 1=OpenAi, 2=Gemini, 3=Anthropic, 4=GroqCloud, 7=Ollama |
| `MaIN__OllamaBaseUrl` | `http://host.docker.internal:11434` | Ollama endpoint (`:ollama` tag only) |
| `ASPNETCORE_ENVIRONMENT` | `Production` | ASP.NET Core environment |
| `ASPNETCORE_URLS` | `http://+:5555` | Listening address |
| `NVIDIA_VISIBLE_DEVICES` | `all` | GPU visibility (`:cuda` tag, set automatically) |
| `MaIN__GeminiKey` | — | Gemini API key when using Gemini backend |
| `MaIN__OpenAiKey` | — | OpenAI API key when using OpenAI backend |
| `MaIN__AnthropicKey` | — | Anthropic API key when using Anthropic backend |

## Quick start commands

### Mac (Apple Silicon) or Linux CPU

```bash
docker run -d \
  -p 5555:5555 \
  -v ~/models:/app/Models \
  ghcr.io/wisedev-code/main-inferpage:cpu
```

### Windows / Linux with NVIDIA GPU

Requires the [NVIDIA Container Toolkit](https://docs.nvidia.com/datacenter/cloud-native/container-toolkit/install-guide.html).

```bash
docker run -d \
  --gpus all \
  -p 5555:5555 \
  -v ~/models:/app/Models \
  ghcr.io/wisedev-code/main-inferpage:cuda
```

### External Ollama (Ollama already running on the host)

```bash
docker run -d \
  -p 5555:5555 \
  ghcr.io/wisedev-code/main-inferpage:ollama
```

The `:ollama` image defaults `MaIN__OllamaBaseUrl` to `http://host.docker.internal:11434`. Override this to point at a remote Ollama.

### Bundled Ollama (Ollama inside the container)

```bash
docker run -d \
  -p 5555:5555 \
  -v ollama-data:/root/.ollama \
  ghcr.io/wisedev-code/main-inferpage:ollama-bundled
```

The entrypoint script starts Ollama first, then InferPage. Pull models via `docker exec <id> ollama pull gemma3:4b` or from the UI.

## Docker Compose examples

### CPU / Mac

```yaml
services:
  inferpage:
    image: ghcr.io/wisedev-code/main-inferpage:cpu
    ports:
      - "5555:5555"
    volumes:
      - ./models:/app/Models
    restart: unless-stopped
```

### CUDA (add the deploy block)

```yaml
services:
  inferpage:
    image: ghcr.io/wisedev-code/main-inferpage:cuda
    ports:
      - "5555:5555"
    volumes:
      - ./models:/app/Models
    deploy:
      resources:
        reservations:
          devices:
            - driver: nvidia
              count: all
              capabilities: [gpu]
    restart: unless-stopped
```

### Cloud backend (e.g., Gemini)

```yaml
services:
  inferpage:
    image: ghcr.io/wisedev-code/main-inferpage:cpu
    ports:
      - "5555:5555"
    environment:
      MaIN__BackendType: "2"
      MaIN__GeminiKey: "${GEMINI_KEY}"
    restart: unless-stopped
```

## Choosing the right tag

- **Windows + NVIDIA GPU** → `:cuda`. Provides the best local inference performance.
- **Mac (M1/M2/M3/M4)** → `:cpu`. LLamaSharp uses Apple's Accelerate framework natively.
- **Linux (no GPU)** → `:cpu`. Runs on any VPS or CI box.
- **Already running Ollama** → `:ollama`. Lightest image; no model storage needed.
- **Want everything self-contained** → `:ollama-bundled`. One container, batteries included.

## Backend type values (for `MaIN__BackendType`)

| Value | Backend |
|---|---|
| `0` | Self (local LLamaSharp — default) |
| `1` | OpenAI |
| `2` | Gemini |
| `3` | Anthropic |
| `4` | GroqCloud |
| `5` | DeepSeek |
| `6` | xAI |
| `7` | Ollama |
| `8` | Vertex |

## Accessing the UI

Once the container is running, open `http://localhost:5555` in any browser. On first launch with a fresh models volume, InferPage will prompt you to pick and download a model. Subsequent starts load from the cache immediately.
