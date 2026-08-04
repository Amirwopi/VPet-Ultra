# VPet-Ultra — هوش مصنوعی Desktop Companion آفلاین

[VPet](https://github.com/LorisYounger/VPet) رو به یک companion هوشمند تبدیل می‌کنه که با LLM inference درون‌پروسه‌ای، memory، personality، emotion، voice و agent capabilities — همه آفلاین، بدون نیاز به server خارجی.

> **نکته:** این fork از پروژه‌ی اصلی VPet، وابستگی‌های Steam کاملاً حذف شده و برنامه به‌صورت standalone اجرا می‌شه. هیچ نیازی به Steam client یا Steam SDK نیست.

## Features

- **In-Process LLM Inference** — مدل‌های GGUF مستقیماً داخل app اجرا می‌شن via [LLamaSharp](https://github.com/SciSharp/LLamaSharp) (llama.cpp). بدون Ollama، بدون Python، بدون server. CPU و GPU پشتیبانی می‌شن.
- **Model Manager** — browse، download، install، update و delete مدل‌های GGUF از HuggingFace. 9 مدل curated از قبل ثبت شده (Qwen2.5، Llama-3.2، SmolVLM، LLaVA، BGE embeddings).
- **Vision Module** — پشتیبانی optional از multimodal vision (LLaVA/SmolVLM). در runtime قابل toggle، به‌صورت پیش‌فرض disabled برای save RAM.
- **Online Mode** — اتصال optional به Ollama یا هر OpenAI-compatible endpoint (Qwen، GLM، DeepSeek، OpenAI) به‌جای local inference.
- **Memory** — SQLite-backed episodic + semantic memory با embedding retrieval، importance decay و consolidation.
- **Personality** — Big Five core traits + adaptive traits که در طول conversation تکامل پیدا می‌کنن. Relationship state (trust، familiarity، affection).
- **Emotion** — 10-dimensional emotion vector با stimulus blending، antagonistic suppression و decay. به animation، voice prosody و decision bias map می‌شه.
- **Voice** — Whisper (whisper.cpp) برای speech-to-text، Piper یا Kokoro برای text-to-speech. همه آفلاین.
- **Agent** — Goal-driven autonomous runtime با plan execution، tool dispatch و permission gating.
- **Tools** — Open application، read file، open browser، clipboard. Path validation و command blocklist.
- **Security** — Secret redaction در logs، path traversal prevention، dangerous command detection.

## Quick Start

### Prerequisites

1. **.NET 8 SDK** — [download](https://dotnet.microsoft.com/download/dotnet/8.0)
2. **VPet** — solution اصلی رو روی Windows build کن.
3. **یک GGUF model** — یکی از curated models رو download کن (ببین `docs/MODELS.md`):
   ```bash
   # مثال: Qwen2.5-3B (recommended، ~2GB، روی CPU اجرا می‌شه)
   # توی پوشه‌ی models/ کنار VPet.exe قرار بده
   ```

### Build

```bash
git clone https://github.com/Amirwopi/VPet-Ultra.git
cd VPet-Ultra
dotnet restore
dotnet build
```

### Build روی Linux

```bash
dotnet build VPet-Simulator.Windows/VPet-Simulator.Windows.csproj \
  -c Release -p:Platform=x64 -p:EnableWindowsTargeting=true
```

### Test

```bash
dotnet test VPet.Cognition.Tests/VPet.Cognition.Tests.csproj -c Release
# 129 tests، همه passing
```

### Configure

فایل `appsettings.json` توی output directory بساز. default، local inference (LLamaSharp) هست:

```json
{
  "Cognition": {
    "Enabled": true,
    "Llm": {
      "Provider": "local",
      "Model": "qwen2.5-3b-instruct-q4_k_m",
      "ModelsDirectory": "models",
      "GpuLayerCount": 0,
      "ContextSize": 4096,
      "Temperature": 0.8
    }
  }
}
```

برای online mode:

```json
{
  "Cognition": {
    "Enabled": true,
    "Llm": {
      "Provider": "online",
      "Endpoint": "http://localhost:11434",
      "Model": "qwen2.5:3b"
    }
  }
}
```

## Architecture

| Component | Description |
|-----------|-------------|
| `VPet-Simulator.Core` | Core engine: animation، graph، display، save system |
| `VPet-Simulator.Windows.Interface` | Interface definitions: IMainWindow، IModInfo، ExtensionFunction |
| `VPet-Simulator.Windows` | WPF app: MainWindow، WinDesign windows، MOD system |
| `VPet.Cognition` | AI layer: LLM، memory، personality، emotion، voice، agent |

## Steam Dependency Removal

این fork تمام وابستگی‌های Steam رو حذف کرده:

- `Facepunch.Steamworks` NuGet package — حذف شد
- `steam_api.dll` / `steam_api64.dll` — حذف شد
- Steam Workshop loading — حذف شد (فقط local mod path)
- Steam Cloud saves — حذف شد
- Steam Multiplayer (Lobby/P2P) — حذف کامل
- Steam Leaderboards/Rankings — حذف شد
- Steam Rich Presence — حذف شد
- Steam Screenshots — حذف شد
- Mod publishing به Workshop — حذف شد

`IsSteamUser` property به‌صورت `false` ثابت نگه‌داشته شده برای backward compatibility.

## Documentation

- [`INSTALL.md`](INSTALL.md) — راهنمای نصب صفر تا صد (فارسی)
- [`Changelog.md`](Changelog.md) — تغییرات
- [`CONTRIBUTING.md`](CONTRIBUTING.md) — راهنمای مشارکت
- [`docs/`](docs/) — مستندات فنی architecture، AI pipeline، memory system و ...

## License

این پروژه license upstream رو به ارث می‌بره. ببین [VPet](https://github.com/LorisYounger/VPet) repository برای جزئیات.