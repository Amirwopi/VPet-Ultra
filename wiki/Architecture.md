# Architecture

## Overview

VPet-Ultra از 4 پروژه‌ی اصلی + سیستم mod تشکیل شده:

```
┌─────────────────────────────────────────────────┐
│                  VPet-Ultra                     │
├─────────────────────────────────────────────────┤
│                                                 │
│  ┌─────────────┐  ┌──────────────┐  ┌─────────┐ │
│  │  VPet-Sim   │  │  VPet-Sim    │  │ VPet    │ │
│  │  .Windows   │──│  .Windows    │──│ .Cogni  │ │
│  │  (WPF App)  │  │  .Interface  │  │ -tion   │ │
│  └──────┬──────┘  └──────────────┘  └─────────┘ │
│         │                                       │
│  ┌──────┴──────┐                               │
│  │  VPet-Sim   │                               │
│  │  .Core      │                               │
│  │  (Engine)   │                               │
│  └─────────────┘                               │
│                                                 │
│  ┌─────────────────────────────────────────┐   │
│  │              mod/                        │   │
│  │  ├── 0000_core/    (game data)          │   │
│  │  └── 1102_Live2D/  (Live2D plugin)      │   │
│  └─────────────────────────────────────────┘   │
└─────────────────────────────────────────────────┘
```

## Components

### VPet-Simulator.Core
موتور اصلی بازی: animation system، graph rendering، display management، save system. مستقل از WPF و Windows.

### VPet-Simulator.Windows.Interface
تعریف interfaceها: `IMainWindow`، `IModInfo`، `ExtensionFunction`. لایه‌ی abstraction بین Core و Windows app.

### VPet-Simulator.Windows
app اصلی WPF: `MainWindow`، WinDesign windows (settings، character panel، report)، MOD loading system، plugin management.

### VPet.Cognition
لایه‌ی AI: LLM inference (LLamaSharp)، memory (SQLite)، personality (Big Five)، emotion (10-dimensional)، voice (Whisper/Piper)، agent (goal-driven).

### mod/0000_core
داده‌های اصلی بازی: pet animations، food، themes، images، sounds. بدون این mod بازی اجرا نمی‌شود.

### mod/1102_Live2DAnimation
پلاگین Live2D: پشتیبانی از مدل‌های `.moc3`، رندر OpenGL از طریق OpenTK.

## Data Flow

```
User Input (click/chat/voice)
    │
    ▼
MainWindow.xaml.cs (event handler)
    │
    ├──► VPet-Simulator.Core (animation/display)
    │
    └──► VPet.Cognition (AI processing)
            │
            ├──► LLM (local LLamaSharp or online Ollama)
            ├──► Memory (SQLite: store + retrieve)
            ├──► Personality (trait evolution)
            ├──► Emotion (stimulus → vector update)
            └──► Agent (goal → plan → tool dispatch)
                    │
                    ▼
            Response (text + animation + voice)
```

## Key Design Decisions

1. **Steam-free** — تمام وابستگی‌های Steam حذف شده. `IsSteamUser = false` ثابت.
2. **Offline-first** — AI inference به‌صورت محلی از طریق LLamaSharp. Online mode optional.
3. **Mod-based** — تمام محتوای بازی از طریق سیستم mod بارگذاری می‌شود.
4. **Plugin architecture** — پلاگین‌ها از طریق `MainPlugin` base class بارگذاری می‌شوند.