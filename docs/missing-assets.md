# 待制作资源清单

> 记录当前仍缺的**美术 / 音频 / 视觉配置**，以及各自的规格。代码侧缺的东西会直接写进设计表，不列在这里。
> 现状：**战斗立绘已换成塞雷娅本人**（Arknights `char_202_demkni` 改造，含战斗动画别名），商店与火堆也已换成塞雷娅骨骼（商店播 `Relax`、火堆播 `Sit`），角色选择选中音效已换成 `resources/ogg/selected.ogg`；**卡图（89 张）与状态图标（31 个）已全部换成自制图**，其余（遗物图标、能量球等）仍借原版素材占位。

---

## 1. 一图流总表（还剩哪些）

| # | 资源 | 数量 | 现状 | 规格 |
|---|---|---|---|---|
| 1 | 遗物图标 | 8 个 | 借原版图集（anchor / vajra / lantern…） | §4.1 |
| 2 | 地图标记 | 1 个 | **已用 head.png 派生（98×128）** | §4.3 |
| 3 | 多人（猜拳）手部贴图 | 4 张 | 借铁甲战士 | §4.4 |
| 4 | 角色选择过渡材质 | 1 个 | 借铁甲战士 | §4.5 |

> 已完成：战斗/商店/火堆 Spine、选人三张图、选中音效、卡图 89 张、状态图标 32 个、能量球 5 层 + 能量图标。
> 详细规格与「已实装」说明见对应小节；素材存放位置见 [`art-assets.md`](art-assets.md)。

### 1.1 能量球（能量计数器）详解

> **当前状态**：塞雷娅的 5 层自制图层已实装（`resources/png/orb/saria_orb_layer_1..5.png`，各 256×256），
> 粒子沿用原版场景并按 `#f5f98e` 染色，数字描边 `#0b3692`。下面保留完整规格，供后续调整或替换参考。

原版每个角色一套，结构完全一致（铁甲战士 / 静默猎手 / 缺陷 / 观者 都是同一套骨架，只换贴图和配色）。
下面按「文件 → 节点树 → 运行时行为 → 粒子参数 → 制作清单」拆开写。

#### 1.1.1 需要的文件

| 文件 | 是否必需 | 规格 |
|---|---|---|
| `images/ui/combat/energy_counters/<角色>/<角色>_orb_layer_1.png` | ✅ | **256×256 PNG**，透明背景 |
| …`_orb_layer_2.png` … `_orb_layer_5.png` | ✅ | 同上，共 5 张 |
| `scenes/vfx/energy/<角色>/<角色>_energy_vfx_back.tscn` | 建议 | 背景辉光粒子（可先留空） |
| `scenes/vfx/energy/<角色>/<角色>_energy_vfx_front.tscn` | 建议 | 前景火焰/火星/光环粒子 |
| `materials/ui/energy_orb_dark.tres` | 复用 | 现成的 HSV 材质（h=1, s=0.5, v=0.85），能量为 0 时自动套上 |
| `themes/kreon_bold_shared.tres` | 复用 | 数字字体，36 px |

> `energy_orb_burst.png` / `energy_orb_shine.png`（各 512×512）是通用特效贴图，与角色球体无关，不用重画。

> **参考素材已导出**：铁甲战士整套原版资源（5 张图层 PNG、场景、粒子、材质、shader、字体）在
> `resources/vanilla/energy_counter_ironclad/`，可直接打开对照着画。
> 该目录被 `.gitignore` 排除，不会入库，也不随 Release 分发（游戏资源，仅作参考）。

#### 1.1.2 场景节点树（原版 `ironclad_energy_counter.tscn`）

```
Control                       ← 128×128，pivot (64,64)，脚本 NEnergyCounter
├─ EnergyVfxBack              ← 背景粒子容器，position (64,64)，unique_name
├─ Layers                     ← %Layers，全铺满
│  ├─ Layer1  (TextureRect)   ← 静态
│  └─ RotationLayers          ← %RotationLayers，全铺满
│     ├─ Layer2 (TextureRect) ← pivot (64,64)，会旋转
│     └─ Layer3 (TextureRect) ← pivot (64,64)，会旋转
│  ├─ Layer4  (TextureRect)   ← 静态
│  └─ Layer5  (TextureRect)   ← 静态
├─ EnergyVfxFront             ← 前景粒子容器，position (64,64)，unique_name
└─ Label                      ← MegaLabel，"3/3"，36 px（Min 32 / Max 36）
```

