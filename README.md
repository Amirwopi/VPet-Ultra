# VPet-Ultra: Offline-First AI Desktop Companion

Transforms the [VPet](https://github.com/LorisYounger/VPet) desktop pet into an autonomous AI companion with LLM conversation, memory, personality, emotion, voice, and agent capabilities — all running offline.

## Features

- **LLM Conversation** — Ollama (default, offline) or any OpenAI-compatible endpoint (Qwen, GLM, DeepSeek, OpenAI). Streaming text with tool-call support.
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
2. **Ollama** — [install](https://ollama.com) then pull the default model:
   ```bash
   ollama pull qwen2.5:3b
   ```
3. **VPet** — build the original VPet solution on Windows.

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
```

### Configure

Create `appsettings.json` in the VPet output directory:

```json
{
  "Cognition": {
    "Enabled": true,
    "Llm": {
      "Provider": "ollama",
      "Endpoint": "http://localhost:11434",
      "Model": "qwen2.5:3b"
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

### Switch LLM Provider

Change one field to switch from Ollama to any OpenAI-compatible endpoint:

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
VPet.Cognition.Domain        — abstractions, entities, value objects, events, errors, enums (net8.0)
VPet.Cognition.LLM           — Ollama + OpenAI-compatible providers, PromptAssembler, StreamAdapter (net8.0)
VPet.Cognition.Memory        — SQLite memory store + MemoryService (net8.0)
VPet.Cognition.Personality    — Personality + Emotion engines (net8.0)
VPet.Cognition.Voice          — Whisper STT + Piper/Kokoro TTS (net8.0)
VPet.Cognition.Agent          — Agent runtime + tools + permission service (net8.0)
VPet.Cognition.Security       — Secret redaction, path/command validation (net8.0)
VPet.Cognition.Host           — DI composition, event bus, MainPlugin entry (net8.0-windows)
VPet.Cognition.Wpf            — Conversation window, settings window (net8.0-windows)
VPet.Cognition.Tests          — 102 tests, all passing (net8.0)
```

The cognition system loads as a `MainPlugin` via the existing VPet `CoreMOD` plugin loader — zero changes to the original 5 projects.

## Development

### Run Tests

```bash
dotnet test VPet.Cognition.Tests/VPet.Cognition.Tests.csproj -c Release
# 102 tests, all passing
```

### Project Conventions

- `TreatWarningsAsErrors` on all projects
- `Nullable` enabled everywhere
- `GenerateDocumentationFile` on all projects
- All async methods accept `CancellationToken`
- All providers implement `IAsyncDisposable`
- No secrets in logs (use `SecretRedactor`)

### Acceptance Criteria

- [x] All 102 tests pass in Release
- [x] 0 warnings, 0 errors on all buildable projects
- [x] No existing VPet API modified
- [x] LLM provider switchable via config (Ollama ↔ OpenAI-compatible)
- [x] Model switchable via config (qwen2.5:3b → any model)
- [x] Memory persists to SQLite
- [x] Personality evolves through conversation
- [x] Emotion decays over time
- [x] Voice providers are offline (whisper.cpp, piper, kokoro)
- [x] Agent tools gated by permission system
- [x] Secrets redacted in logs and config dumps
- [x] Path traversal prevented in file tools
- [x] Dangerous commands blocked

## Documentation

- `docs/` — 21-file enterprise engineering specification
- `docs/adr/` — Architecture Decision Records (0001–0003)
- `Changelog.md` — Version history

## License

Same as the original VPet project.

## Credits

- [VPet](https://github.com/LorisYounger/VPet) by LorisYounger
- [Ollama](https://ollama.com)
- [whisper.cpp](https://github.com/ggerganov/whisper.cpp)
- [Piper](https://github.com/rhasspy/piper)
- [Kokoro](https://github.com/kokoro-ons/kokoro)