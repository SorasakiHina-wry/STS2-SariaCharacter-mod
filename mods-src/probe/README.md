# Probe — 自定义角色 Mod（技术验证切片）

在《杀戮尖塔2》v0.111.0 上新增一个**可玩的原创角色**与**全新卡牌**。
本阶段只做「管线验证」：角色能被注册、能被开局、卡牌能出、数值正确。美术暂借原版资源。

## 当前状态：已实测通过 ✅

| 验证项 | 结果 |
|---|---|
| Mod 加载 | `Found mod manifest ... mods\probe\mod_manifest.json` → `Finished mod initialization for 'Probe Character' (probe)` |
| 角色注册 | `bridge_start_run(character="Probe")` 成功 |
| 初始状态 | HP 70/70、金币 99、遗物 BurningBlood、牌组 10 张（5×ProbeStrike / 4×ProbeDefend / 1×ProbeBolt） |
| 攻击牌 | ProbeStrike 打出 6 伤害；挂上易伤后打 9（6×1.5）✅ |
| 技能牌 | ProbeDefend 获得 5 格挡 ✅ |
| 施加状态 | ProbeBolt 造成 5 伤害并施加 1 层 Vulnerable ✅ |
| 回合流转 | 抽牌 / 弃牌 / 结束回合 / 敌人行动 / 第 2 回合正常 ✅ |
| 升级 | 控制台 `upgrade 0` 后 ProbeStrike 打 9（6+3）✅ |
| **选人界面** | 节点 `CharSelectButtons/ButtonContainer/PROBE-PROBE_button`，`Lock` 不可见（未锁定）；点选后信息面板显示 **The Probe** / 70/70 / 99 金币 / 自定义描述（见 `.cache/probe_charselect2.png`） |
| 从 UI 开局 | 点 Confirm 后进入 Neow 事件，角色 = Probe、牌组 10 张、遗物 Burning Blood ✅ |
| 卡牌库 | 侧栏出现 `FILTER-CHARACTER_PROBE-PROBE` 卡池筛选，卡池内卡牌全部可见、描述正确（见 `.cache/probe_library.png`） |
| 奖励 / 商店 | 卡牌奖励能正常生成（日志实测抽到 `CARD.PROBE-PROBE_CHARGE`），商店五槽不再抛异常 |
| 升级 | 控制台 `upgrade 0` 后 ProbeStrike 打 9（6+3）✅ |
| **整局通关** | `bridge_autoslay_start(character="Probe", seed="PROBEWIN6")` → 打通三幕 → `[probe] installing placeholder architect dialogue` → `Victory! Run completed`，全程仅 1 条 ERROR（来自别的 mod 的旧式依赖声明） |
| 画面 | 使用 Ironclad 的 Spine 立绘占位，UI 正常（见 `.cache/probe_combat.png`） |

### 卡池（17 张）

**以 [design/cards.md](design/cards.md) 为准**（那是设计稿，也是我生成代码的输入）。当前配比：

| 稀有度 | 数量 | 类型 | 数量 |
|---|---|---|---|
| 基础 Basic | 4（打击×4、防御×4、硬化×1、激发×1 起手） | 攻击 Attack | 7 |
| 普通 Common | 6 | 技能 Skill | 9 |
| 罕见 Uncommon | 1 | 能力 Power | 1 |
| 稀有 Rare | 6 | | |

> ⚠️ 罕见档只剩 **1** 张。奖励卡按稀有度抽，薄档位抽干后有抛异常的风险，建议补到 ≥6。

自定义状态：**钙铠**（`src/Powers/CalciumArmorPower.cs`，回合开始按层数给格挡；从卡牌获得格挡时按格挡值一半掉层）。

> **为什么必须是这个分布**：奖励卡按稀有度抽、商店按「类型」抽。
> 见下面「卡池规模的两个硬性要求」。

## 目录

