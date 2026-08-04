# Mod System

VPet-Ultra از سیستم mod برای بارگذاری محتوا استفاده می‌کند.

## Mod Structure

```
mod/
└── [mod-id]/
    ├── info.lps          # metadata (required)
    ├── icon.png          # icon (optional)
    ├── lang/             # translations
    │   ├── en/
    │   │   └── *.lps
    │   └── zh-Hans/
    │       └── *.lps
    ├── pet/              # pet animations
    ├── food/             # food items
    ├── photo/            # photos
    ├── plugin/           # code plugins (DLLs)
    └── ...
```

## info.lps Format

```
vupmod#ModName:|author#AuthorName:|gamever#11000:|ver#100:|
intro#Mod description:|
authorid#0:|
itemid#0:|
dllskip:|SomeDLL.dll:|
lang#en:|English Name#English Name:|
lang#zh-Hans:|Chinese Name#Chinese Name:|
```

### Fields

| Field | Description |
|-------|-------------|
| `vupmod` | نام داخلی mod (unique) |
| `author` | نام سازنده |
| `gamever` | حداقل نسخه‌ی بازی مورد نیاز |
| `ver` | نسخه‌ی mod |
| `intro` | توضیحات کوتاه |
| `authorid` | Steam Author ID (0 برای non-Steam) |
| `itemid` | Steam Workshop Item ID (0 برای non-Steam) |
| `dllskip` | DLLهایی که نباید بارگذاری شوند |

## Code Plugins

پلاگین‌های کد از طریق `MainPlugin` base class بارگذاری می‌شوند:

```csharp
using VPet_Simulator.Windows.Interface;

public class MyPlugin : MainPlugin
{
    public MyPlugin(IMainWindow mainwin) : base(mainwin)
    {
        // Constructor — runs at load time
    }

    public override string PluginName => "MyPlugin";

    public override void LoadPlugin()
    {
        // Called after all mods are loaded
        // Access: MW.Main, MW.Core, MW.Set, etc.
    }

    public override void GameLoaded()
    {
        // Called after game is fully loaded
    }

    public override void Save()
    {
        // Called when game saves
    }
}
```

### Plugin DLL Location

```
mod/[mod-id]/plugin/
├── MyPlugin.dll
├── MyPlugin.deps.json
└── [dependency DLLs]
```

## Creating a New Mod

1. پوشه‌ی جدید در `mod/` بسازید: `mod/my_mod/`
2. فایل `info.lps` بسازید
3. محتوا اضافه کنید (pet، food، etc.)
4. VPet را اجرا کنید — mod به‌طور خودکار بارگذاری می‌شود

## Mod Loading Order

1. `0000_core` همیشه اول بارگذاری می‌شود
2. بقیه‌ی modها بر اساس نام پوشه (alphabetical)
3. پلاگین‌های کد بعد از تمام modها بارگذاری می‌شوند

## Disabled Mods

برای غیرفعال کردن یک mod، فایل `info.lps` را به `info.lps.disabled` تغییر نام دهید.