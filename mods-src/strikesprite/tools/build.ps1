<#
Builds the mod into out\ (Release by default).

Usage:
  .\tools\build.ps1
  .\tools\build.ps1 -Configuration Debug -GameDir 'D:\SteamLibrary\steamapps\common\Slay the Spire 2'
#>
param(
    [string]$Configuration = 'Release',
    [string]$GameDir = $env:STS2_GAME_DIR
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$out = Join-Path $root 'out'

if (-not $GameDir) {
    $GameDir = 'D:\SteamLibrary\steamapps\common\Slay the Spire 2'
}

Write-Host "game dir : $GameDir"
Write-Host "output   : $out"

dotnet build (Join-Path $root 'strikesprite.csproj') -c $Configuration -o $out -p:Sts2GameDir="$GameDir"
if ($LASTEXITCODE -ne 0) { throw "build failed ($LASTEXITCODE)" }

Get-ChildItem $out | Select-Object Name, Length
