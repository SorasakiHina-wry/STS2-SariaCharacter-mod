<#
Copies the built mod into <game>\mods\strikesprite.

Usage:
  .\tools\deploy.ps1
  .\tools\deploy.ps1 -GameDir 'D:\SteamLibrary\steamapps\common\Slay the Spire 2'

The game must be closed (the DLL is locked while it runs). Restart the game afterwards:
mods are only loaded at startup.
#>
param(
    [string]$GameDir = $env:STS2_GAME_DIR
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$out = Join-Path $root 'out'

if (-not $GameDir) {
    $GameDir = 'D:\SteamLibrary\steamapps\common\Slay the Spire 2'
}

$modId = (Get-Content (Join-Path $root 'mod_manifest.json') -Raw | ConvertFrom-Json).id
$target = Join-Path $GameDir ("mods\" + $modId)

if (-not (Test-Path (Join-Path $out 'strikesprite.dll'))) {
    throw "build output missing - run tools\build.ps1 first"
}

New-Item -ItemType Directory -Force -Path $target | Out-Null
Copy-Item (Join-Path $out '*') $target -Recurse -Force

Write-Host "deployed -> $target"
Get-ChildItem $target -Recurse -File | Select-Object FullName, Length