所有 `TextureRect` 的 `expand_mode = 1`、`stretch_mode = 5`，即**按 128×128 拉伸铺满**，
所以源图 256×256 是「2 倍图」，缩到一半显示。

#### 1.1.3 运行时行为（代码里写死的，做素材时按这个来设计）

| 时机 | 表现 |
|---|---|
| **每帧** | 只有 `RotationLayers` 里的 **Layer2 / Layer3** 在转：<br>能量 > 0 时速度 **30°/s × (i+1)** → Layer2 = 30°/s，Layer3 = 60°/s；<br>能量 = 0 时降到 **5°/s × (i+1)** → 5°/s、10°/s |
| **能量 = 0** | 全部 5 层套上 `energy_orb_dark.tres`（HSV 压暗）+ `Layers.Modulate = 暗灰`；数字变**红色**、描边换成「费用不足」色 |
| **获得能量** | `EnergyVfxBack` 与 `EnergyVfxFront` 各 `Restart()` 一次（粒子重新爆发） |
| **进入战斗** | 整个控件从 `(-480, 128)` 补间到 `(0, 0)`，**0.6 s**，`Expo/Out` |
| **离开战斗** | 反向补间到 `(-480, 128)`，0.6 s，`Back/In` |
| **鼠标悬停** | 弹出能量说明，位置偏移 `(-70, -200)` |

#### 1.1.4 各层画什么（按旋转与否区分职责）

| 层 | 是否旋转 | 建议内容 |
|---|---|---|
| Layer1 | 否 | **球体本体**：底色、内发光、外轮廓（不透明主体都在这层） |
| Layer2 | **是** | 环形能量流 / 旋转光带 —— 必须**360° 无缝**，转到任何角度都不能看出接缝 |
| Layer3 | **是** | 更细的一圈：粒子带、电弧、旋转高光点（同样要无缝） |
| Layer4 | 否 | 前景高光 / 反光 / 球面上的亮斑 |
| Layer5 | 否 | 最上层叠加：边缘辉光、飘出的能量火花 |

> 关键：**Layer2 / Layer3 是整圈旋转的**，如果画的是不对称图形（比如一个亮斑），转起来会很明显；
> 这两层请画成中心对称或整圈重复的图案。其余三层是静态，可以自由构图。

#### 1.1.5 背景粒子（`*_energy_vfx_back.tscn`）

原版就是一个 `vfx_common_glow` 粒子实例，参数：

| 参数 | 值 |
|---|---|
| `self_modulate` | `(1, 0.485, 0.303, 1)` 橙色 |
| gravity | 0 |
| `scale_min / scale_max` | 3.5 / 3.5 |
| `scale_curve` | (0 → 0) 起、0.2 处 0.84、1 处 1.0 |
| `alpha_curve` | 0 处 0.35 → 1 处 0 |
| `particle_flag_disable_z` | true |

作用：球体外围一圈缓慢辉光，获得能量时闪一下。

#### 1.1.6 前景粒子（`*_energy_vfx_front.tscn`）

5 个子发射器（都用 `NParticlesContainer` 统一 Restart）：

| 子节点 | 位置 | 特点 |
|---|---|---|
| `vfx_fire_flipbook_1` | (11, -55) | 火焰 flipbook，`lifetime 0.3`，角度抖动 ±10°，scale 0.4~0.6 |
| `vfx_fire_flipbook_2` | (-51, -3)，旋转 -45° | 角度 ±30°，scale 0.2~0.3 |
| `vfx_fire_flipbook_3` | (50, 9) | 角度 ±20°，scale 0.2~0.3 |
| `vfx_fire_embers_straight` | (0, 0) | 火星直线上升 |
| `vfx_common_ring_polar_a` | (1, -3) | 橙色光环 `(1, 0.33, 0.11, 1)`，`lifetime 0.4` |

共同点：`gravity = 0`、`emission_shape = sphere(0.01)`、颜色渐变 HDR（白 → 灰）、`alpha` 在 0.5 处开始衰减。

#### 1.1.7 给美术的制作清单

1. 画 **5 张 256×256 透明 PNG**，命名 `<角色>_orb_layer_1..5.png`；
2. Layer1 放不透明球体，Layer2 / Layer3 画成**可整圈循环**的环状图案（这两层会被转）；
3. Layer4 / Layer5 放静态高光与叠加辉光；
4. 中心对齐画布中心（游戏按 128×128 拉伸，pivot 在中心）；
5. 想省事可以先只做 5 张图、粒子场景复用铁甲战士的（颜色会偏暖，但可用）；
6. 想完全自定义氛围，再单独做 back/front 两个粒子场景。

