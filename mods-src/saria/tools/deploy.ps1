#requires -Version 5.1
<#
Deploy the Saria character mod into the game's mods folder.

The mods folder lives outside this workspace, so the DSH file sandbox blocks writes to it:
run this through an approved (elevated) command, or run it yourself in a normal shell.

  powershell -ExecutionPolicy Bypass -File saria\mods-src\saria\tools\deploy.ps1

Result:
  <game>\mods\saria\   saria.dll + mod_manifest.json
  (probe.pck / images / spine are copied by tools\package.ps1 or tools\apply.ps1)

BaseLib is NOT installed here: this machine already has it as a Steam Workshop mod
(v3.4.8, item 3737335127), and the game loads the newer of the two anyway.
#>
$ErrorActionPreference = 'Stop'

$GameDir = if ($env:STS2_GAME_DIR) { $env:STS2_GAME_DIR } else { 'D:\SteamLibrary\steamapps\common\Slay the Spire 2' }
$ModsDir = Join-Path $GameDir 'mods'
$ProjectDir = Split-Path -Parent $PSScriptRoot

if (-not (Test-Path $ModsDir)) { throw "Game mods folder not found: $ModsDir" }

$built = Join-Path $ProjectDir 'bin\Release\saria.dll'
if (-not (Test-Path $built)) { throw "Build output missing: $built (run tools\build.ps1 first)" }

$dest = Join-Path $ModsDir 'saria'
New-Item -ItemType Directory -Force -Path $dest | Out-Null
Copy-Item $built (Join-Path $dest 'saria.dll') -Force
Copy-Item (Join-Path $ProjectDir 'mod_manifest.json') (Join-Path $dest 'mod_manifest.json') -Force
Write-Host "installed saria -> $dest"

# Sanity check: BaseLib must be available at runtime (either Workshop or local).
$baseLibLocal = Test-Path (Join-Path $ModsDir 'BaseLib\BaseLib.dll')
$baseLibWorkshop = Test-Path 'D:\SteamLibrary\steamapps\workshop\content\2868840\3737335127\BaseLib\BaseLib.dll'
if (-not ($baseLibLocal -or $baseLibWorkshop)) {
    Write-Warning 'BaseLib was not found locally or in the Steam Workshop - the saria mod will fail its dependency check.'
}

Get-ChildItem $dest -File | Select-Object FullName, Length