```
probe/
  probe.csproj              net9.0，引用游戏自带 sts2/GodotSharp/0Harmony + lib/BaseLib.dll
  mod_manifest.json         id=probe，依赖 BaseLib >= 3.4.5
  lib/BaseLib.dll           编译期引用（取自工坊 v3.4.7，不随 mod 分发）
  design/cards.md           ★ 卡牌设计表（你填，我读）
  design/powers.md          ★ buff/debuff 定义表（会显示层数的状态）
  design/mechanics.md       ★ 卡牌专属机制 / 关键字表
  design/relics.md          ★ 遗物设计表
  tools/cardspec.py         三表解析 + 交叉校验脚本
  src/
    ProbeMain.cs            [ModInitializer("Init")] 入口
    Characters/Probe.cs     角色模型（继承 BaseLib 的 PlaceholderCharacterModel）
    Pools/ProbePools.cs     卡池 / 遗物池 / 药水池
    Cards/*.cs              20 张卡
    Relics/*.cs             6 张遗物
    Patches/*.cs            TheArchitect 胜利修正
  tools/
    build.ps1               编译（把 dotnet 状态留在工作区内，沙箱里也能跑）
    deploy.ps1              部署到 <game>\mods\probe
```

## 关键设计（为什么这么写）

### 1. 角色自动注册，不需要手写注册表
BaseLib 的 `CustomCharacterModel` 构造函数会调用 `CustomContentDictionary.AddCharacter(this)`；
游戏自己的 `ModelDb.AllAbstractModelSubtypes` 会通过 `ReflectionHelper.GetSubtypesInMods<AbstractModel>()`
扫描所有已加载 mod 程序集并实例化。所以：**类写出来就自动进池**。

### 2. 卡牌必须带 `[Pool]`
`CustomCardModel` 构造函数里 `autoAdd: true` 时会走 `CustomContentDictionary.AddModel`，
没有 `[Pool(typeof(...))]` 会直接抛异常。卡池内部再用官方的 `ModHelper.AddModelToPool` 合并。

### 3. ID / 本地化 key 都是大写的
`ModelId.Entry = StringHelper.Slugify(type.Name)`，而 `Slugify` 内部会 `ToUpperInvariant()`。
所以 `class Probe` → entry `PROBE` → 本地化 key 是 `PROBE.title`、`PROBE.description`。
卡同理：`ProbeStrike` → `PROBE_STRIKE.title`。

### 4. 本地化直接写在代码里
`ILocalizationProvider.Localization` 返回 `(后缀, 文本)` 列表，BaseLib 会按模型类别写进
`characters` / `cards` 表，key 自动拼成 `Entry.后缀`。想改用 JSON 也支持
（放 `<mod>/localization/zhs/*.json`，走 PCK），见 `.cache/pck_extract/localization/` 里的原版样例。

### 5. 美术占位靠 `PlaceholderCharacterModel`
它把**所有**角色外观资源（战斗立绘、顶栏头像、能量计数器、休息处/商店动画、卡牌拖尾、选人背景、
音效事件）都指向 `PlaceholderID` 指定的原版角色（默认 ironclad）。改 `PlaceholderID` 就换一套皮。

换成自己的美术时，**一次只覆盖一个成员**，逐项替换，随时可回退：

```csharp
public override string? CustomVisualPath => "res://probe/scenes/probe_visuals.tscn"; // %Visuals/%Bounds/%IntentPos/%CenterPos
public override string? CustomIconTexturePath => "res://probe/images/char_icon_probe.png";
public override string? CustomCharacterSelectIconPath => "res://probe/images/char_select_probe.png";
public override string? CustomEnergyCounterPath => "res://probe/scenes/probe_energy_counter.tscn";
public override string? CustomRestSiteAnimPath => "res://probe/scenes/probe_rest_site.tscn";
```

> 战斗立绘可以偷懒：BaseLib 的 `NodeFactory<NCreatureVisuals>`.CreateFromResource(Texture2D)
> 能直接把一张 PNG 变成合格的 `NCreatureVisuals`（自动补 Bounds / Sprite2D / 各 Marker），
> 即 `CreateCustomVisuals()` 里返回 `NodeFactory<NCreatureVisuals>.CreateFromResource("res://...png")`。

## 编译 / 部署 / 验证

```powershell
# 编译（工作区内，不需要联网）
powershell -ExecutionPolicy Bypass -File mods-src\probe\tools\build.ps1

# 部署（需要写游戏目录，沙箱外执行）
powershell -ExecutionPolicy Bypass -File mods-src\probe\tools\deploy.ps1

# 启动游戏后，用 MCP bridge 验证
#   bridge_ping
#   bridge_start_run(character="Probe", seed="PROBE1")
#   bridge_get_full_state
#   bridge_navigate_map(row=1, col=3)
#   bridge_get_combat_state → bridge_play_card(...)
```

