# ADR 0003: Ollama as Default LLM Provider

## Status
Accepted — 2026-08-01

## Context
The VPet AI transformation requires an offline-first LLM provider. The spec (`04_ai_pipeline.md`)
lists Qwen, GLM, DeepSeek, Llama, and OpenAI-compatible APIs as interchangeable providers.
For the default offline-first experience, we need a provider that:
- Runs locally with no internet.
- Supports the Qwen2.5-3B model (small, multilingual, good quality).
- Supports streaming and tool-calling.
- Is easy to install and configure.

## Decision
**Ollama** is the default LLM provider (`llm.provider: "ollama"`), using the **Qwen2.5-3B** model
as the default (`llm.model: "qwen2.5:3b"`). The `OllamaLlmProvider` speaks Ollama's native
`/api/chat` endpoint (newline-delimited JSON streaming).

An `OpenAICompatibleLlmProvider` is also provided for cloud/remote providers (DeepSeek, GLM,
Qwen via DashScope, OpenAI, any OpenAI-compatible endpoint). It speaks the standard
`/v1/chat/completions` SSE streaming format.

Provider selection is a config switch in `CognitionOptions.Llm.Provider`. The `CognitionHostBuilder`
resolves the active `ILLMProvider` by name at DI registration time. Switching providers is a
config change only — no code changes, no recompile.

For local models that lack native tool-calling, an `XmlToolCallParser` extracts
`<tool name="..." args='...'/>` tags from text output as a fallback.

## Consequences
- + Offline-first: Ollama + Qwen2.5-3B runs on modest hardware with no internet.
- + Easy provider swap: change one config field to switch to DeepSeek/GLM/OpenAI.
- + Both providers share the same `ILLMProvider` interface; the rest of the system is agnostic.
- − Ollama must be installed separately by the user (documented in setup guide).
- − Qwen2.5-3B is small; quality is lower than 7B+ models. Users can change the model in config.
- − The `OpenAICompatibleLlmProvider` uses a raw `HttpClient` (not the OpenAI SDK) to avoid
  vendor lock-in and to support any OpenAI-compatible endpoint.