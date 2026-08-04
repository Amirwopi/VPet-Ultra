# Build Guide

## Prerequisites

- **.NET 8 SDK** (8.0.423 یا بالاتر) — [download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Git** — برای clone کردن repo
- **Windows** (برای اجرای WPF) یا **Linux با Wine**

## Build روی Windows

```bash
git clone https://github.com/Amirwopi/VPet-Ultra.git
cd VPet-Ultra
dotnet restore VPet.sln
dotnet build VPet.sln -c Release -p:Platform=x64
```

### Publish (self-contained)

```bash
dotnet publish VPet-Simulator.Windows/VPet-Simulator.Windows.csproj \
  -c Release -p:Platform=x64 -r win-x64 --self-contained false -o publish/vpet-ultra
```

## Build روی Linux

Linux از WPF به‌صورت native پشتیبانی نمی‌کند، اما می‌توان build کرد:

```bash
git clone https://github.com/Amirwopi/VPet-Ultra.git
cd VPet-Ultra
dotnet build VPet-Simulator.Windows/VPet-Simulator.Windows.csproj \
  -c Release -p:Platform=x64 -p:EnableWindowsTargeting=true
```

> **نکته:** برای اجرای روی Linux به Wine نیاز است. Build فقط برای verify کردن compilation است.

## Build با GitHub Actions

repo دارای workflow `build-windows.yml` است که روی هر tag `v*` اجرا می‌شود:

1. Tag بسازید: `git tag v0.3.0 && git push origin v0.3.0`
2. GitHub Actions به‌طور خودکار build می‌کند
3. Release با دو zip file ایجاد می‌شود:
   - `VPet-Ultra-Full.zip` — کل برنامه + داده‌ها
   - `VPet-Ultra-Plugin.zip` — فقط پلاگین AI (برای آپدیت)

## Build Output

```
publish/vpet-ultra/
├── VPet-Simulator.Windows.exe    # main executable
├── appsettings.json               # AI config
├── mod/
│   ├── 0000_core/                 # game data
│   └── 1102_Live2DAnimation/      # Live2D plugin
├── models/                        # GGUF models (download separately)
├── data/                          # SQLite memory DB
└── logs/                          # runtime logs
```

## Troubleshooting Build

### Error: `MC3000: XML is not valid`
- فایل XAML را بررسی کنید — ممکن است کاراکتر `&` escape نشده باشد (`&amp;`)

### Error: `NETSDK1138: Platform not supported`
- `-p:EnableWindowsTargeting=true` را اضافه کنید (Linux)

### Error: `Missing file: Res/TopLogo2019.PNG`
- یک placeholder PNG در `VPet-Simulator.Windows/Res/TopLogo2019.PNG` بسازید