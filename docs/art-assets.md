# 美术素材索引（已完成的资源都在哪）

> 给「素材做完了 / 要改图 / 要对尺寸」时用的一页速查。
> 只列**当前实装**的东西；还缺的项在 [`missing-assets.md`](missing-assets.md)。

---

## 1. 目录总览

| 素材 | 源文件目录 | 命名规则 | 部署到（游戏内） | 入库 |
|---|---|---|---|---|
| 卡图（原图） | `resources/png/cards/` | `<类名>.png`，如 `ProbeStrike.png` | —（只在本地） | ❌ |
| 卡图（发布用压缩版） | `resources/png/cards-little/` | 同上 | `mods/saria/images/portraits/` | ✅ |
| 状态（power）图标 | `resources/png/powers/` | `<类名>.png`，如 `ProbeOverclockPower.png` | `mods/saria/images/powers/` | ✅ |
| 状态图标 PSD 源文件 | `resources/psd/` | `<类名>.psd` | —（设计稿） | ❌ |
| 角色选择背景 | `resources/png/38_i01_fix.png` | 固定文件名 | `mods/saria/images/char_select_bg_saria.png` | ❌ |
| 角色选择栏头像 | `resources/png/38_i01_head.png` | 固定文件名 | `mods/saria/images/char_select_saria.png` | ❌ |
| 顶部栏 / 先古头像 | `resources/png/head.png`（840×840 母图） | 固定文件名 | `mods/saria/images/character_icon_saria.png`（派生 170×170） | ❌ |
| 地图标记 | `resources/png/map_marker_saria.png`（专用图 196×256；缺失时才从 `head.png` 派生） | 固定文件名 | `mods/saria/images/map_marker_saria.png`（98×128） | ❌ |
| 派生图生成 | `tools/prepare_icons.py` | — | 输出 `resources/png/derived/`（不入库） | ❌ |
| 战斗立绘（Spine） | `resources/spine/fight/4.2/char_202_demkni.{atlas,png,skel}` | 固定文件名 | `mods/saria/spine/` | ❌ |
| 商店 / 火堆立绘（Spine） | `resources/spine/4.2/build_char_202_demkni.{atlas,png,skel}` | 固定文件名 | `mods/saria/spine/` | ❌ |
| 能量球图层 | `resources/png/orb/`（`saria_orb_layer_1..5.png`，各 256×256） | `<角色>_orb_layer_<n>.png` | `mods/saria/images/orb/` | ❌ |
| 能量图标 | `resources/png/orb/energy_saria.png`（源图 148×148） | `energy_<卡池名>.png`（原版 74×74） | `mods/saria/images/orb/` | ❌ |
| 能量球 / 图标 PSD | `resources/psd/orb/`（与 PNG 同名） | 与 PNG 同名 | — | ❌ |
| 选中音效 | `resources/ogg/selected.ogg` | 固定文件名 | `mods/saria/audio/selected.ogg` | ❌ |
| 猜拳手部 | `resources/png/hand/`（`multiplayer_hand_saria_<手势>.png`，4 张 424×1200） | 原版命名 | `mods/saria/images/hand/` | ❌ |
| 转场遮罩 | `resources/png/saria_transition.png`（2560×1200 灰度） | 固定文件名 | `mods/saria/images/transition/` | ❌ |
| 遗物图标 | `resources/png/relics/derived/`（8 张 256×256，`cropped/` 8 张 80×80，`outline/` 8 张白色剪影） | `<类名>.png` | `mods/saria/images/relics/`（小图 `cropped/`、影子 `outline/`） | ❌ |
| 遗物原图 | `resources/png/relics/sources/`（8 张 2~4 MB 的绘制稿） | `<类名>.png` | — | ❌ |
| 原版参考素材 | `resources/vanilla/energy_counter_ironclad/` | 见目录内 README | —（只作参考） | ❌ |

> 「入库」= 是否进 git。`resources/` 整体被 `.gitignore` 排除，只有 `cards-little/` 与 `powers/` 开了例外。

---

## 2. 各素材的关键规格

### 卡图

| 项 | 值 |
|---|---|
| 原图尺寸 | 一般卡 **1536×1024**（87 张）、先古卡 **1024×1536**（2 张：渗透、极限功率） |
| 发布用尺寸 | 一般卡 **500×380**、先古卡 **820×1146**（按显示比例居中裁切 + 256 色抖动） |
| 游戏内显示区 | 一般卡 250×190、先古卡 410×573 |
| 命名 | 与 [`../../mods-src/probe/design/card-ids.md`](../../mods-src/probe/design/card-ids.md) 的英文别名一致 |
| 数量 | 89 张 |

