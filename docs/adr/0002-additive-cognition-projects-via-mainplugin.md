# ADR 0002: Additive Cognition Projects via MainPlugin

## Status
Accepted — 2026-08-01

## Context
The VPet solution has 5 existing projects with a working MOD/plugin ecosystem, animation engine,
save format, and Steam integration. The AI transformation must add cognition (memory, personality,
emotion, agent, voice, tools) without breaking any of these. Two approaches were considered:

1. **Modify existing projects** — add cognition directly into `VPet-Simulator.Windows` and `Core`.
2. **Additive new projects** — create `VPet.Cognition.*` projects that load as a `MainPlugin`.

Modifying existing projects risks breaking the ecosystem, complicates the existing code, and
couples AI concerns with the rendering/game-sim concerns. The existing `MainPlugin` + `IMainWindow`
contract already provides a clean plugin seam used by `VPet.Plugin.Demo` and others.

## Decision
All cognition code lives in **new additive projects** (`VPet.Cognition.Domain`, `VPet.Cognition.LLM`,
`VPet.Cognition.Memory`, `VPet.Cognition.Voice`, `VPet.Cognition.Tools`, `VPet.Cognition.Host`,
`VPet.Cognition.Wpf`). The host project's `CognitionPlugin : MainPlugin` is the entry point,
discovered and loaded by the existing `CoreMOD` plugin loader — exactly like any other plugin.

**Zero changes to existing projects are required for Phases 0–1.** The cognition host subscribes
to existing events (`Main.TimeHandle`, `Main.GraphDisplayHandler`, `Main.SayProcess`) and uses
existing APIs (`Main.Say(SayInfoWithStream)`, `MW.Set`, `MW.GameSavesData`).

Dependency direction: `VPet.Cognition.Domain` (net8.0, no deps) ← infrastructure projects ←
`VPet.Cognition.Host` (net8.0-windows, references `VPet-Simulator.Windows.Interface`) ← loaded
as `MainPlugin` by the existing app.

## Consequences
- + The existing MOD ecosystem, animation engine, save format, and Steam integration are untouched.
- + Removing the `VPet.Cognition.*` DLLs (uninstalling the plugin) returns the app to exact existing behavior.
- + New code is isolated, testable, and follows Clean Architecture (domain has no deps).
- + The `VPet.Cognition.Domain` and `VPet.Cognition.Tests` projects target `net8.0` (headless) and
  build/test on Linux CI; only the `net8.0-windows` projects (Host, Wpf) require Windows.
- − The `CognitionPlugin` DLL must be signed (or `passmod` enabled) for the existing loader to accept it.
- − Some integration requires subscribing to existing events rather than direct calls, which is
  slightly less ergonomic but preserves the existing API.
- − The `VPet.Cognition.Host` project cannot build on Linux (WPF targets); it builds on Windows CI.