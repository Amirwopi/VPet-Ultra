# VPet-Ultra Windows Build Script
# Run this on Windows to build the full solution and package the cognition plugin.
#
# Usage:
#   .\build-windows.ps1
#
# Output:
#   publish/vpet-ultra/          — Full VPet + cognition plugin ready to run
#   VPet-Ultra-Windows.zip       — Zipped version for distribution

param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64",
    [string]$OutputDir = "publish/vpet-ultra",
    [string]$ZipPath = "VPet-Ultra-Windows.zip"
)

$ErrorActionPreference = "Stop"

Write-Host "=== VPet-Ultra Windows Build ===" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration"
Write-Host "Platform: $Platform"
Write-Host "Output: $OutputDir"
Write-Host ""

# Step 1: Restore
Write-Host "[1/7] Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore VPet.sln
if ($LASTEXITCODE -ne 0) { Write-Host "Restore failed!" -ForegroundColor Red; exit 1 }

# Step 2: Build solution
Write-Host "[2/7] Building solution ($Configuration $Platform)..." -ForegroundColor Yellow
dotnet build VPet.sln -c $Configuration -p:Platform=$Platform --no-restore
if ($LASTEXITCODE -ne 0) { Write-Host "Build failed!" -ForegroundColor Red; exit 1 }

# Step 3: Test
Write-Host "[3/7] Running tests..." -ForegroundColor Yellow
dotnet test VPet.Cognition.Tests/VPet.Cognition.Tests.csproj -c $Configuration --no-build --logger "console;verbosity=minimal"
if ($LASTEXITCODE -ne 0) { Write-Host "Tests failed!" -ForegroundColor Red; exit 1 }

# Step 4: Publish VPet-Simulator.Windows
Write-Host "[4/7] Publishing VPet-Simulator.Windows ($Platform)..." -ForegroundColor Yellow
dotnet publish VPet-Simulator.Windows/VPet-Simulator.Windows.csproj -c $Configuration -p:Platform=$Platform -r win-$Platform --self-contained false -o $OutputDir
if ($LASTEXITCODE -ne 0) { Write-Host "Publish failed!" -ForegroundColor Red; exit 1 }

# Step 4b: Copy mod data (essential game data not included by dotnet publish)
Write-Host "[4b/7] Copying mod data..." -ForegroundColor Yellow
$srcMod = "VPet-Simulator.Windows/mod"
$dstMod = Join-Path $OutputDir "mod"
if (Test-Path $dstMod) { Remove-Item $dstMod -Recurse -Force }
Copy-Item -Path $srcMod -Destination $dstMod -Recurse -Force
$modFileCount = (Get-ChildItem $dstMod -Recurse | Measure-Object).Count
Write-Host "  Copied mod folder ($modFileCount files)"

# Step 5: Build cognition Host
Write-Host "[5/7] Building cognition Host..." -ForegroundColor Yellow
dotnet restore VPet.Cognition.Host/VPet.Cognition.Host.csproj
dotnet build VPet.Cognition.Host/VPet.Cognition.Host.csproj -c $Configuration
if ($LASTEXITCODE -ne 0) { Write-Host "Cognition Host build failed!" -ForegroundColor Red; exit 1 }

# Step 6: Copy cognition plugin DLLs to mod folder
Write-Host "[6/7] Copying cognition plugin DLLs..." -ForegroundColor Yellow
$modPath = Join-Path $OutputDir "mod/0000_cognition"
New-Item -ItemType Directory -Force -Path $modPath | Out-Null

$hostBin = "VPet.Cognition.Host/bin/$Configuration/net8.0-windows"
if (-not (Test-Path $hostBin)) {
    $hostBin = "VPet.Cognition.Host/bin/$Configuration/net8.0-windows/publish"
}

# Copy all cognition DLLs
$cognitionDlls = Get-ChildItem -Path $hostBin -Filter "VPet.Cognition.*.dll" -Recurse -ErrorAction SilentlyContinue
foreach ($dll in $cognitionDlls) {
    Copy-Item $dll.FullName -Destination $modPath -Force
    Write-Host "  Copied: $($dll.Name)"
}

# Copy dependency DLLs (non-VPet, non-LinePutScript)
$depDlls = Get-ChildItem -Path $hostBin -Filter "*.dll" -Recurse -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -notlike "VPet.Cognition.*" -and $_.Name -notlike "VPet-Simulator.*" -and $_.Name -notlike "LinePutScript.*" }
foreach ($dll in $depDlls) {
    $dest = Join-Path $modPath $dll.Name
    if (-not (Test-Path $dest)) {
        Copy-Item $dll.FullName -Destination $modPath -Force
        Write-Host "  Copied dep: $($dll.Name)"
    }
}

# Copy LLamaSharp native libraries
$nativeLibs = Get-ChildItem -Path $hostBin -Filter "*.dll" -Recurse -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -like "llama.dll" -or $_.Name -like "ggml*.dll" -or $_.Name -like "cuda*.dll" }
foreach ($lib in $nativeLibs) {
    $dest = Join-Path $modPath $lib.Name
    if (-not (Test-Path $dest)) {
        Copy-Item $lib.FullName -Destination $modPath -Force
        Write-Host "  Copied native: $($lib.Name)"
    }
}

# Step 7: Create appsettings.json and directories
Write-Host "[7/7] Creating appsettings.json and directories..." -ForegroundColor Yellow
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
      "DbPath": "data/cognition.db"
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
$appsettings | Out-File -FilePath $appsettingsPath -Encoding UTF8
Write-Host "  Created: $appsettingsPath"

# Create runtime directories
New-Item -ItemType Directory -Force -Path (Join-Path $OutputDir "models") | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $OutputDir "data") | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $OutputDir "logs") | Out-Null

# Copy documentation
Copy-Item "INSTALL.md" -Destination $OutputDir -Force
Copy-Item "TESTING.md" -Destination $OutputDir -Force
Copy-Item "docs/MODELS.md" -Destination $OutputDir -Force

# Zip
Write-Host ""
Write-Host "Zipping output..." -ForegroundColor Yellow
if (Test-Path $ZipPath) { Remove-Item $ZipPath -Force }
Compress-Archive -Path "$OutputDir/*" -DestinationPath $ZipPath -Force
Write-Host "  Created: $ZipPath"

Write-Host ""
Write-Host "=== Build Complete ===" -ForegroundColor Green
Write-Host "Output folder: $OutputDir"
Write-Host "Zip file: $ZipPath"
Write-Host ""
Write-Host "Next steps:"
Write-Host "  1. Download qwen2.5-3b-instruct-q4_k_m.gguf from HuggingFace"
Write-Host "  2. Place it in the models/ folder"
Write-Host "  3. (Optional) Download voice models — see MODELS.md"
Write-Host "  4. Run VPet-Simulator.Windows.exe"
Write-Host "  5. Click the Chat button on the pet's toolbar"
Write-Host ""