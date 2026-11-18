# 待制作资源清单

> 记录当前仍缺的**美术 / 音频 / 视觉配置**，以及各自的规格。代码侧缺的东西会直接写进设计表，不列在这里。
> 现状：所有立绘、图标、音效都是**借原版素材**占位，能玩但不像自己的角色。

---

## 1. 一图流总表

| # | 资源 | 数量 | 现状 | 规格要点 |
|---|---|---|---|---|
| 1 | 卡图（一般） | 87 张 | 借用铁甲战士 5 张卡图循环 | **1000×760 PNG**（显示区 250×190） |
| 2 | 卡图（先古） | 2 张（渗透、极限功率） | 同上 | **竖幅**，显示区 410×573，建议 820×1146 |
| 3 | 战斗骨骼（角色 Spine） | 1 套 | 借用 `char_202_demkni` | Spine **4.2.43**；需 idle_loop / attack / cast / powerup / hurt / die / stun / revive / relaxed_loop |
| 4 | 能量球（能量计数器） | 1 套 | 借用铁甲战士场景 | 5 张图层 **256×256 PNG**，显示 128×128；另加前后 2 个 VFX 场景 |
| 5 | 商店骨骼 | 1 套 | 借用铁甲战士 | Spine；默认动画 `relaxed_loop`；场景 scale ≈ 0.47 |
| 6 | 火堆骨骼 | 1 套 | 借用铁甲战士 | Spine；场景 scale ≈ 0.76，position (-2, 42) |
| 7 | 角色选择选中音效 | 1 个 | 借用铁甲战士事件 | 见 §3（FMOD 事件无法新增，需文件播放补丁） |
| 8 | 能力（power）图标 | 31 个 | 全部借 `thorns_power` | 见 §4 |
| 9 | 遗物图标 | 8 个 | 借原版图集（anchor/vajra/lantern…） | 见 §4 |
| 10 | 能量图标（卡面/UI） | 2–3 个 | 借铁甲战士 | `EnergyColorName = ironclad` |
| 11 | 地图标记 | 1 个 | 借铁甲战士 | 小图，见 §4 |
| 12 | 多人（猜拳）手部贴图 | 3 张 | 借铁甲战士 | 石头/剪刀/布 + 指人 |
| 13 | 角色选择过渡材质 | 1 个 | 借铁甲战士 | `.tres` 材质 |
| 14 | 地图画线颜色 | 1 项 | **未设置**（默认黑） | 代码一行，见 §5 |
| 15 | 卡牌背景/边框颜色 | 1 项 | 已设 `#7FD4FF` | 见 §5，可微调 |

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
2. **战斗 Spine（塞雷娅本体）** —— 现在整场战斗都是别人的骨骼动作，这是最"出戏"的一处。
3. **能力图标 31 个 + 遗物图标 8 个** —— 数量多但单张成本低，适合批量出。
4. **能量球 / 商店 / 火堆骨骼** —— 场景固定、复用度高，可以一次做完。
5. **选中音效 + 地图画线颜色** —— 成本最低，随时可做。

---

## 7. 技术约束备忘

- Spine 版必须是 **4.2.43**（spine-godot 4.2），骨架/图集/贴图三件套；PCK 里只装 `.atlas/.skel` 与本地化 JSON，图片走散文件（见 [spine-conversion.md](spine-conversion.md)）。
- 图片资源请提供 **PNG**；mod 运行时用 `Image.LoadPngFromBuffer` 读散文件，**不吃 `.ctex`**。
- 音频若走文件播放，同样放散文件目录（`mods/saria/audio/`），不要塞进 PCK。
