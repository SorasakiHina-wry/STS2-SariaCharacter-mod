#requires -Version 5.1
<#
把 Saria 角色 mod 打包成「可直接放进游戏 mods 目录」的分发文件。

用法：
  powershell -ExecutionPolicy Bypass -File mods-src\probe\tools\package.ps1

产物：
  dist\saria\...                解压/拷贝到 <游戏>\mods\ 下的文件夹
  dist\saria-<version>.zip      给别人测试用（解压后把 saria 文件夹放进 mods）

前置：
  - 已安装 .NET 9 SDK
  - PCK 已构建到 build\saria.pck（改动 assets\ 后需要重新打包，见 README）
#>
$ErrorActionPreference = 'Stop'

$ProjectDir = Split-Path -Parent $PSScriptRoot
$RepoRoot   = Split-Path -Parent (Split-Path -Parent $ProjectDir)
$DistDir    = Join-Path $RepoRoot 'dist'
$StageDir   = Join-Path $DistDir 'saria'
$Manifest   = Get-Content (Join-Path $ProjectDir 'mod_manifest.json') -Raw | ConvertFrom-Json
$Version    = $Manifest.version

Write-Host "[1/5] 编译 Release ..."
Push-Location $ProjectDir
try {
    & dotnet build -c Release --nologo *> $null
    if ($LASTEXITCODE -ne 0) { throw 'dotnet build 失败' }
} finally { Pop-Location }

$dll = Join-Path $ProjectDir 'bin\Release\saria.dll'
$pck = Join-Path $ProjectDir 'build\saria.pck'
foreach ($f in @($dll, $pck)) {
    if (-not (Test-Path $f)) { throw "缺少文件：$f" }
}

# PCK 比 assets 旧时提醒（PCK 只装 spine 与本地化 JSON，改卡不改它）
$assetsDir = Join-Path $ProjectDir 'assets'
if (Test-Path $assetsDir) {
    $newest = Get-ChildItem $assetsDir -Recurse -File | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($newest -and $newest.LastWriteTime -gt (Get-Item $pck).LastWriteTime) {
        Write-Warning "assets\ 比 build\saria.pck 新（$($newest.Name)），PCK 可能需要重新打包。"
    }
}

