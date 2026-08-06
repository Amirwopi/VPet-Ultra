# VPet-Ultra Local Build Script for Windows
# Builds the full solution + cognition plugin and packages into zip
#
# Usage:
#   .\build-local.ps1
#
# Output:
#   publish\vpet-ultra\          — Full VPet + cognition plugin ready to run
#   VPet-Ultra-Full.zip          — Full package for distribution
#   VPet-Ultra-Plugin.zip        — Plugin-only update package
#
# Requirements:
#   - .NET 8 SDK (dotnet --version)
#   - Windows 10/11 x64
#   - Git (for version info)

param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64",
    [string]$OutputDir = "publish/vpet-ultra",
    [string]$Version = ""
)

$ErrorActionPreference = "Stop"

# Get version from git tag or default
if ([string]::IsNullOrEmpty($Version)) {
    $gitTag = git tag --points-at HEAD 2>$null | Select-Object -First 1
    if ($gitTag) {
        $Version = $gitTag.TrimStart('v')
    } else {
        $Version = "0.2.0-dev"
    }
}

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  VPet-Ultra Build Script v$Version" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Config: $Configuration | Platform: $Platform"
Write-Host "Output:  $OutputDir"
Write-Host ""

# Step 1: Check prerequisites
Write-Host "[1/8] Checking prerequisites..." -ForegroundColor Yellow
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    Write-Host "ERROR: .NET 8 SDK not found. Install from https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Red
    exit 1
}
$dotnetVersion = (dotnet --version)
Write-Host "  .NET SDK: $dotnetVersion"

$git = Get-Command git -ErrorAction SilentlyContinue
if ($git) {
    $gitVersion = (git --version)
    Write-Host "  Git: $gitVersion"
}
Write-Host "  OK" -ForegroundColor Green
Write-Host ""

# Step 2: Restore
Write-Host "[2/8] Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore VPet.sln
if ($LASTEXITCODE -ne 0) { Write-Host "Restore failed!" -ForegroundColor Red; exit 1 }
Write-Host "  OK" -ForegroundColor Green
Write-Host ""

# Step 3: Build solution
Write-Host "[3/8] Building solution ($Configuration $Platform)..." -ForegroundColor Yellow
dotnet build VPet.sln -c $Configuration -p:Platform=$Platform --no-restore
if ($LASTEXITCODE -ne 0) { Write-Host "Build failed!" -ForegroundColor Red; exit 1 }
Write-Host "  OK" -ForegroundColor Green
Write-Host ""

# Step 4: Run tests
Write-Host "[4/8] Running tests..." -ForegroundColor Yellow
dotnet test VPet.Cognition.Tests/VPet.Cognition.Tests.csproj -c $Configuration --no-build --logger "console;verbosity=minimal"
if ($LASTEXITCODE -ne 0) { Write-Host "Tests failed!" -ForegroundColor Red; exit 1 }
Write-Host "  OK" -ForegroundColor Green
Write-Host ""

# Step 5: Publish VPet-Simulator.Windows
Write-Host "[5/8] Publishing VPet-Simulator.Windows ($Platform)..." -ForegroundColor Yellow
if (Test-Path $OutputDir) { Remove-Item $OutputDir -Recurse -Force }
dotnet publish VPet-Simulator.Windows/VPet-Simulator.Windows.csproj -c $Configuration -p:Platform=$Platform -r win-$Platform --self-contained false -o $OutputDir
if ($LASTEXITCODE -ne 0) { Write-Host "Publish failed!" -ForegroundColor Red; exit 1 }
Write-Host "  OK" -ForegroundColor Green
Write-Host ""

# Step 5b: Copy mod/0000_core data
Write-Host "[5b/8] Copying mod/0000_core data..." -ForegroundColor Yellow
$srcCore = "VPet-Simulator.Windows/mod/0000_core"
$dstMod = Join-Path $OutputDir "mod"
$dstCore = Join-Path $dstMod "0000_core"
New-Item -ItemType Directory -Force -Path $dstMod | Out-Null
if (Test-Path $dstCore) { Remove-Item $dstCore -Recurse -Force }
Copy-Item -Path $srcCore -Destination $dstCore -Recurse -Force
$modFileCount = (Get-ChildItem $dstCore -Recurse | Measure-Object).Count
Write-Host "  Copied 0000_core ($modFileCount files)"
Write-Host ""

# Step 6: Publish cognition Host (use publish to get all NuGet dependencies)
Write-Host "[6/8] Publishing cognition Host..." -ForegroundColor Yellow
dotnet restore VPet.Cognition.Host/VPet.Cognition.Host.csproj
dotnet publish VPet.Cognition.Host/VPet.Cognition.Host.csproj -c $Configuration -o "VPet.Cognition.Host/bin/$Configuration/publish" --no-restore
if ($LASTEXITCODE -ne 0) { Write-Host "Cognition Host publish failed!" -ForegroundColor Red; exit 1 }
Write-Host "  OK" -ForegroundColor Green
Write-Host ""

