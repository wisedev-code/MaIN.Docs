# Introduction

**MaIN.NET** is a modular AI framework for .NET that makes it straightforward to integrate large language models into your applications.

## What is MaIN.NET?

MaIN.NET provides a unified API for working with multiple LLM backends — cloud providers like OpenAI, Anthropic, Google Gemini, GroqCloud, xAI, and DeepSeek, as well as local GGUF models via llama.cpp.

The core principle is **backend-agnostic code**: write your AI logic once, swap the underlying model with a single configuration change.

## Key Features

- **Multi-backend support** — OpenAI, Anthropic, Gemini, GroqCloud, Ollama, local GGUF
- **Fluent API** — readable, composable builder pattern via `AIHub`
- **Streaming** — real-time token streaming with callbacks
- **Agents** — multi-step reasoning with tool use and skill composition
- **Flows** — orchestrate multiple agents with conditional branching
- **InferPage** — ready-made Blazor Server web UI

## Architecture Overview

```
AIHub (static facade)
  ├── ChatContext    → single-turn or multi-turn completions
  ├── AgentContext   → tool-using agents with skills
  ├── FlowContext    → multi-agent orchestration
  ├── ModelContext   → model management and downloads
  └── McpContext     → Model Context Protocol integration
```

## Who is it for?

MaIN.NET is designed for .NET developers who want to:

- Add AI chat to existing ASP.NET Core applications
- Build AI-powered agents and automation workflows
- Run local LLMs without cloud costs or privacy concerns
- Quickly prototype AI features with minimal boilerplate
