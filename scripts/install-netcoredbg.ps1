# Installs netcoredbg so CLion can debug .NET 8 code.
#
# netcoredbg is NOT available as a dotnet global tool. It must be downloaded
# from GitHub releases and placed on PATH.
#
# Usage:
#   .\scripts\install-netcoredbg.ps1
#
# After install, re-run CMake configure in CLion so it picks up netcoredbg.

$ErrorActionPreference = 'Stop'

$installDir = Join-Path $env:USERPROFILE 'netcoredbg'
$binDir = Join-Path $installDir 'bin'

Write-Host 'Installing netcoredbg...' -ForegroundColor Cyan

# Detect architecture
$arch = if ([Environment]::Is64BitOperatingSystem) { 'x64' } else { 'x86' }

# Get the latest release URL from GitHub API
$apiUrl = 'https://api.github.com/repos/Samsung/netcoredbg/releases/latest'
Write-Host "Fetching latest release info..." -ForegroundColor Yellow
$release = Invoke-RestMethod -Uri $apiUrl -Headers @{ 'User-Agent' = 'VPet-Ultra' }

# Find the Windows asset
$asset = $release.assets | Where-Object {
    $_.name -match "windows.*$arch" -or $_.name -match "win.*$arch"
} | Select-Object -First 1

if (-not $asset) {
    # Fallback: pick any windows zip
    $asset = $release.assets | Where-Object { $_.name -match "windows|win" } | Select-Object -First 1
}

if (-not $asset) {
    Write-Host 'ERROR: Could not find a Windows release asset.' -ForegroundColor Red
    Write-Host "Browse: https://github.com/Samsung/netcoredbg/releases" -ForegroundColor Yellow
    exit 1
}

$downloadUrl = $asset.browser_download_url
$zipPath = Join-Path $env:TEMP $asset.name

Write-Host "Downloading: $downloadUrl" -ForegroundColor Yellow
Invoke-WebRequest -Uri $downloadUrl -OutFile $zipPath

# Extract
if (Test-Path $installDir) { Remove-Item $installDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path $installDir | Out-Null
Write-Host "Extracting to: $installDir" -ForegroundColor Yellow
Expand-Archive -Path $zipPath -DestinationPath $installDir -Force
Remove-Item $zipPath -Force

# Find the netcoredbg.exe
$exe = Get-ChildItem -Path $installDir -Filter 'netcoredbg.exe' -Recurse | Select-Object -First 1
if (-not $exe) {
    Write-Host 'ERROR: netcoredbg.exe not found in archive.' -ForegroundColor Red
    exit 1
}

Write-Host "Installed: $($exe.FullName)" -ForegroundColor Green

# Add to user PATH (persistent)
$userPath = [Environment]::GetEnvironmentVariable('PATH', 'User')
$exeDir = Split-Path $exe.FullName
if ($userPath -and ($userPath -split ';') -notcontains $exeDir) {
    [Environment]::SetEnvironmentVariable('PATH', "$exeDir;$userPath", 'User')
    Write-Host "Added to user PATH: $exeDir" -ForegroundColor Green
} elseif (-not $userPath) {
    [Environment]::SetEnvironmentVariable('PATH', $exeDir, 'User')
    Write-Host "Set user PATH: $exeDir" -ForegroundColor Green
} else {
    Write-Host "Already on PATH: $exeDir" -ForegroundColor Yellow
}

# Also set for this session
$env:PATH = "$exeDir;$env:PATH"

Write-Host ''
Write-Host 'netcoredbg installed successfully.' -ForegroundColor Green
Write-Host ''
Write-Host 'Next steps:' -ForegroundColor Cyan
Write-Host '  1. Restart CLion (so it picks up the new PATH)'
Write-Host '  2. Open VPet-Ultra project'
Write-Host '  3. Tools > CMake > Reload CMake Project'
Write-Host '  4. Pick "Debug VPet.Windows" or "Debug VPet.Solution" from the toolbar'
Write-Host '  5. Set breakpoints in .cs files and press the Debug button'