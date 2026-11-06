<#
Install the two bundled bridge mods into the game's mods/ folder and build the
Roslyn analyzer + index once. Needs to run outside the file sandbox because it
writes into the game installation and restores NuGet packages over HTTPS.
#>
$ErrorActionPreference = 'Stop'

$Vendor = 'D:\STS2\vendor\sts2-modding-mcp'
$GameDir = 'D:\SteamLibrary\steamapps\common\Slay the Spire 2'
$ModsRoot = Join-Path $GameDir 'mods'

$env:STS2_GAME_DIR = $GameDir
$env:NUGET_PACKAGES = 'D:\STS2\.nuget'
$env:TMP = 'D:\STS2\.tmp'
$env:TEMP = 'D:\STS2\.tmp'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'

Write-Host '=== 1. install bridge mods ==='
$mods = @(
    @{ Dir = 'test_mod';     Assembly = 'MCPTest';       Id = 'mcptest' },
    @{ Dir = 'explorer_mod'; Assembly = 'GodotExplorer'; Id = 'godotexplorer' }
)
foreach ($m in $mods) {
    $bin = Join-Path $Vendor ($m.Dir + '\bin\Debug\net9.0')
    $dll = Join-Path $bin ($m.Assembly + '.dll')
    if (-not (Test-Path $dll)) { throw "missing build output: $dll" }
    $target = Join-Path $ModsRoot $m.Id
    New-Item -ItemType Directory -Force -Path $target | Out-Null
    Copy-Item $dll (Join-Path $target ($m.Assembly + '.dll')) -Force
    foreach ($extra in @('.pdb', '.deps.json', '.runtimeconfig.json', '0Harmony.dll', 'GodotSharp.dll')) {
        $file = Join-Path $bin ($m.Assembly + $extra)
        if ($extra -like '.*') { $file = Join-Path $bin ($m.Assembly + $extra) }
        else { $file = Join-Path $bin $extra }
        if (Test-Path $file) { Copy-Item $file (Join-Path $target (Split-Path $file -Leaf)) -Force }
    }
    $manifest = Join-Path $Vendor ($m.Dir + '\mod_manifest.json')
    if (Test-Path $manifest) { Copy-Item $manifest (Join-Path $target 'mod_manifest.json') -Force }
    Write-Host "installed $($m.Assembly) -> $target"
}

Write-Host '=== 2. roslyn analyzer ==='
$roslyn = Join-Path $Vendor 'tools\roslyn_analyzer'
Push-Location $roslyn
try {
    dotnet restore --configfile nuget.config
    if ($LASTEXITCODE -ne 0) { throw 'roslyn restore failed' }
    dotnet build -c Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'roslyn build failed' }

    $analyzerDll = Join-Path $roslyn 'bin\Release\net9.0\RoslynAnalyzer.dll'
    if (-not (Test-Path $analyzerDll)) { throw "missing analyzer dll: $analyzerDll" }
    $decompiled = Join-Path $Vendor 'decompiled'
    $index = Join-Path $decompiled 'roslyn_index.json'
    Write-Host 'indexing decompiled sources...'
    dotnet $analyzerDll $decompiled $index
    if ($LASTEXITCODE -ne 0) { throw "analyzer run failed ($LASTEXITCODE)" }
    Get-Item $index | Select-Object FullName, Length
} finally {
    Pop-Location
}

Write-Host '=== 3. final state ==='
Get-ChildItem $ModsRoot -Recurse -File | Select-Object FullName, Length