#### 1.1.8 技术接入（已实现，记录现状）

`PlaceholderCharacterModel` 提供 `CustomEnergyCounterPath` / `CustomEnergyCounter`，但有两个已知限制：

- **.tscn 不能塞进 mod 的 PCK**（PCK 内的场景加载有问题，和贴图一样受限）；
- 因此要用自定义能量球，需要**运行时用代码构建**这个 Control（5 个 TextureRect + 2 个粒子容器 + Label），
  再由补丁替换 `EnergyCounterPath` 的实例化结果。

**现状**：已用「运行时换图」实现——`NEnergyCounter.Create` 之后把 5 层 `TextureRect` 换成散文件解码的
`saria_orb_layer_1..5.png`，并把前后粒子容器递归 `Modulate` 成 `#f5f98e`；数字描边色由
`CharacterModel.EnergyLabelOutlineColor`（`#0b3692`）控制。


---
## 2. 卡图（已实装）

89 张自制卡图已接入：

| 项 | 说明 |
|---|---|
| 源图 | 
esources/png/cards/（不入库） |
| 发布用压缩图 | 
esources/png/cards-little/（入库，5.7 MB）：500×380 / 820×1146，256 色抖动 |
| 运行时位置 | mods/saria/images/portraits/<类名>.png（散文件） |
| 加载方式 | Image.LoadPngFromBuffer → ImageTexture，在 NCard.UpdatePortrait 之后替换贴图 |

> 卡图**不能**放 mod PCK：里面贴图的 import remap 指向游戏的 
es://.godot/imported/，
> ResourceLoader.Exists 为 true 但 Load 返回 null（实测）。所以卡图必须走散文件。

## 3. 音效（角色选择选中）—— 已实装

原版这一项是 **FMOD 事件路径**（`event:/sfx/characters/<id>/<id>_select`），mod 无法注册新事件，
所以改成了「哨兵值 + 拦截播放」：

| 项 | 内容 |
|---|---|
| 素材 | `resources/ogg/selected.ogg` → 打包到 `mods/saria/audio/selected.ogg` |
| 代码 | `CharacterSelectSfx => ProbeSelectSfx.Key`；Harmony 拦截 `SfxCmd.Play` 后播本地文件 |
| 格式 | Ogg Vorbis，44.1 kHz，立体声，**0.906 s**（36.9 KB） |

> 以后换音效：直接替换 `resources/ogg/selected.ogg`，重新打包即可，不用改代码。
> 仍走 FMOD 的音效（攻击 / 施法 / 死亡）继续借用铁甲战士事件。

## 4. 图标与小图规格（逐项详解）

下面每项都给出「原版用什么文件 / 多大 / 我们缺什么」，可以直接照着出图。
统一约定：**PNG、透明背景、不要圆角与外发光外的底色**（圆角/边框由游戏叠加或另有限定）。

### 4.1 遗物图标（8 个）

原版每个遗物**两张图**：

| 用途 | 文件 | 规格 |
|---|---|---|
| 状态栏小图标 | 图集区域 `images/atlases/relic_atlas.png` 里的 `relic_atlas.sprites/<IconBaseName>.tres` | 原版 `burning_blood` 区域 **71×81**，`margin = (8, 2, 14, 4)`（九宫格，用来拉伸） |
| 悬停/详情大图 | `images/relics/<IconBaseName>.png` | **256×256** |

细节：

- 小图标是**图集里的一块**，带 9-slice 边距；自制时按 71×81 画、四周留 8/2/14/4 的可拉伸边即可；
- 大图 256×256 是独立 PNG，游戏直接缩放显示；
- 我们 mod 现在用 `IconBaseName` 借原版（`anchor` / `vajra` / `lantern` / `bag_of_marbles` / `burning_blood`），
  自制后需要**运行时替换**（做法同卡图：`NRelic.Icon` / `BigIcon` 后置补丁 + 散文件加载）。

需要出的 8 个（中文名 → 建议文件名，与类名一致）：

| 中文名 | 建议文件名 |
|---|---|
| 钙质技艺 | `CalciumCraft.png` |
| 校准装置 | `CalibrationDevice.png` |
| 富集仓 | `EnrichmentVat.png` |
| 工作牌 | `WorkBadge.png` |
| 谐振回路 | `ResonanceCircuit.png` |
| 腕带绷带 | `WristBandage.png` |
| 源石样本 | `OriginiumSample.png` |
| 钙质操纵 | `CalciumManipulation.png` |

