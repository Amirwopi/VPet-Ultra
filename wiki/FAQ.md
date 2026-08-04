# FAQ

## سؤالات متداول

### آیا VPet-Ultra نیاز به Steam دارد؟
**خیر.** تمام وابستگی‌های Steam حذف شده. برنامه به‌صورت کاملاً standalone اجرا می‌شود.

### آیا VPet-Ultra نیاز به اینترنت دارد؟
**خیر.** AI inference به‌صورت محلی (offline) از طریق LLamaSharp انجام می‌شود. فقط اگر از online mode (Ollama/OpenAI) استفاده کنید به اینترنت نیاز است.

### چه مدل‌های AI پشتیبانی می‌شوند؟
هر مدل GGUF. توصیه‌شده: **Qwen2.5-3B-Instruct** (Q4_K_M). ببینید [AI-Models](AI-Models).

### حداقل سخت‌افزار چیست؟
- **بدون AI:** 2GB RAM، هر GPU
- **با AI (CPU):** 4GB RAM (مدل 3B)
- **با AI (GPU):** 4GB VRAM (مدل 3B)
- **با Vision:** 8GB RAM (مدل LLaVA)

### آیا Live2D پشتیبانی می‌شود؟
**بله.** پلاگین `1102_Live2DAnimation` نصب شده. فقط `Live2DCubismCore.dll` نیاز است. ببینید [Live2D-Setup](Live2D-Setup).

### آیا multiplayer پشتیبانی می‌شود؟
**خیر.** سیستم multiplayer (lobby/P2P) کاملاً حذف شده چون به Steam وابسته بود.

### آیا Steam Workshop پشتیبانی می‌شود؟
**خیر.** فقط modهای محلی (از پوشه‌ی `mod/`) بارگذاری می‌شوند.

### چطور mod جدید اضافه کنم؟
پوشه‌ی mod را در `mod/` کپی کنید. ببینید [Mod-System](Mod-System).

### چطور زبان برنامه را تغییر دهم؟
در Settings → Language. فعلاً English و Chinese پشتیبانی می‌شوند.

### آیا روی Linux کار می‌کند؟
Build می‌شود ولی اجرا نیاز به Wine دارد (چون WPF است). ببینید [Build-Guide](Build-Guide).

### آیا روی macOS کار می‌کند؟
**خیر.** WPF فقط روی Windows (یا Wine) کار می‌کند.

### چطور backup بگیرم؟
فایل‌های زیر را کپی کنید:
- `Save.lps` — save اصلی
- `Setting.lps` — تنظیمات
- `data/cognition.db` — حافظه‌ی AI
- `models/` — مدل‌های AI

### آیا پروژه رایگان است؟
**بله.** تحت Apache License 2.0. اما Live2D SDK license جداگانه دارد.

### چطور مشارکت کنم؟
ببینید [CONTRIBUTING.md](../CONTRIBUTING.md). PRها welcome هستند!

### تفاوت VPet-Ultra با VPet اصلی چیست؟
1. Steam حذف شده
2. AI cognition layer اضافه شده (LLM، memory، personality، emotion، voice، agent)
3. تمام متن چینی به انگلیسی تبدیل شده
4. Live2D plugin از قبل نصب شده