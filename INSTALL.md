# راهنمای نصب صفر تا صد — VPet-Ultra v3.5.2

این راهنما از صفر تا اجرای کامل پت هوشمند با صدا رو توضیح می‌ده. مرحله به مرحله پیش برو.

---

## مرحله ۰: پیش‌نیازها

### ۰.۱ — Ollama (سرور LLM)

1. برو به **[ollama.com/download](https://ollama.com/download)**
2. نسخه Windows رو دانلود و نصب کن
3. بعد از نصب، Command Prompt رو باز کن و مدل رو بکش:
   ```powershell
   ollama pull qwen2.5:3b
   ```
   (این حدود ۲ گیگابایت دانلود می‌کنه — یکبار)
4. تست کن:
   ```powershell
   ollama list
   ```
   باید `qwen2.5:3b` رو نشون بده.

> **نکته:** Ollama به‌صورت خودکار تو بک‌گراند اجرا میشه. نیازی به دستی اجرا کردن نیست.

### ۰.۲ — .NET 8 Runtime (معمولاً داخل پکیج هست)

اگه برنامه خطای `.NET` داد:

1. برو به **[dotnet.microsoft.com/download/dotnet/8.0](https://dotnet.microsoft.com/download/dotnet/8.0)**
2. **.NET Desktop Runtime 8.0.x** برای Windows رو دانلود کن
3. نصب کن

---

## مرحله ۱: دانلود VPet-Ultra

1. برو به **[github.com/Amirwopi/VPet-Ultra/releases](https://github.com/Amirwopi/VPet-Ultra/releases)**
2. آخرین نسخه `VPet-Ultra-Full.zip` رو دانلود کن (~۹۲۰ مگابایت)
3. فایل zip رو از حالت فشرده خارج کن (مثلاً تو `C:\VPet-Ultra\`)
4. ساختار پوشه‌ها باید این‌طور باشه:
   ```
   C:\VPet-Ultra\
   ├── VPet-Simulator.Windows.exe
   ├── appsettings.json
   ├── mod\
   │   ├── 0000_core\        (داده‌های بازی)
   │   └── 0000_cognition\   (پلاگین AI)
   ├── transcribe\            (ابزار تشخیص صدا)
   ├── models\
   │   ├── whisper\           (اینجا مدل صدا بذار)
   │   └── piper\             (اینجا مدل TTS بذار)
   └── ...
   ```

---

## مرحله ۲: دانلود مدل‌های AI

برنامه خودش مدل‌ها رو نداره. باید جداگانه دانلود کنی:

### ۲.۱ — مدل تشخیص صدا (Whisper STT) — اجباری برای صدا

| فایل | حجم | لینک دانلود مستقیم | محل قرارگیری |
|---|---|---|---|
| **Whisper Medium Q8** | ~793 MB | **[⬇️ ggml-medium-q8_0.bin](https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-medium-q8_0.bin)** | `models/whisper/whisper-medium-Q8_0.gguf` |

**دستورالعمل:**
1. از لینک بالا فایل `ggml-medium-q8_0.bin` رو دانلود کن
2. اسم فایل رو به `whisper-medium-Q8_0.gguf` تغییر بده
3. توی پوشه `models/whisper/` بذارش

### ۲.۲ — مدل تبدیل صدا به متن (Piper TTS) — اجباری برای صدا

| فایل | حجم | لینک دانلود مستقیم | محل قرارگیری |
|---|---|---|---|
| **Piper Persian Voice** | ~63 MB | **[⬇️ fa_IR-mana-medium.onnx](https://huggingface.co/rhasspy/piper-voices/resolve/v1.0.3/fa/ir/mana/medium/fa_IR-mana-medium.onnx)** | `models/piper/fa_IR-mana-medium.onnx` |
| **Piper Voice Config** | ~1 KB | **[⬇️ fa_IR-mana-medium.onnx.json](https://huggingface.co/rhasspy/piper-voices/resolve/v1.0.3/fa/ir/mana/medium/fa_IR-mana-medium.onnx.json)** | `models/piper/fa_IR-mana-medium.onnx.json` |

**دستورالعمل:**
1. هر دو فایل رو از لینک‌های بالا دانلود کن
2. توی پوشه `models/piper/` بذار

### ۲.۳ — مدل LLM (مغز پت) — از طریق Ollama

این مدل رو Ollama مدیریت می‌کنه. تو مرحله ۰.۱ با `ollama pull qwen2.5:3b` دانلودش کردی.

> **اگه RAM قوی داری (۸+ گیگابایت):** می‌تونی مدل بهتر بکشی:
> ```powershell
> ollama pull qwen2.5:7b
> ```
> و تو `appsettings.json` مدل رو به `qwen2.5:7b` تغییر بده.

### ۲.۴ — مدل Embedding (اختیاری — برای حافظه بهتر)

| فایل | حجم | لینک دانلود مستقیم | محل قرارگیری |
|---|---|---|---|
| **BGE Small EN** | ~130 MB | **[⬇️ model.gguf](https://huggingface.co/BAAI/bge-small-en-v1.5/resolve/main/model.gguf)** | `models/bge-small.onnx` |

> **اختیاری:** بدون این مدل هم حافظه کار می‌کنه، ولی با embedding بهتر جستجو می‌کنه.

---

## مرحله ۳: بررسی appsettings.json

فایل `appsettings.json` رو با Notepad باز کن. باید این‌طور باشه:

```json
{
  "Cognition": {
    "Enabled": true,
    "Llm": {
      "Provider": "ollama",
      "Endpoint": "http://localhost:11434",
      "Model": "qwen2.5:3b",
      "MaxTokens": 1024,
      "Temperature": 0.7
    },
    "Voice": {
      "Enabled": true,
      "Stt": {
        "Provider": "handy",
        "ExecutablePath": "transcribe/vpet-transcribe.exe",
        "ModelPath": "models/whisper/whisper-medium-Q8_0.gguf",
        "Language": "fa"
      },
      "Tts": {
        "Provider": "piper",
        "ExecutablePath": "piper/piper/piper.exe",
        "ModelPath": "models/piper/fa_IR-mana-medium.onnx"
      }
    }
  }
}
```

> **نکته:** اگه مدل 7B خواستی، `"Model": "qwen2.5:7b"` کن.

---

## مرحله ۴: اجرای پت

1. `VPet-Simulator.Windows.exe` رو اجرا کن
2. پت روی صفحه ظاهر میشه
3. روی پت راست‌کلیک کن → منو باز میشه
4. دکمه **Chat** رو تو toolbar پیدا کن
5. کلیک کن → پنجره گفتگو باز میشه

### چت متنی
- بنویس: `سلام` و Enter بزن
- پت جواب میده

### چت صوتی (Push-to-Talk)
- کلید **Alt+Q** رو نگه دار (یك بيب ميشه — شروع ضبط)
- صحبت کن (فارسی یا انگلیسی)
- کلید رو رها کن (یك بيب ميشه — ارسال)
- پت جواب میده و صحبت می‌كنه

> **نکته:** اولین جواب ممکنه چند ثانیه طول بکشه چون مدل داره لود میشه.

---

## مرحله ۵: تست عملکرد

| تست | چطور | نتیجه مورد انتظار |
|---|---|---|
| گفتگو | بنویس `سلام` | پت جواب میده |
| حافظه | بگو `اسم من امیر است`، بعد بپرس `اسم من چیه؟` | پت یادش میاد |
| شخصیت | چند بار حرف بزن | سبک جواب عوض میشه |
| صدا | Alt+Q رو نگه دار و حرف بزن | پت میشنوه و جواب میده |
| احساس | بگو `خوشحالم` | پت خوشحال میشه (انیمیشن عوض میشه) |

---

## مرحله ۶: حل مشکل (Troubleshooting)

### پت جواب نمیده

1. **Ollama در حال اجراست؟** Task Manager رو چک کن. `ollama.exe` باید تو پروسه‌ها باشه.
2. **مدل Ollama نصبه؟** `ollama list` بزن. باید `qwen2.5:3b` رو نشون بده.
3. **لاگ‌ها رو چک کن:** پوشه `logs/` رو باز کن. فایل `cognition-*.log` رو بخون.

### صدا کار نمیکنه

1. **مدل Whisper هست؟** فایل `models/whisper/whisper-medium-Q8_0.gguf` رو چک کن (حدود 793 مگابایت باید باشه).
2. **مدل Piper هست؟** فایل `models/piper/fa_IR-mana-medium.onnx` رو چک کن (حدود 63 مگابایت باید باشه).
3. **فایل config Piper هست؟** فایل `models/piper/fa_IR-mana-medium.onnx.json` رو چک کن.
4. **transcribe/vpet-transcribe.exe هست؟** چک کن که فایل اجرایی وجود داره.
5. **میکروفون وصل هست؟** ویندوز → Settings → Privacy → Microphone → دسترسی رو چک کن.

### پت اصلاً باز نمیشه

1. **.NET 8 نصبه؟** `dotnet --version` بزن. اگه خطا داد، از [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/8.0) نصب کن.
2. **DLL‌ها هستن؟** پوشه `mod/0000_cognition/` رو چک کن. باید چندتا `.dll` باشه.
3. **appsettings.json درسته؟** JSON رو با [jsonlint.com](https://jsonlint.com) اعتبارسنجی کن.

### جواب‌ها خیلی کندن

1. **مدل کوچک‌تر:** `ollama pull qwen2.5:1.5b` و تو appsettings.json `"Model": "qwen2.5:1.5b"` کن.
2. **MaxTokens کم کن:** از 1024 به 512 تغییر بده.
3. **ContextSize کم کن:** از 4096 به 2048 تغییر بده.

### حافظه کار نمیکنه

1. پوشه `data/` وجود داره؟
2. فایل `cognition.db` ساخته شده؟
3. اگه نه، دستی بساز:
   ```powershell
   mkdir data
   ```

---

## مرحله ۷: شخصی‌سازی

### عوض کردن اسم پت

تو `appsettings.json`:
```json
"Personality": {
  "Seed": {
    "Name": "Luna",
    "Description": "A friendly moon companion."
  }
}
```

### عوض کردن زبان تشخیص صدا

تو `appsettings.json`، تو بخش STT:
```json
"Language": "fa"    // فارسی
"Language": "en"    // انگلیسی
"Language": "auto"  // خودکار
```

### عوض کردن سرعت جواب

```json
"Llm": {
  "Temperature": 0.5   // دقیق‌تر
  "Temperature": 0.9   // خلاقانه‌تر
}
```

---

## خلاصه سریع (۵ مرحله)

```
1. Ollama نصب کن → ollama.com/download
2. مدل LLM بکش → ollama pull qwen2.5:3b
3. VPet-Ultra-Full.zip رو دانلود و از حالت فشرده خارج کن
4. مدل‌های صدا دانلود کن:
   - models/whisper/whisper-medium-Q8_0.gguf (از HuggingFace)
   - models/piper/fa_IR-mana-medium.onnx (از HuggingFace)
5. VPet-Simulator.Windows.exe رو اجرا کن → Chat → Alt+Q → حرف بزن!
```

**لینک‌های دانلود مستقیم:**
- [Ollama](https://ollama.com/download)
- [Whisper Medium Q8](https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-medium-q8_0.bin)
- [Piper Persian Voice](https://huggingface.co/rhasspy/piper-voices/resolve/v1.0.3/fa/ir/mana/medium/fa_IR-mana-medium.onnx)
- [Piper Voice Config](https://huggingface.co/rhasspy/piper-voices/resolve/v1.0.3/fa/ir/mana/medium/fa_IR-mana-medium.onnx.json)
- [.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)