# 发版流程（维护者）

面向仓库维护者：**改完内容 → 打包 → 推送 → 发 Release** 的固定步骤。
其他文档：[README](../README.md)（项目总览）、[`agent-workflow.md`](agent-workflow.md)（协作约定）、
[`../mods-src/probe/README.md`](../mods-src/probe/README.md)（实现细节与踩坑记录）。

---

## 0. 仓库里有什么、没有什么

| 入库 | 不入库（`.gitignore`） |
|---|---|
| `mods-src/probe/`：C# 源码、设计表、工具、`mod_manifest.json`、`build\saria.pck` | `resources/`：立绘与 Spine **源文件**（版权素材，仅本地构建用） |
| `docs/`、根 `README.md`、`LICENSE` | `dist/`：打包产物 |
| | `vendor/`、`.cache/`、`.tools/`、`.nuget/`、`GDRE_tools*/` 等工具链目录 |

> 因为 `resources/` 不入库，**别人克隆后能编译出 `saria.dll`，但打不出带立绘的完整 zip**。
> 这是有意的：发型包里包含同人美术，但源文件不进公开仓库。

---

## 1. 改版本号

`mods-src/probe/mod_manifest.json`：

```json
"version": "0.1.1"     ← 改成新版本，游戏内与 Release 标题都用它
```

命名习惯：`v` + 语义化版本；小修 → `0.1.x`，机制大改 → `0.2.0`。

---

## 2. 打包

```powershell
cd D:\STS2
powershell -ExecutionPolicy Bypass -File mods-src\probe\tools\package.ps1
```

产出：

```
dist\saria\                 ← 解压后直接放进 <游戏>\mods\ 的文件夹
dist\saria-<version>.zip    ← Release 上传用
```

