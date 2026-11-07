#requires -Version 5.1
<#
一次性把改动装进游戏：编译 -> （按需关闭游戏）-> 覆盖部署 -> （可选）启动。

  # 改完 C# 后，最小用法（不启动游戏）
  powershell -ExecutionPolicy Bypass -File mods-src\probe\tools\apply.ps1

  # 部署完顺手开游戏
  powershell -ExecutionPolicy Bypass -File mods-src\probe\tools\apply.ps1 -Launch

  # 游戏开着但不想让它被关掉（会部署失败，仅用于确认编译能过）
  powershell -ExecutionPolicy Bypass -File mods-src\probe\tools\apply.ps1 -NoKill

说明：
* 游戏运行时会锁住 probe.dll，所以默认会自动关掉游戏再覆盖；-Launch 再拉起来。
* 只改数值时不需要重启游戏之外的任何工具。
#>
param(
    [switch]$Launch,
    [switch]$NoKill
)

$ErrorActionPreference = 'Stop'
$ProjectDir = Split-Path -Parent $PSScriptRoot
$GameDir = if ($env:STS2_GAME_DIR) { $env:STS2_GAME_DIR } else { 'D:\SteamLibrary\steamapps\common\Slay the Spire 2' }
$Dll = Join-Path $GameDir 'mods\probe\probe.dll'

Write-Host '==> 1/3 编译' -ForegroundColor Cyan
& (Join-Path $PSScriptRoot 'build.ps1')

$running = Get-Process -Name SlayTheSpire2 -ErrorAction SilentlyContinue
if ($running) {
    if ($NoKill) {
        Write-Warning '游戏正在运行，probe.dll 被占用；部署会失败（-NoKill 生效）'
    }
    else {
        Write-Host '==> 2/3 关闭游戏（覆盖 DLL 需要）' -ForegroundColor Cyan
        $running | Stop-Process -Force
        Start-Sleep -Seconds 4
    }
}

Write-Host '==> 3/3 部署' -ForegroundColor Cyan
& (Join-Path $PSScriptRoot 'deploy.ps1')

$item = Get-Item $Dll
Write-Host ("完成：{0} 字节，{1:yyyy-MM-dd HH:mm:ss}" -f $item.Length, $item.LastWriteTime) -ForegroundColor Green

if ($Launch) {
    Write-Host '==> 启动游戏' -ForegroundColor Cyan
    $result = Invoke-CimMethod -ClassName Win32_Process -MethodName Create -Arguments @{
        CommandLine      = 'cmd.exe /c start "" "steam://rungameid/2868840"'
        CurrentDirectory = 'C:\Windows'
    }
    if ($result.ReturnValue -ne 0) { throw "启动失败（Win32_Process.Create 返回 $($result.ReturnValue)）" }
    Start-Sleep -Seconds 35
    Get-Process -Name SlayTheSpire2 -ErrorAction SilentlyContinue | Select-Object Name, Id
}
