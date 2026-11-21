# Harmony 补丁清单（调试用）

> 每加一个补丁，**必须**在本表登记一行（用户 2026-11-22 要求）。
> 调试时按「目标方法」或「日志前缀」定位；上线前用它核对补丁数量。

---

## 0. 先看这里：怎么判断补丁生效

启动日志（`%APPDATA%\SlayTheSpire2\logs\godot.log`）里有且只有一行：

```
[saria] harmony patches: 24 applied, 0 skipped
```

- **数量不对**（比如少了一个） → 说明某个类没被 `PatchAll` 扫到，或目标方法改名（游戏更新）；
- **skipped > 0** → 日志里会有 `[saria] patch <类名> skipped: <原因>`，通常是参数名对不上或方法不存在；
- 运行时行为日志统一带 `[saria]` 前缀，按子系统再分：

| 前缀 | 含义 |
|---|---|
| `[saria][art]` | 卡图（解码失败、缺文件、NCard 挂钩异常） |
| `[saria][power-art]` | 状态图标 |
| `[saria][orb]` | 能量球图层、能量图标、悬停提示图标 |
| `[saria][spine]` | 战斗/商店/火堆骨骼、缩放 |
| `[saria] dialogue …` | 先古对话构建 |

调试某个补丁时，先 grep 它对应的日志前缀，确认**是否被调用**，再判断逻辑。

---

## 1. 视觉与资源类

| 补丁类 | 目标 | 类型 | 作用 | 关键日志 |
|---|---|---|---|---|
| `ProbeCardPortraitPatch` | `NCard.UpdatePortrait` | Postfix | 卡面换成自制卡图（散文件） | `[saria][art] missing portrait` |
| `ProbeCardBigPortraitPatch` | `NCard.SetPortraitTexture` | Postfix | 检视大图同样换图（否则回落到打击图） | 同上 |
| `ProbePowerIconPatch` | `NPower.Reload` | Postfix | 状态栏图标换成 64×64 自制图 | `[saria][power-art]` |
| `ProbePowerHoverIconPatch` | `PowerModel.get_Icon` | Postfix | 状态悬停提示图标 | 同上 |
| `ProbeOrbPatch` | `NEnergyCounter.Create` | Postfix | 能量球 5 层换图 + 粒子染色 `#f5f98e` | `[saria][orb] custom energy orb applied` |
| `ProbeCardEnergyIconPatch` | `NCard.Reload` | Postfix | 卡面费用图标换成 `energy_saria.png` | 无（失败才 warn） |
| `ProbeEnergyHoverTipPatch` | `HoverTipFactory.ForEnergyWithIconPath` | Postfix | **能量悬停提示框图标**（原版按 res:// 路径取图，mod 加载不了） | `[saria][orb] energy hover tip icon replaced` |
| `ProbeCharacterSelectBgPatch` | `NCharacterSelectScreen.SelectCharacter` | Postfix | 选人界面背景换成自制图 | `[saria] character select background replaced` |
| `ProbeCharacterIconPatch` | `CharacterModel.get_Icon` / `get_IconTexture` / `get_IconOutlineTexture`、`NCharacterSelectButton.Init` | Postfix | 顶部栏/选人栏头像 | `[saria] character select portrait applied` |
| `ProbeVisualsPatch` | `CharacterModel.CreateVisuals` | Prefix | 程序化构建战斗 Spine 视觉 | `[saria][spine] creature visuals built` |
| `ProbeRestSiteSpinePatch` | `NRestSiteCharacter._Ready` | Postfix | 火堆骨骼换塞雷娅 + `Sit` + 缩放 1.6 | `[saria][spine] rest site: swapping…` |
| `ProbeMerchantSpinePatch` | `NMerchantRoom.AfterRoomIsLoaded` | Postfix | 商店骨骼换塞雷娅 + `Relax` + 缩放 1.15 | `[saria][spine] merchant: swapping…` |
| `ProbeSelectSfxPatch` | `NCharacterSelectScreen.SelectCharacter` | Postfix | 选中音效播本地 ogg（+0.2 s 空档） | `[saria] character select sfx scheduled` |

> `ProbeEnergyHoverTipPatch` 的判定是「当前角色是塞雷娅 **且** 提示图标路径含 `energy_ironclad`」，
> 后者用来避免把无色/其他卡池的提示也换掉。若某天能量图标换了配色池名，这里要同步改。

---

## 2. 游戏规则类

