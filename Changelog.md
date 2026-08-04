# Changelog

All notable changes to the VPet AI transformation are documented here.
Format based on [Keep a Changelog](https://keepachangelog.com/).

## [Unreleased]

## [0.3.0] — 2026-08-04

### Fixed — Critical
- **Mod loading**: `0000_cognition` was missing `info.lps` — VPet requires this file to
  recognize a mod folder. Without it, the mod loader skipped the folder entirely.
  Now creates `info.lps` with `vupmod#Cognition` + metadata in CI and local build.
- **Dependency filter**: CI now skips Panuon/Steam/NAudio/Skia DLLs (already in main app)

### Added — Update System
- `UpdateChecker`: checks GitHub releases API for latest version
  - Compares local `version.txt` against latest release tag
  - Downloads only Plugin zip (~50MB) not Full zip (914MB)
  - Extracts and replaces `mod/0000_cognition/` files
  - Updates `version.txt` after successful update
- `version.txt` created in each build for version tracking

### Added — Local Build
- `build-local.ps1`: complete Windows build script (no CI needed)
  - Restore + Build + Test + Publish + Package
  - Produces `VPet-Ultra-Full-vX.Y.Z.zip` and `VPet-Ultra-Plugin-vX.Y.Z.zip`
  - Auto-detects version from git tag

### Added — UI Windows (4 new)
- `EmotionDashboardWindow`: live 10-dimensional emotion bar chart (updates every 500ms)
- `PersonalityWindow`: Big Five + adaptive traits + relationship + preferences
- `MemoryInspectorWindow`: searchable memory list with GridView
- `PermissionPromptWindow`: modal dialog (Deny / Allow Once / Allow Always)

### Added — Voice Audio
- `AudioCapture`: NAudio microphone capture (16kHz mono 16-bit, 10s chunks)
  ChunkReady event for streaming STT integration, silence filtering
- `AudioPlayer`: NAudio WAV playback with barge-in support
  PlayAsync with CancellationToken, BargeInTriggered event
- `EnergyWakeWordProvider`: energy-based VAD wake word detector
  RMS energy calculation, threshold + min duration trigger

### Added — Agent Infrastructure
- `GoalPriorityQueue`: weighted priority formula
- `GoalManager`: goal persistence + in-memory cache
- `AgentScheduler`: periodic background tasks
- Tool audit logging: `ExecuteToolCallAsync` writes to `CognitionStore` hash chain

### Added — Security
- `EgressFilterHandler`: HTTP delegating handler, blocks non-allowlisted domains (SSRF prevention)
- `SecretResolver`: env var expansion (`${ENV_VAR}`) + Windows DPAPI encryption/decryption

### Added — Integration
- Likability bridge: syncs `GameSave.Likability` → personality `Relationship.Affection`
- Emotion state restore: loads vector from `CognitionStore` JSON on startup
- Tool audit log wired to `CognitionStore.LogToolInvocationAsync`

### Changed
- CI only runs on tags (saves storage quota)
- Artifact upload skipped (use Release downloads)

## [0.2.0] — 2026-08-02

### Summary
- **129 tests, all passing**. CI green on Windows.
- Major integration fixes: cognition now actually wired to pet, memory, personality, emotion.
- LLM fallback chain + retry. Embedding providers. Prompt injection defense.
- 5 new tool providers. ConversationFsm. Streaming STT. Viseme estimation.
- Agent LLM planning + retry backoff + task verification.

### Fixed — Critical Integration (was no-op/stubs)
- **Config loading**: `appsettings.json` now read via `IConfiguration` + bound to `CognitionOptions`
  (was `new CognitionOptions()` — config file ignored entirely)
- **AttachToPet**: real implementation — subscribes to `EmotionChangedEvent`, maps 10 emotions →
  `GraphInfo.GraphType` + `GraphInfo.AnimatType`, emotion decay timer, loads persisted state
  (was a no-op that only logged)
- **Memory wiring**: `CognitionHost.ProcessConversationTurnAsync` retrieves memory context,
  applies personality prompt, analyzes sentiment → emotion stimulus, evolves personality,
  stores conversation turns. `ConversationVM` now calls `CognitionHost` for memory + personality + emotion
  (was hardcoded `_memoryContext = ""`)
- **Personality `ReflectAsync`**: now uses LLM to generate trait adjustments
  (was just `Confidence +0.01`)
- **Personality `EvaluateAsync`**: real core trait drift clamping — compares against seed snapshot,
  resets toward seed if drift > maxDrift (was a stub with "In a full implementation..." comment)
- **Emotion `EmotionChangedEvent`**: now published via EventBus on each change
  (was only logged, never published)
- **Emotion decay**: now targets personality baseline (was decaying toward 0)
- **Emotion `Calmness↔Stress`**: antagonistic pair added (was missing — doc lists 4 pairs)
- **Memory `ForgetAsync`**: only forgets low-importance/old memories
  (was marking ALL memories as forgotten unconditionally)
- **Memory `ConsolidateAsync`**: 4-step process — decay, spaced repetition boost, prune, promote
  (was just `importance * 0.95`)
- **Permission persistence**: `DefaultPermissionService` now persists grants to SQLite
  (was in-memory only — grants lost on restart)
- **Whisper STT streaming**: `StartStreamingAsync` now actually works — monitors temp dir for .wav files
  (was a no-op that only logged)
- **TTS visemes**: Piper and Kokoro now return estimated visemes from text
  (was returning empty arrays)

### Added — Persistence
- `CognitionStore`: 5 SQLite tables — `personality_state`, `emotion_state`, `goals`,
  `permission_grants`, `tool_invocations` (with tamper-evident hash chain)
- Personality persistence: `LoadAsync`/`PersistAsync` callbacks wired to `CognitionStore`
- Emotion persistence: `EmotionPersistenceService` saves state every 1 minute
- Background services: `MemoryMaintenanceService` (consolidation + forgetting + compression),
  `PersonalityEvaluationService` (core trait drift check) — all `IHostedService`

### Added — LLM
- `FallbackLlmProvider`: wraps primary + fallback providers with retry + exponential backoff
  (1s, 2s, 4s). Falls back to secondary if primary fails after all retries.
  Streaming fallback: if primary stream throws, switches to fallback.
- `TokenCounter`: approximates token count (~4 chars/token), truncates history to budget
- `SayInfoWithStream` wiring: `CognitionHost.StreamToPetAsync` streams LLM tokens directly
  to pet's `MessageBar` (in-pet conversation mode, no separate window)
- Prompt injection defense: user input wrapped in `<user_input>` delimiters, system prompt
  includes security instruction

### Added — Embedding
- `LocalEmbeddingProvider`: offline-first hashing-based embeddings (384 dims, no model files needed)
- `OnlineEmbeddingProvider`: calls OpenAI-compatible `/embeddings` endpoint
- Both registered in DI, selectable via config (`EmbeddingProvider: local|online`)
- `MemoryService` now gets embedding provider injected

### Added — Tools (5 new providers)
- `PowerShellTool`: execute PowerShell commands with blocklist + timeout
- `CmdTool`: execute CMD commands (Windows only) with blocklist + timeout
- `FileWriteTool`: write/append text to files with path allowlist
- `FolderTool`: list/create/delete folders
- All require confirmation (dangerous operations)

### Added — Voice
- `ConversationFsm`: state machine (Idle/Listening/Thinking/Speaking/Interrupted)
  with valid transition checking and `StateChanged` event

### Added — Agent
- LLM-based planning: `PlanGoalAsync` decomposes goals into steps via LLM
  (was null → goal marked done with confidence=1.0)
- Retry with exponential backoff (1s, 2s, 4s) between attempts
- Task verification: checks `ToolResult.Success` before marking Done
- `TaskFailedEvent` published on final failure (for reflection/audit)
- `LlmProvider` wired to agent in DI

### Added — Architecture
- `ICognitionFacade` interface in Domain — breaks circular dependency (Wpf↔Host)
- `IHttpClientFactory` replaces `new HttpClient()` (fixes socket exhaustion)
- Serilog file+console logging with rolling day, log level from config

### Changed
- `VPet.Cognition.Host` now references `VPet-Simulator.Core` (for `GraphInfo.GraphType`/`AnimatType`)
- `VPet.Cognition.Agent` now references `VPet.Cognition.Memory` (for `CognitionStore`)
- `VPet.Cognition.Wpf` no longer references `VPet.Cognition.Host` (uses `ICognitionFacade`)

## [0.1.0] — 2026-08-02

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