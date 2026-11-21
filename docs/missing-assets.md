# 待制作资源清单

> 记录当前仍缺的**美术 / 音频 / 视觉配置**，以及各自的规格。代码侧缺的东西会直接写进设计表，不列在这里。
> 现状：**战斗立绘已换成塞雷娅本人**（Arknights `char_202_demkni` 改造，含战斗动画别名），商店与火堆也已换成塞雷娅骨骼（商店播 `Relax`、火堆播 `Sit`），角色选择选中音效已换成 `resources/ogg/selected.ogg`；**卡图（89 张）与状态图标（31 个）已全部换成自制图**，其余（遗物图标、能量球等）仍借原版素材占位。

---

## 1. 一图流总表

| # | 资源 | 数量 | 现状 | 规格要点 |
|---|---|---|---|---|
| 1 | 能量球（能量计数器） | 1 套 | 借用铁甲战士场景 | 5 张图层 **256×256 PNG**，显示 128×128；另加前后 2 个 VFX 场景 |
| 2 | 遗物图标 | 8 个 | 借原版图集（anchor/vajra/lantern…） | 见 §4 |
| 3 | 能量图标（卡面/UI） | 2–3 个 | 借铁甲战士 | `EnergyColorName = ironclad` |
| 4 | 地图标记 | 1 个 | 借铁甲战士 | 小图，见 §4 |
| 5 | 多人（猜拳）手部贴图 | 3 张 | 借铁甲战士 | 石头/剪刀/布 + 指人 |
| 6 | 角色选择过渡材质 | 1 个 | 借铁甲战士 | `.tres` 材质 |

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

## 4. 图标与小图规格

| 资源 | 推荐尺寸 | 说明 |
|---|---|---|
| 遗物图标 | **128×128**（大图）+ 图集小图 | 现在用 `IconBaseName` 借原版（anchor / vajra / lantern / bag_of_marbles / burning_blood） |
| 能量图标 | 32×32（文本内）/ 64×64（大） | `CustomCardPoolModel.BigEnergyIconPath`、`TextEnergyIconPath` |
| 地图标记 | 48×48 左右 | `CustomMapMarkerPath`；建议等比图标，深色描边保证浅色地图可读 |
| 猜拳手部 | 与铁甲战士同尺寸即可 | `CustomArmPointingTexturePath` / Rock / Paper / Scissors |

---

## 5. 纯代码配置（不用画图）

| 项 | 位置 | 现状 | 建议 |
|---|---|---|---|
| 地图画线颜色 | `CharacterModel.MapDrawingColor` | `#e8ebbe` | 已设 |
| 联机瞄准线 | `CharacterModel.RemoteTargetingLineColor` | `#e8ebbe` | 已设 |
| 卡牌背景色 | `CardPoolModel.DeckEntryCardColor` | `#e8ebbe` | 已设 |
| 卡框着色 | `CustomCardPoolModel.ShaderColor` | `#e8ebbe` | 已设（走 BaseLib HSV 着色，框材质仍是红色） |
| 能量文字描边 | `CardPoolModel.EnergyOutlineColor` | 默认 `#5C5440` | 可按新卡框配色微调 |
| 角色名颜色 | `CharacterModel.NameColor` | `#e8ebbe` | 已设 |

---

## 6. 优先级建议

2. **商店 / 火堆骨骼** —— 只是把同一套骨骼的 `Relax` / `Sit` 复制成引擎要的动画名（见 §8），成本很低。
3. **能力图标 31 个 + 遗物图标 8 个** —— 数量多但单张成本低，适合批量出。
4. **能量球** —— 场景固定、复用度高。
5. **能量球** 与剩下的 **角色选择过渡材质** —— 都只影响观感的一小块。

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
