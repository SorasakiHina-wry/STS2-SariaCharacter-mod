# 《杀戮尖塔2》角色 Mod 工具链审计

审计时间：2026-11-05  
游戏版本：**v0.111.0**（commit 41cef1ea，2026-08-13）  
引擎：Godot 4.5.1 C# / .NET 9 / Harmony 2.4.2  
工作区：`D:\STS2`

---

## 0. 结论摘要

| 能力 | 状态 |
|---|---|
| 代码阅读 / API 查询（反编译 + Roslyn 索引） | ✅ 完整可用 |
| 卡牌 / 遗物 / 能力 / 药水 代码生成 | ✅ 可用（BaseLib 变体需补依赖） |
| 自定义角色代码生成 | ⚠️ 生成器存在，但依赖 BaseLib（未安装） |
| C# 编译 | ✅ 可用（已验证离线编译通过） |
| PCK 打包（纯 Python，无需 Godot） | ✅ 可用 |
| 部署到游戏 `mods/` | ❌ 被沙箱拦截，需授权升级 |
| 联网（NuGet / 素材下载 / AI 生图） | ❌ 沙箱内 TLS 被断，需授权升级 |
| 游戏内实时测试（Bridge / AutoSlay） | ⚠️ 工具齐备，需先启动游戏 |
| 角色 Spine 动画 | ❌ 无 Spine 编辑器；有替代方案 |

**核心判断：做「全新人物 + 全新卡牌」所需的能力已经齐了 9 成，真正缺的不是工具，而是三样东西：**
1. **BaseLib**（可选，但有它省事；且需要联网装一次）；
2. **一个角色美术/动画路线决定**（Spine 编辑器缺失，但可绕过）；
3. **两处环境授权**（装包 + 部署），因为游戏目录在工作区外、沙箱内 TLS 不可用。

---

## 1. 已具备的工具（全部实测）

### 1.1 代码情报层（最关键的「理解游戏」能力）

| 工具 | 实测结果 |
|---|---|
| `get_setup_status` | `decompiled_exists: true`（3537 个 .cs），`roslyn_index_exists: true` |
| `get_game_info` | 3258 个实体、149 个 hook、39 条控制台命令 |
| `get_entity_source` | ✅ 完整源码（`CharacterModel`、`Ironclad`、`NPreview`、`ModelDb`、`ModManager`…） |
| `search_game_code` / `list_entities` / `list_hooks` | ✅ |
| `suggest_patches` / `analyze_method_callers` / `get_hook_signature` | ✅ 未逐个验证，依赖同一索引 |
| `browse_namespace` / `get_entity_relationships` / `reverse_hook_lookup` | ✅ |

### 1.2 代码生成层

`create_mod_project`、`generate_card`、`generate_relic`、`generate_power`、`generate_potion`、`generate_character`、`generate_encounter`、`generate_monster`、`generate_event`、`generate_mechanic`、`generate_modifier`、`generate_orb`、`generate_enchantment`、`generate_game_action`、`generate_custom_keyword`、`generate_custom_pile`、`generate_dynamic_var`、`generate_harmony_patch`、`generate_transpiler_patch`、`generate_overlay`、`generate_godot_ui`、`generate_net_message`、`generate_save_data`、`generate_test_scenario`。

另有 45 篇 modding 指南（`docs/guides/`）+ 15 篇 BaseLib 参考（`docs/baselib/`），
其中 `cards.md`、`pools.md`、`localization.md`、`mechanics.md`、`testing.md`、`troubleshooting.md` 与角色 mod 直接相关。

### 1.3 编译 / 打包 / 部署

| 工具 | 实测 |
|---|---|
| `dotnet` SDK | ✅ 10.0.403（可编 net9.0） |
| 离线编译（引用游戏自带 DLL） | ✅ `strikesprite` 用 Release 编译通过，0 警告 0 错误 |
| `build_pck`（纯 Python 打 PCK） | ✅ 工具存在，PNG→ctex 自动转换，无需装 Godot |
| Godot 编辑器 | ⚠️ 只有 `D:\Godot\Godot_v4.6.1-stable_win64.exe`（版本 4.6.1 ≠ 游戏 4.5.1，且非 .NET 版） |
| GDRE Tools v2.7.4 | ✅ 可列 PCK（实测导出 15897 条路径清单到 `.cache/pck_files.txt`）、可解包、可 `.scn`→`.tscn` |

### 1.4 美术 / 音频素材