Write-Host "[2/5] 准备 dist\saria ..."
if (Test-Path $StageDir) { Remove-Item $StageDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path $StageDir | Out-Null

Write-Host "[3/5] 复制 DLL / manifest / PCK ..."
Copy-Item $dll (Join-Path $StageDir 'saria.dll') -Force
Copy-Item $pck (Join-Path $StageDir 'saria.pck') -Force
Copy-Item (Join-Path $ProjectDir 'mod_manifest.json') (Join-Path $StageDir 'mod_manifest.json') -Force

Write-Host "[4/5] 复制图片与 Spine（运行时按散文件读取）..."
$pngDir   = Join-Path $RepoRoot 'resources\png'
$spineDir = Join-Path $RepoRoot 'resources\spine\fight\4.2'
# 顺序有意义：先拷原图兜底，再拷 derived（居中裁剪 + 缩放的派生图），后者覆盖前者。
$imageMap = @(
    @('38_i01_fix.png',  'char_select_bg_saria.png'),
    @('38_i01_head.png', 'char_select_saria.png'),
    @('head.png',        'character_icon_saria.png'),
    @('derived\char_select_bg_saria.png', 'char_select_bg_saria.png'),
    @('derived\char_select_saria.png',    'char_select_saria.png'),
    @('derived\character_icon_saria.png', 'character_icon_saria.png'),
    @('derived\map_marker_saria.png',     'map_marker_saria.png')
)

# 派生图缺失时自动生成（需要 Pillow；可用环境变量 SARIA_PYTHON 指定解释器）
$derivedDir = Join-Path $pngDir 'derived'
if (-not (Test-Path (Join-Path $derivedDir 'map_marker_saria.png'))) {
    $py = $env:SARIA_PYTHON
    if (-not $py) { $py = 'python' }
    Write-Host "派生图标缺失，先运行 tools\prepare_icons.py ..."
    & $py (Join-Path $PSScriptRoot 'prepare_icons.py')
    if ($LASTEXITCODE -ne 0) { throw "prepare_icons.py 失败（请设置 SARIA_PYTHON 指向带 Pillow 的 python）" }
}

if (Test-Path $pngDir) {
    New-Item -ItemType Directory -Force -Path (Join-Path $StageDir 'images') | Out-Null
    foreach ($pair in $imageMap) {
        $from = Join-Path $pngDir $pair[0]
        if (-not (Test-Path $from)) { throw "缺少图片：$from" }
        Copy-Item $from (Join-Path $StageDir ('images\' + $pair[1])) -Force
    }
    # 状态图标：resources\png\powers\*.png -> images\powers\
    $powerArtDir = Join-Path $RepoRoot 'resources\png\powers'
    if (Test-Path $powerArtDir) {
        New-Item -ItemType Directory -Force -Path (Join-Path $StageDir 'images\powers') | Out-Null
        Copy-Item (Join-Path $powerArtDir '*.png') (Join-Path $StageDir 'images\powers') -Force
    }
    else {
        Write-Warning "resources\png\powers 不存在，本次不含状态图标。"
    }

    # 能量球图层：resources\png\orb\*.png -> images\orb\
    $orbArtDir = Join-Path $RepoRoot 'resources\png\orb'
    if (Test-Path $orbArtDir) {
        New-Item -ItemType Directory -Force -Path (Join-Path $StageDir 'images\orb') | Out-Null
        Copy-Item (Join-Path $orbArtDir '*.png') (Join-Path $StageDir 'images\orb') -Force
    }
    else {
        Write-Warning "resources\png\orb 不存在，本次不含能量球图层。"
    }

    # 获得能量特效贴图：resources\png\derived\vfx\*.png -> images\vfx\
    $vfxArtDir = Join-Path $RepoRoot 'resources\png\derived\vfx'
    if ((Test-Path $vfxArtDir) -and (Get-ChildItem $vfxArtDir -Filter '*.png' | Select-Object -First 1)) {
        New-Item -ItemType Directory -Force -Path (Join-Path $StageDir 'images\vfx') | Out-Null
        Copy-Item (Join-Path $vfxArtDir '*.png') (Join-Path $StageDir 'images\vfx') -Force
    }

    # 猜拳手部贴图：resources\png\hand\*.png -> images\hand\
    $handArtDir = Join-Path $RepoRoot 'resources\png\hand'
    if (Test-Path $handArtDir) {
        New-Item -ItemType Directory -Force -Path (Join-Path $StageDir 'images\hand') | Out-Null
        Copy-Item (Join-Path $handArtDir '*.png') (Join-Path $StageDir 'images\hand') -Force
    }

    # 转场遮罩：resources\png\saria_transition.png -> images\transition\
    $transitionSrc = Join-Path $RepoRoot 'resources\png\saria_transition.png'
    if (Test-Path $transitionSrc) {
        New-Item -ItemType Directory -Force -Path (Join-Path $StageDir 'images\transition') | Out-Null
        Copy-Item $transitionSrc (Join-Path $StageDir 'images\transition\saria_transition.png') -Force
    }

    # 遗物图标：resources\png\relics\derived\*.png -> images\relics\（大图 256）
#            resources\png\relics\derived\cropped\*.png -> images\relics\cropped\（小图 80）
    $relicArtDir = Join-Path $RepoRoot 'resources\png\relics\derived'
    if (Test-Path $relicArtDir) {
        New-Item -ItemType Directory -Force -Path (Join-Path $StageDir 'images\relics\cropped') | Out-Null
        Copy-Item (Join-Path $relicArtDir '*.png') (Join-Path $StageDir 'images\relics') -Force
        $relicSmallDir = Join-Path $relicArtDir 'cropped'
        if (Test-Path $relicSmallDir) {
            Copy-Item (Join-Path $relicSmallDir '*.png') (Join-Path $StageDir 'images\relics\cropped') -Force
        }
        $relicOutlineDir = Join-Path $relicArtDir 'outline'
        if (Test-Path $relicOutlineDir) {
            New-Item -ItemType Directory -Force -Path (Join-Path $StageDir 'images\relics\outline') | Out-Null
            Copy-Item (Join-Path $relicOutlineDir '*.png') (Join-Path $StageDir 'images\relics\outline') -Force
        }
    }

    # 卡图：优先用压缩版 resources\png\cards-little\*.png -> images\portraits\（没有则退回原图）
    $cardArtDir = Join-Path $RepoRoot 'resources\png\cards-little'

    if (-not (Test-Path $cardArtDir)) { $cardArtDir = Join-Path $RepoRoot 'resources\png\cards' }
    if (Test-Path $cardArtDir) {
        New-Item -ItemType Directory -Force -Path (Join-Path $StageDir 'images\portraits') | Out-Null
        $cardRenames = @{ 'ProbeArmpng.png' = 'ProbeArm.png'; 'ProbeInsightpng.png' = 'ProbeInsight.png' }
        foreach ($png in Get-ChildItem $cardArtDir -Filter '*.png') {
            $target = if ($cardRenames.ContainsKey($png.Name)) { $cardRenames[$png.Name] } else { $png.Name }
            Copy-Item $png.FullName (Join-Path $StageDir ('images\portraits\' + $target)) -Force
        }
    }
    else {
        Write-Warning "resources\png\cards 不存在，本次不含卡图。"
    }
    New-Item -ItemType Directory -Force -Path (Join-Path $StageDir 'audio') | Out-Null
    Copy-Item (Join-Path $RepoRoot 'resources\ogg\selected.ogg') (Join-Path $StageDir 'audio\selected.ogg') -Force
    New-Item -ItemType Directory -Force -Path (Join-Path $StageDir 'spine') | Out-Null
    # 战斗骨骼在 fight\4.2，非战斗（商店/火堆）骨骼在 4.2
    $spineSources = @{
        'char_202_demkni.atlas'       = $spineDir
        'char_202_demkni.png'         = $spineDir
        'char_202_demkni.skel'        = $spineDir
        'build_char_202_demkni.atlas' = (Join-Path $RepoRoot 'resources\spine\4.2')
        'build_char_202_demkni.png'   = (Join-Path $RepoRoot 'resources\spine\4.2')
        'build_char_202_demkni.skel'  = (Join-Path $RepoRoot 'resources\spine\4.2')
    }
    foreach ($kv in $spineSources.GetEnumerator()) {
        $from = Join-Path $kv.Value $kv.Key
        if (-not (Test-Path $from)) { throw "缺少 Spine 文件：$from" }
        Copy-Item $from (Join-Path $StageDir ('spine\' + $kv.Key)) -Force
    }
}
else {
    Write-Warning "resources\png 不存在（公开仓库里被排除），本次只打包代码与 PCK，不含立绘/头像。"
}

Write-Host "[5/5] 打包 zip ..."
$zipPath = Join-Path $DistDir ("saria-$Version.zip")
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
Write-Host "  2. 解压 zip，把里面的 saria 文件夹整体放进 <游戏目录>\mods\"
Write-Host "  3. 启动游戏，角色选择界面最右侧多出的「塞雷娅」即为本 mod"
exit 0
