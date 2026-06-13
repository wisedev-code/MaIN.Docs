# Docker — MaIN.NET InferPage

MaIN.NET ships a ready-to-run Docker image called **InferPage** — a self-hosted web UI that lets you chat with any MaIN.NET-supported model without writing a line of code. Pull it, point it at a GPU (or don't), and you have a private AI inference endpoint in under a minute.

## What InferPage Is

InferPage is a Blazor Server app that wires up MaIN.NET under the hood. It exposes a clean browser-based chat UI on **port 5555** and supports every backend the framework does — local GGUF models, Ollama, and all cloud providers.

It is **genuinely multiplatform**:

| Platform | Tag | What it uses |
|---|---|---|
| Windows / Linux (NVIDIA GPU) | `:cuda` | CUDA 12.9 runtime, full GPU acceleration |
| Mac (Apple Silicon) | `:cpu` / `:latest` | CPU inference via LLamaSharp |
| Linux (CPU-only) | `:cpu` / `:latest` | CPU inference, no GPU required |
| Any OS (external Ollama) | `:ollama` | Delegates to your running Ollama instance |
| Any OS (Ollama bundled) | `:ollama-bundled` | Ships Ollama inside the image |

## Quick Start

### Mac (Apple Silicon) or Linux CPU

```bash
docker run -d \
  -p 5555:5555 \
  -v ~/models:/app/Models \
  ghcr.io/wisedev-code/main-inferpage:cpu
```

Open `http://localhost:5555` — done.

### Windows / Linux with NVIDIA GPU

```bash
docker run -d \
  --gpus all \
  -p 5555:5555 \
  -v ~/models:/app/Models \
  ghcr.io/wisedev-code/main-inferpage:cuda
```

The `:cuda` image is based on `nvidia/cuda:12.9.1-runtime-ubuntu24.04` and automatically enables all visible GPUs. Make sure the [NVIDIA Container Toolkit](https://docs.nvidia.com/datacenter/cloud-native/container-toolkit/install-guide.html) is installed.

### External Ollama

If you already have Ollama running locally, skip the model volume entirely:

```bash
docker run -d \
  -p 5555:5555 \
  -e MaIN__OllamaBaseUrl=http://host.docker.internal:11434 \
  ghcr.io/wisedev-code/main-inferpage:ollama
```

### Bundled Ollama (zero dependencies)

```bash
docker run -d \
  -p 5555:5555 \
  -v ollama-data:/root/.ollama \
  ghcr.io/wisedev-code/main-inferpage:ollama-bundled
```

Ollama starts automatically inside the container. Pull models through the UI or via `docker exec`.

## Docker Compose

```yaml
services:
  inferpage:
    image: ghcr.io/wisedev-code/main-inferpage:cpu
    ports:
      - "5555:5555"
    volumes:
      - ./models:/app/Models
    environment:
      ASPNETCORE_ENVIRONMENT: Production
    restart: unless-stopped
```

For the CUDA variant, add:

```yaml
    deploy:
      resources:
        reservations:
          devices:
            - driver: nvidia
              count: all
              capabilities: [gpu]
```

## Environment Variables

| Variable | Default | Purpose |
|---|---|---|
| `MaIN__ModelsPath` | `/app/Models` | Where GGUF model files are stored |
| `MaIN__BackendType` | `0` (Self) | Backend override (7 = Ollama) |
| `MaIN__OllamaBaseUrl` | — | Ollama endpoint for `:ollama` tag |
| `ASPNETCORE_URLS` | `http://+:5555` | Listening address |

## Models Volume

All local (GGUF) models are loaded from `/app/Models`. Mount a host directory there to persist models across container restarts:

```bash
-v /your/models/directory:/app/Models
```

First run with a fresh volume will prompt you to download a model from the UI. Subsequent runs load instantly from the cache.

## Cloud Backends

InferPage isn't limited to local models. Pass cloud keys as environment variables to use any provider:

```bash
docker run -d \
  -p 5555:5555 \
  -e MaIN__BackendType=2 \
  -e MaIN__GeminiKey=your-key \
  ghcr.io/wisedev-code/main-inferpage:cpu
```

Backend type values: `0` = Self (local), `1` = OpenAI, `2` = Gemini, `3` = Anthropic, `4` = GroqCloud, `7` = Ollama.
