# Appendix

## Purpose
Provide reference material: glossary, LPS format reference, provider matrix, enums, error codes, and file index.

## Business Goal
Single-location reference for terms and formats used across the specification.

## Technical Goal
Reduce ambiguity by centralizing definitions.

## Glossary

| Term | Definition |
|---|---|
| **VPet** | The desktop pet application (this repository). |
| **Companion** | The AI-transformed VPet with cognition. |
| **Cognition** | The collective AI capabilities: memory, personality, emotion, agent, voice, tools. |
| **LPS** | LinePutScript — the custom `.lps` text serialization format used throughout VPet. |
| **MOD** | A modification package (folder in `mod/`) containing assets and optionally code plugins. |
| **MainPlugin** | The abstract base class all code plugins inherit. |
| **IMainWindow** | The interface contract between plugins and the host app. |
| **CoreMOD** | The internal MOD loader in `VPet-Simulator.Windows`. |
| **GraphCore** | The animation registry/cache in `VPet-Simulator.Core`. |
| **Main** | The root display control in Core that renders the pet. |
| **MessageBar** | The speech bubble control; has the streaming seam (`SayInfoWithStream`). |
| **ModeType** | Animation mood: Happy/Nomal/PoorCondition/Ill (ordered 0..3). |
| **GraphType** | Animation type enum (28 values). |
| **AnimatType** | Animation phase: Single/A_Start/B_Loop/C_End. |
| **passmod** | User consent list in `Setting.lps` for loading unsigned/foreign-signed plugins. |
| **HashCheck** | Save tamper-detection flag in `GameSave_v2`. |
| **CognitionPlugin** | The `MainPlugin` entry point for the cognition host. |
| **CognitionHost** | The orchestrator that attaches cognition to the pet. |
| **ILLMProvider** | The LLM provider abstraction. |
| **IMemoryStore** | The memory persistence abstraction. |
| **IPersonalityEngine** | The personality engine abstraction. |
| **IEmotionEngine** | The emotion engine abstraction. |
| **ISttProvider/ITtsProvider/IWakeWordProvider** | Voice provider abstractions. |
| **IToolProvider** | The tool execution abstraction. |
| **IPermissionService** | The permission system abstraction. |
| **IAgentRuntime** | The autonomous agent abstraction. |
| **IEventBus** | The typed in-process event bus. |
| **CognitionOptions** | The strongly-typed config object. |
| **SqliteMemoryStore** | The SQLite + sqlite-vec memory implementation. |
| **SayInfoWithStream** | The existing Core class for streaming text to MessageBar — the LLM integration seam. |
| **SimpleObservable** | The custom MVVM framework in `VPet.Solution` (`HKW.HKWUtils.Observable`). |
| **Panuon.WPF.UI** | The UI control library used for `WindowX`, `MessageBoxX`, theming. |
| **sqlite-vec** | A SQLite extension for vector similarity search. |
| **OpenWakeWord** | An open-source wake word detection library (ONNX models). |
| **Faster Whisper** | A fast CTranslate2-based Whisper STT implementation. |
| **Piper** | A fast local neural TTS. |
| **Kokoro** | A lightweight TTS model. |
| **XTTS v2** | Coqui's voice-cloning TTS. |
| **DPAPI** | Windows Data Protection API for secret storage. |
| **SQLCipher** | SQLite encryption extension. |
| **ADR** | Architecture Decision Record. |

## LPS Format Reference

LinePutScript (`.lps`) is a line-based text format. Each line:
```
name#info:|sub1#value1 sub2#value2|
```
- `name` — line identifier (case-insensitive).
- `#info` — the line's main value (string).
- `:|...|` — sub entries, each `key#value` space-separated.
- Typed accessors: `[(gint)"x"]`, `[(gstr)"x"]`, `[(gbol)"x"]`, `[(gdbe)"x"]`, `[(gi64)"x"]`, `[(gdat)"x"]`.
- `LPSConvert.SerializeObject/DeserializeObject<T>` for `[Line]`-annotated POCOs.
- `LpsDocument` (file), `LPS`/`LPS_D` (in-memory list, `_D` = dictionary-keyed), `Line`/`Sub` (single record).

