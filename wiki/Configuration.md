# Configuration

فایل `appsettings.json` کنار `VPet-Simulator.Windows.exe` قرار می‌گیرد.

## Full Configuration

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
      "EnableVision": false,
      "MaxTokens": 1024,
      "Temperature": 0.7,
      "Endpoint": "http://localhost:11434",
      "ApiKey": ""
    },
    "Memory": {
      "DbPath": "data/cognition.db"
    },
    "Voice": {
      "Enabled": false,
      "Stt": {
        "Provider": "whisper",
        "ExecutablePath": "whisper-cli.exe",
        "ModelPath": "models/ggml-base.bin",
        "Language": "auto",
        "Threads": 4
      },
      "Tts": {
        "Provider": "piper",
        "ExecutablePath": "piper.exe",
        "ModelPath": "models/en_US-amy-medium.onnx"
      }
    }
  }
}
```

## Settings

### Cognition.Enabled
- `true` — AI فعال است (پلاگین cognition بارگذاری می‌شود)
- `false` — فقط pet بدون AI

### Llm.Provider
- `"local"` — LLamaSharp (آفلاین، GGUF model)
- `"online"` — Ollama یا OpenAI-compatible endpoint

### Llm.Model
نام مدل GGUF (بدون پسوند `.gguf`). مثال: `qwen2.5-3b-instruct-q4_k_m`

### Llm.GpuLayerCount
- `0` — CPU-only (کندتر ولی بدون GPU نیاز)
- `20` یا بالاتر — GPU acceleration (نیاز به CUDA)

### Llm.ContextSize
بازه‌ی context (token). بالاتر = حافظه‌ی بیشتر ولی RAM بیشتر. پیش‌فرض: `4096`

### Llm.Temperature
خلاقیت پاسخ. `0.0` = دقیق، `1.0` = خلاقانه. پیش‌فرض: `0.7`

### Llm.EnableVision
- `true` — پشتیبانی از vision (LLaVA/SmolVLM). RAM بیشتر نیاز است.
- `false` — text-only (پیش‌فرض)

### Memory.DbPath
مسیر فایل SQLite برای memory. پیش‌فرض: `data/cognition.db`

### Voice.Enabled
- `true` — STT + TTS فعال
- `false` — بدون voice (پیش‌فرض)

## Online Mode (Ollama)

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

## Online Mode (OpenAI-compatible)

```json
{
  "Cognition": {
    "Enabled": true,
    "Llm": {
      "Provider": "online",
      "Endpoint": "https://api.openai.com/v1",
      "ApiKey": "sk-...",
      "Model": "gpt-4o-mini"
    }
  }
}
```

> **هشدار:** API key در فایل `appsettings.json` به‌صورت plaintext ذخیره می‌شود. این فایل را در git commit نکنید.