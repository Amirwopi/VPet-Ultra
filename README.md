# VPet-Ultra: Offline-First AI Desktop Companion

Transforms the [VPet](https://github.com/LorisYounger/VPet) desktop pet into an autonomous AI companion with in-process LLM inference, memory, personality, emotion, voice, and agent capabilities — all running offline, no external server required.

## Features

- **In-Process LLM Inference** — GGUF models run directly inside the app via [LLamaSharp](https://github.com/SciSharp/LLamaSharp) (llama.cpp). No Ollama, no Python, no server. CPU and GPU supported.
- **Model Manager** — Browse, download, install, update, and delete GGUF models from HuggingFace. 9 curated models pre-registered (Qwen2.5, Llama-3.2, SmolVLM, LLaVA, BGE embeddings).
- **Vision Module** — Optional multimodal vision support (LLaVA/SmolVLM). Toggleable at runtime, disabled by default to save RAM.
- **Online Mode** — Optionally connect to Ollama or any OpenAI-compatible endpoint (Qwen, GLM, DeepSeek, OpenAI) instead of local inference.
- **Memory** — SQLite-backed episodic + semantic memory with embedding retrieval, importance decay, and consolidation.
- **Personality** — Big Five core traits + adaptive traits that evolve through conversation. Relationship state (trust, familiarity, affection).
- **Emotion** — 10-dimensional emotion vector with stimulus blending, antagonistic suppression, and decay. Maps to animation, voice prosody, and decision bias.
- **Voice** — Whisper (whisper.cpp) for speech-to-text, Piper or Kokoro for text-to-speech. All offline.
- **Agent** — Goal-driven autonomous runtime with plan execution, tool dispatch, and permission gating.
- **Tools** — Open application, read file, open browser, clipboard. Path validation and command blocklist.
- **Security** — Secret redaction in logs, path traversal prevention, dangerous command detection.

## Quick Start

### Prerequisites

1. **.NET 8 SDK** — [download](https://dotnet.microsoft.com/download/dotnet/8.0)
2. **VPet** — build the original VPet solution on Windows.
3. **A GGUF model** — download one of the curated models (see `docs/MODELS.md`):
   ```bash
   # Example: Qwen2.5-3B (recommended, ~2GB, runs on CPU)
   # Place in the models/ folder next to VPet.exe
   ```

### Build

```bash
git clone https://github.com/Amirwopi/VPet-Ultra.git
cd VPet-Ultra
dotnet restore
dotnet build
```

### Test

```bash
dotnet test VPet.Cognition.Tests/VPet.Cognition.Tests.csproj -c Release
# 129 tests, all passing
```

### Configure

Create `appsettings.json` in the VPet output directory. The default is local inference (LLamaSharp):

```json
{
  "Cognition": {
    "Enabled": true,
    "Llm": {
      "Provider": "local",
      "Model": "qwen2.5-3b-instruct-q4_k_m",
      "ModelsDirectory": "models",
      "GpuLayerCount": 0,
      "ContextSize": 4096,
      "EnableVision": false,
      "MaxTokens": 1024,
      "Temperature": 0.7
    },
    "Memory": {
      "DbPath": "data/cognition.db"
    },
    "Voice": {
      "Enabled": false,
      "Stt": {
        "Provider": "whisper",
        "ExecutablePath": "whisper-cli",
        "ModelPath": "models/ggml-base.en.bin",
        "Language": "auto"
      },
      "Tts": {
        "Provider": "piper",
        "ExecutablePath": "piper",
        "ModelPath": "models/en_US-amy-medium.onnx"
      }
    }
  }
}
```

### Switch to Online Mode (Ollama)

Change `Provider` to `ollama` and set the endpoint:

```json
{
  "Cognition": {
    "Llm": {
      "Provider": "ollama",
      "Endpoint": "http://localhost:11434",
      "Model": "qwen2.5:3b"
    }
  }
}
```

### Switch to Online Mode (OpenAI-compatible)

```json
{
  "Cognition": {
    "Llm": {
      "Provider": "openai-compatible",
      "Endpoint": "https://api.deepseek.com/v1/",
      "Model": "deepseek-chat",
      "ApiKey": "your-key-here"
    }
  }
}
```

### GPU Acceleration (Optional)

For NVIDIA GPU acceleration, install the CUDA backend package:

```bash
dotnet add VPet.Cognition.Inference package LLamaSharp.Backend.Cuda12
```

Set `GpuLayerCount` to the number of layers to offload to GPU (e.g., 20-35 depending on VRAM):

```json
{
  "Cognition": {
    "Llm": {
      "GpuLayerCount": 25
    }
  }
}
```

### Voice Setup (Optional)

**Whisper (STT):**
```bash
# Build whisper.cpp
git clone https://github.com/ggerganov/whisper.cpp
cd whisper.cpp && cmake -B build && cmake --build build -j
# Download a model
./models/download-ggml-model.sh base.en
```

**Piper (TTS):**
```bash
# Download from https://github.com/rhasspy/piper/releases
# Get a voice model: en_US-amy-medium.onnx
```

**Kokoro (TTS):**
```bash
# Download from https://github.com/kokoro-ons/kokoro
# Get a model: kokoro.onnx
```

## Architecture

```
VPet.Cognition.Domain         — abstractions, entities, value objects, events, errors, enums (net8.0)
VPet.Cognition.Inference      — IInferenceProvider, LocalInferenceProvider (LLamaSharp), OnlineInferenceProvider, ModelManager, ModelRegistry, VisionModule (net8.0)
VPet.Cognition.LLM            — Ollama + OpenAI-compatible providers, PromptAssembler, StreamAdapter (net8.0)
VPet.Cognition.Memory         — SQLite memory store + MemoryService (net8.0)
VPet.Cognition.Personality     — Personality + Emotion engines (net8.0)
VPet.Cognition.Voice          — Whisper STT + Piper/Kokoro TTS (net8.0)
VPet.Cognition.Agent          — Agent runtime + tools + permission service (net8.0)
VPet.Cognition.Security       — Secret redaction, path/command validation (net8.0)
VPet.Cognition.Host           — DI composition, event bus, MainPlugin entry (net8.0-windows)
VPet.Cognition.Wpf            — Conversation window, settings window (net8.0-windows)
VPet.Cognition.Tests          — 129 tests, all passing (net8.0)
```

The cognition system loads as a `MainPlugin` via the existing VPet `CoreMOD` plugin loader — zero changes to the original 5 projects.

### Inference Engine Design

The `IInferenceProvider` abstraction decouples the cognition layer from the LLM backend:

```
┌─────────────────────────────────────────────────┐
│              Cognition Layer                     │
│  (Conversation, Memory, Personality, Agent)      │
└──────────────────┬──────────────────────────────┘
                   │ IInferenceProvider
          ┌────────┴────────┐
          ▼                 ▼
┌─────────────────┐ ┌──────────────────┐
│ LocalInference   │ │ OnlineInference   │
│ (LLamaSharp)     │ │ (Ollama/OpenAI)   │
│ GGUF in-process  │ │ HTTP API          │
│ CPU + GPU        │ │ Remote server     │
└─────────────────┘ └──────────────────┘
```

**Capabilities flags** (`InferenceCapabilities`): Text, Streaming, Vision, Embedding, ToolCalling, Json. Each provider declares what it supports; the cognition layer adapts accordingly.

## Development

### Run Tests

```bash
dotnet test VPet.Cognition.Tests/VPet.Cognition.Tests.csproj -c Release
# 129 tests, all passing
```

### Project Conventions

- `TreatWarningsAsErrors` on all projects
- `Nullable` enabled everywhere
- `GenerateDocumentationFile` on all projects
- All async methods accept `CancellationToken`
- All providers implement `IAsyncDisposable`
- No secrets in logs (use `SecretRedactor`)

### Acceptance Criteria

- [x] All 129 tests pass in Release
- [x] 0 warnings, 0 errors on all buildable projects
- [x] No existing VPet API modified
- [x] LLM provider switchable via config (local ↔ ollama ↔ openai-compatible)
- [x] GGUF models run in-process (no external server)
- [x] Model manager downloads from HuggingFace
- [x] Vision module toggleable at runtime
- [x] Memory persists to SQLite
- [x] Personality evolves through conversation
- [x] Emotion decays over time
- [x] Voice providers are offline (whisper.cpp, piper, kokoro)
- [x] Agent tools gated by permission system
- [x] Secrets redacted in logs and config dumps
- [x] Path traversal prevented in file tools
- [x] Dangerous commands blocked
- [x] CI builds and packages on Windows (GitHub Actions)

## Documentation

- `docs/` — 21-file enterprise engineering specification
- `docs/adr/` — Architecture Decision Records (0001–0006)
- `docs/MODELS.md` — Model download guide (Persian)
- `INSTALL.md` — Zero-to-complete installation guide (Persian)
- `TESTING.md` — Step-by-step testing guide (Persian)
- `Changelog.md` — Version history

## License

Same as the original VPet project.

## Credits

- [VPet](https://github.com/LorisYounger/VPet) by LorisYounger
- [LLamaSharp](https://github.com/SciSharp/LLamaSharp) — .NET binding for llama.cpp
- [llama.cpp](https://github.com/ggerganov/llama.cpp) — GGUF inference
- [Ollama](https://ollama.com) — online mode
- [whisper.cpp](https://github.com/ggerganov/whisper.cpp) — speech-to-text
- [Piper](https://github.com/rhasspy/piper) — text-to-speech
- [Kokoro](https://github.com/kokoro-ons/kokoro) — text-to-speech