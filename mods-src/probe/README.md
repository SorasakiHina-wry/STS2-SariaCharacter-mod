# Probe / 塞雷娅 — 自定义角色 Mod

在《杀戮尖塔2》v0.113.0 上新增一个**可玩的原创角色「塞雷娅」**与**一整套原创卡牌**（钙铠机制）。
美术部分自绘资源逐步替换中，未画的部位暂借原版。

## 当前状态（实测通过）

| 项目 | 状态 |
|---|---|
| Mod 加载 | `Found mod manifest ... mods\probe\mod_manifest.json` → `[probe] harmony patches: N applied` |
| 角色 | 内部 ID `SARIA`（完整 `PROBE-SARIA`），显示名 **塞雷娅**，HP 70、金币 99 |
| 起手遗物 | **钙质操作**（每场战斗开始获得 5 层钙铠） |
| 起手牌组 | 打击×4、防御×4、硬化×1、激发×1 |
| 卡池 | **78 张**（含 2 张先古、4 张无色 token），配比表见 `design/cards.md` 顶部（自动生成） |
| 自定义状态 | **31 个 power**，核心机制 **钙铠** |
| 先古对话 | 9 位先古共 **54 行**，通过 `design/character-writing.md` → `assets/localization/zhs/ancients.json` 注入 |
| 战斗立绘 | Spine 骨骼动画（自制），死亡/受击/攻击/能力动画已接 |
| 选人界面 | 背景大图 + 底部头像 + 顶栏头像均已换成自定义资源 |
| 通关 | 战胜建筑师可正常结算（`ArchitectWinRunPatch` 补占位台词） |
| 仍占位 | 能量球、地图标记、休息处/商店动画、开局过渡、卡框、卡图 |

角色/卡牌文本仍在 C# 的 `Localization` 里；只有**先古对话**走 PCK 里的 JSON（原因见「已知的坑」）。

## 钙铠（角色核心机制）

`src/Powers/CalciumArmorPower.cs`：

- 你的回合开始时，获得与层数相等的格挡（Early 回合开始阶段结算）；
- 当你**从卡牌**获得格挡时，失去「获得格挡数值一半」的层数（向下取整），随后「骨矿化」补层；
- 该 power 从战斗开始就以 0 层常驻（`CalciumArmorPersistPatch` 阻止它被清理）；
- 额外记录：`TotalGainedThisCombat`（本场累计获得层数，供「战略终结」使用），并在
  `AfterPowerAmountChanged` 里驱动「共振」「防卫经验」。

## 卡池规模的两个硬性要求（实测踩出来的）

自定义角色卡池太小会让**别的系统直接抛异常**，表现是「画面卡住 / 没有奖励屏」。

1. **每个稀有度都要有牌**：`CardFactory.CreateForReward` 按权重掷稀有度，再向上找「池里真有牌」的档位，
   找不到就抛 `Tried to create a card for a reward, but we couldn't generate a valid rarity!`。
   Basic 不参与奖励，且牌组里已有的牌会被拉黑 → Common / Uncommon / Rare 三档都必须有货。
2. **Attack / Skill / Power 三种类型都要有非 Basic 牌**：`MerchantInventory._coloredCardTypes` 写死为
   `[Attack, Attack, Skill, Skill, Power]`，少一种就抛
   `Can't generate valid rarity for merchant card type Power ...`。

`tools/cardspec.py` 默认校验会检查这两条（当前配比已满足）。

## 目录

