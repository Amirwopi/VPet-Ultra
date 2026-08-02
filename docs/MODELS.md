# مدل‌های AI — راهنمای دانلود و نصب

این فایل لیست کامل مدل‌های AI مورد نیاز VPet-Ultra رو داره. هر مدل رو جداگانه دانلود کن و تو مسیر درست بذار.

---

## ۱. مدل زبانی (LLM) — Qwen2.5-3B

این مدل مغز پت هست — گفتگو، استدلال، و تولید جواب.

### نصب با Ollama (پیشنهادی — ساده‌ترین روش)

1. **Ollama رو نصب کن:**
   - برو به https://ollama.com/download
   - نسخه Windows رو دانلود و نصب کن

2. **مدل Qwen2.5-3B رو دانلود کن:**
   ```powershell
   ollama pull qwen2.5:3b
   ```
   این حدود ۲ گیگابایت دانلود می‌کنه. صبر کن تا تموم شه.

3. **تست کن که کار می‌کنه:**
   ```powershell
   ollama run qwen2.5:3b "سلام، چطوری؟"
   ```
   باید جواب بده.

### مدل‌های جایگزین (اختیاری)

اگه کیفیت بهتری خواستی، می‌تونی مدل‌های بزرگتر بکشی:

| مدل | حجم | کیفیت | سرعت |
|---|---|---|---|
| `qwen2.5:3b` | ~۲GB | متوسط | سریع ✅ پیشنهادی |
| `qwen2.5:7b` | ~۴.۷GB | خوب | متوسط |
| `qwen2.5:14b` | ~۹GB | عالی | کند |
| `llama3.2:3b` | ~۲GB | متوسط | سریع |
| `deepseek-r1:7b` | ~۴.۷GB | عالی (استدلال) | متوسط |

برای تعویض مدل:
```powershell
ollama pull qwen2.5:7b
```
بعد تو `appsettings.json`:
```json
"Llm": {
  "Provider": "ollama",
  "Model": "qwen2.5:7b"
}
```

### استفاده از مدل ابری (اختیاری)

اگه اینترنت داری و می‌خوای از مدل ابری استفاده کنی:

| سرویس | Endpoint | مدل |
|---|---|---|
| DeepSeek | `https://api.deepseek.com/v1/` | `deepseek-chat` |
| Zhipu (GLM) | `https://open.bigmodel.cn/api/paas/v4/` | `glm-4-flash` |
| Qwen (DashScope) | `https://dashscope.aliyuncs.com/compatible-mode/v1/` | `qwen-turbo` |
| OpenAI | `https://api.openai.com/v1/` | `gpt-4o-mini` |

تو `appsettings.json`:
```json
"Llm": {
  "Provider": "openai-compatible",
  "Endpoint": "https://api.deepseek.com/v1/",
  "Model": "deepseek-chat",
  "ApiKey": "کلید-API-اینجا"
}
```

---

## ۲. مدل تشخیص گفتار (STT) — Whisper

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
   VPet-Ultra/
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

## ۳. مدل تبدیل متن به گفتار (TTS) — Piper

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
   VPet-Ultra/
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

## ۴. مدل تبدیل متن به گفتار (TTS) — Kokoro (اختیاری)

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

## ۵. ساختار پوشه‌های نهایی

بعد از نصب همه مدل‌ها، ساختار پوشه‌ها باید اینطوری باشه:

```
VPet-Ultra/
├── VPet-Simulator.Windows.exe      ← برنامه اصلی پت
├── appsettings.json                ← تنظیمات cognition
├── mod/
│   └── 0000_cognition/
│       ├── VPet.Cognition.Host.dll ← پلاگین cognition
│       ├── VPet.Cognition.LLM.dll
│       ├── VPet.Cognition.Memory.dll
│       ├── VPet.Cognition.Personality.dll
│       ├── VPet.Cognition.Voice.dll
│       ├── VPet.Cognition.Agent.dll
│       ├── VPet.Cognition.Security.dll
│       └── VPet.Cognition.Wpf.dll
├── models/                          ← مدل‌های AI
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

## ۶. خلاصه سریع

| قابلیت | مدل | از کجا دانلود کن | حجم |
|---|---|---|---|
| **گفتگو (الزامی)** | Qwen2.5-3B | `ollama pull qwen2.5:3b` | ~۲GB |
| **تشخیص صدا (اختیاری)** | Whisper base | https://github.com/ggerganov/whisper.cpp/releases | ~۱۴۲MB |
| **صدا دادن (اختیاری)** | Piper | https://github.com/rhasspy/piper/releases | ~۶۰MB |
| **صدا دادن (جایگزین)** | Kokoro | https://github.com/kokoro-ons/kokoro/releases | ~۱۰۰MB |

> **حداقل نصب:** فقط Ollama + Qwen2.5-3B کافیه برای گفتجوی متنی. صدا اختیاریه.