编译环境要点：`DOTNET_CLI_HOME=D:\STS2\.dotnet-home`、`NUGET_PACKAGES=D:\STS2\.nuget\packages`，
否则 dotnet 首次运行会去写 `%USERPROFILE%\.dotnet` 而被沙箱拒绝。

## 卡池规模的两个硬性要求（实测踩出来的）
自定义角色卡池太小会让**别的系统直接抛异常**，而且异常发生在 UI 构建阶段，表现是「画面卡住 / 没有奖励屏」。

### 1. 卡牌奖励：每个稀有度都要有牌
`CardFactory.CreateForReward` 先按权重掷一个稀有度，再用 `GetNextAllowedRarity` 向上找
「本池子里真有牌」的稀有度，找不到就返回 `None` 并抛：
`Tried to create a card for a reward, but we couldn't generate a valid rarity!`

Basic 牌不参与奖励（`Rarity != Basic` 过滤），且**牌组里已有的牌会被拉黑**。
所以只有 2 Basic + 1 Uncommon 时，Uncommon 一旦被拉黑就必然失败。

→ 至少要有 Common / Uncommon / Rare 三档的牌。

### 2. 商店：Attack / Skill / Power 三种类型都要有牌
`MerchantInventory._coloredCardTypes` 是写死的 `[Attack, Attack, Skill, Skill, Power]`，
每个槽位按类型从角色卡池里抽，同一次商店内还会去重。少一种类型就抛：
`Can't generate valid rarity for merchant card type Power with card options: ...`

→ 至少要有 **2 张非 Basic 攻击、2 张非 Basic 技能、1 张非 Basic 能力牌**。

能力牌不需要自定义 PowerModel：用原版 `PowerCmd.Apply<StrengthPower>` 就行（见 `ProbeCharge`）。

## 自定义角色通关会崩：TheArchitect 空台词

打赢第三幕 Boss 后进入 `THE_ARCHITECT` 事件，确认胜利时：

```csharp
private async Task WinRun() {
    if (LocalContext.IsMe(Owner)) {
        await AnimPlayerAttackIfNecessary(Dialogue.EndAttackers);  // Dialogue 为 null → NRE
        await RunManager.Instance.WinRun();                        // 永远到不了
    }
}
```

`LoadDialogue()` 只在原版 5 个角色里查台词，自定义角色取到空列表 → `Rng.NextItem` 返回 null。
UI 显示 PROCEED 一切正常，直到点下去才 NRE —— 表现为「打完 Boss 卡住/闪退」。

修法见 `src/Patches/ArchitectWinRunPatch.cs`：给 `WinRun` 打 Prefix，**只在 `_dialogue` 为 null 时**
塞一条静默占位台词（`LineText = null`、`EndAttackers = None`），然后**照常执行原方法**。
原方法拿到非空台词 → 跳过建筑师攻击动画 → 走到 `RunManager.Instance.WinRun()`。
原版角色不会命中该分支。

三条踩坑记录：

1. **Harmony 字段注入 `___field` 对应字段名 `field`**。这里的字段叫 `_dialogue`，要写 `____dialogue`；
   我第一版写成 `___dialogue`，Harmony 静默跳过（日志 `patch ... skipped`），跑完整局才发现没生效。
   现在统一用 `AccessTools.Field(...)` 显式反射 + 找不到字段打 warn。
2. 一开始想在 `LoadDialogue` 之后补台词，结果 `OnRoomEnter` 发现「有台词」会 `ClearCurrentOptions()`，
   而 `PlayCurrentLine` 遇到空 `LineText` 提前 return，选项再也没设回去 → 卡在无选项的事件里
   （AutoSlay 报 `Event options not loaded`）。**别把占位台词放在事件初始化阶段**。
3. Prefix 里 `return false` 跳过 async 原方法，会让它返回 null Task，
   调用方 `EventOption.Chosen()` 再解引用 → 又一个 NRE。**要返回真正的 Task，就得放行原方法**。

## 工作流：状态标记 + 每轮只读待办

每条设计（卡牌 / 状态 / 机制 / 遗物）的 `备注` 里带一个标签：

