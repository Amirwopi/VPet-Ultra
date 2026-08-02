# VPet-Ultra Windows Build Script
# Run this on Windows to build the full solution and package the cognition plugin.
#
# Usage:
#   .\build-windows.ps1
#
# Output:
#   publish/vpet-windows/          — Full VPet + cognition plugin ready to run
#   VPet-Ultra-Windows.zip         — Zipped version for distribution

param(
    [string]$Configuration = "Release",
    [string]$OutputDir = "publish/vpet-windows",
    [string]$ZipPath = "VPet-Ultra-Windows.zip"
)

$ErrorActionPreference = "Stop"

Write-Host "=== VPet-Ultra Windows Build ===" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration"
Write-Host "Output: $OutputDir"
Write-Host ""

# Step 1: Restore
Write-Host "[1/6] Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore VPet.sln
if ($LASTEXITCODE -ne 0) { Write-Host "Restore failed!" -ForegroundColor Red; exit 1 }

# Step 2: Build
Write-Host "[2/6] Building solution ($Configuration)..." -ForegroundColor Yellow
dotnet build VPet.sln -c $Configuration --no-restore
if ($LASTEXITCODE -ne 0) { Write-Host "Build failed!" -ForegroundColor Red; exit 1 }

# Step 3: Test
Write-Host "[3/6] Running tests..." -ForegroundColor Yellow
dotnet test VPet.Cognition.Tests/VPet.Cognition.Tests.csproj -c $Configuration --no-build --logger "console;verbosity=minimal"
if ($LASTEXITCODE -ne 0) { Write-Host "Tests failed!" -ForegroundColor Red; exit 1 }

# Step 4: Publish VPet
Write-Host "[4/6] Publishing VPet-Simulator.Windows..." -ForegroundColor Yellow
dotnet publish VPet-Simulator.Windows/VPet-Simulator.Windows.csproj -c $Configuration -r win-x64 --self-contained false -o $OutputDir
if ($LASTEXITCODE -ne 0) { Write-Host "Publish failed!" -ForegroundColor Red; exit 1 }

# Step 5: Copy cognition DLLs to mod folder
Write-Host "[5/6] Copying cognition plugin DLLs..." -ForegroundColor Yellow
$modPath = Join-Path $OutputDir "mod/0000_cognition"
New-Item -ItemType Directory -Force -Path $modPath | Out-Null

$hostBin = "VPet.Cognition.Host/bin/$Configuration/net8.0-windows"
if (-not (Test-Path $hostBin)) {
    # Try publish folder
    $hostBin = "VPet.Cognition.Host/bin/$Configuration/net8.0-windows/publish"
}

$cognitionDlls = Get-ChildItem -Path $hostBin -Filter "VPet.Cognition.*.dll" -Recurse -ErrorAction SilentlyContinue
foreach ($dll in $cognitionDlls) {
    Copy-Item $dll.FullName -Destination $modPath -Force
    Write-Host "  Copied: $($dll.Name)"
}

# Copy dependency DLLs (non-VPet ones)
$depDlls = Get-ChildItem -Path $hostBin -Filter "*.dll" -Recurse -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -notlike "VPet.Cognition.*" -and $_.Name -notlike "VPet-Simulator.*" -and $_.Name -notlike "LinePutScript.*" }
foreach ($dll in $depDlls) {
    $dest = Join-Path $modPath $dll.Name
    if (-not (Test-Path $dest)) {
        Copy-Item $dll.FullName -Destination $modPath -Force
        Write-Host "  Copied dep: $($dll.Name)"
    }
}

# Step 6: Create appsettings.json
Write-Host "[6/6] Creating appsettings.json..." -ForegroundColor Yellow
$appsettings = @'
{
  "Cognition": {
    "Enabled": true,
    "Llm": {
      "Provider": "ollama",
      "Endpoint": "http://localhost:11434",
      "Model": "qwen2.5:3b",
      "ApiKey": "",
      "Timeout": "00:01:00",
      "MaxTokens": 1024,
      "Temperature": 0.7
    },
    "Memory": {
      "DbPath": "data/cognition.db"
    },
    "Voice": {
      "Enabled": false,
      "Stt": {
        "Provider": "whisper",
        "ExecutablePath": "whisper-cli",
        "ModelPath": "models/ggml-base.en.bin",
        "Language": "auto",
        "Threads": 4
      },
      "Tts": {
        "Provider": "piper",
        "ExecutablePath": "piper",
        "ModelPath": "models/en_US-amy-medium.onnx"
      }
    }
  }
}
'@
$appsettingsPath = Join-Path $OutputDir "appsettings.json"
$appsettings | Out-File -FilePath $appsettingsPath -Encoding UTF8
Write-Host "  Created: $appsettingsPath"

# Create models directory
$modelsPath = Join-Path $OutputDir "models"
New-Item -ItemType Directory -Force -Path $modelsPath | Out-Null

# Create data directory
$dataPath = Join-Path $OutputDir "data"
New-Item -ItemType Directory -Force -Path $dataPath | Out-Null

# Create logs directory
$logsPath = Join-Path $OutputDir "logs"
New-Item -ItemType Directory -Force -Path $logsPath | Out-Null

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
Write-Host "  1. Install Ollama: https://ollama.com"
Write-Host "  2. Run: ollama pull qwen2.5:3b"
Write-Host "  3. (Optional) Download voice models — see docs/MODELS.md"
Write-Host "  4. Run VPet-Simulator.Windows.exe"
Write-Host "  5. Click the Chat button on the pet's toolbar"
Write-Host ""