Example MOD `info.lps`:
```
vupmod#MyMod:|intro#A mod|gamever#11068|ver#100|author#Author|authorid#123|itemid#456|cachedate#20260801|lang#zh-Hans:...
```

Example `Setting.lps` cognition line:
```
cognition#:|provider#ollama|endpoint#http://localhost:11434|model#qwen2.5:7b|keyref#credmanager:ollama|
```

## Provider Matrix

| Capability | Local (offline-first) | Cloud (optional) | Config key |
|---|---|---|---|
| LLM | Ollama, llama.cpp | OpenAI-compatible (Qwen, GLM, DeepSeek, Llama via Together/Groq, OpenAI) | `llm.provider` |
| Embedding | Local ONNX (bge-small, nomic-embed) | OpenAI-compatible embedding endpoint | `memory.embeddingProvider` |
| STT | Faster Whisper, whisper.cpp | (future) cloud STT | `voice.stt.provider` |
| TTS | Piper, Kokoro, XTTS v2 (local) | XTTS v2 (HTTP) | `voice.tts.provider` |
| Wake Word | OpenWakeWord (ONNX) | — | `voice.wakeWord.provider` |
| Memory | SQLite + sqlite-vec | FAISS (local), Qdrant (future local/remote) | `memory.provider` |
| Sentiment | Rule-based (offline) | LLM-assisted (optional) | `emotion.sentiment.method` |

## Enums

### MemoryType
`Working, ShortTerm, LongTerm, Semantic, Episodic, Relationship, Workspace, Emotional, Reflection, Goal, Identity`

### EmotionType
`Joy, Sadness, Anger, Curiosity, Stress, Excitement, Fear, Calmness, Surprise, Frustration`

### ToolClass
`Application, PowerShell, CMD, File, Folder, Clipboard, Browser, Win32`

### PermissionState
`NotRequested, Prompted, Granted, Denied, Revoked`

### GoalPriority
`Low=0, Normal=1, High=2, Critical=3`

### GoalStatus
`Pending, Planning, Executing, Verifying, Reflecting, Completed, Failed, Cancelled`

### TaskKind
`LlmCall, ToolCall, Wait, Remember, Notify`

### VoiceState
`Idle, Listening, Thinking, Speaking, Interrupted`

### GraphType (existing, 28 values)
`Common, Raised_Dynamic, Raised_Static, Move, Default, Touch_Head, Touch_Body, Idel, Sleep, Say, StateONE, StateTWO, StartUP, Shutdown, Work, Switch_Up, Switch_Down, Switch_Thirsty, Switch_Hunger, SideHide_Left_Main, SideHide_Left_Rise, SideHide_Right_Main, SideHide_Right_Rise`

### AnimatType (existing)
`Single, A_Start, B_Loop, C_End`

### ModeType (existing, ordered)
`Happy=0, Nomal=1, PoorCondition=2, Ill=3`

### WorkingState (existing)
`Nomal, Work, Sleep, Travel, Empty`

### FoodType (existing)
`Food, Star, Meal, Snack, Drink, Functional, Drug, Gift`

## Error Codes