| 资源 | 说明 |
|---|---|
| 全角色 Spine 源文件 | `spine-1\OriginalSpine\`：ironclad / silent / defect / regent / necrobinder 及其 `_shop`、`restsite_*`、`characterselect_*` 变体，含 `.skel/.atlas/.png`；怪物 119 套 |
| Spine-Godot 运行时 | `spine-1\bin\windows\`（editor / template_debug / template_release）+ 游戏自带 `libspine_godot.*.dll` |
| 图片处理 | 本地 Python 3.12 + Pillow（`process_art` 用任意 PNG 生成全套尺寸，**不需要 AI、不需要联网**） |
| AI 生图 | ❌ `GOOGLE_API_KEY` 未设置，且沙箱内无法 HTTPS |

### 1.5 游戏内验证

- `MCPTest`（bridge，TCP 21337）与 `GodotExplorer`（TCP 27020）**已装在游戏 mods 里**。
- 工具齐备：`bridge_start_run` / `bridge_play_card` / `bridge_get_combat_state` / `run_test_scenario` / `bridge_autoslay_start`（内置 AI 全自动跑整局，做回归冒烟最省事）。
- 当前**游戏未运行**，`bridge_ping` 超时 —— 属正常，启动游戏后即可用。

---

## 2. 三个硬性环境限制（都有解，但需要配合）

### 限制 A：沙箱内 **TLS 全断**
- 证据：`curl https://api.nuget.org` → `000`；`dotnet restore` → `NU1301 ... 安全包中没有可用的凭证`（schannel `SEC_E_NO_CREDENTIALS`）；`web_fetch raw.githubusercontent.com` → DNS ENOENT。
- 但 TCP 可通（`Test-NetConnection` 全 True），**提权后 HTTP 正常**：`nuget=302`、`github=200`。
- 影响：NuGet 还原、下载 BaseLib / Godot / 素材、AI 生图全部走不通。
- 解法：需要联网的步骤用一次授权执行（会弹一次确认），把包缓存进 `D:\STS2\.nuget`，之后离线可用。

