# 19 — Future Features

## Purpose
Document the long-term vision beyond the initial scope — features that are desirable but not committed for the first release.

## Business Goal
Provide a roadmap for the companion's continued evolution after the initial AI transformation.

## Technical Goal
List future features with enough detail to guide future implementation, without committing to architecture.

## Functional Requirements
N/A (aspirational).

## Non Functional Requirements
N/A.

## Architecture
N/A.

## Module Design
N/A.

## Folder Structure
N/A.

## Interfaces
N/A.

## Classes
N/A.

## State Machine
N/A.

## Sequence Diagram (Mermaid)
N/A.

## Flow Diagram (Mermaid)
N/A.

## Database Schema
N/A.

## Data Models
N/A.

## APIs
N/A.

## Configuration
N/A.

## Logging
N/A.

## Telemetry
N/A.

## Performance Targets
N/A.

## Security
N/A.

## Error Handling
N/A.

## Edge Cases
N/A.

## Acceptance Criteria
N/A (future).

## Unit Tests
N/A.

## Integration Tests
N/A.

## Future Expansion
This document IS the future expansion.

## Refactoring Notes
N/A.

## Migration Strategy
N/A.

## Implementation Steps
N/A.

## Agent Instructions
These features are NOT in scope for the initial implementation. Do not implement them unless explicitly assigned a future phase.

## Developer Notes

### Cognition

- **Multi-agent collaboration**: multiple specialized agents (research, coding, scheduling) sharing memory and coordinating.
- **Long-horizon planning**: hierarchical plans (goals → sub-goals → tasks) with multi-day horizons.
- **Learning from reflections**: improve planning quality over time by training on past reflection outcomes.
- **Causal reasoning**: model cause-effect for better tool selection and prediction.
- **Theory of mind**: model the user's beliefs/preferences to anticipate needs.

### Memory

- **Qdrant support**: for > 1M memories; remote vector DB.
- **Hierarchical consolidation**: daily → weekly → monthly → yearly summaries.
- **Memory replay during sleep**: offline consolidation that re-processes the day's memories.
- **Cross-user templates**: share personality seeds (not personal data) so new companions start with a proven persona.
- **Memory visualization**: graph view of memories and their relationships.
- **Forgetting curve tuning**: per-user Ebbinghaus curve fitting.

### Personality

- **Multiple personality presets**: user-selectable seeds (e.g. "helpful assistant", "snarky friend", "calm mentor").
- **Personality moods**: short-term trait overrides driven by emotion (e.g. "grumpy" for an hour).
- **Personality import/export**: share companion personas as files.
- **Personality aging**: personality matures over months of use.

### Emotion

- **Multi-modal sentiment**: voice tone analysis from STT audio (not just text).
- **Emotion contagion**: companion mirrors user's emotion more strongly as relationship deepens.
- **Complex emotions**: secondary emotions (e.g. nostalgia, pride, guilt) built from primary 10.
- **Emotion-driven music**: companion suggests/plays music matching its emotion.

### Voice

- **Speaker identification**: recognize the user (and distinguish multiple users).
- **Multi-voice**: different TTS voices for different emotions or contexts.
- **Real-time voice cloning**: XTTS v2 voice clone from a short sample.
- **Full-duplex conversation**: no wake word; always-listening with VAD (like a phone call).
- **Voice activity in noise**: robust VAD in noisy environments.
- **Multilingual voice**: auto-detect and respond in the user's language.

### Tools

- **Custom tool plugins**: MODs register `IToolProvider` implementations.
- **Tool chaining**: output of one tool feeds the next in a single plan step.
- **Sandboxed execution**: AppContainer / job object for process isolation.
- **MCP (Model Context Protocol)**: standard remote tool protocol.
- **Home automation**: IoT device control (lights, thermostat) via tool providers.
- **Code execution**: sandboxed Python/C# execution for computation.

### Agent

- **Proactive scheduling**: companion autonomously plans the day's reminders/checks.
- **Goal negotiation**: companion discusses and refines goals with the user before executing.
- **Multi-modal goals**: goals involving vision (screenshot analysis), audio, files.
- **Collaborative goals**: companion + user work together on a task (pair programming, writing).

### UI/UX

- **Conversation themes**: light/dark, custom bubble styles.
- **Memory graph visualization**: interactive node-link diagram.
- **Emotion timeline**: historical emotion chart.
- **Mobile companion app**: remote control + view from phone (local network).
- **Widget mode**: companion as a desktop widget with mini-conversation.
- **AR/VR mode**: 3D companion (future platform).

### Platform

- **Cross-platform**: Linux/macOS port (WPF → Avalonia or MAUI).
- **Mobile**: companion app for iOS/Android (read-only memory sync).
- **Web companion**: browser-based version (limited; cloud LLM only).

### Security

- **Plugin sandboxing**: out-of-process plugin execution with IPC.
- **Code-signing revocation**: online revocation list check.
- **Hardware key (TPM)**: DB encryption key bound to TPM.
- **SOC2 audit export**: compliance-ready audit reports.
- **Zero-knowledge cloud sync**: encrypted memory sync that the server can't read.

### Performance

- **Speculative decoding**: for local LLM latency reduction.
- **Prompt caching**: cache repeated system prompts.
- **Model quantization**: GGUF Q4/Q5 for faster local inference.
- **Streaming vector search**: incremental retrieval during LLM generation.

### Integration

- **Calendar integration**: read/write user's calendar (via tool).
- **Email integration**: summarize/draft emails (via tool).
- **Code editor integration**: VS Code extension for pair programming.
- **Steam Rich Presence enhancement**: show companion state in Steam status.