| Code | Domain | Meaning |
|---|---|---|
| `LLM_TIMEOUT` | LLM | Provider did not respond within timeout. |
| `LLM_5XX` | LLM | Provider returned server error. |
| `LLM_DESERIALIZE` | LLM | Malformed stream chunk. |
| `LLM_CONTENT_FILTER` | LLM | Cloud content filter blocked response. |
| `MEM_DB_LOCKED` | Memory | SQLite DB locked by another process. |
| `MEM_DISK_FULL` | Memory | Disk full; cannot write. |
| `MEM_EMBEDDING_FAIL` | Memory | Embedding provider failed. |
| `PERM_DENIED` | Tools | User denied permission. |
| `PERM_REVOKED` | Tools | Previously granted permission revoked. |
| `TOOL_INVALID_ARGS` | Tools | Args failed schema validation. |
| `TOOL_PATH_BLOCKED` | Tools | Path outside allowed roots. |
| `TOOL_COMMAND_BLOCKED` | Tools | Command matched blocklist. |
| `TOOL_TIMEOUT` | Tools | Tool execution exceeded timeout. |
| `TOOL_NON_ZERO_EXIT` | Tools | Process exited with non-zero code. |
| `VOICE_NO_MIC` | Voice | No microphone available. |
| `VOICE_MODEL_MISSING` | Voice | Configured model file not found. |
| `AGENT_PLAN_FAILED` | Agent | LLM failed to produce a valid plan. |
| `AGENT_MAX_REPLANS` | Agent | Exceeded max replan attempts. |
| `SEC_EGRESS_BLOCKED` | Security | Network request to non-allowlisted host. |
| `SEC_SIGNATURE_INVALID` | Security | Plugin signature validation failed. |

## File Index

| File | Title |
|---|---|
| `task.md` | Master document |
| `01_project_overview.md` | Project Overview |
| `02_current_architecture.md` | Current Architecture |
| `03_target_architecture.md` | Target Architecture |
| `04_ai_pipeline.md` | AI Pipeline |
| `05_memory_system.md` | Memory System |
| `06_personality_engine.md` | Personality Engine |
| `07_emotion_engine.md` | Emotion Engine |
| `08_voice_pipeline.md` | Voice Pipeline |
| `09_tool_calling.md` | Tool Calling |
| `10_agent_system.md` | Agent System |
| `11_plugin_system.md` | Plugin System |
| `12_animation_system.md` | Animation System |
| `13_ui_ux.md` | UI/UX |
| `14_configuration.md` | Configuration |
| `15_security.md` | Security |
| `16_testing.md` | Testing |
| `17_development_rules.md` | Development Rules |
| `18_roadmap.md` | Roadmap |
| `19_future_features.md` | Future Features |
| `appendix.md` | This file |

## Existing Project Reference

| Project | Path | Target | Key types |
|---|---|---|---|
| `VPet-Simulator.Core` | `VPet-Simulator.Core/` | net8.0-windows | `Main`, `GraphCore`, `IGraph`, `GraphInfo`, `GameCore`, `GameSave`, `IGameSave`, `IController`, `IFood`, `PetLoader`, `Function`, `SayInfo`, `SayInfoWithStream`, `MessageBar`, `ToolBar`, `WorkTimer` |
| `VPet-Simulator.Windows.Interface` | `VPet-Simulator.Windows.Interface/` | net8.0-windows | `IMainWindow`, `MainPlugin`, `ISetting`, `GameSave_VPet`, `GameSave_v2`, `Statistics`, `ScheduleTask`, `ActivityLog`, `Food`, `Item`, `ClickText`, `SelectText`, `LowText`, `Photo`, `IModInfo`, `IMPFriend`, `IMPWindows`, `MPMessage` |
| `VPet-Simulator.Windows` | `VPet-Simulator.Windows/` | net8.0-windows | `MainWindow`, `MWController`, `CoreMOD`, `Setting`, `PetHelper`, `MPController`, `winGameSetting`, `winBetterBuy`, `winConsole`, `winGallery`, `winInventory`, `winWorkMenu`, `winSaveManager` |
| `VPet.Solution` | `VPet.Solution/` | net8.0-windows | `SettingWindowVM`, `SaveWindowVM`, `SettingModel`, `ModModel`, `ModLoader`, `SaveModel`, `ObservableClass<T>`, `ReflectionUtils` |
| `VPet-Simulator.Tool` | `VPet-Simulator.Tool/` | net48 | `Program` |

## New Project Reference