# Step 7: Copy cognition plugin to mod folder
Write-Host "[7/8] Copying cognition plugin..." -ForegroundColor Yellow
$modPath = Join-Path $OutputDir "mod/0000_cognition"
New-Item -ItemType Directory -Force -Path $modPath | Out-Null

# Create info.lps - VPet REQUIRES this to recognize a mod folder.
# LPS format: first line has name#info:| with sub-fields separated by |
# Use WriteAllText with UTF8NoBOM because LpsDocument parser chokes on BOM.
$infoLps = "vupmod#Cognition:|author#VPet-Ultra:|gamever#11000:|ver#100:|`nintro#AI Cognition Plugin - Offline LLM Memory Personality Emotion Voice Agent:|`nauthorid#0:|`nitemid#0:|`nlang#en:|`nCognition#Cognition:|`n"
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText((Join-Path $modPath "info.lps"), $infoLps, $utf8NoBom)
Write-Host "  Created: info.lps"

# Create version.txt
[System.IO.File]::WriteAllText((Join-Path $modPath "version.txt"), $Version, $utf8NoBom)
Write-Host "  Created: version.txt ($Version)"

# Copy cognition DLLs to plugin/ subfolder (VPet loader expects this structure)
$pluginPath = Join-Path $modPath "plugin"
New-Item -ItemType Directory -Force -Path $pluginPath | Out-Null

$hostBin = "VPet.Cognition.Host/bin/$Configuration/publish"

$cognitionDlls = Get-ChildItem -Path $hostBin -Filter "VPet.Cognition.*.dll" -Recurse -ErrorAction SilentlyContinue
foreach ($dll in $cognitionDlls) {
    $dest = Join-Path $pluginPath $dll.Name
    if (-not (Test-Path $dest)) {
        Copy-Item $dll.FullName -Destination $pluginPath -Force
        Write-Host "  Copied: $($dll.Name)"
    }
}

# Copy dependency DLLs to plugin/ — skip ones already in main app
$skipPatterns = @("VPet.Cognition.*","VPet-Simulator.*","LinePutScript.*","Panuon.*","steam_api*","Facepunch.*","NAudio.*","Skia*","libSkia*","WpfAnimatedGif*")
$depDlls = Get-ChildItem -Path $hostBin -Filter "*.dll" -Recurse -ErrorAction SilentlyContinue |
    Where-Object {
        $name = $_.Name
        -not ($skipPatterns | Where-Object { $name -like $_ })
    }
foreach ($dll in $depDlls) {
    $dest = Join-Path $pluginPath $dll.Name
    if (-not (Test-Path $dest)) {
        Copy-Item $dll.FullName -Destination $pluginPath -Force
        Write-Host "  Copied dep: $($dll.Name)"
    }
}

# Copy LLamaSharp native libraries and other native DLLs to main app folder
# Native DLLs must be in the app root so the runtime can find them via P/Invoke
$nativeLibNames = @("llama.dll", "ggml.dll", "ggml-base.dll", "llava_shared.dll", "e_sqlite3.dll", "onnxruntime.dll", "espeak-ng.dll", "piper_phonemize.dll", "onnxruntime_providers_shared.dll")
$nativeLibs = Get-ChildItem -Path $hostBin -Filter "*.dll" -Recurse -ErrorAction SilentlyContinue |
    Where-Object { $nativeLibNames -contains $_.Name }
foreach ($lib in $nativeLibs) {
    $dest = Join-Path $OutputDir $lib.Name
    if (-not (Test-Path $dest)) {
        Copy-Item $lib.FullName -Destination $OutputDir -Force
        Write-Host "  Copied native to app root: $($lib.Name)"
    }
    # Also remove from plugin folder to avoid BadImageFormatException in loader
    $pluginCopy = Join-Path $pluginPath $lib.Name
    if (Test-Path $pluginCopy) {
        Remove-Item $pluginCopy -Force
        Write-Host "  Removed native from plugin: $($lib.Name)"
    }
}
Write-Host "  OK" -ForegroundColor Green
Write-Host ""

# Step 7b: Create appsettings.json
Write-Host "[7b/8] Creating config files..." -ForegroundColor Yellow
$appsettings = @'
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
      "Temperature": 0.7,
      "Endpoint": "http://localhost:11434",
      "ApiKey": ""
    },
    "Memory": {
      "DbPath": "data/cognition.db",
      "EmbeddingProvider": "local",
      "VectorDimensions": 384
    },
    "Voice": {
      "Enabled": false,
      "Stt": {
        "Provider": "whisper",
        "ExecutablePath": "whisper-cli.exe",
        "ModelPath": "models/ggml-base.bin",
        "Language": "auto",
        "Threads": 4
      },
      "Tts": {
        "Provider": "piper",
        "ExecutablePath": "piper.exe",
        "ModelPath": "models/en_US-amy-medium.onnx"
      }
    }
  }
}
'@
$appsettingsPath = Join-Path $OutputDir "appsettings.json"
[System.IO.File]::WriteAllText($appsettingsPath, $appsettings, $utf8NoBom)
Write-Host "  Created: appsettings.json"

