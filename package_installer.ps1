# Package Installer Script
# Automates the build and packaging of payload.zip for ActivityMonitor.Installer

$ErrorActionPreference = "Stop"
$ScriptDir = $PSScriptRoot
$TempDir = Join-Path $ScriptDir "TempBuild"
$PayloadDir = Join-Path $TempDir "payload"

Write-Host "Starting Build & Package Process..." -ForegroundColor Cyan

# 1. Cleanup
if (Test-Path $TempDir) { Remove-Item $TempDir -Recurse -Force }
New-Item -ItemType Directory -Path $PayloadDir | Out-Null
New-Item -ItemType Directory -Path (Join-Path $PayloadDir "Background") | Out-Null
New-Item -ItemType Directory -Path (Join-Path $PayloadDir "UI") | Out-Null

# 1.5 Read Version
$MonitorConfig = Get-Content (Join-Path $ScriptDir "monitor.json") | ConvertFrom-Json
$Version = $MonitorConfig.version
Write-Host "Build Version: $Version" -ForegroundColor Cyan

# 2. Build & Publish Projects
Write-Host "Building Projects..." -ForegroundColor Yellow

# Background (To Payload/Background)
Write-Host "  - ActivityMonitor.Background..."
dotnet publish "$ScriptDir\ActivityMonitor.Background\ActivityMonitor.Background.csproj" -c Release -r win-x64 --self-contained false -o (Join-Path $PayloadDir "Background") /p:Version=$Version | Out-Null

# UI (To Payload/UI)
Write-Host "  - ActivityMonitor.UI..."
dotnet publish "$ScriptDir\ActivityMonitor.UI\ActivityMonitor.UI.csproj" -c Release -r win-x64 --self-contained false -o (Join-Path $PayloadDir "UI") /p:Version=$Version | Out-Null

# Uninstaller (To Payload Root)
Write-Host "  - ActivityMonitor.Uninstaller..."
dotnet publish "$ScriptDir\ActivityMonitor.Uninstaller\ActivityMonitor.Uninstaller.csproj" -c Release -r win-x64 --self-contained false -o $PayloadDir /p:Version=$Version | Out-Null

# Copy Logo (To Payload Root as logo.svg)
$LogoSource = Join-Path $ScriptDir "activity-monitor-logo.svg"
if (Test-Path $LogoSource) {
    Copy-Item $LogoSource (Join-Path $PayloadDir "logo.svg")
}

# Copy monitor.json (To Payload Root)
Copy-Item (Join-Path $ScriptDir "monitor.json") (Join-Path $PayloadDir "monitor.json")

# Copy Frontend Assets (To Payload/application)
$AppSource = Join-Path $ScriptDir "ActivityMonitor.UI\application"
$AppDest = Join-Path $PayloadDir "application"
if (Test-Path $AppSource) {
    Copy-Item $AppSource $AppDest -Recurse -Force
}

# 3. Create Payload.zip
Write-Host "Creating payload.zip..." -ForegroundColor Yellow
$ZipPath = Join-Path $TempDir "payload.zip"
Compress-Archive -Path "$PayloadDir\*" -DestinationPath $ZipPath -Force

# 4. Copy to Installer Project
Write-Host "Updating Installer Resource..." -ForegroundColor Yellow
$InstallerResDir = Join-Path $ScriptDir "ActivityMonitor.Installer"
Copy-Item $ZipPath -Destination (Join-Path $InstallerResDir "payload.zip") -Force

# 5. Build Installer
Write-Host "Building Installer..." -ForegroundColor Yellow
dotnet build "$ScriptDir\ActivityMonitor.Installer\ActivityMonitor.Installer.csproj" -c Release
# Note: Installer is .NET 4.8, usually doesn't support 'publish' same way, build is enough.

# 6. Cleanup
Remove-Item $TempDir -Recurse -Force

Write-Host "`n--------------------------------------------------" -ForegroundColor Green
Write-Host "Packaging Complete!" -ForegroundColor Green
Write-Host "Installer is ready at: ActivityMonitor.Installer\bin\Release\net48\ActivityMonitor.Installer.exe"
Write-Host "--------------------------------------------------" -ForegroundColor Green