### 状态图标

| 项 | 值 |
|---|---|
| 尺寸 | **64×64**（源图即显示用图，不做多档） |
| 命名 | 与 [`../../mods-src/probe/design/power-ids.md`](../../mods-src/probe/design/power-ids.md) 的英文别名一致 |
| 数量 | 32 个 |

### 角色三张图

| 用途 | 文件 | 尺寸 |
|---|---|---|
| 选人背景 | `38_i01_fix.png` | 1600×750（32:15，与原生 2560×1200 同比例） |
| 选人栏头像 | `38_i01_head.png` | 264×390（原生 132×195 的 2 倍） |
| 顶部栏/先古头像 | `head.png` | 220×220（原生 85×85） |

### Spine

| 用途 | 骨骼 | 版本 |
|---|---|---|
| 战斗 | `char_202_demkni.*`（`resources/spine/fight/4.2/`） | 4.2.43 |
| 商店 / 火堆 | `build_char_202_demkni.*`（`resources/spine/4.2/`） | 4.2.43 |

`resources/spine/` 根目录与 `resources/spine/fight/` 下还有 **3.8 原始版**，仅作转换参考，游戏不读。

---

## 3. 颜色与代码位置

| 项 | 值 | 代码位置 |
|---|---|---|
| 角色名颜色 | `#7FD4FF` | [`Saria.cs`](../../mods-src/probe/src/Characters/Saria.cs) → `NameColor` |
| 能量数字描边（能量球上的 3/3） | `#0b3692` | [`Saria.cs`](../../mods-src/probe/src/Characters/Saria.cs) → `EnergyLabelOutlineColor` |
| 卡背色（牌组/历史） | `#7FD4FF` | [`ProbePools.cs`](../../mods-src/probe/src/Pools/ProbePools.cs) → `DeckEntryCardColor` |
| 卡框着色 | `#7FD4FF` | 同上 → `ShaderColor`（BaseLib HSV 着色，底材质仍是红色框） |
| 地图画线 / 联机瞄准线 | 原版默认（黑） | 未覆写 |
| 能量图标配色池 | `saria`（自制） | `ProbePools.cs` → `EnergyColorName`；图标由 `ProbeEnergyIcons` 用 `TakeOverPath` 注册虚拟 res:// 路径 |

改色只需要改这三处常量，**不用动素材**。

---

## 4. 打包时怎么走

`tools/package.ps1` 会把源目录映射到 `dist/saria/`：

| 源 | 目标 |
|---|---|
| `resources/png/cards-little/*.png` | `images/portraits/`（缺失时退回 `resources/png/cards/`） |
| `resources/png/powers/*.png` | `images/powers/` |
| `resources/png/38_i01_fix.png` 等 3 张 | `images/char_select_bg_saria.png` 等 |
| `resources/spine/fight/4.2/char_202_demkni.*` | `spine/` |
| `resources/spine/4.2/build_char_202_demkni.*` | `spine/` |
| `resources/ogg/selected.ogg` | `audio/` |
| `build/saria.pck` | `saria.pck`（只含本地化 JSON 与 Spine，**不含图片**） |

> 图片不进 PCK：mod PCK 里的贴图 `ResourceLoader.Load` 会返回 null（import remap 指向游戏目录），
> 所以卡图/图标/立绘全部走散文件，运行时用 `Image.LoadPngFromBuffer` 解码。

---

## 5. 未使用的素材（留档）

`resources/png/` 根目录还有这些，当前**没有**被任何代码引用，属于草稿/参照：

| 文件 | 尺寸 | 说明 |
|---|---|---|
| `38_i01.png` | 1600×900 | 选人背景的未裁切版（16:9） |
| `参照图.png` | 3072×2048 | 参考图 |
| `拳击手套.png` | 511×511 | 单件素材草稿 |
| `科技新星.png` | 511×511 | 单件素材草稿 |
| `立绘_塞雷娅_1.png` | 1024×1024 | 立绘草稿 |
| `立绘_塞雷娅_2.png` | 2048×2048 | 立绘草稿 |
| `立绘_克里斯滕_1.png` | 900×900 | 其他角色草稿 |

另有 `resources/png/素材对照.md`（较早的对照表）：其中「部署到 `mods/probe/…`」等路径已过时，
以本文档为准。