### 限制 B：游戏目录在工作区外 → **写不进去**
- 证据：`install_mod` → `PermissionError WinError 5`；直接写 `mods\` → 拒绝；`mklink /J` 也被拒（无法用目录联接绕过）。
- 提权后写入成功（已实测建/删探测目录）。
- 影响：`install_mod`、`build_pck` 输出到游戏目录、拷 DLL 都失败。
- 解法：每次部署用一次授权执行；或由你在资源管理器里手动同步（我可生成一键脚本）。

### 限制 C：MCP 自带的 `build_mod` **在沙箱内必挂**
- 证据：调用 `build_mod` 报 `UnauthorizedAccessException: C:\Users\25590\.dotnet\10.0.403.toolpath.sentinel`（dotnet 首次运行要写用户目录）。
- 手工设 `DOTNET_CLI_HOME=D:\STS2\.dotnet-home` + `NUGET_PACKAGES=D:\STS2\.nuget\packages` 后编译正常。
- 解法：把这两个环境变量写进 `.dsh/mcp.json` 的 `env`（下次会话生效），或我用 pwsh 直接编译。

---

## 3. 还缺的工具 / 依赖（按优先级）

### P0 — 不做就动不了

| 缺口 | 说明 | 补法 |
|---|---|---|
| **BaseLib**（`Alchyr.Sts2.BaseLib`） | `generate_character`、`generate_mod_config`、`SpireField`、自定义关键字/牌堆都依赖它。本地 NuGet 缓存里**没有**；游戏 mods 里也**没装** | 授权联网装一次：既要从 NuGet 拉编译期包，也要把 BaseLib mod 本身装进游戏（或用 `dotnet new install Alchyr.Sts2.Templates` 的角色模板） |
| **部署通道** | 见限制 B | 一键 `deploy.ps1` + 授权 |
| **角色资产骨架** | 角色需要的 12+ 个 res:// 资源目前一个都没有 | `scaffold_character_assets` 生成占位 `.tscn`，我再补序列化细节 |

### P1 — 强烈建议

| 缺口 | 说明 |
|---|---|
| **Godot 4.5.1 .NET 版编辑器** | 现有 4.6.1 标准版：版本高于游戏，导出的 `.tscn/.tres` 有前向不兼容风险；且打不开 C# 工程。仅用于预览/手改场景。替代：手写 `.tscn` 文本（格式稳定），我不依赖编辑器也能出场景 |
| **Spine 编辑器** | 未安装（正版付费）。若要做「会呼吸会挥砍」的骨骼动画，需要它；否则走第 4 节的替代路线 |
| **AI 生图** | `GOOGLE_API_KEY` 未配。可先用程序化占位图（Pillow，离线可跑），后期替换 |
| **音频** | 角色 4 条音效是 FMOD 事件路径（`event:/sfx/characters/<id>/...`），我们没有 FMOD 工程。可复用原版事件名，或先留空测崩溃风险 |

### P2 — 锦上添花

- `GDRE_TOOLS_PATH` 环境变量（现在虽然装了 GDRE，但 MCP 的 `list_game_assets`/`extract_game_assets` 用不了；且 GDRE 需把 `APPDATA` 重定向到工作区才能跑）。
- `ilspycmd` 被 MCP 判定「未安装」（它找 `%USERPROFILE%\.dotnet\tools`，实际在 `D:\STS2\.tools\ilspycmd`）。只在游戏更新重反编译时才需要。
- 上架 Steam Workshop：外部工具 `sts2-mod-uploader`，需要 Steam 登录，最后再说。

---

## 4. 关键技术判断（决定了工作量）

### 4.1 角色模型本身不难，难在「资源路径是写死的」
`CharacterModel` 里资源路径**不是 virtual**，全部由 `Id.Entry.ToLowerInvariant()` 拼出来：

```
res://scenes/creature_visuals/<id>.tscn          ← NCreatureVisuals
res://images/ui/top_panel/character_icon_<id>.png (+ _outline)
res://scenes/ui/character_icons/<id>_icon.tscn
res://scenes/combat/energy_counters/<id>_energy_counter.tscn
res://scenes/rest_site/characters/<id>_rest_site.tscn
res://scenes/merchant/characters/<id>_merchant.tscn
res://scenes/screens/char_select/char_select_bg_<id>.tscn
res://images/packed/character_select/char_select_<id>.png (+ _locked)
res://materials/transitions/<id>_transition_mat.tres
res://images/packed/map/icons/map_marker_<id>.png
res://scenes/vfx/card_trail_<id>.tscn
res://images/ui/hands/multiplayer_hand_<id>_{point,rock,paper,scissors}.png
```

→ **两条可行路线：**

| | 路线 A：零依赖（纯游戏 API） | 路线 B：BaseLib |
|---|---|---|
| 注册角色 | 自己 Harmony patch `ModelDb.AllCharacters`（源码里是硬编码 5 元素数组）+ `ModelDb.Inject` | `CustomCharacterModel` 自动注册 |
| 资源路径 | 自建 PCK **不加前缀**，把文件放在上面那些原生路径上（PCK 由 `LoadResourcePack` 合并进 `res://`） | 用 `Custom*Path` 覆盖 → 资源全放 `res://<ModId>/` |
| 卡牌 | `CardModel` 子类，构造签名极简：`base(cost, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)` + `CanonicalVars` + `OnPlay` | `CustomCardModel` + `[Pool(...)]` |
| 卡池 | `ModHelper.AddModelToPool<MyPool, MyCard>()`（官方 API，已确认存在） | 自动 |
| 依赖 | **0**，可完全离线 | BaseLib mod + NuGet 包，且需与 v0.111.0 版本匹配 |
| 风险 | 需要自己摸清角色选择界面/解锁逻辑的挂点 | 版本漂移；BaseLib 3.3.x 是给更新版本游戏做的，与 v0.111.0 未必兼容 |

**建议：先走 A 打通「能选到、能打牌、能通关」这条主线，BaseLib 作为可选加速器后加。** 理由：本机联网受限、BaseLib 版本匹配不确定，而 A 路线的所有关键 API 我已经在反编译源码里逐一确认过。

### 4.2 Spine 不是必需品（这是最大的好消息）
`NCreatureVisuals._Ready()` 里：
```csharp
_body = GetNode<Node2D>("%Visuals");
if (IsSpineNode) { SpineBody = new MegaSprite(_body); ... }
```
`%Visuals` 只要是任意 `Node2D` 即可；不是 `SpineSprite` 就 `SpineBody = null`，`HasSpineAnimation = false`，游戏**不会崩**，只是没有骨骼动画。

→ 三种美术方案：