| 补丁类 | 目标 | 类型 | 作用 |
|---|---|---|---|
| `CalciumArmorPersistPatch` | `PowerModel.ShouldRemoveDueToAmount`、`PowerModel.SetAmount` | Prefix | 钙铠永不移除、层数不为负（这两个方法非 virtual，只能打补丁） |
| `CalcifyDamagePatch` | `Hook.ModifyDamage` | Postfix | 钙质化 / 脆化技艺的「易伤倍率之后」加伤（钩子层无法表达顺序） |
| `ProbeHemostasisTickPatch` | `Hook.BeforeDamageReceived` | Postfix | 止血的「受击次数」计数（power 自身的钩子在这条链路上不触发） |
| `MadSciencePowerAnimPatch` | `MadScience` 的 `ExecutePower` 状态机 `MoveNext` | Transpiler | 把 `"Cast"` 换成 `"PowerUp"`，让能力型疯狂科学播能力动画 |
| `ProbeAncientDialoguePatch` | `AncientDialogueSet.GetValidDialogues` | Postfix | 塞雷娅的先古对话按原版优先级参与抽取 |
| `ArchitectWinRunPatch` | `TheArchitect.WinRun` | Prefix | 自定义角色缺少终局对话时补占位对话（否则 NRE、无法通关） |
| `ProbeNextButtonLabelPatch` | `NAncientEventLayout.UpdateFakeNextButton` | Postfix | 先古对话「继续」按钮文字 |

> `MadSciencePowerAnimPatch` 是 **async 方法**：IL 在编译器生成的状态机里，
> 所以目标写成 `TargetMethod()` 动态定位 `<ExecutePower>d__N.MoveNext`；直接打 `ExecutePower` 会找不到 `"Cast"` 字面量。

---

## 3. 已废弃 / 需要注意

| 项 | 说明 |
|---|---|
| `get_Portrait` 补丁 | 早期尝试，属性 getter 可能被内联，已删除；改用 `NCard.UpdatePortrait` |
| `SfxCmd.Play` 补丁 | 早期尝试，日志显示未触发；改用 `NCharacterSelectScreen.SelectCharacter` |
| 文本内联能量图标（卡面描述里的 `{Energy}`） | **已实装**（v0.4.12–14）：不走补丁，见 §5「虚拟 res:// 路径」 |

---

## 4. 新增补丁的检查清单

1. 目标方法是 **virtual 或 public/private 方法**（避免属性 getter，可能被内联）；
2. 有明确的**判定条件**，不要影响原版角色/卡牌；
3. 至少一条**成功或失败日志**（带 `[saria]` 前缀），否则出问题无法定位；
4. 在本文件登记：目标、类型、作用、日志关键词、已知限制；
5. 启动日志里的补丁计数 +1，确认 `applied` 数量对得上。

---

## 5. 虚拟 res:// 路径（能量图标用的机制，不是补丁）

正文里的 `{Energy}` 走的是 RichTextLabel 的 BBCode：

```csharp
"[img]res://images/packed/sprite_fonts/<前缀>_energy_icon.png[/img]"
```

这类图片由引擎按 `res://` 路径加载，mod 的 PCK 贴图加载不了（`Exists=True / Load=null`）。
解决办法不是补丁，而是 **`Resource.TakeOverPath()`**：

```csharp
var tex = ImageTexture.CreateFromImage(image);
tex.TakeOverPath("res://images/packed/sprite_fonts/saria_energy_icon.png");
// 之后 ResourceLoader.Load(该路径) 直接返回这张运行时贴图
```

配套：`ProbeCardPool / ProbeRelicPool / ProbePotionPool` 的 `EnergyColorName` 改成 `saria`，
这样只有我们的内容用这个前缀，**不会劫持原版角色**。

### 两个必须记住的坑

1. **一个 Resource 只能占住一个路径**：对同一张贴图连续 `TakeOverPath` 两次，第二次会覆盖第一次
   （先前表现为 `text=False packed=True`，正文图标变成缺失图）。→ 每条路径各建一张 `ImageTexture`。
2. **正文 `[img]` 按贴图原始像素渲染**：注册 148×148 会在正文里显示成巨图。
   → 正文那条路径要单独注册一张 **24×24**（原版 sprite_fonts 尺寸）的副本；
   卡面/提示那条保留原尺寸。

### 相关日志

```
[saria][energy-icon] registered: text=True packed=True size=148x148
```

`text` / `packed` 分别对应「正文内联」与「卡面/提示」两条虚拟路径是否可被 `ResourceLoader` 取到。

---
