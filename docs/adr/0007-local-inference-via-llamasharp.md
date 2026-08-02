# ADR 0007: Local Inference via LLamaSharp as Default

**Date:** 2026-08-02
**Status:** Accepted
**Supersedes:** ADR 0003 (partially — Ollama is now an alternative, not the default)

## Context

ADR 0003 established Ollama as the default LLM provider. While Ollama is excellent for
development and server use, it requires:

1. A separate installation (`ollama` binary + daemon).
2. A running HTTP server on `localhost:11434`.
3. Users to manage models via the `ollama` CLI.
4. Inter-process HTTP communication for every token.

For a desktop pet application targeting non-technical users, these requirements create
friction. The user should be able to download a model file, drop it in a folder, and
start chatting — without installing or configuring a separate server.

## Decision

Replace Ollama as the default with **LLamaSharp** (a .NET binding for llama.cpp),
running GGUF models in-process. Ollama and OpenAI-compatible endpoints remain
available as alternatives via the `Provider` config field.

### Architecture

Introduce `IInferenceProvider` as the abstraction layer between the cognition system
and the LLM backend:

- `LocalInferenceProvider` — LLamaSharp, GGUF in-process, CPU + GPU
- `OnlineInferenceProvider` — Ollama or OpenAI-compatible HTTP API

The cognition layer (Conversation, Memory, Personality, Agent) depends only on
`IInferenceProvider` and `InferenceCapabilities` flags — it never knows whether the
model runs locally or remotely.

### Model Management

Add `ModelManager` and `ModelRegistry`:
- `ModelRegistry` — 9 curated official models (text, vision, embedding).
- `ModelManager` — search, download from HuggingFace, install, update, delete.
- Models stored as `.gguf` files in a configurable `ModelsDirectory`.

### Vision

Add `VisionModule` — optional, toggleable at runtime via `EnableVision` config flag.
Disabled by default to save RAM. When enabled, loads a multimodal model (LLaVA or
SmolVLM) for image understanding.

### Configuration

```json
{
  "Cognition": {
    "Llm": {
      "Provider": "local",
      "Model": "qwen2.5-3b-instruct-q4_k_m",
      "ModelsDirectory": "models",
      "GpuLayerCount": 0,
      "ContextSize": 4096,
      "EnableVision": false
    }
  }
}
```

## Consequences

**Positive:**
- Zero external dependencies for the default mode — just download a model file.
- Lower latency (no HTTP overhead per token).
- Simpler user experience for non-technical users.
- GPU acceleration via CUDA backend (`LLamaSharp.Backend.Cuda12`).
- Model management integrated into the app (no separate CLI).

**Negative:**
- Larger app binary (LLamaSharp native libraries ~50-100MB).
- RAM usage tied to model size (2-7GB depending on model).
- GPU support requires installing the CUDA backend package separately.
- Model download is a one-time cost (2-5GB per model).

**Neutral:**
- Ollama remains fully supported as an alternative (`Provider: "ollama"`).
- OpenAI-compatible endpoints remain supported (`Provider: "openai-compatible"`).

## Alternatives Considered

1. **Keep Ollama as default** — rejected: too much friction for end users.
2. **ONNX Runtime** — rejected: smaller model ecosystem for instruction-tuned LLMs;
   LLamaSharp/llama.cpp has better GGUF support and wider model availability.
3. **ML.NET** — rejected: limited LLM model support, no GGUF, no llama.cpp ecosystem.
4. **Custom llama.cpp P/Invoke** — rejected: LLamaSharp already does this well and
   is actively maintained.