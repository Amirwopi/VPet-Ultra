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

# Step 6: Build cognition Host
Write-Host "[6/8] Building cognition Host..." -ForegroundColor Yellow
dotnet restore VPet.Cognition.Host/VPet.Cognition.Host.csproj
dotnet build VPet.Cognition.Host/VPet.Cognition.Host.csproj -c $Configuration
if ($LASTEXITCODE -ne 0) { Write-Host "Cognition Host build failed!" -ForegroundColor Red; exit 1 }
Write-Host "  OK" -ForegroundColor Green
Write-Host ""

# Step 7: Copy cognition plugin to mod folder
Write-Host "[7/8] Copying cognition plugin..." -ForegroundColor Yellow
$modPath = Join-Path $OutputDir "mod/0000_cognition"
New-Item -ItemType Directory -Force -Path $modPath | Out-Null

# Create info.lps — VPet REQUIRES this to recognize a mod folder
$infoLps = "vupmod#Cognition:|author#VPet-Ultra:|gamever#11000:|ver#100:|intro#AI Cognition Plugin - Offline LLM, Memory, Personality, Emotion, Voice, Agent:|authorid#0:|itemid#0:|lang#en:|Cognition#Cognition:|"
$infoLps | Out-File -FilePath (Join-Path $modPath "info.lps") -Encoding UTF8
Write-Host "  Created: info.lps"

# Create version.txt
$Version | Out-File -FilePath (Join-Path $modPath "version.txt") -Encoding UTF8
Write-Host "  Created: version.txt ($Version)"

# Copy cognition DLLs
$hostBin = "VPet.Cognition.Host/bin/$Configuration/net8.0-windows"
if (-not (Test-Path $hostBin)) {
    # Try publish subfolder
    $hostBin = "VPet.Cognition.Host/bin/$Configuration/net8.0-windows/publish"
}

$cognitionDlls = Get-ChildItem -Path $hostBin -Filter "VPet.Cognition.*.dll" -Recurse -ErrorAction SilentlyContinue
foreach ($dll in $cognitionDlls) {
    $dest = Join-Path $modPath $dll.Name
    if (-not (Test-Path $dest)) {
        Copy-Item $dll.FullName -Destination $modPath -Force
        Write-Host "  Copied: $($dll.Name)"
    }
}

# Copy dependency DLLs — skip ones already in main app
$skipPatterns = @("VPet.Cognition.*","VPet-Simulator.*","LinePutScript.*","Panuon.*","steam_api*","Facepunch.*","NAudio.*","Skia*","libSkia*","WpfAnimatedGif*")
$depDlls = Get-ChildItem -Path $hostBin -Filter "*.dll" -Recurse -ErrorAction SilentlyContinue |
    Where-Object {
        $name = $_.Name
        -not ($skipPatterns | Where-Object { $name -like $_ })
    }
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
$appsettings | Out-File -FilePath $appsettingsPath -Encoding UTF8
Write-Host "  Created: appsettings.json"

# Create runtime directories
New-Item -ItemType Directory -Force -Path (Join-Path $OutputDir "models") | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $OutputDir "data") | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $OutputDir "logs") | Out-Null
Write-Host "  Created: models/, data/, logs/"

# Copy documentation
Copy-Item "INSTALL.md" -Destination $OutputDir -Force
Copy-Item "TESTING.md" -Destination $OutputDir -Force
Copy-Item "docs/MODELS.md" -Destination $OutputDir -Force
Write-Host "  Copied: INSTALL.md, TESTING.md, MODELS.md"
Write-Host "  OK" -ForegroundColor Green
Write-Host ""

# Step 8: Zip
Write-Host "[8/8] Creating zip packages..." -ForegroundColor Yellow

# Full zip
$fullZip = "VPet-Ultra-Full-v$Version.zip"
if (Test-Path $fullZip) { Remove-Item $fullZip -Force }
Compress-Archive -Path "$OutputDir/*" -DestinationPath $fullZip -Force
$fullSize = [math]::Round((Get-Item $fullZip).Length / 1MB, 1)
Write-Host "  Full zip: $fullZip ($fullSize MB)"

# Plugin-only zip (for updates)
$pluginZip = "VPet-Ultra-Plugin-v$Version.zip"
$pluginPath = "publish/vpet-cognition-update"
New-Item -ItemType Directory -Force -Path $pluginPath | Out-Null
Copy-Item -Path $modPath -Destination "$pluginPath/mod/0000_cognition" -Recurse -Force
Copy-Item -Path $appsettingsPath -Destination $pluginPath -Force
Copy-Item "INSTALL.md" -Destination $pluginPath -Force
Copy-Item "TESTING.md" -Destination $pluginPath -Force
Copy-Item "docs/MODELS.md" -Destination $pluginPath -Force

if (Test-Path $pluginZip) { Remove-Item $pluginZip -Force }
Compress-Archive -Path "$pluginPath/*" -DestinationPath $pluginZip -Force
$pluginSize = [math]::Round((Get-Item $pluginZip).Length / 1MB, 1)
Write-Host "  Plugin zip: $pluginZip ($pluginSize MB)"

# Cleanup temp
Remove-Item $pluginPath -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "  OK" -ForegroundColor Green
Write-Host ""

# Done
Write-Host "==========================================" -ForegroundColor Green
Write-Host "  BUILD COMPLETE — v$Version" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Output:" -ForegroundColor White
Write-Host "  Folder:     $OutputDir\" -ForegroundColor White
Write-Host "  Full zip:   $fullZip ($fullSize MB)" -ForegroundColor White
Write-Host "  Plugin zip: $pluginZip ($pluginSize MB)" -ForegroundColor White
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Download qwen2.5-3b-instruct-q4_k_m.gguf from HuggingFace"
Write-Host "  2. Place it in the models\ folder"
Write-Host "  3. Run VPet-Simulator.Windows.exe"
Write-Host "  4. Click Chat on the pet's toolbar"
Write-Host ""
Write-Host "To distribute:" -ForegroundColor Cyan
Write-Host "  - Full zip: for new users (first install)"
Write-Host "  - Plugin zip: for updates (replace mod\0000_cognition\ only)"
Write-Host ""