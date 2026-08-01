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

### Notes
- The existing 5 projects (`VPet-Simulator.Core`, `VPet-Simulator.Windows`,
  `VPet-Simulator.Windows.Interface`, `VPet.Solution`, `VPet-Simulator.Tool`) are unchanged.
- `VPet.Cognition.Host` cannot build on Linux (WPF targets are Windows-only); it builds on Windows CI.
- .NET 8 SDK is required (`dotnet-sdk-8.0`).