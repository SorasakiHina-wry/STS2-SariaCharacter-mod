#requires -Version 5.1
<#
Bootstrap the STS2 modding toolchain once, outside the file sandbox:
  1. install ilspycmd as a local dotnet tool under the workspace
  2. restore + build the two bundled test mods
  3. copy them into the game's mods/ folder
Everything lands inside D:\STS2 except the final mod install into the game dir.
#>
$ErrorActionPreference = 'Stop'

$Vendor = 'D:\STS2\vendor\sts2-modding-mcp'
$GameDir = 'D:\SteamLibrary\steamapps\common\Slay the Spire 2'
$NugetCache = 'D:\STS2\.nuget'
$ToolsDir = 'D:\STS2\.tools\ilspycmd'

$env:STS2_GAME_DIR = $GameDir
$env:NUGET_PACKAGES = $NugetCache
$env:TMP = 'D:\STS2\.tmp'
$env:TEMP = 'D:\STS2\.tmp'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'

New-Item -ItemType Directory -Force -Path $NugetCache, 'D:\STS2\.tmp' | Out-Null

Write-Host '=== 1. dotnet tool install ilspycmd ==='
if (Test-Path (Join-Path $ToolsDir 'ilspycmd.exe')) {
    Write-Host 'ilspycmd already present, skipping'
} else {
    dotnet tool install ilspycmd --tool-path $ToolsDir
    if ($LASTEXITCODE -ne 0) { throw "ilspycmd install failed ($LASTEXITCODE)" }
}

Write-Host '=== 2. build bundled mods ==='
$mods = @(
    @{ Dir = 'test_mod';     Project = 'MCPTest.csproj';      Assembly = 'MCPTest';       Id = 'mcptest' },
    @{ Dir = 'explorer_mod'; Project = 'GodotExplorer.csproj'; Assembly = 'GodotExplorer'; Id = 'godotexplorer' }
)
foreach ($m in $mods) {
    $src = Join-Path $Vendor $m.Dir
    Write-Host "--- building $($m.Assembly) ---"
    dotnet build (Join-Path $src $m.Project) -c Debug
    if ($LASTEXITCODE -ne 0) { throw "build failed for $($m.Assembly)" }

    $built = Join-Path $src ('bin\Debug\net9.0\' + $m.Assembly + '.dll')
    if (-not (Test-Path $built)) { $built = Join-Path $src ('bin\Debug\' + $m.Assembly + '.dll') }
    if (-not (Test-Path $built)) { throw "built dll missing for $($m.Assembly)" }

    $target = Join-Path (Join-Path $GameDir 'mods') $m.Id
    New-Item -ItemType Directory -Force -Path $target | Out-Null
    Copy-Item $built (Join-Path $target ($m.Assembly + '.dll')) -Force
    $pdb = [IO.Path]::ChangeExtension($built, '.pdb')
    if (Test-Path $pdb) { Copy-Item $pdb (Join-Path $target ([IO.Path]::GetFileName($pdb))) -Force }
    $manifest = Join-Path $src 'mod_manifest.json'
    if (Test-Path $manifest) { Copy-Item $manifest (Join-Path $target 'mod_manifest.json') -Force }
    Write-Host "installed $($m.Assembly) -> $target"
}

Write-Host '=== done ==='
Get-ChildItem (Join-Path $GameDir 'mods') -Recurse -File | Select-Object FullName, Length
