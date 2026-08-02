# مدل‌های AI — راهنمای دانلود و نصب

این فایل لیست کامل مدل‌های AI مورد نیاز VPet-Ultra رو داره. هر مدل رو جداگانه دانلود کن و تو مسیر درست بذار.

> **مهم:** VPet-Ultra به صورت پیش‌فرض مدل‌ها رو **داخل خودش** اجرا می‌کنه (با LLamaSharp). نیازی به Ollama نیست! Ollama فقط یه حالت جایگزین هست.

---

## ۱. مدل زبانی (LLM) — اجرای محلی با LLamaSharp

این مدل مغز پت هست — گفتگو، استدلال، و تولید جواب. مدل به صورت فایل `.gguf` دانلود میشه و داخل برنامه اجرا میشه.

### نصب (حالت محلی — پیشنهادی)

1. **مدل Qwen2.5-3B رو دانلود کن:**
   - برو به https://huggingface.co/Qwen/Qwen2.5-3B-Instruct-GGUF/tree/main
   - فایل `qwen2.5-3b-instruct-q4_k_m.gguf` رو دانلود کن (~۲ گیگابایت)

2. **فایل رو تو پوشه models بذار:**
   ```
   VPet/
   ├── models/
   │   └── qwen2.5-3b-instruct-q4_k_m.gguf
   ```

3. **تو `appsettings.json`:**
   ```json
   "Llm": {
     "Provider": "local",
     "Model": "qwen2.5-3b-instruct-q4_k_m",
     "ModelsDirectory": "models",
     "GpuLayerCount": 0,
     "ContextSize": 4096
   }
   ```

### مدل‌های متن جایگزین

| مدل | حجم | کیفیت | سرعت | RAM لازم |
|---|---|---|---|---|
| `qwen2.5-1.5b-instruct-q4_k_m` | ~۱GB | پایین | خیلی سریع | ۲GB |
| `qwen2.5-3b-instruct-q4_k_m` | ~۲GB | متوسط | سریع ✅ پیشنهادی | ۴GB |
| `qwen2.5-7b-instruct-q4_k_m` | ~۴.۵GB | خوب | متوسط | ۸GB |
| `llama-3.2-3b-instruct-q4_k_m` | ~۲GB | متوسط | سریع | ۴GB |

> **لینک‌های دانلود:**
> - Qwen2.5: https://huggingface.co/Qwen/Qwen2.5-3B-Instruct-GGUF
> - Llama 3.2: https://huggingface.co/meta-llama/Llama-3.2-3B-Instruct-GGUF

### GPU Acceleration (کارت گرافیک NVIDIA)

اگه GPU داری، می‌تونی سرعت رو چند برابر کنی:

1. پکیج CUDA backend رو نصب کن:
   ```powershell
   dotnet add VPet.Cognition.Inference package LLamaSharp.Backend.Cuda12
   ```

2. تو `appsettings.json`:
   ```json
   "GpuLayerCount": 25
   ```

| VRAM | GpuLayerCount پیشنهادی |
|---|---|
| ۴GB | 10-15 |
| ۸GB | 20-30 |
| ۱۲GB+ | 35+ |

> بدون GPU هم روی CPU کار می‌کنه، فقط کندتر (۳-۱۰ توکن در ثانیه).

### حالت آنلاین (Ollama) — اختیاری

اگه Ollama نصبه و می‌خوای ازش استفاده کنی:

```powershell
ollama pull qwen2.5:3b
```

```json
"Llm": {
  "Provider": "ollama",
  "Endpoint": "http://localhost:11434",
  "Model": "qwen2.5:3b"
}
```

### حالت ابری (OpenAI-compatible) — اختیاری

| سرویس | Endpoint | مدل |
|---|---|---|
| DeepSeek | `https://api.deepseek.com/v1/` | `deepseek-chat` |
| Zhipu (GLM) | `https://open.bigmodel.cn/api/paas/v4/` | `glm-4-flash` |
| Qwen (DashScope) | `https://dashscope.aliyuncs.com/compatible-mode/v1/` | `qwen-turbo` |
| OpenAI | `https://api.openai.com/v1/` | `gpt-4o-mini` |

```json
"Llm": {
  "Provider": "openai-compatible",
  "Endpoint": "https://api.deepseek.com/v1/",
  "Model": "deepseek-chat",
  "ApiKey": "کلید-API-اینجا"
}
```

---

## ۲. مدل Vision (بینایی) — اختیاری

اگه می‌خوای پت بتونه عکس ببینه و توضیح بده:

| مدل | حجم | RAM لازم | لینک |
|---|---|---|---|
| `smolvlm-500m-instruct-q4_k_m` | ~۵۰۰MB | ۴GB | https://huggingface.co/ggml-org/smolvlm-500m-instruct-gguf |
| `smolvlm-2.2b-instruct-q4_k_m` | ~۱.۵GB | ۶GB | https://huggingface.co/ggml-org/smolvlm-2.2b-instruct-gguf |
| `llava-onevision-qwen2-7b-q4_k_m` | ~۴.۵GB | ۱۰GB | https://huggingface.co/lmms-lab/llava-onevision-qwen2-7b-gguf |

تو `appsettings.json`:
```json
"EnableVision": true
```

> **هشدار:** مدل Vision حداقل ۴ گیگابایت RAM اضافه نیاز داره. اگه RAM کم داری، فعال نکن.

---

## ۳. مدل Embedding (حافظه) — اختیاری

برای جستجوی بهتر در حافظه:

| مدل | حجم | لینک |
|---|---|---|
| `bge-small-en-v1.5` | ~۱۳۰MB | https://huggingface.co/BAAI/bge-small-en-v1.5 |
| `bge-m3` | ~۱.۲GB | https://huggingface.co/BAAI/bge-m3 |

> **اختیاری:** بدون این مدل هم حافظه کار می‌کنه، ولی با embedding بهتر جستجو می‌کنه.

---

## ۴. مدل تشخیص گفتار (STT) — Whisper

این مدل صدای تو رو به متن تبدیل می‌کنه.

### نصب whisper.cpp

1. **دانلود whisper.cpp:**
   - برو به https://github.com/ggerganov/whisper.cpp/releases
   - آخرین نسخه `whisper-bin-x64.zip` رو دانلود کن
   - محتویات رو تو پوشه `whisper/` کنار VPet اکسترکت کن

2. **دانلود مدل:**

   | مدل | حجم | کیفیت | سرعت |
   |---|---|---|---|
   | `ggml-tiny.bin` | ۷۷MB | پایین | خیلی سریع |
   | `ggml-base.en.bin` | ۱۴۲MB | متوسط (انگلیسی) | سریع |
   | `ggml-base.bin` | ۱۴۲MB | متوسط (چندزبانه) | سریع ✅ پیشنهادی |
   | `ggml-small.bin` | ۴۶۶MB | خوب | متوسط |
   | `ggml-medium.bin` | ۱.۵GB | عالی | کند |

   ```powershell
   # با git:
   git clone https://github.com/ggerganov/whisper.cpp
   cd whisper.cpp
   bash models/download-ggml-model.sh base

   # یا دستی:
   # دانلود از: https://huggingface.co/ggerganov/whisper.cpp
   ```

3. **فایل‌ها رو تو مسیر درست بذار:**
   ```
   VPet/
   ├── whisper-cli.exe              ← از release دانلود کن
   ├── models/
   │   └── ggml-base.bin            ← مدل Whisper
   ```

4. **تو `appsettings.json`:**
   ```json
   "Stt": {
     "Provider": "whisper",
     "ExecutablePath": "whisper-cli.exe",
     "ModelPath": "models/ggml-base.bin",
     "Language": "fa",
     "Threads": 4
   }
   ```

   > **نکته:** برای فارسی `Language` رو `"fa"` بذار. برای انگلیسی `"en"`. برای خودکار `"auto"`.

---

## ۵. مدل تبدیل متن به گفتار (TTS) — Piper

این مدل جواب پت رو به صدا تبدیل می‌کنه.

### نصب Piper

1. **دانلود Piper:**
   - برو به https://github.com/rhasspy/piper/releases
   - `piper_windows_amd64.zip` رو دانلود کن
   - `piper.exe` رو تو پوشه `piper/` کنار VPet اکسترکت کن

2. **دانلود مدل صوتی:**
   - برو به https://huggingface.co/rhasspy/piper-voices/tree/main
   - زبان مورد نظر رو انتخاب کن (مثلا `fa/` برای فارسی)
   - یه مدل `.onnx` و فایل `.onnx.json` هم‌نامش رو دانلود کن

   **مدل‌های پیشنهادی:**

   | زبان | مدل | حجم |
   |---|---|---|
   | انگلیسی | `en_US-amy-medium.onnx` | ~۶۰MB |
   | انگلیسی | `en_US-lessac-medium.onnx` | ~۶۰MB |
   | فارسی | `fa_IR-amir-medium.onnx` | ~۶۰MB |
   | فارسی | `fa_IR-gyro-medium.onnx` | ~۶۰MB |

   > **نکته:** مدل فارسی Piper ممکنه کیفیت پایینی داشته باشه. برای کیفیت بهتر از Kokoro استفاده کن.

