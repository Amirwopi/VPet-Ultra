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
2. آخرین نسخه `VPet-Ultra-Windows.zip` رو دانلود کن
3. تو یه پوشه اکسترکت کن (مثلا `C:\VPet-Ultra\`)
4. برو به **مرحله ۳**

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

خروجی تو پوشه `publish/vpet-windows/` ساخته میشه.

---

## مرحله ۲: نصب Ollama (مغز پت)

1. برو به https://ollama.com/download
2. نسخه Windows رو دانلود و نصب کن
3. Ollama اجرا میشه (آیکون تو system tray ظاهر میشه)
4. مدل Qwen2.5-3B رو دانلود کن:

   ```powershell
   ollama pull qwen2.5:3b
   ```

   صبر کن (~۲ گیگابایت دانلود).

5. تست کن:

   ```powershell
   ollama run qwen2.5:3b "سلام"
   ```

   باید جواب بده. برای خروج `Ctrl+C` بزن.

> **نکته:** Ollama باید همیشه اجرا باشه تا پت بتونه حرف بزنه. آیکونش تو system tray هست.

---

## مرحله ۳: تنظیم فایل config

1. برو به پوشه VPet-Ultra (جایی که `VPet-Simulator.Windows.exe` هست)
2. فایل `appsettings.json` رو با Notepad باز کن
3. تنظیمات پیش‌فرض برای Ollama + Qwen2.5-3B اینه:

   ```json
   {
     "Cognition": {
       "Enabled": true,
       "Llm": {
         "Provider": "ollama",
         "Endpoint": "http://localhost:11434",
         "Model": "qwen2.5:3b"
       }
     }
   }
   ```

4. اگه مدل دیگه‌ای خواستی (مثلا 7B):
   ```json
   "Model": "qwen2.5:7b"
   ```

5. فایل رو ذخیره کن.

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

1. **Ollama اجراست؟** آیکونش تو system tray هست؟
   ```powershell
   ollama list
   ```
   باید `qwen2.5:3b` رو نشون بده.

2. **پورت درسته؟** تو مرورگر برو به:
   ```
   http://localhost:11434/api/tags
   ```
   باید JSON نشون بده.

3. **مدل درسته؟**
   ```powershell
   ollama run qwen2.5:3b "test"
   ```

4. **لاگ‌ها رو چک کن:** پوشه `logs/` رو باز کن. فایل `cognition-*.log` رو بخون.

### پت اصلاً باز نمیشه

1. **.NET 8 نصبه؟** `dotnet --version` بزن.
2. **DLL‌ها هستن؟** پوشه `mod/0000_cognition/` رو چک کن. باید چندتا `.dll` باشه.
3. **appsettings.json درسته؟** JSON رو با https://jsonlint.com اعتبارسنجی کن.

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

## خلاصه سریع (۵ مرحله)

```
1. Ollama نصب کن → ollama pull qwen2.5:3b
2. VPet-Ultra.zip رو دانلود و اکسترکت کن
3. appsettings.json رو چک کن (پیش‌فرض درسته)
4. VPet-Simulator.Windows.exe رو اجرا کن
5. روی دکمه Chat کلیک کن و حرف بزن!
```

**تمام!** 🎉