### 4.2 能量图标（卡面 / 提示 / 消耗文本）

三种使用场景，实际用的是**同一张图标**：

| 场景 | 来源 |
|---|---|
| 卡面左上角费用 | `CardPoolModel.EnergyIconPath` → 图集区域 `ui_atlas.sprites/card/energy_<pool>.tres` |
| 卡牌/状态提示文本里的 `{Energy}` | 同上 |
| 部分 UI 的大号能量图标 | `CustomCardPoolModel.BigEnergyIconPath`（可单独指定） |

规格：

- 图集区域原版 **74×74**（`energy_ironclad`），游戏内按约 32×32 显示 → **2 倍图**；
- 没有图集版本时会退回 `images/packed/energy_icons/energy_<pool>.png`（同样是 74×74）；
- 我们 mod 现在 `EnergyColorName = "saria"`：卡面/提示/正文三处都由 `ProbeEnergyIcons` 用
  `TakeOverPath` 注册的虚拟 res:// 路径指向 `energy_saria.png`（详见 [`patches.md`](patches.md) §5）；
- 自制时建议出 **74×74 PNG**，另可出一个 148×148 的用于大图位（可选）。

> 注意：`energy_orb_burst.png` / `energy_orb_shine.png`（512×512）是能量球特效贴图，**不是**这个图标。

### 4.3 地图标记（已完成：由 head.png 派生）

| 项 | 内容 |
|---|---|
| 路径 | `images/packed/map/icons/map_marker_<角色>.png` |
| 原版尺寸 | **46×60**（另有 49×64 的高分辨率导入版本） |
| 内容 | 角色头部特写，透明背景，底部对齐地图节点 |
| 建议 | 按 **49×64 的 2 倍图（98×128）** 出图；深色描边，保证浅色地图上可读 |

**已实现**：优先使用专用图 `resources/png/map_marker_saria.png`（196×256，等比 49:64）；缺失时 `tools/prepare_icons.py` 才从 `head.png` **居中裁剪**，
再 Lanczos 缩放到 **98×128**，输出 `resources/png/derived/map_marker_saria.png`；
运行时由 `ProbeMapMarkerPatch`（`NMapMarker.Initialize` 后置）把 `TextureRect.Texture` 换成该图
（原版 `CharacterModel.MapMarker` 返回 `CompressedTexture2D`，mod 造不出来，所以换在出口处）。

### 4.4 多人猜拳手部贴图（4 张）

多人模式里的「石头剪刀布」要用**手臂**贴图，每个角色 4 张：

| 文件 | 手势 |
|---|---|
| `images/ui/hands/multiplayer_hand_<角色>_rock.png` | 石头 |
| `..._paper.png` | 布 |
| `..._scissors.png` | 剪刀 |
| `..._point.png` | 指向（选人/指向队友） |

规格：原版 **422×1200**（竖长构图，整条手臂从画布底部伸上来），透明背景；
游戏内会缩放并做轻微旋转动画，所以**手臂根部要留在画布底边**，手指朝上。

我们 mod 现在用 `CustomArmPointingTexturePath` / `Rock` / `Paper` / `Scissors` 指向铁甲战士。

### 4.5 角色选择过渡材质（转场遮罩）

| 项 | 内容 |
|---|---|
| 路径 | `materials/transitions/<角色>_transition_mat.tres` |
| 用到的贴图 | `images/ui/transitions/<角色>_transition.png`，原版 **2560×1200** |
| 材质类型 | `ShaderMaterial`，内联 shader（见下） |

原版 shader 逻辑：把贴图的**红通道**当遮罩，`threshold`（默认 **0.332**）控制扫描进度，
`COLOR.a = step(1.0 - tex.r, mix(-0.1, 1.1, threshold))` —— 也就是**用一张灰度图做「擦除/显现」动画**。

出图要求：

1. 一张 **2560×1200 的灰度 PNG**（用红通道即可，白=显现、黑=遮住）；
2. 图案建议做成角色的标志性元素（塞雷娅可用钙质结晶/盾形裂纹的扩散）；
3. `.tres` 素材可以复用原版结构，只把 `transitionTex` 换成自己的图，`threshold` 保持 0.332。

> 该图只出现在「进入/退出战斗、进入角色选择」的转场瞬间，属于锦上添花项。

### 4.6 本节汇总（出图清单）