```
probe/
  probe.csproj              net9.0，引用游戏自带 sts2 / GodotSharp / 0Harmony + lib/BaseLib.dll
  mod_manifest.json         id=probe，依赖 BaseLib
  lib/BaseLib.dll           编译期引用（工坊 v3.4.7，不随 mod 分发）
  README.md                 本文件
  design/                   ★ 设计表（你写，我读）
    cards.md                  卡牌（含自动生成的「当前配比」）
    powers.md                 状态/能力
    mechanics.md              机制与关键字
    relics.md                 遗物
    character-writing.md      角色选择界面文案 + 各先古对话
    card-ids.md               （生成物）中文名 ↔ 英文代号
    ancient-dialogue-vanilla.md（生成物）原版角色×先古对话参考
  docs/ → 见仓库根目录 docs/
  tools/                    15 个维护脚本，见下
  assets/                   PCK 源（只放 spine 与本地化 JSON，不放图片）
  src/
    ProbeMain.cs             入口 + 逐类应用 Harmony 补丁
    Characters/Saria.cs      角色模型
    Pools/ProbePools.cs      卡池 / 遗物池 / 药水池
    Cards/*.cs               78 张卡
    Powers/*.cs              31 个状态
    Relics/*.cs              4 张遗物
    Patches/*.cs             钙铠常驻、建筑师胜利、钙质化加伤、止血计数
    ProbeSpineAssets.cs      加载自制 Spine 资源
    ProbeSpineVisuals.cs     运行时构建 NCreatureVisuals + 命中框
    ProbeCharacterBg.cs      选人界面背景替换
    ProbeCharacterIcons.cs   选人头像 / 顶栏头像 / 对话头像
    ProbeAncientDialogues.cs 把自定义角色的先古对话注入原版先古
```

部署到 `<game>\mods\probe\`：

```
probe.dll  probe.pck  mod_manifest.json
images\   char_select_bg_saria.png / char_select_saria.png / character_icon_saria.png
spine\    char_202_demkni.{skel,atlas,png}
```

> **图片必须是散文件**，不能打进 PCK（原因见「已知的坑」第一条）。

## 关键设计（为什么这么写）

### 1. 角色自动注册
BaseLib 的 `CustomCharacterModel` 构造时调用 `CustomContentDictionary.AddCharacter(this)`，游戏的
`ModelDb.AllAbstractModelSubtypes` 会扫所有已加载 mod 程序集并实例化 → **类写出来就自动进池**。

### 2. 卡牌必须带 `[Pool]`
`CustomCardModel` 构造时 `autoAdd: true` 会走 `CustomContentDictionary.AddModel`，缺
`[Pool(typeof(...))]` 直接抛异常。

### 3. ID / 本地化 key 都是大写
`ModelId.Entry = StringHelper.Slugify(type.Name)`（内部 `ToUpperInvariant()`）。
注意**自定义角色的完整 entry 是 `modId-Id`**，即 `class Saria` → `PROBE-SARIA`：
判定角色、拼本地化键、对话键都必须用这个完整形式（或用 `EndsWith`）。

### 4. 本地化
- 角色/卡牌/power/遗物：写在代码里的 `Localization`（`(后缀, 文本)` 列表），BaseLib 按类别写表；
- 先古对话：只能走 `<mod>/localization/zhs/ancients.json`（PCK 内）。

### 5. 美术占位与替换
角色继承 `PlaceholderCharacterModel`，`PlaceholderID => "ironclad"`，未覆盖的资源全指向铁甲战士。
替换时**一次只覆盖一项**，逐项验收：

| 资源 | 状态 | 实现方式 |
|---|---|---|
| 战斗立绘 | ✅ 自制 Spine | 运行时构建 `NCreatureVisuals`（见下） |
| 选人背景 | ✅ | `NCharacterSelectScreen.SelectCharacter` 后替换背景容器内容 |
| 选人栏头像 | ✅ | `NCharacterSelectButton.Init` 后覆盖 `%Icon` 纹理 |
| 顶栏/对话头像 | ✅ | `CharacterModel.Icon` getter 返回运行时 TextureRect |
| 能量球 / 地图标记 / 休息处 / 商店 / 过渡 / 卡框 / 卡图 | ⬜ 仍占位 | — |

素材对照（哪一处用哪张 png、各自尺寸）见 [`resources/png/素材对照.md`](D:\STS2\resources\png\素材对照.md)。

## 自定义角色接入的四个「必须」

这几条都是实测踩出来的，改这块之前先读：

1. **视觉节点必须有 5 个**：`%Visuals`（SpineSprite）、`%Bounds`、`%IntentPos`、`%CenterPos`、**`%FormVfx`**。
   缺 `%FormVfx` 时死亡动画 `RemoveFormVfx()` 会抛异常，**死亡后的事件链全部不触发**（表现为"死了没反应"）。
2. **`%Bounds` 决定鼠标判定框**：引擎用它的**左上角 + 尺寸**生成 Hitbox，所以必须是
   `Position = (-W/2, -H)`、`Size = (W, H)`（覆盖身体，而不是从脚下往下），并设 `MouseFilter = Ignore`
   （原版 `ironclad.tscn` 也是这么写的）。否则会出现"鼠标在角色下方也显示详情、buff 图标无法悬停"。
3. **`%CenterPos` 决定特效位置**：血条按 `%Bounds` 排、特效按 `%CenterPos` 排，两者必须同中心，
   否则"特效在左、角色在右"。
4. **路径型属性要保持指向原版**：`CharacterSelectIconPath` / `IconTexturePath` 等如果指向不存在的资源，
   `AssetPathsCharacterSelect` 预加载会失败，**角色会整只从选择栏消失**；改用显示层覆盖。

## 先古对话接入

只写 JSON 不够：原版每个先古在代码里写死了 `CharacterDialogues` 字典（只含 5 位原版角色），
`GetValidDialogues` 用 `characterId.Entry` 查表，自定义角色永远查不到。

`src/ProbeAncientDialogues.cs` 的做法：patch `AncientDialogueSet.GetValidDialogues`，从已有对话的本地化键
推断先古 ID（`NEOW.talk.IRONCLAD.0-0.ancient` → `NEOW`），命中就返回我们自己构造的对话列表。

键格式注意：[钮] 不是一句台词，而是**上一句**的按钮文字，键是 {套号}-{上一句行号}.next（行号只数 [古]/[她]）；PopulateLines 只填台词，按钮文字要在注入后自己补 NextButtonText，否则按钮上不显示文字。 后缀挂在**行号**后面，形如 1-0r。
-
键格式注意：**可重复标记 `r` 挂在行号后面**，形如 `NEOW.talk.PROBE-SARIA.1-0r.ancient`
（不是 `1r-0`）。全存档首次（`firstVisitEver`）是所有角色共用的，我们没写就沿用原版。

## 编译 / 部署 / 验证

```powershell
# 编译（工作区内）
$env:DOTNET_CLI_HOME='D:\STS2\.dotnet-home'; $env:NUGET_PACKAGES='D:\STS2\.nuget\packages'
Set-Location D:\STS2\mods-src\probe; dotnet build -c Release