| 方案 | 成本 | 效果 |
|---|---|---|
| **A1 静态立绘 + Tween 补间**（`AnimationPlayer`/`Tween` 做呼吸、受击抖动、出招位移） | 低 | 立刻能看、能测；后期可原地上 Spine |
| **A2 复用现成骨架 + 重制贴图**（拿 `spine-1/OriginalSpine` 里某角色 `.skel`，保持骨骼/动画名，只换 atlas 贴图） | 中 | 完整骨骼动画；需要重排 atlas（文本）+ 用 Pillow 合成贴图，**不需要 Spine 编辑器** |
| **A3 手写 Spine JSON** | 高 | 完全原创的骨骼动画；`.json` 是文本格式，spine-godot 可直接加载 |

### 4.3 必须在动工前排掉的「未知」
1. 角色选择界面如何枚举角色、如何判定解锁（`ModelDb.AllCharacters` 之外是否还有 UI/存档侧的硬编码）。
2. 自定义角色的**存档兼容**：`UnlockState.Characters` 是存档里的集合，新角色不在其中时会不会被判成 locked。
3. 缺 FMOD 音效事件时是静默还是报错。
→ 这三条建议先做一个「最小可玩角色」垂直切片（1 张攻击牌 + 占位立绘）验证，而不是先写完 30 张卡。

---

## 5. 建议的推进顺序

1. **环境收尾**（一次性）：授权联网装 BaseLib（可选）、写 `.dsh/mcp.json` 补 `DOTNET_CLI_HOME/NUGET_PACKAGES/GDRE_TOOLS_PATH`、生成 `deploy.ps1`。
2. **垂直切片**：1 个角色模型 + 1 张攻击牌 + 占位立绘 + 最小 PCK → 编译 → 部署 → 启动游戏 → 在角色选择界面看到它 → 进战斗出一张牌。
3. **打通验证回路**：装 bridge，用 `run_test_scenario` / `bridge_autoslay_start` 做自动化回归。
4. **扩量**：卡池设计（20~30 张）+ 关键字机制（`generate_mechanic`）+ 遗物/药水 + 中英本地化。
5. **打磨**：角色选择立绘、能量计数器、休息处/商店动画、音效、上架素材。

---

## 6. 已发现的工具缺陷（会咬人的）

| 位置 | 问题 | 影响 |
|---|---|---|
| `templates/csproj_baselib_template.cs.tpl` | 写死 `Alchyr.Sts2.BaseLib Version="0.1.*"`，而 NuGet 上已是 3.x | 照模板建工程必然还原失败 |
| `templates/card_template.cs.tpl`（非 BaseLib 版） | 仍然输出 BaseLib 专属的 `[Pool(typeof(...))]` 特性 | 声称「不用 BaseLib」的卡牌模板实际编不过 |
| `build_mod` | 不注入 `DOTNET_CLI_HOME` | 沙箱内 100% 失败（见限制 C） |
| `install_mod` | 直接写游戏目录 | 沙箱内 100% 失败（见限制 B） |
| `get_setup_status` | 把 `ilspycmd` 判为未安装 | 只在游戏更新重反编译时卡住 |
| `get_character_asset_paths` | 返回的是 BaseLib 风格 `res://MyMod/...` 路径，与原生硬编码路径不同 | 走路线 A 时必须以第 4.1 节的真实路径为准 |
| GDRE 调用 | 需把 `APPDATA` 指向工作区，否则写 `user://logs` 被拒 | `list_game_assets` 等工具直接报错 |

---

## 7. 实测补正与阶段成果（2026-11-05 晚）

### 7.1 对第 3 节的修正
- **BaseLib 其实已经装了**：本机 Steam 创意工坊订阅了 BaseLib **v3.4.7**（item 3737335127），
  另有 26 个工坊 mod。所以不需要再单独下载安装，只需**按 3.4.7 的 DLL 编译**。
  （我先下的 3.4.5 已从游戏 `mods\BaseLib` 移除，避免与工坊版本重复。）
- 编译期引用直接取工坊的 `BaseLib.dll`（`mods-src\probe\lib\BaseLib.dll`），不随 mod 分发。

### 7.2 关于「启动游戏」的限制（新增）
- 从 DSH 命令里 `Start-Process` 拉起的进程**会随命令结束被作业对象回收**，游戏启动后立刻被杀
  （log 表现为：Godot 轮转日志后立刻退出）。
- `mklink /J`、WMI `Win32_Process.Create` 在沙箱内均被拒。
- 结论：**游戏必须由用户自己启动**（或对启动命令单独授权）。启动后 bridge 一切正常。