脚本会：编译 Release → 校验 `saria.dll` / `build\saria.pck` 存在 → 把 `resources/png` 的三张图
改名归位、复制 `resources/spine/fight/4.2` 的骨骼 → 打包 zip。
若 `assets\` 比 PCK 新会**警告**（PCK 只装 Spine 与本地化 JSON，改卡不需要重打；改了 `assets\` 才需要）。

> 改了 `assets/`（本地化 JSON、Spine）时，PCK 需要用 MCP 的 `build_pck` 重新生成：
> 源目录 `mods-src/probe/assets`，前缀 **`saria/`**，输出 `mods-src/probe/build/saria.pck`。

### 打包前自检（推荐）

```powershell
cd mods-src\probe
$env:PYTHONUTF8='1'
D:\STS2\.python312\python.exe tools\cardspec.py            # 全量校验，必须「校验通过 ✅」
D:\STS2\.python312\python.exe tools\cardspec.py --pending  # 应显示「没有待处理的条目」
```

---

## 3. 推送

```powershell
cd D:\STS2
git add -A
git commit -m "v0.1.x: <一句话说明>"
git push
```

首次配置（本机已配好，换机器时才需要）：

```powershell
git remote add origin https://github.com/SorasakiHina-wry/STS2-SariaCharacter-mod.git
git branch -M main
```

### 临时令牌：`.local/repo.json`

`.dsh/` 只管 DSH 自己的东西（MCP、代理）；**令牌这类运维数据放在 `.local/`**，同样已被 `.gitignore` 排除：

```json
{
  "token_name": "dsh-push-temp",
  "token": null
}
```

- 仓库地址不用写：git remote 就是 `origin`（URL 里不带凭据，已验证）；
- 需要推送时，你把 fine-grained PAT 写进 `token`（只授权本仓库，Contents: Read and write，7 天有效），然后告诉 agent「token 已写入」；
- agent 推送 / 发 Release 完，立刻把 `token` 改回 `null`；
- 你再顺手到 GitHub 撤销该令牌。

> 不需要额外的推送脚本：读一个 JSON 加一条 `git push origin main` 就是全部；包一层脚本反而让令牌多经过参数与日志。
> 别 `git add -f .local/`，这个目录只在本机存在。

### 用令牌推送（一次性的做法）

不要把令牌写进 `.git/config`。用带凭据的 URL 推一次即可：

```powershell
git push "https://x-access-token:<TOKEN>@github.com/SorasakiHina-wry/STS2-SariaCharacter-mod.git" main
```

推完**立即去 https://github.com/settings/personal-access-tokens 撤销令牌**。

令牌权限（Fine-grained PAT，只授权本仓库）：**Contents: Read and write** + **Metadata: Read**，
有效期选最短（7 天）。

---

## 4. 发 Release

### 方式 A：网页（推荐，最省事）

1. 仓库 → **Releases** → **Draft a new release**
2. **Choose a tag** 输入 `v0.1.x` → Create new tag on publish（目标分支 `main`）
3. **Release title**：`v0.1.x — Saria Character (塞雷娅)`
4. 描述：贴下面的模板
5. **Attach binaries**：上传 `dist\saria-0.1.x.zip`
6. Publish release

GitHub 会自动附带 **Source code (zip / tar.gz)**，不用手动传。

### 方式 B：命令行（有令牌时）

```powershell
$token = '<TOKEN>'
$h = @{ Authorization = "Bearer $token"; Accept = 'application/vnd.github+json'; 'User-Agent' = 'dsh-agent' }
# 1) 建 release
$body = @{ tag_name='v0.1.x'; target_commitish='main'; name='v0.1.x — Saria Character (塞雷娅)'; body='见模板'; draft=$false; prerelease=$false } | ConvertTo-Json
$rel = Invoke-RestMethod -Method Post -Uri 'https://api.github.com/repos/SorasakiHina-wry/STS2-SariaCharacter-mod/releases' -Headers $h -Body ([Text.Encoding]::UTF8.GetBytes($body)) -ContentType 'application/json; charset=utf-8'
# 2) 传附件
$zip = [IO.File]::ReadAllBytes('D:\STS2\dist\saria-0.1.x.zip')
Invoke-RestMethod -Method Post -Uri "$($rel.upload_url -replace '\{\?name,label\}','')?name=saria-0.1.x.zip" -Headers $h -Body $zip -ContentType 'application/zip'
```

### Release 描述模板

```markdown
<一句话概述本次改动>

- 新增 / 调整内容 1
- 新增 / 调整内容 2

**安装**
1. 订阅 Steam 工坊的 BaseLib（必需依赖）
2. 解压 `saria-<version>.zip`，把里面的 `saria` 文件夹整体放进 `<游戏目录>\mods\`
3. 启动游戏，角色选择界面最右侧多出的「塞雷娅」即为本 mod

游戏版本：v0.113.x
```

---

## 5. 发版后检查

- [ ] Release 页面能下到 zip，大小合理（约 2.5 MB）
- [ ] 解压后是 `saria\` 目录，内含 `saria.dll`、`saria.pck`、`mod_manifest.json`、`images\`、`spine\`
- [ ] 把该目录放进 `mods\` 启动游戏：日志里出现
      `Loading assembly DLL ...\mods\saria\saria.dll` 与 `[saria] harmony patches: 13 applied`
- [ ] 角色选择界面能看到并用「塞雷娅」开一局
- [ ] 撤销临时令牌

---

## 6. 版本与内容速查

| 项目 | 值 |
|---|---|
| Mod ID | `saria` |
| 角色 entry | `SARIA-SARIA` |
| 游戏内 DLL / PCK | `saria.dll` / `saria.pck` |
| PCK 内路径前缀 | `saria/`（`res://saria/localization/...`） |
| 模型 ID 前缀 | 取自**根命名空间** `Saria` → `SARIA-`（见 `mods-src/probe/README.md` 已知的坑） |
| 依赖 | BaseLib ≥ 3.4.5（Steam 工坊） |
| 最低游戏版本 | 0.111.0（实测 0.113.x） |
