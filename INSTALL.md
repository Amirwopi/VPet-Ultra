# راهنمای نصب صفر تا صد — VPet-Ultra

این راهنما از صفر تا اجرای کامل پت هوشمند رو توضیح می‌ده. مرحله به مرحله پیش برو.

---

## مرحله ۰: پیش‌نیازها

### ۰.۱ — .NET 8 SDK

1. برو به https://dotnet.microsoft.com/download/dotnet/8.0
2. **.NET SDK 8.0.x** برای Windows رو دانلود کن (نه Runtime، SDK رو)
3. نصب کن
4. تست کن:
   ```powershell
   dotnet --version
   ```
   باید `8.0.x` نشون بده.

### ۰.۲ — Git (برای کلون کردن)

```powershell
git --version
```
اگه نصب نیست، از https://git-scm.com دانلود کن.

---

## مرحله ۱: دانلود VPet-Ultra

### روش A: دانلود ZIP آماده (ساده‌ترین)

1. برو به https://github.com/Amirwopi/VPet-Ultra/releases
2. آخرین نسخه `VPet-Ultra-Cognition.zip` رو دانلود کن
3. محتویاتش رو کنار VPet اصلی اکسترکت کن
4. پوشه `mod/0000_cognition/` باید ساخته بشه
5. برو به **مرحله ۳**

### روش B: کلون و build دستی

```powershell
git clone https://github.com/Amirwopi/VPet-Ultra.git
cd VPet-Ultra
.\build-windows.ps1
```

اگه `build-windows.ps1` خطا داد:
```powershell
Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned
.\build-windows.ps1
```

خروجی تو پوشه `publish/vpet-cognition/` ساخته میشه.

---

## مرحله ۲: دانلود مدل AI (مغز پت)

VPet-Ultra به صورت پیش‌فرض مدل‌های AI رو **داخل خودش** اجرا می‌کنه (با LLamaSharp). نیازی به Ollama یا سرور جداگانه نیست!

### ۲.۱ — مدل متن (اجباری)

1. برو به https://huggingface.co/Qwen/Qwen2.5-3B-Instruct-GGUF/tree/main
2. فایل `qwen2.5-3b-instruct-q4_k_m.gguf` رو دانلود کن (~۲ گیگابایت)
3. تو پوشه `models/` کنار VPet قرار بده:
   ```
   VPet\
   ├── models\
   │   └── qwen2.5-3b-instruct-q4_k_m.gguf
   ├── mod\
   │   └── 0000_cognition\
   └── VPet-Simulator.Windows.exe
   ```

> **نکته:** اگه RAM/CPU قوی داری، می‌تونی مدل 7B رو دانلود کنی (بهتر ولی کندتر):
> `qwen2.5-7b-instruct-q4_k_m.gguf` (~۴.۵ گیگابایت)

### ۲.۲ — مدل Vision (اختیاری)

اگه می‌خوای پت بتونه عکس ببینه و توضیح بده:

1. برو به https://huggingface.co/lmms-lab/llava-onevision-qwen2-7b-gguf
2. فایل مدل رو دانلود کن و تو پوشه `models/` بذار
3. تو `appsettings.json`:
   ```json
   "EnableVision": true
   ```

> **هشدار:** مدل Vision حداقل ۸ گیگابایت RAM نیاز داره.

### ۲.۳ — مدل Embedding (برای حافظه بهتر)

1. برو به https://huggingface.co/BAAI/bge-small-en-v1.5
2. فایل `model.gguf` رو دانلود کن
3. تو پوشه `models/` بذار

> **اختیاری:** بدون این مدل هم حافظه کار می‌کنه، ولی با embedding بهتر جستجو می‌کنه.

### ۲.۴ — GPU Acceleration (اختیاری)

اگه کارت گرافیک NVIDIA داری:

1. پکیج CUDA backend رو نصب کن:
   ```powershell
   dotnet add VPet.Cognition.Inference package LLamaSharp.Backend.Cuda12
   ```
2. تو `appsettings.json`:
   ```json
   "GpuLayerCount": 25
   ```
   (عدد ۲۵ یعنی ۲۵ لایه مدل روی GPU اجرا میشه — بسته به VRAM تنظیم کن)

> بدون GPU هم روی CPU کار می‌کنه، فقط کندتر.

---

## مرحله ۳: تنظیم فایل config

