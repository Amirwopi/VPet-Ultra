# AI Models — مدل‌های مورد نیاز

VPet-Ultra از چند مدل AI استفاده می‌کنه. برنامه خودش مدل‌ها رو نداره — باید جداگانه دانلود کنی.

---

## مدل‌های مورد نیاز

### ۱. مدل LLM (مغز پت) — از طریق Ollama

| مدل | دستور | حجم | RAM | کیفیت |
|---|---|---|---|---|
| **Qwen2.5:3b** (پیشنهادی) | `ollama pull qwen2.5:3b` | ~2 GB | 4 GB | خوب |
| Qwen2.5:7b | `ollama pull qwen2.5:7b` | ~4.5 GB | 8 GB | بهتر |
| Qwen2.5:1.5b (سبک) | `ollama pull qwen2.5:1.5b` | ~1 GB | 2 GB | متوسط |
| Llama3.2:3b | `ollama pull llama3.2:3b` | ~2 GB | 4 GB | خوب |

**نصب:**
1. Ollama رو از [ollama.com/download](https://ollama.com/download) نصب کن
2. Command Prompt رو باز کن:
   ```powershell
   ollama pull qwen2.5:3b
   ```
3. تو `appsettings.json`:
   ```json
   "Llm": {
     "Provider": "ollama",
     "Endpoint": "http://localhost:11434",
     "Model": "qwen2.5:3b"
   }
   ```

---

### ۲. مدل تشخیص صدا (STT — Whisper)

| مدل | حجم | لینک دانلود مستقیم | محل قرارگیری |
|---|---|---|---|
| **Whisper Medium Q8** (پیشنهادی) | ~793 MB | **[⬇️ ggml-medium-q8_0.bin](https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-medium-q8_0.bin)** | `models/whisper/whisper-medium-Q8_0.gguf` |
| Whisper Small Q5 | ~300 MB | **[⬇️ ggml-small-q5_1.bin](https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small-q5_1.bin)** | `models/whisper/whisper-small-q5_1.gguf` |
| Whisper Tiny Q5 | ~75 MB | **[⬇️ ggml-tiny-q5_1.bin](https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-tiny-q5_1.bin)** | `models/whisper/whisper-tiny-q5_1.gguf` |

**نصب:**
1. فایل رو از لینک مستقیم بالا دانلود کن
2. اسم فایل رو به فرمت `.gguf` تغییر بده (مثلاً `whisper-medium-Q8_0.gguf`)
3. توی پوشه `models/whisper/` بذار
4. تو `appsettings.json`:
   ```json
   "Stt": {
     "Provider": "handy",
     "ExecutablePath": "transcribe/vpet-transcribe.exe",
     "ModelPath": "models/whisper/whisper-medium-Q8_0.gguf",
     "Language": "fa"
   }
   ```

> **نکته:** مدل Medium برای فارسی بهترین نتیجه رو میده. مدل Tiny سریع‌تره ولی دقتش کمتره.

---

### ۳. مدل تبدیل متن به صدا (TTS — Piper)

| مدل | حجم | لینک دانلود مستقیم | محل قرارگیری |
|---|---|---|---|
| **Piper Persian (Mana)** | ~63 MB | **[⬇️ fa_IR-mana-medium.onnx](https://huggingface.co/rhasspy/piper-voices/resolve/v1.0.3/fa/ir/mana/medium/fa_IR-mana-medium.onnx)** | `models/piper/fa_IR-mana-medium.onnx` |
| **Piper Config** | ~1 KB | **[⬇️ fa_IR-mana-medium.onnx.json](https://huggingface.co/rhasspy/piper-voices/resolve/v1.0.3/fa/ir/mana/medium/fa_IR-mana-medium.onnx.json)** | `models/piper/fa_IR-mana-medium.onnx.json` |

**نصب:**
1. هر دو فایل رو از لینک‌های مستقیم بالا دانلود کن
2. توی پوشه `models/piper/` بذار
3. تو `appsettings.json`:
   ```json
   "Tts": {
     "Provider": "piper",
     "ExecutablePath": "piper/piper/piper.exe",
     "ModelPath": "models/piper/fa_IR-mana-medium.onnx"
   }
   ```

> **نکته:** فایل `.onnx.json` کنار فایل `.onnx` باید باشه، وگرنه Piper کار نمی‌کنه.

#### سایر زبان‌های Piper

برای زبان‌های دیگه، به [rhasspy/piper-voices](https://huggingface.co/rhasspy/piper-voices/tree/v1.0.3) برو و مدل مورد نظر رو دانلود کن.

---

### ۴. مدل Embedding (اختیاری — برای حافظه بهتر)

| مدل | حجم | لینک دانلود مستقیم | محل قرارگیری |
|---|---|---|---|
| **BGE Small EN** | ~130 MB | **[⬇️ model.gguf](https://huggingface.co/BAAI/bge-small-en-v1.5/resolve/main/model.gguf)** | `models/bge-small.onnx` |

> **اختیاری:** بدون این مدل هم حافظه کار می‌کنه، ولی با embedding بهتر جستجو می‌کنه.

---

## ساختار پوشه مدل‌ها

```
VPet-Ultra/
├── models/
│   ├── whisper/
│   │   └── whisper-medium-Q8_0.gguf       # مدل تشخیص صدا
│   ├── piper/
│   │   ├── fa_IR-mana-medium.onnx         # مدل تبدیل متن به صدا
│   │   └── fa_IR-mana-medium.onnx.json    # تنظیمات مدل صدا
│   └── bge-small.onnx                     # مدل embedding (اختیاری)
├── transcribe/
│   └── vpet-transcribe.exe               # ابزار تشخیص صدا
├── piper/
│   └── piper/
│       └── piper.exe                      # ابزار تبدیل متن به صدا
├── mod/
│   ├── 0000_core/                         # داده‌های بازی
│   └── 0000_cognition/                    # پلاگین AI
├── appsettings.json                       # تنظیمات
└── VPet-Simulator.Windows.exe             # برنامه اصلی
```

---

## عوض کردن مدل‌ها

### عوض کردن مدل LLM

1. مدل جدید رو با Ollama بکش: `ollama pull qwen2.5:7b`
2. تو `appsettings.json`:
   ```json
   "Model": "qwen2.5:7b"
   ```
3. VPet رو ری‌استارت کن

### عوض کردن مدل Whisper

1. فایل جدید رو توی `models/whisper/` بذار
2. تو `appsettings.json` مسیر `ModelPath` رو عوض کن
3. VPet رو ری‌استارت کن

### عوض کردن مدل Piper

1. فایل‌های `.onnx` و `.onnx.json` جدید رو توی `models/piper/` بذار
2. تو `appsettings.json` مسیر `ModelPath` رو عوض کن
3. VPet رو ری‌استارت کن

---

## حالت Local (بدون Ollama)

اگه نمی‌خوای از Ollama استفاده کنی، می‌تونی مدل GGUF مستقیم داخل برنامه اجرا کنی (با LLamaSharp):

1. مدل GGUF رو از HuggingFace دانلود کن:
   - **[Qwen2.5-3B-Instruct GGUF](https://huggingface.co/Qwen/Qwen2.5-3B-Instruct-GGUF/resolve/main/qwen2.5-3b-instruct-q4_k_m.gguf)** (~2 GB)
2. توی پوشه `models/` بذار
3. تو `appsettings.json`:
   ```json
   "Llm": {
     "Provider": "local",
     "Model": "qwen2.5-3b-instruct-q4_k_m",
     "ModelsDirectory": "models",
     "GpuLayerCount": 0,
     "ContextSize": 4096
   }
   ```

> **نکته:** حالت Ollama ساده‌تره و مدیریت مدل آسون‌تره. حالت Local برای وقتیه که نمی‌خوای Ollama نصب کنی.

---

## GPU Acceleration

### Ollama GPU

Ollama خودش به‌صورت خودکار از GPU استفاده می‌کنه اگه CUDA نصب باشه. نیازی به تنظیم نیست.

### Local (LLamaSharp) GPU

اگه کارت گرافیک NVIDIA داری و از حالت Local استفاده می‌کنی:

1. پکیج CUDA backend رو نصب کن:
   ```powershell
   dotnet add VPet.Cognition.Inference package LLamaSharp.Backend.Cuda12
   ```
2. تو `appsettings.json`:
   ```json
   "GpuLayerCount": 25
   ```
   (بسته به VRAM تنظیم کن — 25 یعنی 25 لایه روی GPU)

> بدون GPU هم روی CPU کار می‌کنه، فقط کندتر (~5-15 توکن بر ثانیه).

---

## خلاصه لینک‌های دانلود

| چیزی | لینک مستقیم |
|---|---|
| **Ollama** | [ollama.com/download](https://ollama.com/download) |
| **Whisper Medium Q8** | [ggml-medium-q8_0.bin](https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-medium-q8_0.bin) |
| **Piper Persian Voice** | [fa_IR-mana-medium.onnx](https://huggingface.co/rhasspy/piper-voices/resolve/v1.0.3/fa/ir/mana/medium/fa_IR-mana-medium.onnx) |
| **Piper Voice Config** | [fa_IR-mana-medium.onnx.json](https://huggingface.co/rhasspy/piper-voices/resolve/v1.0.3/fa/ir/mana/medium/fa_IR-mana-medium.onnx.json) |
| **BGE Embedding** | [model.gguf](https://huggingface.co/BAAI/bge-small-en-v1.5/resolve/main/model.gguf) |
| **Qwen2.5-3B GGUF** | [qwen2.5-3b-instruct-q4_k_m.gguf](https://huggingface.co/Qwen/Qwen2.5-3B-Instruct-GGUF/resolve/main/qwen2.5-3b-instruct-q4_k_m.gguf) |
| **.NET 8 Runtime** | [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/8.0) |