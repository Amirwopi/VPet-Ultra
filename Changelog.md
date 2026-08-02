# Changelog

All notable changes to the VPet AI transformation are documented here.
Format based on [Keep a Changelog](https://keepachangelog.com/).

## [Unreleased]

### Added — Enterprise Engineering Specification
- Complete 21-file documentation project in `docs/` covering the full AI transformation:
  vision, current/target architecture, AI pipeline, memory, personality, emotion, voice,
  tools, agent, plugin system, animation, UI/UX, configuration, security, testing,
  development rules, roadmap, future features, and appendix.
- Master document `docs/task.md` with dependency graph, development order, and acceptance rules.
- ADR process established (`docs/adr/`).

### Added — Phase 0: Foundation
- `VPet.Cognition.Domain` (net8.0): all domain abstractions (`ILLMProvider`, `IMemoryStore`,
  `IPersonalityEngine`, `IEmotionEngine`, `ISttProvider`, `ITtsProvider`, `IWakeWordProvider`,
  `IToolProvider`, `IPermissionService`, `IAgentRuntime`, `IEventBus`), entities (`Memory`,
  `Personality`, `Emotion`, `Goal`, `Conversation`, `ToolInvocation`), value objects (`MemoryId`,
  `GoalId`, `Importance`, `Embedding`, `EmotionVector`), events (memory/personality/emotion/
  agent/voice/tool), errors (`Result<T>`, `ProviderError`, `MemoryError`, `PermissionError`),
  and enums (11 memory types, 10 emotions, 8 tool classes, etc.).
- `VPet.Cognition.Tests` (net8.0): fakes for every interface (`FakeLlmProvider`,
  `FakeMemoryStore`, `FakeEmbeddingProvider`, `FakePersonalityEngine`, `FakeEmotionEngine`,
  `FakeWakeWordProvider`, `FakeSttProvider`, `FakeTtsProvider`, `FakeToolProvider`,
  `FakePermissionService`, `FakeAgentRuntime`, `FakeEventBus`) + 19 smoke tests (all passing).
- `VPet.Cognition.Host` (net8.0-windows): DI composition root (`CognitionHostBuilder`),
  typed event bus (`EventBus`), `CognitionOptions` (all sub-options), and `CognitionPlugin :
  MainPlugin` no-op entry point (loads into existing app via `CoreMOD` loader).
- ADR 0001 (record architecture decisions) and ADR 0002 (additive cognition projects via MainPlugin).
- Projects added to `VPet.sln` (Domain + Tests build and pass on Linux; Host requires Windows).

### Added — Phase 1: Conversation (LLM + text)
- `VPet.Cognition.LLM` (net8.0): `OllamaLlmProvider` (native `/api/chat` streaming, default
  offline-first), `OpenAICompatibleLlmProvider` (`/v1/chat/completions` SSE streaming, covers
  Qwen/GLM/DeepSeek/Llama/OpenAI), `PromptAssembler` (token budget, memory/personality/emotion
  injection, history truncation), `StreamAdapter` (token-to-callback, tool-call collection),
  `ConversationService` (orchestrates a full turn: assemble → stream → collect tools),
  `ToolCallProtocol` (defensive arg parsing), `XmlToolCallParser` (fallback for non-native
  tool models).
- `VPet.Cognition.Wpf` (net8.0-windows): `ConversationWindow` + `ConversationVM`
  (CommunityToolkit.Mvvm, streaming typewriter, cancel, history), `CognitionSettingsWindow`
  + `CognitionSettingsVM` (provider/endpoint/model, test connection), `CognitionWpfModule`
  (window factory), converters.
- `CognitionPlugin` updated: `LoadDIY` adds a "Chat" toolbar button via existing
  `ToolBar.AddMenuButton`; `Setting` opens `CognitionSettingsWindow`; DI wires LLM provider
  by config (`ollama` → `OllamaLlmProvider`, `openai-compatible` → `OpenAICompatibleLlmProvider`).
- ADR 0003 (Ollama as default LLM provider, Qwen2.5-3B default model).
- 13 new LLM tests with WireMock.Net (Ollama + OpenAI-compatible: chat, streaming, tool calls,
  errors, availability, prompt assembly, stream adapter, conversation service). Total: 32 tests,
  all passing.

### Notes
- The existing 5 projects (`VPet-Simulator.Core`, `VPet-Simulator.Windows`,
  `VPet-Simulator.Windows.Interface`, `VPet.Solution`, `VPet-Simulator.Tool`) are unchanged.
- `VPet.Cognition.Host` and `VPet.Cognition.Wpf` cannot build on Linux (WPF targets are
  Windows-only); they build on Windows CI. `VPet.Cognition.Domain`, `VPet.Cognition.LLM`, and
  `VPet.Cognition.Tests` build and test on Linux.
- .NET 8 SDK is required (`dotnet-sdk-8.0`).
- Default LLM: Ollama with `qwen2.5:3b`. Switch to any OpenAI-compatible endpoint via config.

### Added — Phase 2: Memory
- `VPet.Cognition.Memory` (net8.0, RootNamespace `VPet.Cognition.MemoryStore`):
  `SqliteMemoryStore` (full `IMemoryStore` implementation: store, get, retrieve, search,
  consolidate, forget, compress, count, delete, deleteAll, export), `MemoryService`
  (remember conversation/fact/preference, retrieve context for LLM prompt, consolidate,
  compress, export). SQLite + Dapper. Embedding-based cosine-similarity retrieval.
- 16 memory tests with in-memory SQLite — all passing.

### Added — Phase 3: Personality + Emotion
- `VPet.Cognition.Personality` (net8.0, RootNamespace `VPet.Cognition.PersonalityEngine`):
  `DefaultPersonalityEngine` (Big Five core traits, 7 adaptive traits, relationship state
  with trust/familiarity/affection, preference tracking, habit/interest lists, humor style,
  communication style, system prompt rendering), `DefaultEmotionEngine` (10-dimensional
  `EmotionVector`, stimulus blending, antagonistic suppression joy↔sadness/calm↔anger/
  excitement↔fear, time-based decay, animation hint mapping to VPet `GraphType`/`ModeType`,
  voice prosody mapping, decision bias mapping).
- 11 personality/emotion tests — all passing.

### Added — Phase 4: Voice
- `VPet.Cognition.Voice` (net8.0): `WhisperSttProvider` (whisper.cpp CLI wrapper, offline STT,
  JSON output parsing, streaming interface), `PiperTtsProvider` (piper CLI wrapper, offline TTS,
  PCM chunk streaming from WAV, prosody-adjusted speed), `KokoroTtsProvider` (kokoro CLI wrapper,
  offline TTS, PCM chunk streaming, voice selection).
- 11 voice tests — all passing.

### Added — Phase 5: Agent + Tools
- `VPet.Cognition.Agent` (net8.0, RootNamespace `VPet.Cognition.AgentRuntime`):
  `DefaultAgentRuntime` (goal management, plan execution, tool dispatch through permission
  system, retry with max retries, confidence scoring, event emission), `DefaultPermissionService`
  (in-memory grants, auto-grant for non-confirming tools, prompt for dangerous tools, revoke).
- Built-in tools: `OpenApplicationTool`, `FileReadTool` (with path allowlist), `BrowserTool`
  (URL scheme validation), `ClipboardTool` (get/set).
- 16 agent/permission/tool tests — all passing.

### Added — Phase 6: Hardening
- `VPet.Cognition.Security` (net8.0): `SecretRedactor` (redact API keys/tokens/passwords from
  JSON and individual values), `PathValidator` (path traversal prevention, allowlist checking),
  `CommandValidator` (dangerous pattern detection, blocked command list).
- `VPet.Cognition.Host/Config/CognitionConfigLoader` (load/save `appsettings.json` with
  secret redaction on save).
- 15 security tests — all passing.

### Summary
- **129 tests, all passing** in Release. 0 warnings, 0 errors.
- 11 cognition projects (9 build on Linux, 2 require Windows for WPF).
- 0 changes to existing VPet projects.
- Full offline-first: LLamaSharp + Qwen2.5-3B + Whisper + Piper/Kokoro.
- Config-switchable inference provider (local / ollama / openai-compatible).

### Added — Modular Inference Engine
- `VPet.Cognition.Inference` (net8.0): decouples cognition from LLM backend.
  - `IInferenceProvider` abstraction with `InferenceCapabilities` flags (Text, Streaming,
    Vision, Embedding, ToolCalling, Json).
  - `LocalInferenceProvider` — runs GGUF models in-process via LLamaSharp 0.16. No
    external server needed. CPU and GPU (CUDA) supported.
  - `OnlineInferenceProvider` — connects to Ollama or OpenAI-compatible endpoints.
    Lists and categorizes models (text, vision, embedding).
  - `ModelRegistry` — 9 curated official models (Qwen2.5-3B/7B/1.5B, Llama-3.2-3B,
    SmolVLM-500M/2.2B, LLaVA-7B, BGE-Small/M3).
  - `ModelManager` — search, download from HuggingFace (with progress events), install,
    update, delete, disk usage tracking.
  - `VisionModule` — optional, toggleable at runtime, disabled by default to save RAM.
- Host DI updated to register `IInferenceProvider`, `ModelManager`, `VisionModule`.
- `CognitionOptions.Llm` updated: `Provider` default "local", `ModelsDirectory`,
  `GpuLayerCount`, `ContextSize`, `EnableVision`.
- 27 inference tests — all passing.
- ADR 0007 (local inference via LLamaSharp as default, online as fallback).

### Added — CI/CD
- `.github/workflows/build-windows.yml` — GitHub Actions workflow: restore, build, test,
  package cognition DLLs into `VPet-Ultra-Cognition.zip` artifact.
- `build-windows.ps1` — local Windows build script.
- CI passes on Windows. Artifact: `VPet-Ultra-Cognition` (cognition plugin DLLs +
  LLamaSharp native libs + appsettings template + docs).