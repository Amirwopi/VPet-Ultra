# Troubleshooting

## App Stuck on Loading Screen

### Symptom
برنامه باز می‌شود اما روی صفحه‌ی loading می‌ماند و به بازی وارد نمی‌شود.

### Cause
broken symlinks در پوشه‌ی `mod/` که به مسیرهای Windows اشاره می‌کنند.

### Fix
```bash
# Find broken symlinks
find mod/ -maxdepth 1 -type l ! -exec test -e {} \; -print

# Delete them
find mod/ -maxdepth 1 -type l ! -exec test -e {} \; -delete
```

فقط `0000_core` (پوشه‌ی واقعی) باید باقی بماند.

---

## Missing Core MOD Error

### Symptom
```
Missing module Core, can't start up
```

### Cause
پوشه‌ی `mod/0000_core` وجود ندارد یا `info.lps` آن گم شده.

### Fix
مطمئن شوید `mod/0000_core/info.lps` وجود دارد:
```bash
ls mod/0000_core/info.lps
```

---

## Build Error: XML is not valid

### Symptom
```
error MC3000: ''&' is an unexpected token.
```

### Cause
کاراکتر `&` در فایل XAML escape نشده.

### Fix
`&` را با `&amp;` جایگزین کنید در فایل XAML.

---

## Build Error: Quotes in MarkupExtension

### Symptom
```
error MC3043: Names and Values in a MarkupExtension cannot contain quotes.
```

### Cause
آپوستروف (`'`) داخل `{ll:Str '...'}` در XAML.

### Fix
از `&quot;` استفاده کنید:
```xml
<!-- Wrong -->
<TextBlock Text="{ll:Str 'Don't close'}" />

<!-- Correct -->
<TextBlock Text="{ll:Str &quot;Don't close&quot;}" />
```

---

## Live2D Not Rendering

### Symptom
پلاگین Live2D بارگذاری می‌شود اما مدل‌های Live2D نمایش داده نمی‌شوند.

### Cause
`Live2DCubismCore.dll` گم شده.

### Fix
1. از [Live2D SDK](https://www.live2d.com/sdk/download/native/) دانلود کنید
2. `Live2DCubismCore.dll` (x64) را کنار `VPet.exe` قرار دهید

---

## AI Not Responding

### Symptom
دکمه‌ی Chat وجود دارد ولی پاسخی دریافت نمی‌شود.

### Causes & Fixes

1. **Model not found**
   - بررسی کنید مدل GGUF در `models/` وجود دارد
   - نام مدل در `appsettings.json` با نام فایل مطابقت دارد

2. **Not enough RAM**
   - مدل کوچک‌تر استفاده کنید (Qwen2.5-1B یا Llama-3.2-1B)
   - `GpuLayerCount` را افزایش دهید (اگر GPU دارید)

3. **Cognition disabled**
   - `"Enabled": true` در `appsettings.json` بررسی کنید

4. **Plugin not loaded**
   - بررسی کنید `mod/0000_cognition/` وجود دارد
   - DLLهای cognition در پوشه‌ی plugin هستند

---

## App Crashes on Startup

### Symptom
برنامه بلافاصله بعد از اجرا crash می‌کند.

### Fix
1. `.NET 8 Runtime` نصب است؟
2. فایل `appsettings.json` معتبر JSON است؟
3. پوشه‌ی `mod/0000_core` کامل است؟
4. لاگ‌ها را در `logs/` بررسی کنید

---

## Save File Corrupted

### Symptom
```
Save file corrupted, unable to load
```

### Fix
1. فایل `Save.lps` را از `Saves_BKP/` بازیابی کنید
2. یا فایل `Save.lps` را حذف کنید تا save جدید ساخته شود

---

## High RAM Usage

### Cause
مدل LLM بزرگ + context size بزرگ.

### Fix
- مدل کوچک‌تر: `Qwen2.5-1B` به‌جای `3B`
- `ContextSize` را کاهش دهید: `2048` به‌جای `4096`
- `EnableVision: false` (vision RAM زیادی مصرف می‌کند)
- `GpuLayerCount` را افزایش دهید (RAM → VRAM)