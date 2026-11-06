# 遗物设计表格式说明

> **工作流状态标记**：每条 `备注` 里带 `已实装` / `待实装` / `待修改` / `待删除` 之一（含义同卡牌表）。
> 待删除的条目，我删完代码会把这一条从 md 里一并删掉。

和卡牌表同一套思路：**你写自然语言规格，我翻译成代码**。

- 必填：**名称、稀有度、效果**（三项）。
- 其余（触发、描述、图标、备注）都可以留空，我会推断或用默认值。

实际填写的地方：`mods-src/probe/design/relics.md`。

---

## 1. 单遗物块

```relic
名称: 探针核心
稀有度: 初始
触发: 战斗胜利时
效果: 回复 6 点生命
描述:
图标: burning_blood
备注:
```

多条效果用 `|` 分隔：

```relic
名称: 递归引擎
稀有度: 稀有
触发: 进入战斗时
效果: 获得 2 点力量 | 获得 2 点敏捷
图标: bag_of_marbles
```

---

## 2. 字段怎么写

| 字段 | 必填 | 写法 |
|---|---|---|
| `名称` | ✅ | 游戏内显示名，中英文都行 |
| `稀有度` | ✅ | `初始` / `普通` / `罕见` / `稀有` / `商店` / `事件` / `远古`（也认 Starter/Common/Uncommon/Rare/Shop/Event/Ancient） |
| `效果` | ✅ | 用第 3 节的句式；多条用 `｜` 分隔 |
| `触发` | | 什么时候生效。留空我会按效果句式推断 |
| `描述` | | 留空就直接用 `效果` 的文本当卡面描述 |
| `图标` | | 填一个**原版遗物的英文名**（如 `anchor`、`vajra`、`lantern`），我复用它的图标；留空用占位图 |
| `备注` | | 特殊说明、边界条件、以及「这里我不确定」 |

---

## 3. 效果句式

### A. 已验证可用（这几类我直接出代码）

| 句式 | 例子 | 实现钩子 |
|---|---|---|
| 战斗开始时获得 N 点格挡 | `战斗开始时获得 8 点格挡` | `BeforeCombatStart` |
| 战斗胜利后回复 N 点生命 | `战斗胜利后回复 6 点生命` | `AfterCombatVictory` |
| 战斗开始时获得 N 点力量 | `战斗开始时获得 1 点力量` | `AfterRoomEntered(CombatRoom)` + `PowerCmd.Apply` |
| 战斗开始时获得 N 点敏捷 | 同上 | 同上 |

### B. 说法固定、我能快速补上

| 句式 | 说明 |
|---|---|
| 每回合开始时获得 N 点格挡 | 走 `AfterPlayerTurnStart` |
| 每回合开始时抽 N 张牌 | 同上 |
| 进入房间时回复 N 点生命 | 走 `AfterRoomEntered` |
| 每当你打出攻击牌时，…… | 走 `AfterCardPlayed` 系列 |
| 每当你受到伤害时，…… | 走伤害后钩子 |
| 获得 N 层 [自定义状态] | 会在 `powers.md` 里查名字，没定义会报错 |

### C. 需要你多写一句（我来判断实现）

- 「第一次战斗」这类一次性效果
- 改变费用/抽牌规则
- 与地图、商店、事件联动的效果
- 获得能量（能量类遗物很强，我需要你确认数值）

---

## 4. 稀有度与配比

| 稀有度 | 说明 |
|---|---|
| `初始` | 起手遗物。**一个角色正好 1 个**，多写我会问你要哪个 |
| `普通/罕见/稀有` | 主要来源。建议**每档至少 3 个**——奖励遗物是按档位随机抽的，某档抽干会出现和卡牌一样的问题 |
| `商店` | 只在商店出现 |
| `事件` | 只在事件里给 |
| `远古` | 先王/远古事件池，一般不用 |

当前 6 个遗物的分布：初始 1 / 普通 2 / 罕见 2 / 稀有 1 —— **普通和稀有都偏少，建议补到每档 3 个**。

---

## 5. 图标怎么借

写原版遗物的英文名即可（snake_case）。常用的：

```
burning_blood  anchor  vajra  lantern
bag_of_marbles  oddly_smooth_stone  blood_vial  maw_bank
```

写错了我会告诉你「这个图标找不到」，不会静默失败。以后有原创画，我再改成自己的图。

---

## 6. 我会怎么把它变成代码

上面「递归引擎」会生成：

```csharp
[Pool(typeof(ProbeRelicPool))]
public sealed class RecursionEngine : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Rare;
    protected override string IconBaseName => "bag_of_marbles";

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<StrengthPower>(2m),
        new PowerVar<DexterityPower>(2m),
    };

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not CombatRoom) return;
        Flash();
        var ctx = new ThrowingPlayerChoiceContext();
        await PowerCmd.Apply<StrengthPower>(ctx, Owner.Creature, 2m, Owner.Creature, null);
        await PowerCmd.Apply<DexterityPower>(ctx, Owner.Creature, 2m, Owner.Creature, null);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "递归引擎"),
        ("description", "战斗开始时，获得 2 点力量与 2 点敏捷。"),
        ("flavor", "……"),
    };
}
```

---

## 7. 提交前自查

- [ ] 每行都有 `名称 / 稀有度 / 效果`
- [ ] `初始` 遗物只有一个
- [ ] 稀有度分布别太偏（普通/罕见/稀有 各 ≥3 最好）
- [ ] 数字用阿拉伯数字
- [ ] 用了自定义状态名的话，`powers.md` 里已经定义过

## 8. 自己跑一遍校验

```powershell
$env:PYTHONUTF8='1'
D:\STS2\.python312\python.exe D:\STS2\mods-src\probe\tools\cardspec.py
```

它会一起检查卡牌、状态、机制、遗物四份表，并做交叉校验（比如遗物里写了「获得 2 层钙铠」但 `powers.md` 没定义 → 报错）。