| 资源 | 张数 | 尺寸 | 备注 |
|---|---|---|---|
| 遗物小图标 | 8 | 71×81（九宫格边距 8/2/14/4） | 图集风格 |
| 遗物大图标 | 8 | 256×256 | 独立 PNG |
| 能量图标 | 1 | 74×74（可选 148×148） | 卡面/提示/文本通用 |
| 地图标记 | 1 | 49×64（建议 98×128） | 头肩特写 |
| 猜拳手部 | 4 | 422×1200 | 手臂竖构图，根部贴底边 |
| 过渡遮罩 | 1 | 2560×1200 | 红通道遮罩，可复用原版材质 |

## 5. 纯代码配置（不用画图）

| 项 | 位置 | 现状 | 建议 |
|---|---|---|---|
| 地图画线颜色 | `CharacterModel.MapDrawingColor` | 原版默认（黑） | 已回退，未覆写 |
| 联机瞄准线 | `CharacterModel.RemoteTargetingLineColor` | 原版默认（黑） | 已回退，未覆写 |
| 卡牌背景色 | `CardPoolModel.DeckEntryCardColor` | `#7FD4FF` | 已设 |
| 卡框着色 | `CustomCardPoolModel.ShaderColor` | `#7FD4FF` | 已设（走 BaseLib HSV 着色，框材质仍是红色） |
| 能量文字描边 | `CardPoolModel.EnergyOutlineColor` | 默认 `#5C5440` | 可按新卡框配色微调 |
| 角色名颜色 | `CharacterModel.NameColor` | `#7FD4FF` | 已设 |

---

## 6. 优先级建议

1. **遗物图标 8 个**（详细规格见 §4.1）—— 数量少、单张成本低，做完角色栏就完整了。
2. **地图标记 + 猜拳手部**（§4.3–§4.4）—— 都是小图，可批量处理。
3. **角色选择过渡材质**（§4.5）—— 只影响转场瞬间，优先级最低。

---
## 7. 技术约束备忘

- Spine 版必须是 **4.2.43**（spine-godot 4.2），骨架/图集/贴图三件套；PCK 里只装 `.atlas/.skel` 与本地化 JSON，图片走散文件（见 [spine-conversion.md](spine-conversion.md)）。
- 图片资源请提供 **PNG**；mod 运行时用 `Image.LoadPngFromBuffer` 读散文件，**不吃 `.ctex`**。
- 音频若走文件播放，同样放散文件目录（`mods/saria/audio/`），不要塞进 PCK。

---

## 8. `resources/spine/build_char_202_demkni*` 里可用的动画

这是 **非战斗版**骨骼（3.8 原版 + `4.2/` 转换版各一份），动画只有 6 个，另有 2 个事件：

| 动画名 | 适合用途 | 备注 |
|---|---|---|
| `Default` | 空动画 / 占位 | 时长 0 |
| `Interact` | 商店待机、交互 | 与战斗版 `cast` 同源（Skill_Begin 系） |
| `Move` | 走位、入场 | 战斗版借用为 `attack` |
| `Relax` | **商店 `relaxed_loop`** | 原版商人骨骼默认循环动画就叫 `relaxed_loop` |
| `Sit` | **火堆 `overgrowth_loop` / `hive_loop` / `glory_loop`** | 火堆按幕数切这三个名，本质是同一段"坐下"循环 |
| `Sleep` | 火堆睡觉、可选 | 结束时用 |

事件：`OnAttack`、`OnStart`。

### 已采用的方案（不用加别名）

原版场景按固定的动画名播放（商店 `relaxed_loop`、火堆按幕数 `overgrowth_loop` / `hive_loop` / `glory_loop`），
我们的骨骼里没有这些名字，所以改成**运行时直接换骨骼 + 播我们自己的动画**：

| 场景 | 实现 | 动画 |
|---|---|---|
| 商店 | `NMerchantRoom.AfterRoomIsLoaded` 后处理，按玩家顺序找到塞雷娅那个商人 | **`Relax`**（循环） |
| 火堆 | `NRestSiteCharacter._Ready` 后处理 | **`Sit`**（循环） |
| 兜底 | 动画名不存在时 | **`Default`**（并打警告） |

> 想换成 Sleeping 观感，把火堆那处的 `Sit` 改成 `Sleep` 即可（一行）。
> 原版还会尝试设 `_tracks/light_off`（火堆灯光），我们的骨骼没有这条轨道，代码会自动跳过，**不需要补**。