# 打包 PCK（源目录 assets/，前缀 probe/）
#   用 MCP 的 build_pck，或阅读 tools/ 里的说明；产物 build\probe.pck

# 部署：游戏必须关闭
#   probe.dll → <game>\mods\probe\probe.dll
#   build\probe.pck → <game>\mods\probe\probe.pck
#   resources\png\*.png → <game>\mods\probe\images\
#   resources\spine\fight\4.2\* → <game>\mods\probe\spine\

# 启动（直接启动 exe 会 No appID found）
Start-Process 'steam://rungameid/2868840'
```

验证走 MCP bridge：`bridge_focus_game` → `bridge_start_run(character="Saria")` →
`bridge_get_full_state` / `bridge_play_card` / `bridge_capture_screenshot`（截图前必须 focus）。

## 数值定价：`docs/how-to-adjust.md`

调卡时先算「牌效值」= 总 VP ÷ (6 × 费用)，再对照"稀有度 × 费用"区间（原版 1 费普通中位 2.00、2 费普通只有 1.33）。
钙铠要分四种计价：产甲 +1.6/层、条件型「若≥N」1.2/层（另扣哑火 0.5）、
至多型 1.5/层、清空型「失去所有」0.8/层（另扣清空风险 2）。
完整单价表、稀有度区间与八张样例分析见 [docs/how-to-adjust.md](D:\STS2\docs\how-to-adjust.md)。
## 工作流（你 ↔ 我）

> **协作节奏**：只有你说「检查文档更新 / 更新新内容」这类指令时，我才批量实现攒着的待办；
> 其余时候只做当轮明确要求的事。完整约定见 [docs/agent-workflow.md](D:\STS2\docs\agent-workflow.md)。

状态标记写在每条设计的 `备注` 里：

| 标签 | 我的动作 |
|---|---|
| `已实装` | **完全不读** |
| `待实装` | 写代码 + 编译 + 部署 |
| `待修改` | 按备注改 |
| `待删除` | 删代码，并删掉表里这一条 |

多标签按 `待删除 > 待修改 > 待实装 > 已实装` 取最急的；`已 实装`（带空格）同样识别。
已实装条目的**数值差异不归我对齐**（你在反复调数），文案差异才会提示。

### 每轮只跑这两条（不整篇读设计表）

```powershell
$env:PYTHONUTF8='1'
$exe = 'D:\STS2\.python312\python.exe'
$tool = 'D:\STS2\mods-src\probe\tools\cardspec.py'

