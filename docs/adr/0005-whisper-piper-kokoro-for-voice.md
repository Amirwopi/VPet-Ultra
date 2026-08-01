# ADR 0005: Whisper + Piper/Kokoro for Voice

## Status
Accepted — 2026-08-01

## Context
The voice system needs offline-first STT and TTS providers. Requirements:
- Run locally with no internet.
- Support multilingual input (user may speak any language).
- Low latency for interactive use.
- Easy to install and configure.

## Decision
- **STT**: `whisper.cpp` (whisper-cli) — OpenAI's Whisper model in C/C++, optimized for
  CPU inference. Supports any GGML Whisper model (base, small, medium, large).
- **TTS**: `Piper` (default) or `Kokoro` — both are offline neural TTS systems.
  Piper uses ONNX voice models (VITS architecture). Kokoro uses ONNX/GGML models.
  Provider selection is a config switch.

## Consequences
- + Fully offline: no cloud API calls for voice.
- + whisper.cpp is high-performance and supports GPU acceleration (CUDA, Metal).
- + Piper has a wide voice model library (100+ languages).
- + Kokoro offers higher quality for some languages.
- − Users must install whisper.cpp, Piper/Kokoro separately (documented in README).
- − STT is file-based in Phase 1; streaming STT requires a server-mode wrapper (future).
- − TTS synthesizes to temp WAV then streams PCM; direct PCM streaming is a future optimization.