1. برو به پوشه VPet-Ultra (جایی که `VPet-Simulator.Windows.exe` هست)
2. فایل `appsettings.json` رو با Notepad باز کن
3. تنظیمات پیش‌فرض برای اجرای محلی (LLamaSharp) اینه:

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
         "Temperature": 0.7
       }
     }
   }
   ```

4. اگه مدل 7B خواستی:
   ```json
   "Model": "qwen2.5-7b-instruct-q4_k_m"
   ```

5. فایل رو ذخیره کن.

### حالت آنلاین (Ollama) — اختیاری

اگه Ollama نصبه و می‌خوای ازش استفاده کنی:
```json
{
  "Cognition": {
    "Llm": {
      "Provider": "ollama",
      "Endpoint": "http://localhost:11434",
      "Model": "qwen2.5:3b"
    }
  }
}
```

---

## مرحله ۴: (اختیاری) نصب صدا

اگه می‌خوای پت صدا داشته باشه (تشخیص صدا + صحبت کردن)، برو به `docs/MODELS.md` و مدل‌های Whisper و Piper رو دانلود کن.

اگه صدا نمی‌خوای، این مرحله رو رد کن. پت فقط با متن کار می‌کنه.

---

## مرحله ۵: اجرای پت

1. `VPet-Simulator.Windows.exe` رو اجرا کن
2. پت روی صفحه ظاهر میشه
3. روی پت راست‌کلیک کن → منو باز میشه
4. دکمه **Chat** رو تو toolbar پیدا کن (یا تو منوی راست‌کلیک)
5. کلیک کن → پنجره گفتگو باز میشه
6. بنویس: `سلام` و Enter بزن
7. پت باید جواب بده! 🎉

> **نکته:** اولین جواب ممکنه چند ثانیه طول بکشه چون مدل داره تو حافظه بارگذاری میشه.

---

## مرحله ۶: تست عملکرد

بعد از اجرا، این‌ها رو تست کن:

| تست | چطور | نتیجه مورد انتظار |
|---|---|---|
| گفتگو | بنویس `سلام` | پت جواب میده |
| حافظه | بگو `اسم من علیه`، بعد بپرس `اسم من چیه؟` | پت یادش میاد |
| شخصیت | چند بار حرف بزن | سبک جواب عوض میشه |
| احساس | بگو `خوشحالم` | پت خوشحال میشه (انیمیشن عوض میشه) |
| تنظیمات | راست‌کلیک → Settings → Cognition | پنجره تنظیمات باز میشه |

اگه چیزی کار نکرد، برو به **مرحله ۷**.

---

## مرحله ۷: حل مشکل (Troubleshooting)

### پت جواب نمیده

1. **مدل دانلود شده؟** پوشه `models/` رو چک کن. فایل `.gguf` باید باشه.
2. **اسم مدل درسته؟** تو `appsettings.json` فیلد `Model` رو با اسم فایل چک کن.
3. **RAM کافی هست؟** مدل 3B حداقل ۴ گیگابایت RAM آزاد نیاز داره.
4. **لاگ‌ها رو چک کن:** پوشه `logs/` رو باز کن. فایل `cognition-*.log` رو بخون.

### پت اصلاً باز نمیشه

1. **.NET 8 نصبه؟** `dotnet --version` بزن.
2. **DLL‌ها هستن؟** پوشه `mod/0000_cognition/` رو چک کن. باید چندتا `.dll` باشه.
3. **appsettings.json درسته؟** JSON رو با https://jsonlint.com اعتبارسنجی کن.

### جواب‌ها خیلی کندن

1. **GPU فعال کن:** `GpuLayerCount` رو از 0 به 25 (یا بیشتر) تغییر بده.
2. **مدل کوچک‌تر استفاده کن:** `qwen2.5-1.5b-instruct-q4_k_m.gguf` (~۱ گیگابایت)
3. **ContextSize کم کن:** از 4096 به 2048 تغییر بده.

### صدا کار نمیکنه

1. **whisper-cli.exe هست؟** تو پوشه اصلی VPet چک کن.
2. **مدل Whisper هست؟** تو پوشه `models/` چک کن.
3. **piper.exe هست؟** تو پوشه اصلی VPet چک کن.
4. **مدل Piper هست؟** تو پوشه `models/` چک کن.
5. **تو appsettings.json مسیرها درستن؟**

### حافظه کار نمیکنه

1. پوشه `data/` وجود داره؟
2. فایل `cognition.db` ساخته شده؟
3. اگه نه، دستی بساز:
   ```powershell
   mkdir data
   ```

---

## مرحله ۸: شخصی‌سازی

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

### عوض کردن زبان

تو `appsettings.json`، تو بخش STT:
```json
"Language": "fa"
```

### عوض کردن سرعت جواب

```json
"Llm": {
  "Temperature": 0.5
}
```
(عدد کمتر = جواب‌های دقیق‌تر، عدد بیشتر = جواب‌های خلاقانه‌تر)

---

## خلاصه سریع (۴ مرحله)

```
1. مدل GGUF دانلود کن → models/qwen2.5-3b-instruct-q4_k_m.gguf
2. VPet-Ultra-Cognition.zip رو دانلود و کنار VPet اکسترکت کن
3. appsettings.json رو چک کن (پیش‌فرض درسته)
4. VPet-Simulator.Windows.exe رو اجرا کن → دکمه Chat → حرف بزن!
```

**نیازی به Ollama یا سرور نیست — مدل داخل خود برنامه اجرا میشه!** 🎉