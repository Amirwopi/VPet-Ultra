<div align="center">

# 🐾 VPet-Ultra

### هوش مصنوعی Desktop Companion آفلاین

[VPet](https://github.com/LorisYounger/VPet) رو به یک companion هوشمند تبدیل می‌کنه — با LLM inference درون‌پروسه‌ای، memory، personality، emotion، voice و agent capabilities. همه آفلاین، بدون نیاز به server خارجی.

[![Build](https://github.com/Amirwopi/VPet-Ultra/actions/workflows/build-windows.yml/badge.svg)](https://github.com/Amirwopi/VPet-Ultra/actions)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%28Wine%29-lightgrey.svg)](#)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](CONTRIBUTING.md)

</div>

---

> **نکته:** این fork از پروژه‌ی اصلی VPet، وابستگی‌های Steam کاملاً حذف شده و برنامه به‌صورت standalone اجرا می‌شه. هیچ نیازی به Steam client یا Steam SDK نیست.

## ✨ Features

| Feature | Description |
|---------|-------------|
| **In-Process LLM Inference** | مدل‌های GGUF مستقیماً داخل app اجرا می‌شن via [LLamaSharp](https://github.com/SciSharp/LLamaSharp) (llama.cpp). بدون Ollama، بدون Python، بدون server. CPU و GPU پشتیبانی می‌شن. |
| **Model Manager** | browse، download، install، update و delete مدل‌های GGUF از HuggingFace. 9 مدل curated از قبل ثبت شده (Qwen2.5، Llama-3.2، SmolVLM، LLaVA، BGE embeddings). |
| **Vision Module** | پشتیبانی optional از multimodal vision (LLaVA/SmolVLM). در runtime قابل toggle، به‌صورت پیش‌فرض disabled برای save RAM. |
| **Online Mode** | اتصال optional به Ollama یا هر OpenAI-compatible endpoint (Qwen، GLM، DeepSeek، OpenAI) به‌جای local inference. |
| **Memory** | SQLite-backed episodic + semantic memory با embedding retrieval، importance decay و consolidation. |
| **Personality** | Big Five core traits + adaptive traits که در طول conversation تکامل پیدا می‌کنن. Relationship state (trust، familiarity، affection). |
| **Emotion** | 10-dimensional emotion vector با stimulus blending، antagonistic suppression و decay. به animation، voice prosody و decision bias map می‌شه. |
| **Voice** | Whisper (whisper.cpp) برای speech-to-text، Piper یا Kokoro برای text-to-speech. همه آفلاین. |
| **Agent** | Goal-driven autonomous runtime با plan execution، tool dispatch و permission gating. |
| **Tools** | Open application، read file، open browser، clipboard. Path validation و command blocklist. |
| **Security** | Secret redaction در logs، path traversal prevention، dangerous command detection. |
| **Live2D Support** | پشتیبانی از مدل‌های Live2D (`.moc3`) از طریق پلاگین `1102_Live2DAnimation`. |

## 🚀 Quick Start

### Prerequisites

1. **.NET 8 SDK** — [download](https://dotnet.microsoft.com/download/dotnet/8.0)
2. **Windows** (یا Linux با Wine) برای اجرای WPF
3. **یک GGUF model** (برای AI):
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
```

### Debug با CLion

این پروژه C#/.NET هست، اما یک `CMakeLists.txt` حرفه‌ای هم داره که `dotnet` CLI رو wrap می‌کنه تا بتونی توی CLion پروژه رو باز کنی، build کنی، اجرا کنی و debug کنی.

```bash
# 1. CLion رو باز کن و CMakeLists.txt رو انتخاب کن (File ▸ Open)
# 2. CLion خودش configure می‌کنه (Ninja generator، Debug profile)
# 3. از toolbar Run Configurations رو انتخاب کن:
#      Build All          → dotnet build (کل solution)
#      Restore            → dotnet restore
#      Run VPet.Windows   → اجرای desktop pet (x64 Debug)
#      Run VPet.Solution  → اجرای solution launcher
#      Tests              → build + dotnet test (129 test)
#      Clean              → dotnet clean
#      Publish            → build-local.ps1 (full package)
```

برای step-through debugging با `netcoredbg`:

```powershell
# netcoredbg رو نصب کن
.\scripts\install-netcoredbg.ps1

# بعد توی CLion: Tools ▸ CMake ▸ Reload CMake Project
# targetهای debug-VPet.Windows و debug-VPet.Solution ظاهر می‌شن
```

| CMake Target | Description |
|---|---|
| `build-all` | `dotnet build` کل VPet.sln (restore + build) |
| `restore` | `dotnet restore` (NuGet) |
| `clean-all` | `dotnet clean` |
| `test` | `dotnet test` (129 cognition test) |
| `test-build` | build + test در یک مرحله |
| `publish` | `build-local.ps1` (full package + zip) |
| `build-<project>` | build تک‌پروژه‌ای (۱۶ پروژه) |
| `run-VPet.Windows` | اجرای VPet-Simulator.Windows.exe |
| `run-VPet.Solution` | اجرای VPet.Solution.exe |
| `run-VPet.Tool` | اجرای VPet-Simulator.Tool.exe |
| `debug-VPet.Windows` | debug با netcoredbg (اگر نصب باشه) |

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

## 🏗️ Architecture

| Component | Description |
|-----------|-------------|
| `VPet-Simulator.Core` | Core engine: animation، graph، display، save system |
| `VPet-Simulator.Windows.Interface` | Interface definitions: IMainWindow، IModInfo، ExtensionFunction |
| `VPet-Simulator.Windows` | WPF app: MainWindow، WinDesign windows، MOD system |
| `VPet.Cognition` | AI layer: LLM، memory، personality، emotion، voice، agent |
| `mod/0000_core` | Core game data: pets، food، themes، images |
| `mod/1102_Live2DAnimation` | Live2D model support plugin |

## 🔧 Steam Dependency Removal

این fork تمام وابستگی‌های Steam رو حذف کرده:

| Removed | Status |
|---------|--------|
| `Facepunch.Steamworks` NuGet package | ✅ حذف شد |
| `steam_api.dll` / `steam_api64.dll` | ✅ حذف شد |
| Steam Workshop loading | ✅ حذف شد (فقط local mod path) |
| Steam Cloud saves | ✅ حذف شد |
| Steam Multiplayer (Lobby/P2P) | ✅ حذف کامل |
| Steam Leaderboards/Rankings | ✅ حذف شد |
| Steam Rich Presence | ✅ حذف شد |
| Steam Screenshots | ✅ حذف شد |
| Mod publishing به Workshop | ✅ حذف شد |

`IsSteamUser` property به‌صورت `false` ثابت نگه‌داشته شده برای backward compatibility.

## 📦 Live2D Setup

پلاگین Live2D از قبل در `mod/1102_Live2DAnimation` نصب شده. فقط نیاز به `Live2DCubismCore.dll`:

1. به [Live2D SDK](https://www.live2d.com/sdk/download/native/) بروید
2. Cubism Native SDK را دانلود کنید
3. `Live2DCubismCore.dll` را کنار `VPet.exe` یا در `mod/1102_Live2DAnimation/plugin/` قرار دهید

## 📋 Documentation

- [`INSTALL.md`](INSTALL.md) — راهنمای نصب صفر تا صد (فارسی)
- [`Changelog.md`](Changelog.md) — تغییرات
- [`CONTRIBUTING.md`](CONTRIBUTING.md) — راهنمای مشارکت
- [`TESTING.md`](TESTING.md) — راهنمای تست
- [`wiki/`](wiki/) — مستندات کامل (Architecture، Build، Configuration، AI Models، Live2D، Mod System، Steam Removal، Troubleshooting، FAQ)

## 🤝 Contributing

مشارکت‌ها welcome هستند! ببینید [`CONTRIBUTING.md`](CONTRIBUTING.md).

- [Report a bug](https://github.com/Amirwopi/VPet-Ultra/issues/new?template=bug_report.yml)
- [Request a feature](https://github.com/Amirwopi/VPet-Ultra/issues/new?template=feature_request.yml)
- [Open a PR](https://github.com/Amirwopi/VPet-Ultra/compare)

## 📝 License

این پروژه تحت **Apache License 2.0** است. ببینید [`LICENSE`](LICENSE).

پروژه‌ی اصلی: [VPet](https://github.com/LorisYounger/VPet) توسط [@LorisYounger](https://github.com/LorisYounger).

## 🙏 Acknowledgments

- **[LorisYounger](https://github.com/LorisYounger)** — سازنده‌ی پروژه‌ی اصلی VPet
- **[LLamaSharp](https://github.com/SciSharp/LLamaSharp)** — .NET binding برای llama.cpp
- **[Live2D](https://www.live2d.com/)** — Live2D Cubism SDK
- **[OpenTK](https://opentk.net/)** — OpenGL برای .NET

## ⭐ Star History

اگر پروژه مفید بود، Star بدهید!

<div align="center">

[![Star History Chart](https://api.star-history.com/svg?repos=Amirwopi/VPet-Ultra&type=Date)](https://star-history.com/#Amirwopi/VPet-Ultra&Date)

</div>