& $exe $tool --changed    # 自上次提交以来你改了哪些条目（没打标签也能抓到）
& $exe $tool --pending    # 未实装条目的完整规格，一行一条
```

### 一批内容做完了的收尾顺序

```powershell
$env:PYTHONUTF8='1'; $exe = 'D:\STS2\.python312\python.exe'; $t = 'D:\STS2\mods-src\probe\tools'
& $exe "$t\normalize_text.py"            # 去中文空格、补句号（表与 C#）
& $exe "$t\tag_implemented.py" --clean   # 待实装/待修改 → 已实装，清掉括号说明
& $exe "$t\sort_cards.py"                # 各稀有度小节内：攻击 → 技能 → 能力
& $exe "$t\card_ids.py"                  # 刷新 design/card-ids.md
& $exe "$t\card_stats.py"                # 刷新 cards.md 顶部配比表
& $exe "$t\cardspec.py"                  # 全量校验，必须「校验通过 ✅」
```

再编译 → 部署 → 提交 git（用户没要求就不启动游戏）。

`--json --pending` 可机器读取。默认全量校验会报结构/配比/文案一致性问题。

### 工具清单（`tools/`）

| 脚本 | 用途 |
|---|---|
| `cardspec.py` | 四表解析 + 交叉校验（`--changed` / `--pending` / `--json`） |
| `normalize_text.py` | 去中文空格 + 补句号（表与 C# 同步） |
| `prune.py` | `--retag` 比对实现与描述后翻标签、`--clean-notes` 清残留说明、默认删「待删除」 |
| `tag_status.py` | 给代码里已有的条目补 `已实装` |
| `tag_implemented.py` | 把 `待实装/待修改` → `已实装`，并清掉备注里的括号说明（`--clean` 连已实装的残留一起清） |
| `sort_powers.py` | 按 cards.md 顺序重排 powers.md（钙铠恒在首位） |
| `sort_cards.py` | 在 普通/罕见/稀有 各小节内按「攻击 → 技能 → 能力」重排卡牌（`--dry-run` / `--check`；空样板自动沉到小节末尾） |
| `fill_templates.py` | 按计划数量补齐空样板 |
| `card_ids.py` | 生成 `design/card-ids.md` |
| `card_stats.py` | 更新 cards.md 的「当前配比」表 |
| `gold_tags.py` | 给卡面里需要提示的词加 `[gold]` |
| `hover_tips.py` | 补 `ExtraHoverTips`，让卡面提到的状态可悬停 |
| `import_writing.py` | `character-writing.md` → 本地化 JSON + 对话数据表 |
| `export_ancient_dialogue.py` | 导出原版先古对话参考 |
| `spine_atlas_upgrade.py` | Spine 3.x atlas → 4.x |

`build.ps1` / `deploy.ps1` / `apply.ps1` 是 PowerShell 侧的编译/部署/一键应用。

## 已知的坑（实测记录）

1. **mod PCK 里的 `.ctex` 加载失败**：`Failed loading resource: res://probe/images/xxx.png`。
   我们打包工具生成的 ctex 引擎不认 → **所有图片都放散文件**，运行时
   `Image.LoadPngFromBuffer` + `ImageTexture.CreateFromImage`。
2. **mod PCK 里放原始 `.tscn` 也加载不了**：Godot 导出时场景会变成
   `.godot/exported/.../xxx.scn` 并配 `xxx.tscn.remap`（BaseLib.pck 就是这个结构），
   我们没法离线生成 → 需要场景的地方改成**运行时构建节点**。
3. **控制台 `godmode` 在战斗初始化早期会 NRE**（原版 `CombatHistoryEntry` 的 bug，与本 mod 无关）；
   `fight` 强行传送偶尔也会在 `CardPileCmd` 抛 NRE。做奖励/卡池测试尽量用真实打法。
4. **`bridge_click_node` 对 `NClickableControl` 系按钮常无效**（它调 `ui_accept`）。
   可靠做法：GodotExplorer 依次调 `ForceClick` / `OnPressHandler` → `OnReleaseHandler`。
