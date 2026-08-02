# راهنمای تست مرحله به مرحله — VPet-Ultra

این راهنما هر بخش از سیستم رو مرحله به مرحله تست می‌کنه تا مطمئن بشی همه‌چیز درست کار می‌کنه.

---

## بخش ۱: تست کد (تست‌های خودکار)

### ۱.۱ — اجرای تست‌های واحد

```powershell
cd C:\VPet-Ultra
dotnet test VPet.Cognition.Tests/VPet.Cognition.Tests.csproj -c Release --logger "console;verbosity=minimal"
```

**نتیجه مورد انتظار:**
```
Passed!  - Failed: 0, Passed: 129, Skipped: 0, Total: 129
```

اگه هر تستی fail شد:
```powershell
dotnet test VPet.Cognition.Tests/VPet.Cognition.Tests.csproj -c Release --logger "console;verbosity=detailed"
```
و لاگ رو بخون.

### ۱.۲ — تست build کامل

```powershell
dotnet build VPet.sln -c Release
```

**نتیجه مورد انتظار:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

---

## بخش ۲: تست موتور استنتاج (مغز پت)

VPet-Ultra به صورت پیش‌فرض مدل‌ها رو داخل خودش اجرا می‌کنه (LLamaSharp). نیازی به Ollama نیست.

### ۲.۱ — مدل GGUF موجود هست؟

```powershell
dir models\*.gguf
```

**نتیجه مورد انتظار:**
```
qwen2.5-3b-instruct-q4_k_m.gguf    ~۲GB
```

اگه نیست، از https://huggingface.co/Qwen/Qwen2.5-3B-Instruct-GGUF دانلود کن.

### ۲.۲ — تست اجرای محلی (LocalInferenceProvider)

```powershell
dotnet test VPet.Cognition.Tests/VPet.Cognition.Tests.csproj -c Release --filter "Inference"
```

**نتیجه مورد انتظار:**
```
Passed!  - Failed: 0, Passed: 27, Skipped: 0, Total: 27
```

این تست‌ها موتور استنتاج محلی، ModelManager، ModelRegistry، و VisionModule رو تست می‌کنن.

### ۲.۳ — تست حالت آنلاین (Ollama — اختیاری)

اگه Ollama نصبه:

```powershell
ollama list
```

**نتیجه مورد انتظار:**
```
NAME           ID           SIZE     MODIFIED
qwen2.5:3b     abc123...    2.0 GB   2 minutes ago
```

اگه خالی بود:
```powershell
ollama pull qwen2.5:3b
```

تو `appsettings.json`:
```json
"Provider": "ollama",
"Endpoint": "http://localhost:11434",
"Model": "qwen2.5:3b"
```

### ۲.۴ — مدل جواب میده؟

با Ollama:
```powershell
ollama run qwen2.5:3b "سلام، چطوری؟"
```

**نتیجه مورد انتظار:** مدل یه جواب متنی میده.

### ۲.۵ — تست streaming

```powershell
ollama run qwen2.5:3b "یه داستان کوتاه بگو"
```

**نتیجه مورد انتظار:** متن کلمه به کلمه ظاهر میشه (streaming)، نه همه‌ش با هم.

---

## بخش ۳: تست LLM Provider (کد)

این تست با WireMock شبکه رو شبیه‌سازی می‌کنه و LLM provider رو تست می‌کنه.

### ۳.۱ — تست OllamaLlmProvider

```powershell
dotnet test VPet.Cognition.Tests/VPet.Cognition.Tests.csproj -c Release --filter "OllamaLlmProvider"
```

**نتیجه مورد انتظار:**
```
Passed!  - Failed: 0, Passed: 4, Skipped: 0, Total: 4
```

تست‌ها:
- `OllamaLlmProvider_ChatAsync_ShouldReturnContent` — درخواست ساده
- `OllamaLlmProvider_StreamChatAsync_ShouldYieldTextChunks` — streaming
- `OllamaLlmProvider_ChatAsync_ServerError_ShouldReturnFailure` — خطای سرور
- `OllamaLlmProvider_IsAvailableAsync_ShouldReturnTrueWhenServerUp` — دسترسی

### ۳.۲ — تست OpenAICompatibleLlmProvider

```powershell
dotnet test VPet.Cognition.Tests/VPet.Cognition.Tests.csproj -c Release --filter "OpenAICompatibleLlmProvider"
```

**نتیجه مورد انتظار:**
```
Passed!  - Failed: 0, Passed: 2, Skipped: 0, Total: 2
```

### ۳.۳ — تست PromptAssembler

```powershell
dotnet test VPet.Cognition.Tests/VPet.Cognition.Tests.csproj -c Release --filter "PromptAssembler"
```

**نتیجه مورد انتظار:**
```
Passed!  - Failed: 0, Passed: 2, Skipped: 0, Total: 2
```

### ۳.۴ — تست ConversationService

```powershell
dotnet test VPet.Cognition.Tests/VPet.Cognition.Tests.csproj -c Release --filter "ConversationService"
```

