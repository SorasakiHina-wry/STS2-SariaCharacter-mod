#requires -Version 5.1
<#
把 Probe 角色 mod 打包成「可直接放进游戏 mods 目录」的分发文件。

用法：
  powershell -ExecutionPolicy Bypass -File mods-src\probe\tools\package.ps1

产物：
  dist\probe\...                解压/拷贝到 <游戏>\mods\ 下的文件夹
  dist\probe-<version>.zip      给别人测试用（解压后把 probe 文件夹放进 mods）

前置：
  - 已安装 .NET 9 SDK
  - PCK 已构建到 build\probe.pck（改动 assets\ 后需要重新打包，见 README）
#>
$ErrorActionPreference = 'Stop'

$ProjectDir = Split-Path -Parent $PSScriptRoot
$RepoRoot   = Split-Path -Parent (Split-Path -Parent $ProjectDir)
$DistDir    = Join-Path $RepoRoot 'dist'
$StageDir   = Join-Path $DistDir 'probe'
$Manifest   = Get-Content (Join-Path $ProjectDir 'mod_manifest.json') -Raw | ConvertFrom-Json
$Version    = $Manifest.version

Write-Host "[1/5] 编译 Release ..."
Push-Location $ProjectDir
try {
    & dotnet build -c Release --nologo 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'dotnet build 失败' }
} finally { Pop-Location }

$dll     = Join-Path $ProjectDir 'bin\Release\probe.dll'
$pck     = Join-Path $ProjectDir 'build\probe.pck'
foreach ($f in @($dll, $pck)) {
    if (-not (Test-Path $f)) { throw "缺少文件：$f" }
}

# PCK 比 assets 旧时提醒（PCK 只装 spine 与本地化 JSON，改卡不改它）
$assetsDir = Join-Path $ProjectDir 'assets'
if (Test-Path $assetsDir) {
    $newest = Get-ChildItem $assetsDir -Recurse -File | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($newest -and $newest.LastWriteTime -gt (Get-Item $pck).LastWriteTime) {
        Write-Warning "assets\ 比 build\probe.pck 新（$($newest.Name)），PCK 可能需要重新打包。"
    }
}

Write-Host "[2/5] 准备 dist\probe ..."
if (Test-Path $StageDir) { Remove-Item $StageDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path $StageDir | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $StageDir 'images') | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $StageDir 'spine')  | Out-Null

Write-Host "[3/5] 复制 DLL / manifest / PCK ..."
Copy-Item $dll (Join-Path $StageDir 'probe.dll') -Force
Copy-Item $pck (Join-Path $StageDir 'probe.pck') -Force
Copy-Item (Join-Path $ProjectDir 'mod_manifest.json') (Join-Path $StageDir 'mod_manifest.json') -Force

Write-Host "[4/5] 复制图片与 Spine（运行时按散文件读取）..."
$pngDir   = Join-Path $RepoRoot 'resources\png'
$spineDir = Join-Path $RepoRoot 'resources\spine\fight\4.2'

$imageMap = @{
    '38_i01_fix.png'  = 'char_select_bg_saria.png'
    '38_i01_head.png' = 'char_select_saria.png'
    'head.png'        = 'character_icon_saria.png'
}
foreach ($src in $imageMap.Keys) {
    $from = Join-Path $pngDir $src
    if (-not (Test-Path $from)) { throw "缺少图片：$from" }
    Copy-Item $from (Join-Path $StageDir ('images\' + $imageMap[$src])) -Force
}
foreach ($name in @('char_202_demkni.atlas', 'char_202_demkni.png', 'char_202_demkni.skel')) {
    $from = Join-Path $spineDir $name
    if (-not (Test-Path $from)) { throw "缺少 Spine 文件：$from" }
    Copy-Item $from (Join-Path $StageDir ('spine\' + $name)) -Force
}

Write-Host "[5/5] 打包 zip ..."
$zipPath = Join-Path $DistDir ("probe-$Version.zip")
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path $StageDir -DestinationPath $zipPath -CompressionLevel Optimal

$sizeMb = [Math]::Round((Get-ChildItem $StageDir -Recurse -File | Measure-Object Length -Sum).Sum / 1MB, 2)
Write-Host ""
Write-Host "完成："
Write-Host "  文件夹  $StageDir  ($sizeMb MB)"
Write-Host "  zip     $zipPath"
Write-Host ""
Write-Host "安装方法（测试者）："
Write-Host "  1. 先订阅 Steam 工坊的 BaseLib（必需依赖）"
Write-Host "  2. 解压 zip，把里面的 probe 文件夹整体放进 <游戏目录>\mods\"
Write-Host "  3. 启动游戏，角色选择界面最后多出的「塞雷娅」即为本 mod"
exit 0
