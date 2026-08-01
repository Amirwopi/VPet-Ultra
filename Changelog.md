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