**نتیجه مورد انتظار:**
```
Passed!  - Failed: 0, Passed: 1, Skipped: 0, Total: 1
```

---

## بخش ۴: تست حافظه (Memory)

### ۴.۱ — تست‌های خودکار

```powershell
dotnet test VPet.Cognition.Tests/VPet.Cognition.Tests.csproj -c Release --filter "MemoryStoreTests"
```

**نتیجه مورد انتظار:**
```
Passed!  - Failed: 0, Passed: 16, Skipped: 0, Total: 16
```

### ۴.۲ — تست دستی حافظه

1. VPet رو اجرا کن
2. بگو: `اسم من علیه`
3. چند تا حرف دیگه بزن
4. بپرس: `اسم من چیه؟`
5. **نتیجه مورد انتظار:** پت باید بگه `علی`

### ۴.۳ — چک کردن فایل دیتابیس

```powershell
# پوشه data رو چک کن
dir data\cognition.db
```

**نتیجه مورد انتظار:** فایل باید وجود داشته باشه و حجمش > 0 باشه.

### ۴.۴ — تست export حافظه

```powershell
# اگه VPet اجراست، بستنش
# فایل دیتابیس رو با SQLite مرورگر باز کن:
# دانلود: https://sqlitebrowser.org/dl/
# باز کن: data/cognition.db
# جدول memories رو ببین
```

**نتیجه مورد انتظار:** چندتا رکورد با type، content، importance.

---

## بخش ۵: تست شخصیت و احساس

### ۵.۱ — تست‌های خودکار

```powershell
dotnet test VPet.Cognition.Tests/VPet.Cognition.Tests.csproj -c Release --filter "PersonalityEmotionTests"
```

**نتیجه مورد انتظار:**
```
Passed!  - Failed: 0, Passed: 11, Skipped: 0, Total: 11
```

### ۵.۲ — تست دستی احساس

1. VPet رو اجرا کن
2. بگو: `من خیلی خوشحالم!`
3. **نتیجه مورد انتظار:** انیمیشن پت باید عوض بشه (حالت خوشحال)
4. بگو: `من ناراحتم`
5. **نتیجه مورد انتظار:** انیمیشن باید به حالت غمگین عوض بشه

### ۵.۳ — تست شخصیت

1. چند بار با پت حرف بزن (حداقل ۱۰ بار)
2. سبک جواب‌ها رو مقایسه کن
3. **نتیجه مورد انتظار:** پت کم‌کم صمیمی‌تر میشه (trust و familiarity افزایش پیدا می‌کنه)

---

## بخش ۶: تست صدا (اگه نصب کرده‌ای)

### ۶.۱ — تست Whisper (STT)

```powershell
# یه فایل صوتی تستی بساز (یا از samples whisper.cpp استفاده کن)
whisper-cli -m models/ggml-base.bin -f whisper.cpp/samples/jfk.wav -l auto -t 4 -nt -np
```

**نتیجه مورد انتظار:** متن transcript چاپ میشه.

### ۶.۲ — تست Piper (TTS)

```powershell
echo "Hello, this is a test." | piper -m models/en_US-amy-medium.onnx -f test_output.wav
```

**نتیجه مورد انتظار:** فایل `test_output.wav` ساخته میشه. پخشش کن:

```powershell
Start-Process test_output.wav
```

### ۶.۳ — تست‌های خودکار صدا

```powershell
dotnet test VPet.Cognition.Tests/VPet.Cognition.Tests.csproj -c Release --filter "VoiceProviderTests"
```

**نتیجه مورد انتظار:**
```
Passed!  - Failed: 0, Passed: 11, Skipped: 0, Total: 11
```

---

## بخش ۷: تست Agent و ابزارها

### ۷.۱ — تست‌های خودکار

```powershell
dotnet test VPet.Cognition.Tests/VPet.Cognition.Tests.csproj -c Release --filter "AgentTests"
```

**نتیجه مورد انتظار:**
```
Passed!  - Failed: 0, Passed: 16, Skipped: 0, Total: 16
```

### ۷.۲ — تست دستی ابزار مرورگر

1. VPet رو اجرا کن
2. بگو: `سایت گوگل رو باز کن`
3. **نتیجه مورد انتظار:** مرورگر باز میشه و google.com رو لود می‌کنه

### ۷.۳ — تست دستی ابزار فایل

1. بگو: `فایل C:\test.txt رو بخون`
2. **نتیجه مورد انتظار:** پت محتوای فایل رو می‌خونه (اگه فایل وجود داشته باشه و تو allowlist باشه)

---

## بخش ۸: تست امنیت

### ۸.۱ — تست‌های خودکار

```powershell
dotnet test VPet.Cognition.Tests/VPet.Cognition.Tests.csproj -c Release --filter "SecurityTests"
```

**نتیجه مورد انتظار:**
```
Passed!  - Failed: 0, Passed: 15, Skipped: 0, Total: 15
```

### ۸.۲ — تست دستی redaction