5. **从 DSH 命令里启动的游戏进程会随命令结束被杀**：用
   `Start-Process 'steam://rungameid/2868840'` 走 Steam 启动（进程不在沙箱作业对象里）。
6. **Harmony 字段注入 `___field` 对应字段名 `field`**：字段 `_dialogue` 要写 `____dialogue`。
   建议一律用 `AccessTools.Field(...)` + 找不到就打 warn，避免静默跳过。
7. **别在事件初始化阶段塞占位台词**：`OnRoomEnter` 发现"有台词"会清空选项，
   而空 `LineText` 会提前 return → 卡在无选项的事件里。
8. **AutoSlay 默认随机选角色**，`bridge_autoslay_start(character=...)` 靠 MCPTest 的补丁才生效；
   `AutoSlayConfig` 里是 `readonly` 静态字段，`max_floor` / `run_timeout` 改不动。
9. **游戏结束界面的「继续」**在结算动画跑完前是 disabled 的（约十几秒），别误判卡死。
10. **`CalculatedVar` 必须配 `CalculationBaseVar` + `CalculationExtraVar`**（`CalculatedDamageVar` 配 `CalculationBaseVar` + `ExtraDamageVar`）。
    少了这对变量，`Calculate()` 里 `GetBaseVar()/GetExtraVar()` 按 key 取 DynamicVar 会抛 `KeyNotFoundException`，
    而 `UpdateCardPreview()` 每次刷新卡面都会调它 —— 表现是**卡面数字变 0**、牌库滚动到该卡附近时
    卡牌节点初始化到一半中断、出现**重复行/闪烁**（网格虚拟化复用了半初始化的 holder）。
    案例：「策略归纳」曾用 `CalculatedVar("Hits")` 只声明自己一个变量，牌库底部闪成一片；改成
    `RepeatVar` + setter 同步（照原版「巨镰」）后恢复正常。

## 下一步

1. **补齐美术**：能量球（5 层 256×256）、地图标记（49×64）、过渡遮罩（2560×1200）、
   休息处/商店 Spine、卡框、卡图（每张 250×190）。
2. **两张先古卡的可获得性**：原版「欧洛巴斯之触」「尘封魔典」内部写死了卡牌清单，
   不会自动包含「渗透」「极限功率」，需要额外补丁或改由其它途径获得。
3. **遗物表扩充**：目前 4 张（1 初始 + 2 罕见 + 1 稀有），奖励档位偏薄。
4. **卡池微调**：稀有 13 张偏多、普通 18 张偏少，按 `cardspec.py` 的配比提示调整。

## 游戏更新后的适配（v0.113.0 / 2026-11-12）

游戏加了新角色，同时改了几处 API，mod 里对应改过：

| 变化 | 旧写法 | 新写法 |
|---|---|---|
| `NCharacterSelectScreen.SelectCharacter` | `(NCharacterSelectButton, CharacterModel)` | `(NCharacterSelectButton)`，角色从按钮上取 `button.Character` |
| `Hook.AfterDeath` | `(ctx, creature, bool wasRemovalPrevented, float deathAnimLength)` | 多了 `CardModel? source` 参数（在第 3 位） |
| `DynamicVar` 显示路径 | 有 `protected virtual GetBaseValueForIConvertible()` 可覆盖 | 改成读 `ShownValue = (int)PreviewValue`，只能自己把 `PreviewValue` 算好写进去 |

对应处理：

- `ProbeCharacterBg.cs`：补丁签名跟着改，判定从 `characterModel.Id.Entry` 改为 `button.Character?.Id.Entry`，
  并保留按钮节点名（`PROBE-SARIA_button`）作为兜底；
- `ProbeDisorderPower`：`AfterDeath` 补 `CardModel? source` 参数；
- `MultipliedAmountVar`：改为「按倍率刷新」的 `Refresh(powerAmount)`（同时写 `BaseValue` 与 `PreviewValue`），
  `ProbeSwiftPower` 在层数变化（`AfterPowerAmountChanged`）与回合开始时调用它。

> 游戏源码索引（`vendor/sts2-modding-mcp/decompiled`）是 v0.111.0 的旧版；新版用
> `D:\STS2\.tools\ilspycmd\ilspycmd.exe -p -o <dir> <game>\data_sts2_windows_x86_64\sts2.dll`
> 自己反编译，当前版本在 `.cache/decompiled_2026-11-13/`。