# Create runtime directories
New-Item -ItemType Directory -Force -Path (Join-Path $OutputDir "models") | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $OutputDir "data") | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $OutputDir "logs") | Out-Null
Write-Host "  Created: models/, data/, logs/"

# Copy documentation
Copy-Item "INSTALL.md" -Destination $OutputDir -Force
Copy-Item "TESTING.md" -Destination $OutputDir -Force
if (Test-Path "docs/MODELS.md") {
    Copy-Item "docs/MODELS.md" -Destination $OutputDir -Force
    Write-Host "  Copied: INSTALL.md, TESTING.md, MODELS.md"
} elseif (Test-Path "wiki/AI-Models.md") {
    Copy-Item "wiki/AI-Models.md" -Destination $OutputDir -Force
    Write-Host "  Copied: INSTALL.md, TESTING.md, AI-Models.md"
} else {
    Write-Host "  Copied: INSTALL.md, TESTING.md (models doc not found)"
}
Write-Host "  OK" -ForegroundColor Green
Write-Host ""

# Step 7c: Copy optional native dependencies (Live2D, Piper, Whisper)
Write-Host "[7c/8] Copying optional native dependencies..." -ForegroundColor Yellow

# Live2D Cubism Core — required for Live2D model support
$live2dFiles = @("Live2DCubismCore.dll", "Live2DCubismCore.lib")
foreach ($f in $live2dFiles) {
    if (Test-Path $f) {
        Copy-Item $f -Destination $OutputDir -Force
        Write-Host "  Copied: $f"
    } else {
        Write-Host "  Skipped: $f (not found)" -ForegroundColor DarkGray
    }
}

# Piper TTS — extract piper_windows_amd64.zip to output/piper/
$piperZip = "piper_windows_amd64.zip"
if (Test-Path $piperZip) {
    $piperOut = Join-Path $OutputDir "piper"
    if (Test-Path $piperOut) { Remove-Item $piperOut -Recurse -Force }
    New-Item -ItemType Directory -Force -Path $piperOut | Out-Null
    Expand-Archive -Path $piperZip -DestinationPath $piperOut -Force
    # Find piper.exe and ensure it is at piper/piper.exe
    $piperExe = Get-ChildItem $piperOut -Filter "piper.exe" -Recurse | Select-Object -First 1
    if ($piperExe) {
        Write-Host "  Extracted: $piperZip -> piper/ ($($piperExe.Name))"
    } else {
        Write-Host "  Extracted: $piperZip -> piper/ (piper.exe not found inside)" -ForegroundColor Yellow
    }
} else {
    Write-Host "  Skipped: $piperZip (not found)" -ForegroundColor DarkGray
}

# Whisper STT — extract whisper-bin-x64.zip to output/whisper/
$whisperZip = "whisper-bin-x64.zip"
if (Test-Path $whisperZip) {
    $whisperOut = Join-Path $OutputDir "whisper"
    if (Test-Path $whisperOut) { Remove-Item $whisperOut -Recurse -Force }
    New-Item -ItemType Directory -Force -Path $whisperOut | Out-Null
    Expand-Archive -Path $whisperZip -DestinationPath $whisperOut -Force
    $whisperExe = Get-ChildItem $whisperOut -Filter "whisper-cli.exe" -Recurse | Select-Object -First 1
    if (-not $whisperExe) {
        $whisperExe = Get-ChildItem $whisperOut -Filter "whisper*.exe" -Recurse | Select-Object -First 1
    }
    if ($whisperExe) {
        Write-Host "  Extracted: $whisperZip -> whisper/ ($($whisperExe.Name))"
    } else {
        Write-Host "  Extracted: $whisperZip -> whisper/ (whisper exe not found inside)" -ForegroundColor Yellow
    }
} else {
    Write-Host "  Skipped: $whisperZip (not found)" -ForegroundColor DarkGray
}

Write-Host "  OK" -ForegroundColor Green
Write-Host ""

# Done
Write-Host "==========================================" -ForegroundColor Green
Write-Host "  BUILD COMPLETE - v$Version" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Output:" -ForegroundColor White
Write-Host "  Folder:     $OutputDir\" -ForegroundColor White
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Download qwen2.5-3b-instruct-q4_k_m.gguf from HuggingFace"
Write-Host "  2. Place it in the models\ folder"
Write-Host "  3. Run VPet-Simulator.Windows.exe"
Write-Host "  4. Click Chat on the pet''s toolbar"
Write-Host ""