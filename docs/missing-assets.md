# 待制作资源清单

> 记录当前仍缺的**美术 / 音频 / 视觉配置**，以及各自的规格。代码侧缺的东西会直接写进设计表，不列在这里。
> 现状：**战斗立绘已换成塞雷娅本人**（Arknights `char_202_demkni` 改造，含战斗动画别名），商店与火堆也已换成塞雷娅骨骼（商店播 `Relax`、火堆播 `Sit`），其余（卡图、图标、能量球、音效）仍借原版素材占位。

---

## 1. 一图流总表

| # | 资源 | 数量 | 现状 | 规格要点 |
|---|---|---|---|---|
| 1 | 卡图（一般） | 87 张 | 借用铁甲战士 5 张卡图循环 | **1000×760 PNG**（显示区 250×190） |
| 2 | 卡图（先古） | 2 张（渗透、极限功率） | 同上 | **竖幅**，显示区 410×573，建议 820×1146 |
| 3 | 能量球（能量计数器） | 1 套 | 借用铁甲战士场景 | 5 张图层 **256×256 PNG**，显示 128×128；另加前后 2 个 VFX 场景 |
| 4 | 角色选择选中音效 | 1 个 | 借用铁甲战士事件 | 见 §3（FMOD 事件无法新增，需文件播放补丁） |
| 5 | 能力（power）图标 | 31 个 | 全部借 `thorns_power` | 见 §4 |
| 6 | 遗物图标 | 8 个 | 借原版图集（anchor/vajra/lantern…） | 见 §4 |
| 7 | 能量图标（卡面/UI） | 2–3 个 | 借铁甲战士 | `EnergyColorName = ironclad` |
| 8 | 地图标记 | 1 个 | 借铁甲战士 | 小图，见 §4 |
| 9 | 多人（猜拳）手部贴图 | 3 张 | 借铁甲战士 | 石头/剪刀/布 + 指人 |
| 10 | 角色选择过渡材质 | 1 个 | 借铁甲战士 | `.tres` 材质 |
| 11 | 地图画线颜色 | 1 项 | **未设置**（默认黑） | 代码一行，见 §5 |
| 12 | 卡牌背景/边框颜色 | 1 项 | 已设 `#7FD4FF` | 见 §5，可微调 |

---

## 2. 卡图规格

| 项目 | 一般卡 | 先古卡 |
|---|---|---|
| 显示区域 | 250×190（横向 1.32:1） | 约 410×573（竖向 0.72:1） |
| 推荐源尺寸 | **1000×760** | **820×1146**（或 1024×1440） |
| 格式 | PNG（带透明通道，无圆角、无边框） | 同左 |
| 命名 | `<类名>.png`，例如 `ProbeStrike.png` | 同左 |
| 放置 | `mods/saria/images/portraits/`，代码里 `CustomPortraitPath` 指过去 | 同左 |

补充：

- 原版卡图源图是 1000×760，游戏内缩到 250×190 显示；**不要带卡框**（框由游戏叠加）。
- 先古卡（`CardRarity.Ancient`）复用同一张 `Portrait`，但显示在竖幅 `AncientPortrait` 里 —— 所以横向卡图会被裁切，**先古卡必须单独出竖构图**。
- 卡图清单与英文代号见 [`../mods-src/probe/design/card-ids.md`](../mods-src/probe/design/card-ids.md)。

---

## 3. 音效规格（角色选择选中）

现状：`CharacterSelectSfx => "event:/sfx/characters/ironclad/ironclad_select"`。
原版这一项是 **FMOD 事件路径**（`event:/...`），由原生音频节点播放，**mod 无法注册新事件**。

两条路：

1. **继续借用原版事件**（现状，零成本）；或
2. **自备音频文件 + 一小段播放补丁**：Harmony 拦截角色选择点击处的 `SfxCmd.Play(character.CharacterSelectSfx)`，换成 `AudioStreamPlayer` 播放我们自己的文件。

自备音频的建议参数：

| 属性 | 建议值 |
|---|---|
| 格式 | **.ogg（Vorbis）** 优先，或 .mp3 |
| 时长 | **0.6 ~ 1.2 s**（短促确认音，不要拖尾） |
| 采样率 | 44.1 kHz |
| 声道 | 单声道（短音效不需要立体声） |
| 峰值 | ≤ −3 dBFS，起音前留 5 ms 静音、结尾 50 ms 淡出（防爆音） |

---

## 4. 图标与小图规格

| 资源 | 推荐尺寸 | 说明 |
|---|---|---|
| 能力（power）图标 | **64×64**（打包图集后 32×32 显示） | 需要 `CustomPackedIconPath` + `CustomBigIconPath` 两档；现在 31 个全借 `thorns_power` |
| 遗物图标 | **128×128**（大图）+ 图集小图 | 现在用 `IconBaseName` 借原版（anchor / vajra / lantern / bag_of_marbles / burning_blood） |
| 能量图标 | 32×32（文本内）/ 64×64（大） | `CustomCardPoolModel.BigEnergyIconPath`、`TextEnergyIconPath` |
| 地图标记 | 48×48 左右 | `CustomMapMarkerPath`；建议等比图标，深色描边保证浅色地图可读 |
| 猜拳手部 | 与铁甲战士同尺寸即可 | `CustomArmPointingTexturePath` / Rock / Paper / Scissors |

---

## 5. 纯代码配置（不用画图）

| 项 | 位置 | 现状 | 建议 |
|---|---|---|---|
| 地图画线颜色 | `CharacterModel.MapDrawingColor`（还有 `RemoteTargetingLineColor`） | 未覆写 → 黑色 | 改成角色色 `#7FD4FF`，一行属性 |
| 卡牌背景色 | `CardPoolModel.DeckEntryCardColor` | `#7FD4FF` | 已设；如需更暗可调 |
| 卡框着色 | `CustomCardPoolModel.ShaderColor` | `#7FD4FF` | 已设（走 BaseLib HSV 着色，框材质仍是红色） |
| 能量文字描边 | `CardPoolModel.EnergyOutlineColor` | 默认 `#5C5440` | 可按新卡框配色微调 |
| 角色名颜色 | `CharacterModel.NameColor` | `#7FD4FF` | 已设 |

---

## 6. 优先级建议

1. **卡图 89 张** —— 工作量最大，但对面子影响最直接；可以先出 10 张通用底图（打击/防御/技能/能力/先古各一套色彩）顶一阵。
2. **商店 / 火堆骨骼** —— 只是把同一套骨骼的 `Relax` / `Sit` 复制成引擎要的动画名（见 §8），成本很低。
3. **能力图标 31 个 + 遗物图标 8 个** —— 数量多但单张成本低，适合批量出。
4. **能量球** —— 场景固定、复用度高。
5. **选中音效 + 地图画线颜色** —— 成本最低，随时可做。

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

