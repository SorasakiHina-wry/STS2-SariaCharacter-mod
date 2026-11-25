#requires -Version 5.1
<#
Build the Saria character mod. Keeps dotnet's first-run/tool state inside the workspace so it
works under the DSH file sandbox (which forbids writes to %USERPROFILE%\.dotnet).

  pwsh -File saria\mods-src\saria\tools\build.ps1
#>
$ErrorActionPreference = 'Stop'

$ProjectDir = Split-Path -Parent $PSScriptRoot
$Workspace = Resolve-Path (Join-Path $ProjectDir '..\..\..')

$env:DOTNET_CLI_HOME = Join-Path $Workspace '.dotnet-home'
$env:NUGET_PACKAGES = Join-Path $Workspace '.nuget\packages'
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

Push-Location $ProjectDir
try {
    dotnet build -c Release
    if ($LASTEXITCODE -ne 0) { throw "build failed ($LASTEXITCODE)" }
}
finally {
    Pop-Location
}