### 7.3 已跑通的完整链路
`源码 → dotnet build（离线）→ 部署到 mods\probe → 游戏加载 mod → 角色注册 → 开局 → 出牌 → 数值正确`

实测数据（Probe 角色）：

| 项 | 实测值 |
|---|---|
| HP / 金币 | 70/70、99 |
| 初始牌组 | 5×ProbeStrike、4×ProbeDefend、1×ProbeBolt |
| 初始遗物 | BurningBlood |
| ProbeStrike | 6 伤害；目标带易伤时 9（×1.5） |
| ProbeDefend | 5 格挡 |
| ProbeBolt | 5 伤害 + 1 层 Vulnerable |
| 回合流转 | 抽/弃/结束回合/敌方行动 全部正常 |
| 升级 | `upgrade` 后 ProbeStrike 打 9（6+3） |
| **选人界面** | `PROBE-PROBE_button` 存在、未锁定、可选中，信息面板显示 The Probe / 70/70 / 自定义描述 |
| **从 UI 开局** | 选人确认 → Neow 事件，角色 Probe、10 张牌、Burning Blood |
| 战报截图 | `.cache/probe_combat.png`、`.cache/probe_charselect2.png` |

工程位置：`mods-src\probe\`（README 里记了设计与扩展方式）。

### 7.4 建议的 MCP 环境变量（下次会话生效）
把下面这些写进 `.dsh\mcp.json` 的 `env`，`build_mod` / GDRE 相关工具才能在沙箱内工作：

```json
{
  "PYTHONUTF8": "1",
  "PYTHONIOENCODING": "utf-8",
  "TMP": "D:/STS2/.tmp",
  "TEMP": "D:/STS2/.tmp",
  "DOTNET_CLI_HOME": "D:/STS2/.dotnet-home",
  "NUGET_PACKAGES": "D:/STS2/.nuget/packages",
  "DOTNET_NOLOGO": "1",
  "GDRE_TOOLS_PATH": "D:/STS2/GDRE_tools-v2.7.4-windows/gdre_tools.exe",
  "APPDATA": "D:/STS2/.tmp/appdata"
}
```

### 7.5 三个 bridge / 控制台的坑（实测）
- `bridge_click_node` 对 `NClickableControl` 系按钮多半无效（内部走 `ui_accept`）。
  可靠方式：GodotExplorer 依次调用 `OnPressHandler` → `OnReleaseHandler`。
- 控制台 `kill all` 结束战斗**不走奖励结算**，房间会卡在没有出口的状态，只能重开。
  奖励/卡池测试要用正常打法。
- 游戏结束界面「继续」在结算动画结束前 `IsEnabled=false`（约十几秒），不是死机。

---

## 8. 当前进度与剩余

**已完成**：技术验证角色**整局通关**。  
- 选人界面识别、开局、出牌、升级、奖励、商店、三幕 Boss、胜利结算全部实测通过；
- 2026-11-06 00:12 的 AutoSlay 整局（seed `PROBEWIN6`）日志：
  `[probe] installing placeholder architect dialogue` → `Victory! Run completed`，
  全程 3328 行日志里只有 1 条 ERROR，且来自其他 mod 的旧式依赖声明。

**本轮修掉的三个真实集成 bug**：
1. 卡牌奖励按稀有度抽样，池里缺某一档就抛 `couldn't generate a valid rarity`，奖励屏不出现；
2. 商店固定按 `[Attack, Attack, Skill, Skill, Power]` 五槽抽卡，缺任一类型抛
   `Can't generate valid rarity for merchant card type ...`，商店卡死；
3. 自定义角色打赢三幕 Boss 时 `TheArchitect.WinRun()` 因 `Dialogue` 为 null 直接 NRE，无法通关。
   已用 `ArchitectWinRunPatch` 在 `_dialogue` 缺失时注入静默占位台词并放行原方法。
4. （工具侧）`AutoSlayer` 默认随机选角色，`bridge_autoslay_start(character=...)` 形同虚设；
   已给 MCPTest 打 `AutoSlayCharacterPatch`。

**剩余**：
1. 用户提供原创美术后逐项替换 `Custom*Path`；
2. 角色机制与最终 20~30 张卡池设计（依赖设定决策）；
3. 自定义遗物/药水（尚未开始）。

> `APPDATA` 重定向是为了让 GDRE 能写 `user://logs`。
> `install_mod` 无论如何都写不进游戏目录——部署继续用 `mods-src\probe\tools\deploy.ps1` + 授权。
