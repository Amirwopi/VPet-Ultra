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
# Use the win-x64 runtime version (not the ARM version that NuGet puts first)
$nativeLibNames = @("llama.dll", "ggml.dll", "ggml-base.dll", "llava_shared.dll", "e_sqlite3.dll", "onnxruntime.dll", "espeak-ng.dll", "piper_phonemize.dll", "onnxruntime_providers_shared.dll")
foreach ($name in $nativeLibNames) {
    # First try win-x64 runtime folder
    $src = Get-ChildItem -Path $hostBin -Recurse -Filter $name -ErrorAction SilentlyContinue | Where-Object { $_.FullName -like "*win-x64*" } | Select-Object -First 1
    # Fallback to any copy
    if (-not $src) { $src = Get-ChildItem -Path $hostBin -Recurse -Filter $name -ErrorAction SilentlyContinue | Select-Object -First 1 }
    if ($src) {
        $dest = Join-Path $OutputDir $name
        Copy-Item $src.FullName -Destination $dest -Force
        Write-Host "  Copied native to app root: $name"
    }
    # Remove from plugin folder to avoid BadImageFormatException in loader
    $pluginCopy = Join-Path $pluginPath $name
    if (Test-Path $pluginCopy) {
        Remove-Item $pluginCopy -Force
        Write-Host "  Removed native from plugin: $name"
    }
}
Write-Host "  OK" -ForegroundColor Green
Write-Host ""

# Step 7b: Copy model files from repo models/ folder to publish folder
Write-Host "[7b/8] Copying model files..." -ForegroundColor Yellow
$modelsSource = "models"
$modelsDest = Join-Path $OutputDir "models"
if (Test-Path $modelsSource) {
    # Copy piper models
    if (Test-Path "$modelsSource\piper") {
        New-Item -ItemType Directory -Force -Path "$modelsDest\piper" | Out-Null
        Get-ChildItem "$modelsSource\piper" -Filter "*.onnx" | ForEach-Object {
            Copy-Item $_.FullName "$modelsDest\piper\" -Force
            Write-Host "  Copied piper model: $($_.Name)"
        }
        # Copy .json config files if present
        Get-ChildItem "$modelsSource\piper" -Filter "*.json" | ForEach-Object {
            Copy-Item $_.FullName "$modelsDest\piper\" -Force
        }
    }
    # Copy whisper models
    if (Test-Path "$modelsSource\whisper") {
        New-Item -ItemType Directory -Force -Path "$modelsDest\whisper" | Out-Null
        Get-ChildItem "$modelsSource\whisper" -Filter "*.gguf" | ForEach-Object {
            Copy-Item $_.FullName "$modelsDest\whisper\" -Force
            Write-Host "  Copied whisper model: $($_.Name)"
        }
        Get-ChildItem "$modelsSource\whisper" -Filter "*.bin" | ForEach-Object {
            Copy-Item $_.FullName "$modelsDest\whisper\" -Force
            Write-Host "  Copied whisper model: $($_.Name)"
        }
    }
    # Copy LLM models (gguf)
    Get-ChildItem $modelsSource -Filter "*.gguf" -ErrorAction SilentlyContinue | ForEach-Object {
        Copy-Item $_.FullName $modelsDest -Force
        Write-Host "  Copied LLM model: $($_.Name)"
    }
    Write-Host "  OK" -ForegroundColor Green
} else {
    Write-Host "  models/ folder not found, skipping" -ForegroundColor Yellow
}
Write-Host ""

# Step 7c: Create appsettings.json
Write-Host "[7c/8] Creating config files..." -ForegroundColor Yellow
$appsettings = @'
{
  "Cognition": {
    "Enabled": true,
    "Llm": {
      "Provider": "ollama",
      "Endpoint": "http://46.105.71.45:11434",
      "Model": "qwen2.5vl:7b",
      "ApiKey": "",
      "MaxTokens": 1024,
      "Temperature": 0.7,
      "Timeout": "00:02:00",
      "GpuLayerCount": 0,
      "ContextSize": 4096,
      "ModelsDirectory": "models",
      "EnableVision": true
    },
    "Memory": {
      "DbPath": "data/cognition.db",
      "Encrypt": false,
      "VectorDimensions": 768,
      "EmbeddingProvider": "local",
      "EmbeddingModel": "models/bge-small.onnx",
      "ConsolidationInterval": "00:05:00",
      "ForgettingInterval": "1.00:00:00",
      "MaxDbSizeMb": 500
    },
    "Personality": {
      "Seed": {
        "Name": "Vpet",
        "Description": "A desktop companion."
      },
      "EvolutionRate": 0.02,
      "ReflectionIntervalTurns": 10,
      "EvaluationIntervalTurns": 5,
      "CoreTraitMaxDrift": 0.1
    },
    "Emotion": {
      "BlendFactor": 0.3,
      "DecayRatePerSec": 0.01,
      "PersistInterval": "00:01:00",
      "AntagonisticSuppression": 0.5,
      "SentimentMethod": "rule"
    },
    "Voice": {
      "Enabled": true,
      "BargeIn": true,
      "ContinuousMode": false,
      "SilenceTimeoutSec": 3,
      "Stt": {
        "Provider": "handy",
        "ExecutablePath": "C:/Users/Amirwopi/AppData/Local/Handy/handy.exe",
        "ModelPath": "models/whisper/whisper-medium-Q8_0.gguf",
        "ModelId": "handy-computer/whisper-medium-gguf/whisper-medium-Q8_0.gguf",
        "Language": "fa",
        "Threads": 4,
        "Device": "cpu"
      },
      "Tts": {
        "Provider": "piper",
        "ExecutablePath": "piper/piper/piper.exe",
        "ModelPath": "models/piper/fa_IR-mana-medium.onnx",
        "LengthScale": 1.0,
        "NoiseScale": 0.667,
        "NoiseW": 0.8,
        "Device": "cpu"
      }
    },
    "Tools": {
      "Enabled": true,
      "TimeoutSec": 30,
      "PromptTimeoutSec": 30
    },
    "Agent": {
      "Enabled": true,
      "MaxStepsPerPlan": 10,
      "MaxRetries": 3,
      "ConfidenceThreshold": 0.5,
      "MinConfidence": 0.3,
      "MaxConcurrentGoals": 3
    },
    "Animation": {
      "EmotionBiasEnabled": true,
      "EmotionOverrideThreshold": 0.6,
      "LipSyncEnabled": true,
      "LipSyncVisemeCount": 5
    },
    "Logging": {
      "Level": "Information",
      "File": "logs/cognition-.log",
      "Console": true
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