3. **فایل‌ها رو تو مسیر درست بذار:**
   ```
   VPet/
   ├── piper.exe                    ← از release دانلود کن
   ├── models/
   │   └── en_US-amy-medium.onnx    ← مدل صوتی
   │   └── en_US-amy-medium.onnx.json
   ```

4. **تو `appsettings.json`:**
   ```json
   "Tts": {
     "Provider": "piper",
     "ExecutablePath": "piper.exe",
     "ModelPath": "models/en_US-amy-medium.onnx"
   }
   ```

---

## ۶. مدل تبدیل متن به گفتار (TTS) — Kokoro (اختیاری)

Kokoro کیفیت بالاتری داره ولی نصبش کمی پیچیده‌تره.

### نصب Kokoro

1. **دانلود Kokoro:**
   - برو به https://github.com/kokoro-ons/kokoro/releases
   - نسخه Windows رو دانلود کن
   - `kokoro.exe` رو تو پوشه `kokoro/` کنار VPet اکسترکت کن

2. **دانلود مدل:**
   - مدل `kokoro.onnx` رو از صفحه releases دانلود کن
   - تو پوشه `models/` بذار

3. **تو `appsettings.json`:**
   ```json
   "Tts": {
     "Provider": "kokoro",
     "ExecutablePath": "kokoro.exe",
     "ModelPath": "models/kokoro.onnx",
     "KokoroVoice": "af_heart",
     "KokoroSpeed": 1.0
   }
   ```

---

## ۷. ساختار پوشه‌های نهایی

بعد از نصب همه مدل‌ها، ساختار پوشه‌ها باید اینطوری باشه:

```
VPet/
├── VPet-Simulator.Windows.exe      ← برنامه اصلی پت
├── appsettings.json                ← تنظیمات cognition
├── mod/
│   └── 0000_cognition/
│       ├── VPet.Cognition.Host.dll ← پلاگین cognition
│       ├── VPet.Cognition.Inference.dll ← موتور اجرای محلی
│       ├── VPet.Cognition.LLM.dll
│       ├── VPet.Cognition.Memory.dll
│       ├── VPet.Cognition.Personality.dll
│       ├── VPet.Cognition.Voice.dll
│       ├── VPet.Cognition.Agent.dll
│       ├── VPet.Cognition.Security.dll
│       ├── VPet.Cognition.Wpf.dll
│       ├── llama.dll               ← کتابخانه LLamaSharp
│       └── ...                     ← سایر DLL‌های وابسته
├── models/                          ← مدل‌های AI
│   ├── qwen2.5-3b-instruct-q4_k_m.gguf  ← مدل LLM (الزامی)
│   ├── ggml-base.bin               ← مدل Whisper (STT)
│   ├── en_US-amy-medium.onnx       ← مدل Piper (TTS)
│   └── en_US-amy-medium.onnx.json
├── whisper-cli.exe                  ← اجرایی Whisper
├── piper.exe                        ← اجرایی Piper
├── data/
│   └── cognition.db                 ← حافظه پت (خودکار ساخته میشه)
└── logs/
    └── cognition-20260801.log       ← لاگ‌ها (خودکار ساخته میشه)
```

---

## ۸. خلاصه سریع

| قابلیت | مدل | از کجا دانلود کن | حجم |
|---|---|---|---|
| **گفتگو (الزامی)** | Qwen2.5-3B GGUF | https://huggingface.co/Qwen/Qwen2.5-3B-Instruct-GGUF | ~۲GB |
| **بینایی (اختیاری)** | SmolVLM-500M | https://huggingface.co/ggml-org/smolvlm-500m-instruct-gguf | ~۵۰۰MB |
| **حافظه (اختیاری)** | BGE-Small | https://huggingface.co/BAAI/bge-small-en-v1.5 | ~۱۳۰MB |
| **تشخیص صدا (اختیاری)** | Whisper base | https://github.com/ggerganov/whisper.cpp/releases | ~۱۴۲MB |
| **صدا دادن (اختیاری)** | Piper | https://github.com/rhasspy/piper/releases | ~۶۰MB |
| **صدا دادن (جایگزین)** | Kokoro | https://github.com/kokoro-ons/kokoro/releases | ~۱۰۰MB |

> **حداقل نصب:** فقط دانلود `qwen2.5-3b-instruct-q4_k_m.gguf` و گذاشتن تو پوشه `models/` کافیه برای گفتجوی متنی. نیازی به Ollama یا سرور نیست!