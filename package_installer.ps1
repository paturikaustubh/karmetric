# Package Installer Script
# Automates the build and packaging of payload.zip for Karmetric.Installer

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
Write-Host "  - Karmetric.Background..."
dotnet publish "$ScriptDir\Karmetric.Background\Karmetric.Background.csproj" -c Release -r win-x64 --self-contained false -o (Join-Path $PayloadDir "Background") /p:Version=$Version

# Web (React SPA)
Write-Host "  - Karmetric.Web (React)..."
Push-Location "$ScriptDir\Karmetric.Web"
try {
    # Check if node_modules exists to save time? No, safer to ensure install
    # Use cmd /c for npm to avoid powershell parsing issues
    cmd /c "npm install"
    cmd /c "npm run build"
}
finally {
    Pop-Location
}

# Copy Web Assets (To Payload/Background/wwwroot)
$WebDist = Join-Path $ScriptDir "Karmetric.Web\dist"
$WebTarget = Join-Path $PayloadDir "Background\wwwroot"
New-Item -ItemType Directory -Path $WebTarget -Force | Out-Null
if (Test-Path $WebDist) {
    Copy-Item "$WebDist\*" $WebTarget -Recurse -Force
} else {
    Write-Error "Web build failed! 'dist' folder not found."
}

# Uninstaller (To Payload Root)
Write-Host "  - Karmetric.Uninstaller..."
dotnet publish "$ScriptDir\Karmetric.Uninstaller\Karmetric.Uninstaller.csproj" -c Release -r win-x64 --self-contained false -o $PayloadDir /p:Version=$Version | Out-Null

# Copy Logo (To Payload Root as logo.svg)
$LogoSource = Join-Path $ScriptDir "karmetric-logo.svg"
if (Test-Path $LogoSource) {
    Copy-Item $LogoSource (Join-Path $PayloadDir "logo.svg")
}

# Copy monitor.json (To Payload Root)
Copy-Item (Join-Path $ScriptDir "monitor.json") (Join-Path $PayloadDir "monitor.json")

# 3. Create Payload.zip
Write-Host "Creating payload.zip..." -ForegroundColor Yellow
Write-Host "listing payload contents:"
Get-ChildItem -Recurse $PayloadDir | Select-Object Name, Length | Format-Table
$ZipPath = Join-Path $TempDir "payload.zip"
Compress-Archive -Path "$PayloadDir\*" -DestinationPath $ZipPath -Force

# 4. Copy to Installer Project
Write-Host "Updating Installer Resource..." -ForegroundColor Yellow
$InstallerResDir = Join-Path $ScriptDir "Karmetric.Installer"
Copy-Item $ZipPath -Destination (Join-Path $InstallerResDir "payload.zip") -Force

# 5. Build Installer
Write-Host "Building Installer..." -ForegroundColor Yellow
dotnet build "$ScriptDir\Karmetric.Installer\Karmetric.Installer.csproj" -c Release

# 5.1 Rename Output to Karmetric.Installer.exe
$BinDir = Join-Path $ScriptDir "Karmetric.Installer\bin\Release\net48"
if (Test-Path (Join-Path $BinDir "Karmetric.Installer.exe")) {
    Move-Item (Join-Path $BinDir "Karmetric.Installer.exe") (Join-Path $BinDir "Karmetric.Installer.exe") -Force
}
if (Test-Path (Join-Path $BinDir "Karmetric.Installer.exe.config")) {
    Move-Item (Join-Path $BinDir "Karmetric.Installer.exe.config") (Join-Path $BinDir "Karmetric.Installer.exe.config") -Force
}

# 6. Cleanup
Remove-Item $TempDir -Recurse -Force

Write-Host "`n--------------------------------------------------" -ForegroundColor Green
Write-Host "Packaging Complete!" -ForegroundColor Green
Write-Host "Installer is ready at: Karmetric.Installer\bin\Release\net48\Karmetric.Installer.exe"
Write-Host "--------------------------------------------------" -ForegroundColor Green