تو `appsettings.json` یه API key بذار:
```json
"ApiKey": "sk-test-secret-123"
```

VPet رو اجرا کن. لاگ رو چک کن:
```powershell
Get-Content logs\cognition-*.log | Select-String "apiKey"
```

**نتیجه مورد انتظار:** باید `***` نشون بده، نه `sk-test-secret-123`.

### ۸.۳ — تست path traversal

1. بگو: `فایل ../../etc/passwd رو بخون`
2. **نتیجه مورد انتظار:** پت باید رد کنه و بگه "مسیر مجاز نیست"

---

## بخش ۹: تست یکپارچه (End-to-End)

این تست کامل‌ترین حالته — همه بخش‌ها با هم.

### ۹.۱ — سناریوی کامل گفتگو

1. VPet رو اجرا کن
2. روی **Chat** کلیک کن
3. این مکالمه رو انجام بده:

   | تو | پت (حدودی) |
   |---|---|
   | سلام | سلام! چطوری؟ |
   | اسم من علیه | سلام علی! از آشنایتی خوشحالم. |
   | من قهوه دوست دارم | خوبه! قهوه عالیه. |
   | اسم من چیه؟ | اسم تو علیه. |
   | من چی دوست دارم؟ | تو قهوه دوست داری. |
   | من خیلی خوشحالم! | 😊 منم خوشحالم! |

4. **چک کن:**
   - ✅ پت جواب میده (LLM کار می‌کنه)
   - ✅ پت اسمتو یادش میاد (حافظه کار می‌کنه)
   - ✅ پت قهوه رو یادش میاد (حافظه semantic)
   - ✅ سبک جواب‌ها صمیمی میشه (شخصیت)
   - ✅ انیمیشن عوض میشه (احساس)

### ۹.۲ — تست لغو (Cancel)

1. یه سوال طولانی بپرس (مثلا `یه داستان بلند بگو`)
2. موقع جواب دادن، دکمه **Cancel** رو بزن
3. **نتیجه مورد انتظار:** جواب متوقف میشه و `[cancelled]` نشون داده میشه

### ۹.۳ — تست تنظیمات

1. راست‌کلیک روی پت → **Settings**
2. تب **Cognition** رو پیدا کن
3. **Test Connection** رو بزن
4. **نتیجه مورد انتظار:** `✓ Connected` نشون داده میشه

---

## بخش ۱۰: چک‌لیست نهایی

این چک‌لیست رو پر کن تا مطمئن بشی همه‌چیز کار می‌کنه:

### ساختار فایل‌ها
- [ ] `VPet-Simulator.Windows.exe` وجود داره
- [ ] `appsettings.json` وجود داره و JSON معتبره
- [ ] `mod/0000_cognition/` پوشه وجود داره
- [ ] `mod/0000_cognition/VPet.Cognition.Host.dll` وجود داره
- [ ] `mod/0000_cognition/VPet.Cognition.LLM.dll` وجود داره
- [ ] `mod/0000_cognition/VPet.Cognition.Memory.dll` وجود داره
- [ ] `mod/0000_cognition/VPet.Cognition.Wpf.dll` وجود داره

### Ollama (اختیاری)
- [ ] Ollama نصبه و اجراست (فقط اگه از حالت آنلاین استفاده می‌کنی)
- [ ] `ollama list` مدل `qwen2.5:3b` رو نشون میده
- [ ] `http://localhost:11434/api/tags` جواب میده

### تست‌های خودکار
- [ ] `dotnet test` = 129 passed, 0 failed
- [ ] `dotnet build` = 0 warnings, 0 errors

### عملکرد
- [ ] پت اجرا میشه
- [ ] دکمه Chat کار می‌کنه
- [ ] پت جواب میده
- [ ] حافظه کار می‌کنه (اسم رو یادش میاد)
- [ ] احساس کار می‌کنه (انیمیشن عوض میشه)
- [ ] تنظیمات باز میشه
- [ ] Test Connection جواب میده

### صدا (اختیاری)
- [ ] `whisper-cli.exe` وجود داره
- [ ] `models/ggml-base.bin` وجود داره
- [ ] `piper.exe` وجود داره
- [ ] `models/*.onnx` وجود داره

### امنیت
- [ ] API key تو لاگ `***` نشون داده میشه
- [ ] Path traversal بلاک میشه

---

## اگه چیزی کار نکرد

1. **لاگ رو بخون:** `logs/cognition-*.log`
2. **تست‌ها رو اجرا کن:** `dotnet test`
3. **مدل GGUF رو چک کن:** `dir models\*.gguf`
4. **اگه از Ollama استفاده می‌کنی:** `ollama list`
4. **config رو چک کن:** `appsettings.json` رو با JSON validator اعتبارسنجی کن
5. **DLL‌ها رو چک کن:** `mod/0000_cognition/` رو باز کن
6. **مسیرها رو چک کن:** مسیر مدل‌ها تو `appsettings.json` درسته؟

اگه هنوز مشکل داری، یه issue باز کن:
https://github.com/Amirwopi/VPet-Ultra/issues