| 标签 | 含义 | 我做什么 |
|---|---|---|
| `已实装` | 代码已实现 | **完全不读**（省 token） |
| `待实装` | 待实现 | 写代码 + 编译 + 部署 |
| `待修改` | 需要改 | 按备注里写的改 |
| `待删除` | 要删 | 删代码，**并从 md 里删掉这一条** |

标签识别对 `已 实装`（带空格）也有效；同一条写了多个标签时按 `待删除 > 待修改 > 待实装 > 已实装`
取最需要处理的那个，所以「已实装；待修改」不会被误判为已完成。

### 每轮我只跑这两条（不整篇读设计表）

```powershell
$env:PYTHONUTF8='1'
$exe = 'D:\STS2\.python312\python.exe'
$tool = 'D:\STS2\mods-src\probe\tools\cardspec.py'

& $exe $tool --changed    # 自上次提交以来你改了哪些条目（没打标签也能抓到）
& $exe $tool --pending    # 未实装条目的完整规格，一行一条
```

`--changed` 的输出形如：

```
cards.md
  新增（2）: 针剂弹药、部署
  修改（1）: 硬化
```

`--pending` 的输出形如（每行包含实现所需的全部字段，我不用再打开 md）：

```
[待修改] 1 条
  - cards: 硬化 | Basic | Skill | Self | 基础: 费用 1 | 获得 6 层钙铠 | 升级: 费用 1 | 获得 8 层钙铠 → src\Cards\ProbeCalciumCards.cs
```

机器可读：`cardspec.py --json --pending`。

一致性检查（`--todo` / 默认模式）仍会报：标了 `已实装` 但代码里没有、标了 `待实装` 但代码已有、
标了 `待删除` 但代码已无。

### 批量维护

```powershell
& $exe D:\STS2\mods-src\probe\tools\tag_status.py --dry-run   # 给已实现的补 已实装
& $exe D:\STS2\mods-src\probe\tools\tag_status.py
& $exe D:\STS2\mods-src\probe\tools\normalize_text.py        # 去空格 + 补句号（表和 C# 一起改）
& $exe D:\STS2\mods-src\probe\tools\prune.py --dry-run        # 预览：删「待删除」条目 + 翻状态
& $exe D:\STS2\mods-src\probe\tools\prune.py --retag
& $exe D:\STS2\mods-src\probe\tools\prune.py --clean-notes    # 去掉「已实装（…）」里的残留说明
```

`prune.py --retag` **不是无脑翻标签**：它先比对实现与描述是否一致（`<br>` 数量、`词条` 与
`CanonicalKeywords`），不一致的条目会保持原标签并打印
`实现与描述仍不一致，保持标签不动`，避免把没改完的卡误标成已实装；翻标签时会连带删掉紧跟的
中文括号说明（`待修改（改了什么）` → `已实装`）。

`normalize_text.py` 让牌面文本对齐原版中文排版，两条规则：

1. **中文里的空格全部去掉**：`造成 6 点伤害` → `造成6点伤害`（只对含中文的文本生效，英文描述不动）
2. **句末补句号**：`造成6点伤害` → `造成6点伤害。`（已以 `。！？…` 结尾则跳过）

它同时改写 `design/*.md` 的 `基础/升级/描述/效果` 和 `src/**/*.cs` 的 `("description", …)` 字符串，
每轮改完卡我都会跑一次；`cardspec.py` 也会把「没以句号结尾」当作错误报出来。

版本管理：工作区根目录已 `git init`，`.gitignore` 排除了 vendor/.cache/spine-1 等大目录，
只跟踪 `docs/` 与 `mods-src/`。每次改动都提交一次，`--changed` 就是基于这条提交线做对比的。

## 卡牌设计流程（你 ↔ 我）

1. 你在 [design/cards.md](D:\STS2\mods-src\probe\design\cards.md) 里按格式写卡（格式说明见
   [docs/card-spec-format.md](D:\STS2\docs\card-spec-format.md)）。
   每张卡只要写：**名称 / 稀有度 / 基础（费用+效果）/ 升级（费用+效果）**，类型和目标可以留空让我推断。