| Project | Path | Target | Key types |
|---|---|---|---|
| `VPet.Cognition.Domain` | `VPet.Cognition.Domain/` | net8.0 | `ILLMProvider`, `IMemoryStore`, `IPersonalityEngine`, `IEmotionEngine`, `ISttProvider`, `ITtsProvider`, `IWakeWordProvider`, `IToolProvider`, `IPermissionService`, `IAgentRuntime`, `IEventBus`, `Memory`, `Personality`, `Emotion`, `Goal`, `Result<T,Error>` |
| `VPet.Cognition.LLM` | `VPet.Cognition.LLM/` | net8.0 | `OpenAICompatibleLlmProvider`, `OllamaLlmProvider`, `LlamaCppLlmProvider`, `PromptAssembler`, `StreamAdapter`, `ToolCallProtocol` |
| `VPet.Cognition.Memory` | `VPet.Cognition.Memory/` | net8.0 | `SqliteMemoryStore`, `LocalEmbeddingProvider`, `ImportanceScorer`, `Consolidator`, `ForgettingService`, `CompressionService`, `Retriever`, `ContextInjector` |
| `VPet.Cognition.Voice` | `VPet.Cognition.Voice/` | net8.0-windows | `OpenWakeWordProvider`, `FasterWhisperProvider`, `WhisperCppProvider`, `PiperProvider`, `KokoroProvider`, `XttsV2Provider`, `ConversationFsm`, `LipSyncMapper` |
| `VPet.Cognition.Tools` | `VPet.Cognition.Tools/` | net8.0-windows | `ProcessTool`, `PowerShellTool`, `CmdTool`, `FileSystemTool`, `ClipboardTool`, `BrowserTool`, `Win32Tool`, `SafetyLayer`, `PermissionService`, `ToolAuditLog` |
| `VPet.Cognition.Host` | `VPet.Cognition.Host/` | net8.0-windows | `CognitionPlugin`, `CognitionHost`, `CognitionHostBuilder`, `EventBus`, `AgentRuntime`, `Planner`, `Executor`, `Verifier`, `Reflector`, `Scheduler`, `CognitionOptions` |
| `VPet.Cognition.Wpf` | `VPet.Cognition.Wpf/` | net8.0-windows | `ConversationWindow`, `MemoryInspectorWindow`, `PersonalityWindow`, `EmotionDashboardWindow`, `PermissionPromptWindow`, `ToolAuditWindow`, `VoiceSettingsWindow`, `CognitionSettingsWindow`, `LipSyncOverlay` |
| `VPet.Cognition.Tests` | `VPet.Cognition.Tests/` | net8.0 | All fakes + tests |

## NuGet Dependencies (new projects)

| Package | Version | Used by | Purpose |
|---|---|---|---|
| `Microsoft.Extensions.DependencyInjection` | 8.x | Host | DI container |
| `Microsoft.Extensions.Configuration` | 8.x | Host | Config binding |
| `Microsoft.Extensions.Options` | 8.x | Host | IOptions<T> |
| `Microsoft.Extensions.Hosting` | 8.x | Host | IHostedService for background tasks |
| `Microsoft.Extensions.Http` | 8.x | LLM, Voice | IHttpClientFactory |
| `Microsoft.Data.Sqlite` | 8.x | Memory | SQLite |
| `SQLitePCLRaw.bundle_e_sqlcipher` | 8.x | Memory | Encryption (optional) |
| `sqlite-vec` | 0.1.x | Memory | Vector search |
| `Microsoft.ML.OnnxRuntime` | 1.17.x | Voice, Memory | ONNX for wake word + embeddings |
| `Serilog` + `Serilog.Sinks.File` + `Serilog.Sinks.Console` | 3.x/5.x | Host | Logging |
| `CommunityToolkit.Mvvm` | 8.x | Wpf | MVVM source generators |
| `NAudio` | 2.2.1 | Voice | Audio I/O |
| `Dapper` | 2.x | Memory | Micro-ORM |
| `JsonSchema.NET` | 5.x | Tools | Schema validation |
| `xUnit` + `FluentAssertions` + `Moq` + `Verify` + `WireMock.Net` | latest | Tests | Testing |