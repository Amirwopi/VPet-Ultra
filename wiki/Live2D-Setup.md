# Live2D Setup

پلاگین Live2D (`1102_Live2DAnimation`) از قبل در `mod/` نصب شده است. فقط نیاز به `Live2DCubismCore.dll` دارد.

## Step 1: Download Live2DCubismCore.dll

1. به [Live2D Cubism SDK](https://www.live2d.com/sdk/download/native/) بروید
2. ثبت‌نام کنید (رایگان)
3. **Cubism Native SDK** را دانلود کنید
4. فایل `Live2DCubismCore.dll` را استخراج کنید:
   - Windows x64: `Live2DCubismCore.dll` (از پوشه‌ی Windows)

## Step 2: Place the DLL

یکی از دو مسیر زیر:

```
# گزینه 1: کنار VPet.exe
VPet-Simulator.Windows.exe
Live2DCubismCore.dll          ← اینجا

# گزینه 2: در پوشه‌ی پلاگین
mod/1102_Live2DAnimation/plugin/
Live2DCubismCore.dll          ← اینجا
```

## Step 3: Get Live2D Models

مدل‌های Live2D با فرمت `.moc3` باید در پوشه‌ی mod قرار بگیرند:

```
mod/
├── 0000_core/
│   └── pet/
│       └── vup/
│           └── [pet-name]/
│               ├── model.moc3          ← Live2D model
│               ├── model.json
│               ├── textures/
│               │   └── texture_00.png
│               └── motions/
│                   └── *.motion3.json
```

## Step 4: Verify

1. VPet را اجرا کنید
2. اگر پلاگین به‌درستی بارگذاری شود، pet با Live2D animation نمایش داده می‌شود
3. اگر `Live2DCubismCore.dll` پیدا نشود، پلاگین load می‌شود اما مدل‌های Live2D رندر نمی‌شوند

## File Format Support

| Format | Extension | Description |
|--------|-----------|-------------|
| Live2D Model | `.moc3` | مدل Live2D Cubism 3+ |
| Live2D Motion | `.motion3.json` | انیمیشن Live2D |
| Live2D Physics | `.physics3.json` | فیزیک Live2D |
| Live2D Pose | `.pose3.json` | pose Live2D |
| Live2D Expression | `.exp3.json` | expression Live2D |

## Troubleshooting

### Plugin loads but no Live2D models
- `Live2DCubismCore.dll` را بررسی کنید — باید کنار exe یا در پوشه‌ی plugin باشد
- نسخه‌ی x64 (نه x86) استفاده کنید

### Black screen / crash
- GPU driver را به‌روزرسانی کنید
- OpenGL 3.3+ نیاز است

### License
Live2D Cubism SDK تحت license اختصاصی Live2D است. برای استفاده‌ی تجاری به [Live2D License](https://www.live2d.com/en/sdk/license/) مراجعه کنید.