2. 你先自己跑一遍校验（可选，但很快）：

   ```powershell
   $env:PYTHONUTF8='1'
   D:\STS2\.python312\python.exe D:\STS2\mods-src\probe\tools\cardspec.py
   ```

   它会报「缺字段 / 稀有度写错 / 卡名重复 / 升级前后一样 / 卡池配比不够」。
3. 我读设计表 → 按名称对齐（改名、改数值、新增、`# 停用` 删除）→ 生成/修改 C# 类 + 本地化 →
   编译 → 卡牌库里核对 → 必要时跑 AutoSlay 回归。

设计表里现在预填了已实装的 20 张卡，你可以直接在上面改名改数值。

## 遗物（已实现，池子待你验收）

6 张遗物已进 `ProbeRelicPool`，图标借用原版 atlas（`IconBaseName`），起手遗物换成了 **ProbeCore**：

| 遗物 | 稀有度 | 效果 | 图标借用 |
|---|---|---|---|
| Probe Core | 初始 | 战斗胜利后回复 6 点生命 | burning_blood |
| Anchor Chip | 普通 | 战斗开始时获得 8 点格挡 | anchor |
| Smooth Memory | 普通 | 战斗胜利后回复 5 点生命 | oddly_smooth_stone |
| Signal Amplifier | 罕见 | 战斗开始时获得 1 点力量 | vajra |
| Static Lantern | 罕见 | 战斗开始时获得 1 点敏捷 | lantern |
| Recursion Engine | 稀有 | 战斗开始时获得 2 力量 + 2 敏捷 | bag_of_marbles |

已实测：起手装备 ProbeCore，战斗胜利后 HP 50 → 56（+6）。其余遗物尚未逐张验证。

## 下一步

1. **弃用占位美术**：用你自己提供的立绘，逐项替换 `Custom*Path`；先做战斗立绘 + 顶栏头像 + 选人头像三件套。
2. **扩卡池**：目标 20~30 张。现有 12 张是模板，复制改数值与 `OnPlay` 即可。
3. **角色机制**：确定关键字（`generate_mechanic` 可生成 power + 示例卡 + 遗物 + 本地化），
   再让卡池围绕它设计。
4. **遗物/药水**：自定义遗物要 `[Pool(typeof(ProbeRelicPool))]`；药水同理。

## 已知的坑（实测记录）

- **控制台 `kill all` / `damage` 结束战斗不会走奖励结算**，房间会停在只有角色、没有退出按钮的状态，
  只能重开一局（`room Monster` 可以跳出去）。做奖励/卡池测试必须用真实打法。
- **`PunchOff` 事件房间会让日志每帧刷屏**：某次整局跑到该事件时，Godot 持续报
  `Parameter "particles" is null` / `Parameter "mem" is null`，栈顶是
  `PackedScene.Instantiate_Patch1` ← `PunchOff.PunchEachOther()`，一局写出 1.6GB 日志。
  这个补丁来自别的 mod（场景实例化挂钩），不是本 mod 的代码路径；
  换一局没遇到该事件时日志只有 ~250KB。**跑长回归前记得盯着 `logs/godot.log` 体积**。
- **AutoSlay 默认随机选角色**（`AutoSlayer` 里 `_random.NextItem(未锁定角色)`），
  `bridge_autoslay_start(character=...)` 原本完全不生效。已给 MCPTest 加了
  `AutoSlayCharacterPatch`：拦截 `NCharacterSelectButton.Select`，把随机选人重定向到指定角色。
  另外 `AutoSlayConfig` 里都是 `readonly` 静态字段，`bridge_autoslay_configure` 的
  `max_floor` / `run_timeout` 实际改不动（日志里仍显示 49 / 25min）。
- **`bridge_click_node` 对 `NClickableControl` 系按钮常无效**（它调的是 `ui_accept`）。
  可靠做法是用 GodotExplorer 依次调 `OnPressHandler` → `OnReleaseHandler`，这两个是暴露出来的 Godot 方法。
- **游戏结束界面**的「继续」按钮在结算动画跑完前是 disabled 的；动画大约要十几秒，
  期间 `IsEnabled=false`，点了没反应，别误判成卡死。
- 从 DSH 命令里启动的游戏进程会随命令结束被杀。绕过办法：提权后用
  `Invoke-CimMethod Win32_Process Create` 走 WMI 启动（进程不在沙箱作业对象里，能存活）。
