<#
.SYNOPSIS
    Builds SlotLock, optionally installs it into a local Thunderstore profile for
    testing, and packs the Thunderstore upload zip.

.EXAMPLE
    .\build.ps1                      # build + pack dist\SlotLock-<version>.zip
    .\build.ps1 -Deploy              # build + copy the dll into your mod profile
    .\build.ps1 -Deploy -NoPackage   # fast iteration loop while testing in game
#>
[CmdletBinding()]
param(
    [string] $Configuration = 'Release',
    [string] $ValheimDir,
    [string] $ProfileName = 'Introverted Cats',
    [switch] $Deploy,
    [switch] $NoPackage
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

# ---- version comes from the csproj, everything else follows it ----------------
[xml]$proj = Get-Content (Join-Path $root 'SlotLock.csproj')
$version = ($proj.Project.PropertyGroup.Version | Where-Object { $_ }) | Select-Object -First 1
if (-not $version) { throw 'Could not read <Version> from SlotLock.csproj.' }
Write-Host "SlotLock $version" -ForegroundColor Cyan

# ---- build -------------------------------------------------------------------
$buildArgs = @('build', (Join-Path $root 'SlotLock.csproj'), '-c', $Configuration, '--nologo')
if ($ValheimDir) { $buildArgs += "-p:ValheimDir=$ValheimDir" }
& dotnet @buildArgs
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed with exit code $LASTEXITCODE." }

$dll = Join-Path $root "bin\$Configuration\SlotLock.dll"
if (-not (Test-Path $dll)) { throw "Expected build output not found: $dll" }

# ---- deploy to a local profile ----------------------------------------------
if ($Deploy) {
    $profileDir = Join-Path $env:APPDATA "Thunderstore Mod Manager\DataFolder\Valheim\profiles\$ProfileName"
    if (-not (Test-Path $profileDir)) {
        throw "Mod profile not found: $profileDir`nPass -ProfileName with the profile you actually use."
    }
    $target = Join-Path $profileDir 'BepInEx\plugins\SlotLock'
    New-Item -ItemType Directory -Force -Path $target | Out-Null
    Copy-Item $dll $target -Force
    Write-Host "Deployed to $target" -ForegroundColor Green
}

# ---- pack the Thunderstore zip ----------------------------------------------
if (-not $NoPackage) {
    $pkg = Join-Path $root 'package'

    # Keep manifest.json's version in step with the csproj.
    $manifestPath = Join-Path $pkg 'manifest.json'
    $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
    if ($manifest.version_number -ne $version) {
        $manifest.version_number = $version
        ($manifest | ConvertTo-Json -Depth 5) | Set-Content $manifestPath -Encoding utf8
        Write-Host "Updated manifest.json to $version" -ForegroundColor Yellow
    }

    # Thunderstore rejects uploads that break any of these.
    Add-Type -AssemblyName System.Drawing
    $img = [System.Drawing.Image]::FromFile((Join-Path $pkg 'icon.png'))
    $iconOk = ($img.Width -eq 256 -and $img.Height -eq 256)
    $img.Dispose()
    if (-not $iconOk)                                  { throw 'icon.png must be exactly 256x256.' }
    if ($manifest.name -notmatch '^[a-zA-Z0-9_]+$')    { throw 'manifest name may only contain letters, digits and underscores.' }
    if ($manifest.description.Length -gt 250)          { throw 'manifest description must be 250 characters or fewer.' }
    if (-not (Test-Path (Join-Path $pkg 'README.md'))) { throw 'README.md is required in the package.' }

    $staging = Join-Path $root "obj\staging"
    if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
    New-Item -ItemType Directory -Force -Path $staging | Out-Null

    Copy-Item (Join-Path $pkg '*') $staging -Recurse -Force
    Copy-Item $dll $staging -Force

    $dist = Join-Path $root 'dist'
    New-Item -ItemType Directory -Force -Path $dist | Out-Null
    $zip = Join-Path $dist "SlotLock-$version.zip"
    if (Test-Path $zip) { Remove-Item $zip -Force }

    Compress-Archive -Path (Join-Path $staging '*') -DestinationPath $zip
    Remove-Item $staging -Recurse -Force

    Write-Host "Packaged $zip" -ForegroundColor Green
    Get-ChildItem $zip | Select-Object Name, @{n='KB';e={[math]::Round($_.Length/1KB,1)}} | Format-Table -AutoSize
}
