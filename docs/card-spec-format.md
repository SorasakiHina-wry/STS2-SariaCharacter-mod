# 卡牌设计表格式说明

> **工作流状态标记（重要）**
> 每条的 `备注` 里请带一个状态标签：`已实装` / `待实装` / `待修改` / `待删除`。
> - `已实装`：代码已实现，我不再动它
> - `待实装`：我来实现
> - `待修改`：我来改（标签后面写清改什么）
> - `待删除`：我删代码，**然后把这条从 md 里一起删掉**
>
> 查看当前待办（不会打印已实装的内容）：
> ```powershell
> $env:PYTHONUTF8='1'
> D:\STS2\.python312\python.exe D:\STS2\mods-src\probe\tools\cardspec.py --todo
> ```

这份文档定义你写我读的卡牌格式。目标：**你写起来接近自然语言，我读起来能一对一翻译成代码**。

- 你要填的字段：**名称、稀有度、升级前费用与效果、升级后费用与效果**（这就是全部必填项）。
- 其余字段（类型 / 目标 / 英文名 / 备注）**都可以留空**，我会按规则推断；拿不准时我会回来问你。

实际填卡的地方：`mods-src/probe/design/cards.md`。

---

## 1. 单卡块（推荐，复杂卡用它）

每张卡一个 ```` ```card ```` 代码块，字段一行一个，冒号用中文或英文都行：

```card
名称: 探针突刺
稀有度: 普通
类型: 攻击
目标: 单体敌人
基础: 费用 1 | 造成 6 点伤害
升级: 费用 1 | 造成 9 点伤害
英文名: Probe Jab
备注:
```

只有 `名称 / 稀有度 / 基础 / 升级` 是必填。`类型`、`目标`、`英文名`、`备注` 可以整行删掉。

多条效果用 `|` 分隔（中文的 `｜` 我也认）：

```card
名称: 探针过载
稀有度: 稀有
基础: 费用 2 | 造成 14 点伤害
升级: 费用 2 | 造成 18 点伤害
```

---

## 2. 批量表格（简单卡用它，一次写一排）

| 名称 | 稀有度 | 基础 | 升级 | 类型 | 目标 | 英文名 |
|---|---|---|---|---|---|---|
| 探针突刺 | 普通 | 费用 1｜造成 6 点伤害 | 费用 1｜造成 9 点伤害 | 攻击 | 单体敌人 | Probe Jab |
| 探针守御 | 普通 | 费用 1｜获得 5 点格挡 | 费用 1｜获得 8 点格挡 | 技能 | 自己 | Probe Guard |

两种格式可以混用：卡池大的、效果复杂的用卡块；小卡用表格填得快。

## 2.5 游戏内文本 = 你写的原话

**规则：卡面描述就是你写的效果原文，我一字不改。**

- 不润色、不补 `[gold]` 富文本、不加句号、不换语序。
- 唯一会变的是**数字**：升级后数值不同，所以把数字替换成变量占位（`造成 {Damage:diff()} 点伤害`），渲染出来仍是你那句话和对应数值。
- 想改文案就直接改 `基础` / `升级` 里的效果文本，我同步。

同理，`名称`、`描述`、`备注` 里的文字我都不会改写或「优化」。

### 换行：用 `<br>`

原版卡面会在语义分段处断行，所以描述里需要一个换行标记：

```card
名称: 示例卡
稀有度: 普通
类型: 技能
目标: 自己
基础: 费用 1 | 获得 6 点格挡。<br>获得 2 层钙铠
升级: 费用 1 | 获得 9 点格挡。<br>获得 2 层钙铠
```

规则：

- 标记就是 **`<br>`**（大小写都可以，`<BR>` 等同）。
- 我在生成 C# 时把它替换成 `\n`，游戏里就是真正的换行。
- 可以连续用（`<br><br>` 表示空一行）。
- 也可以写在 `描述` 字段里（状态 / 遗物同样适用）。
- **不要**写 `\n`、`／`、`//`、`|` 之类的替代写法——`|` 在本表里另有含义（分隔多条效果）。

校验器会做两道检查，写错了会直接报出来：

| 检查 | 报错示例 |
|---|---|
| 数量一致 | `cards 第 81 行 [针剂弹药]: 设计表有 1 个 <br>，src\Cards\ProbeNewCards.cs 里只有 0 个 \n` |
| 基础/升级一致 | `cards 第 81 行 [针剂弹药]: 基础有 1 个 <br>，升级有 0 个（应一致）` |
| C# 里不许残留 | `ProbeNewCards.cs: 出现了 <br>，C# 里必须写成 \n` |

### 词条（消耗 / 固有 / 保留 / 虚无 …）

加一个 `词条` 字段（写 `关键词` 也认），用 `,`、`、`、`/` 分隔多个：

```card
名称: 示例卡
稀有度: 罕见
类型: 技能
目标: 自己
词条: 消耗, 固有
基础: 费用 0 | 抽 2 张牌。<br>获得 1 点能量
升级: 费用 0 | 抽 3 张牌。<br>获得 1 点能量
```

可用的词条（中文或英文都行）：

| 中文 | 英文 / 代码枚举 | 游戏内含义 |
|---|---|---|
| 消耗 | `Exhaust` | 打出后进入消耗堆 |
| 虚无 | `Ethereal` | 回合结束时若还在手牌则消耗 |
| 固有 | `Innate` | 开局必在手牌 |
| 不可打出 | `Unplayable` | 不能主动打出 |
| 保留 | `Retain` | 回合结束不弃掉 |
| 永恒 | `Eternal` | 无法被移除/消耗（原版同名词条） |
| Sly | `Sly` | STS2 新词条（中文译名待确认，先写英文） |

注意事项：

- **不要在 `基础` / `升级` 的文本里再写一遍「消耗」「保留」**——游戏会把这些词条自动加到牌面描述里，写了会重复。
- 词条是自动行为，不用在效果文本里描述它的规则。
- 我把它实现成 `CanonicalKeywords => new[] { CardKeyword.Exhaust, CardKeyword.Innate }`。
- 校验器会比对：设计表有词条但代码没写 → 报错；代码有词条但设计表没写 → 也报错。


---

## 3. 字段怎么写

| 字段 | 可选写法 | 说明 |
|---|---|---|
| `名称` | 任意中文 / 英文 | 我直接用作游戏内标题 |
| `稀有度` | 基础 / 普通 / 罕见 / 稀有；也接受 Basic / Common / Uncommon / Rare | 决定它在奖励和商店里出现的概率 |
| `类型` | 攻击 / 技能 / 能力；Attack / Skill / Power | **留空我推断**：造成伤害→攻击；格挡/抽牌/上状态→技能；持续生效→能力 |
| `目标` | 单体敌人 / 全体敌人 / 自己 / 随机敌人；或 None / Self / AnyEnemy / AllEnemies | **留空我推断**：伤害类默认单体敌人，其余默认自己 |
| `基础` | `费用 N ｜ 效果` | 费用写 `费用 0` 到 `费用 3`；多条效果用 `｜` 隔开 |
| `升级` | 同上 | 费用和效果都可以跟基础不同；**升级降费就直接写低费用** |
| `英文名` | 英文 | 留空则用中文名兜底 |
| `备注` | 自由文本 | 写特殊逻辑、你想强调的细节、或者「这里我不确定」 |

数字请用阿拉伯数字（`6` 不要写 `六`），我会按数字生成代码。

---

## 4. 效果句式（这部分决定我能不能一键翻译）

### A. 已经验证可用（写下去我直接出代码）

| 句式 | 例子 |
|---|---|
| 造成 N 点伤害 | `造成 6 点伤害` |
| 获得 N 点格挡 | `获得 5 点格挡` |
| 施加 N 层易伤 | `施加 2 层易伤` |
| 获得 N 点力量 | `获得 1 点力量` |
| 获得 N 点敏捷 | `获得 1 点敏捷` |

### B. 说法固定、我很快能补上（写就行，我会实现）

| 句式 | 说明 |
|---|---|
| 对全体敌人造成 N 点伤害 | 群体攻击 |
| 抽 N 张牌 | |
| 获得 N 点能量 | |
| 回复 N 点生命 / 失去 N 点生命 | |
| 施加 N 层虚弱 / 脆弱 / 中毒 / 荆棘 | 状态名用游戏内中文名即可，我查对照表 |
| 消耗 / 固有 / 保留 / 虚无 | 卡牌关键字 |
| 升级一张手牌 / 移除一张手牌 | 需要选牌交互，也能做 |

### C. 需要你多写一句（我来判断实现方式）

- 有条件的效果（例如「若你的格挡≥10，则……」）
- 计数类（「每打出 3 张技能牌……」）
- 生成/变形卡牌（「将一张 X 加入手牌」）
- 改变规则（「本场战斗中，你的攻击牌费用 -1」）

这些也能实现，但要靠 `备注` 里的补充说明，必要时我会回来跟你确认边界。

---

## 5. 卡池配比的硬性要求（实测踩坑得来，不是建议）

这几个数字是游戏代码写死的，卡池不满足会在**运行期抛异常**（奖励屏不出现 / 商店卡死）：

1. **稀有度**：Common、Uncommon、Rare 三档都必须有牌。
   奖励卡是掷稀有度再找对应档位的牌，某档没牌就抛 `couldn't generate a valid rarity`。
   另外**你牌组里已有的牌会被拉黑**，所以每档最好 ≥6 张，避免抽干。
2. **类型**：Attack、Skill、Power 三种都必须有非基础牌，且数量至少
   **攻击 2、技能 2、能力 1**。商店五个卡位写死是 `[攻击, 攻击, 技能, 技能, 能力]`，
   缺一种就抛 `Can't generate valid rarity for merchant card type ...`。
3. **总数**：建议 25~35 张，其中 10 张左右是普通档。

---

## 6. 我会怎么把它变成代码

拿上面那张「探针突刺」举例，我会生成：

```csharp
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeJab : CustomCardModel
{
    public ProbeJab() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(6m, ValueProp.Move),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
        => await ProbeCardKit.Hit(ctx, play, this, "vfx/vfx_attack_slash");

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);

    public override List<(string, string)>? Localization => new()
    {
        ("title", "探针突刺"),
        ("description", "造成 {Damage} 点伤害。"),
    };
}
```

也就是说：一个 `card` 块 → 一个 C# 类 + 两条本地化 + 卡池自动登记。你只写规格，剩下的我处理。

---

## 7. 提交前自查清单

- [ ] 每张卡都有 `名称 / 稀有度 / 基础 / 升级` 四行
- [ ] 升级后的数值确实和升级前不同（除非刻意「升级只改费用」）
- [ ] 稀有度和类型加起来满足第 5 节的配比
- [ ] 数字是阿拉伯数字
- [ ] 看不懂的地方写进了 `备注`，而不是留空

## 7.5 你可以自己先跑一遍检查

填完直接跑这个（不需要我）：

```powershell
$env:PYTHONUTF8='1'
D:\STS2\.python312\python.exe D:\STS2\mods-src\probe\tools\cardspec.py
```

它会告诉你：解析到几张卡、各稀有度/类型分布、以及所有填错的地方，例如：

```
设计表: D:\STS2\mods-src\probe\design\cards.md
解析到 23 张卡（停用 3 张）

稀有度: Basic=2  Common=6  Uncommon=6  Rare=6
类型  : Attack=10  Power=2  Skill=8

校验通过 ✅
```

出错时会长这样，一眼能看出哪一行有问题：

```
发现 11 个问题:
  第 2 行 [测试坏卡]: 稀有度「传说」无法识别（用 基础/普通/罕见/稀有）
  第 2 行 [测试坏卡]: 「基础」里没找到「费用 N」
  卡池: Rare 只有 0 张，至少需要 6 张（奖励抽干会抛异常）
```

校验规则就是第 5 节那几条，加上：必填字段、卡名重复、升级前后完全一样。

---

## 8. 几个约定

- **卡名重名**会被我全局检查，重名的我会回来问你改名。
- 你写中文名我就用中文；要英文版就在 `英文名` 里写，没写就先用中文。
- 想改已存在的卡，直接在 `design/cards.md` 里改那一条即可，不用告诉我「改哪张」——我按名称对齐。
- 想删卡就在那一行前面加 `# 停用 `，我读到就跳过。

---

# 第二部分：新增 buff 与卡牌专属机制

卡牌引用的「状态」和「机制」必须先定义、后引用。三份表的关系：

```
design/powers.md      ← 会显示在生物头顶的层数状态（buff / debuff）
design/mechanics.md   ← 卡牌专属机制 / 关键字（不一定有层数，可能只是一条规则）
design/cards.md       ← 卡牌，引用上面两表里定义的名字
```

我提交前会做交叉校验：卡牌里写了「施加 2 层过载」，但 `powers.md` 里没有「过载」→ 直接报错，不会默默生成一张会崩的卡。

## 9. buff / debuff 写在 `design/powers.md`

```power
名称: 蓄能
类型: buff
叠层: 计数
衰减: 触发后清除
触发: 回合结束时
效果: 若层数 ≥3，获得 1 点能量，然后清空层数
描述: 回合结束时，若层数达到 3，获得 1 点能量。
图标:
备注: 示例卡，可以删
```

| 字段 | 必填 | 写法 |
|---|---|---|
| `名称` | ✅ | 卡牌用「施加 N 层蓄能」引用它 |
| `类型` | ✅ | `buff`（增益，绿框）/ `debuff`（减益，红框） |
| `叠层` | ✅ | `计数`=可以叠多层；`单次`=不叠加，有就是 1 层 |
| `衰减` | | 留空=整场不消失；`回合结束 -1` / `触发后清除` / `战斗结束清除` |
| `触发` | | 什么时候生效（回合开始 / 打出攻击牌时 / 受到伤害时 …） |
| `效果` | ✅ | 用自然语言写清楚数值和条件，复杂就用多行 |
| `描述` | ✅ | **游戏里显示的文本**。层数用 `{Amount}`，N 倍层数用 `{N*Amount}`（见下） |
| `图标` | | 留空我借用原版图标（先跑通，后换图） |
| `备注` | | 特殊说明 |

### 9.1 描述里的变量写法（已用原版数据验证）

引擎只认 `{Amount}`（当前层数），SmartFormat 没有算术扩展，`{3*层数}` 这种写法会原样显示成文字。所以：

| 你在文档里写 | 含义 | 游戏内 |
|---|---|---|
| `{Amount}` | 当前层数 | 显示数字，随层数实时变化 |
| `{N*Amount}` | N 倍层数（如 `{3*Amount}`） | 我生成专用变量（如 `{BlockCost}`）实时计算，显示**算好的数字** |

**原版就是这么做的**（已从游戏 PCK 的 `localization/zhs/powers.json` 核对）：

```
CRIMSON_MANTLE_POWER.description     = 在你的回合开始时，失去生命并获得[gold]格挡[/gold]。
CRIMSON_MANTLE_POWER.smartDescription = 在你的回合开始时，失去[blue]{SelfDamage}[/blue]点生命并获得[blue]{Amount}[/blue]点[gold]格挡[/gold]。
```

- 266 个能力有 `smartDescription`，其中 227 个用 `[blue]{变量}[/blue]` 高亮数字；
- `{SelfDamage}` 就是自定义变量（`new DamageVar("SelfDamage", 0m, …)`），和我们的 `{BlockCost}` 同一套路。

所以你会看到我在代码里写两份：

```csharp
("description",      "在你的回合开始时，失去[gold]格挡[/gold]并获得[gold]钙铠[/gold]。"),           // 无数字（图鉴）
("smartDescription", "…失去[blue]{BlockCost}[/blue]层[gold]格挡[/gold]…获得[blue]{Amount}[/blue]点…"), // 战斗中
```

**你只管写文字和 `{Amount}` / `{N*Amount}`**，`[blue]` 数字高亮和 `[gold]` 关键字高亮我自动加。

> 卡牌不需要这套：原版卡面直接写 `{Damage:diff()}`，不加 `[blue]`。
> `{N*Amount}` 只支持「层数 × 常数」；加/减/取半之类的算式请写进 `备注`，我加专用变量。

### 9.2 同回合触发的先后顺序（踩过的坑）

引擎把「回合开始」拆成 **三个独立阶段**，每个阶段都会把所有监听者完整跑一遍：

```
AfterPlayerTurnStartEarly  →  AfterPlayerTurnStart  →  AfterPlayerTurnStartLate
```

所以**不要靠能力图标的左右顺序**来控制先后（那是不保证的）。要「A 先于 B 结算」，把 A 放早一档：

| 需求 | 做法 |
|---|---|
| 钙铠先给格挡，分神/凝神再结算 | 钙铠用 `AfterPlayerTurnStartEarly`，分神/凝神用 `AfterPlayerTurnStart` ✓ 已实装 |
| 需要「被其它能力结算完之后」再触发 | 用 `AfterPlayerTurnStartLate` |

判定「层数降为 0 是否移除」是 `PowerModel.ShouldRemoveDueToAmount()`，它不是 virtual：
- 默认：层数 ≤0 移除；
- `AllowNegative = true`：只在等于 0 时移除（力量/敏捷这类可负数的）。

所以「0 层也保留、且永不为负」需要额外打补丁，一般不建议——用上面的阶段拆分就够。

### 9.3 原版 power 范式：一个机制 = 一个 power（含内部计数器）

> 依据：反编译 + 中文语料核对原版「狱火 / INFERNO」的实现。

原版**不会**为一个机制的升级版单独建一个 power。狱火的基础版和升级版喂的是**同一个**
`InfernoPower`，图标上只显示一个总层数，另外用一个「不显示在图标上」的内部计数器区分细节。

#### 狱火是怎么做的

卡牌（`Inferno.cs`）：

```csharp
protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
{
    new PowerVar<InfernoPower>(6m),          // 施加 6 层（= 卡面写的 6 点伤害）
};

protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
{
    var power = await PowerCmd.Apply<InfernoPower>(
        ctx, Owner.Creature, DynamicVars["InfernoPower"].BaseValue, Owner.Creature, this);
    power?.IncrementSelfDamage();             // ★ 额外记一次「打出次数」
}

protected override void OnUpgrade() => DynamicVars["InfernoPower"].UpgradeValueBy(3m);
//                                         ↑ 升级只是多给 3 层（6→9），不新建 power
```

能力（`InfernoPower.cs`）：

```csharp
private const string _selfDamageKey = "SelfDamage";

protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
{
    new DamageVar("SelfDamage", 0m, ValueProp.Unblockable | ValueProp.Unpowered),  // ★ 内部计数器
};

// 卡牌调用它来累加，图标上的数字不变
public void IncrementSelfDamage()
{
    AssertMutable();
    DynamicVars["SelfDamage"].BaseValue++;
}
```

文本：

```
description      = 在你的回合开始时，失去[blue]1[/blue]点生命。…造成[blue]4[/blue]点伤害。
smartDescription = 在你的回合开始时，失去[blue]{SelfDamage}[/blue]点生命。…造成[blue]{Amount}[/blue]点伤害。
```

结论（实测：同时打出狱火 + 狱火+ → 图标显示 **15** = 6+9，`SelfDamage` = 2，回合开始失去 2 点生命）：

| 元素 | 职责 |
|---|---|
| `Amount`（图标上的数字） | **总层数**，基础版 + 升级版叠加在一起 |
| `DynamicVar` 内部计数器 | 记录「打出了几次」「其中几次是升级版」这类细节，**不显示在图标上** |
| `IncrementXxx()` 公开方法 | 卡牌施加完 power 后调用，用来累加内部计数器 |
| `description` / `smartDescription` | 静态文字（无数字）/ 战斗中文字（`{Amount}` + 内部计数器变量） |

#### 编写规范（以后所有 power 按这个来）

1. **一个机制只建一个 power 类**，不要 `XPower` / `XPlusPower` 并存。
2. 卡牌的 `PowerVar<XPower>` = **施加多少层**；升级只改层数（`UpgradeValueBy`）或改费用。
3. 需要「升级版行为不同」时，**用内部计数器**表达，而不是新建能力：
   - 在 power 里声明 `new IntVar("XxxCount", 0m)` 之类的 `DynamicVar`（名字自己定）；
   - 提供 `public void IncrementXxx()`（内部先 `AssertMutable()`）供卡牌调用；
   - 结算时用 `Amount` 和这些计数器一起算出结果。
4. 图标数字**永远只表示层数**；任何「次数/比例/额外状态」都放内部计数器。
5. 文本写两份：`description`（不含数字）+ `smartDescription`（`[blue]{Amount}[/blue]`、`[blue]{内部计数器}[/blue]`）。
   卡牌文本用 `{Power:diff()}` 显示「本次施加多少层」。
6. 结算顺序靠钩子阶段控制（见 9.2），不要靠图标顺序。

#### 按此规范重写「分神」的方案（已实装，留作示例）

原方案：`分神` / `分神+` 两个 power 类。已改成单类：

```
power: 分神
  Amount     = 回合开始时要失去的格挡总量（图标显示；分神每次 +3，分神+ 每次 +2）
  ArmorGain  = 打出次数（内部计数器，每次打出 +1，不显示在图标上）
  回合开始：失去 min(Amount, 当前格挡) 层格挡，获得 ArmorGain 层钙铠
```

对应：

| 位置 | 写法 |
|---|---|
| `powers.md` 效果 | 失去与层数相等的格挡，获得"分神""分神+"打出次数的钙铠。 |
| `powers.md` 描述 | 回合开始时，失去{Amount}层格挡，获得{ArmorGain}点钙铠。 |
| 卡牌 | 基础版施加 3 层；升级版施加 2 层；两者都调用 `IncrementArmorGain()` |

### 9.4 多变量能力的写法（含命名建议）

#### 结论先行

原版能力牌的数值结构（扫描全部 117 张能力牌）：

| 升级改动几个数值 | 张数 |
|---|---|
| 0 个 | 50 |
| 1 个 | 65 |
| 2 个 | 2（`Prowess` 非凡技艺、`BulkUp`） |

那 2 张**不是反例**：它们只是直接施加原版的[gold]力量[/gold]与[gold]敏捷[/gold]（升级各 +1），
**没有为它们注册专用 buff**，所以「一个专用能力对应多个会随升级变动的数值」这种情况原版没有。

所以本项目约定：

**一个 power 的描述里，最多 1 个 `{Amount}`，其余数值一律用 `{DynamicVar}`，并在 `备注` 里写清每个变量的变化逻辑。**

#### 命名建议

不要写 `{DynamicVar1}` / `{DynamicVar2}`——我需要靠备注去猜语义，容易错位。按**含义**命名：

| 建议名 | 用在哪 |
|---|---|
| `{BlockCost}` | 要消耗/失去的格挡量 |
| `{ArmorGain}` | 获得/失去的钙铠层数 |
| `{VigorGain}` | 获得的活力 |
| `{TriggerCount}` | 还会触发几次 / 已触发几次 |
| `{HpThreshold}` | 生命比例阈值之类 |

推荐写法：

```
描述：回合开始时，失去{BlockCost}层格挡，获得{ArmorGain}层钙铠。
备注：BlockCost = 每层 3（"分神+" 为 2）；ArmorGain = 打出次数，每次 +1；图标显示 Amount（不用）
```

#### 图标显示约定

**不特别说明时，buff 图标上的数字就是 `{Amount}`**，其他变量只在悬浮详细描述里出现。
如果某个能力想让图标显示别的值（少见），在 `备注` 里写明「图标显示 X」。

#### 我会把它实现成什么

| 文档里的写法 | 代码里 |
|---|---|
| `{Amount}` | 引擎自带，无需声明 |
| `{BlockCost}` / `{ArmorGain}` 等 | `CanonicalVars` 里声明对应 `DynamicVar`（`IntVar` / `PowerVar` / `DamageVar`…） |
| 「每次打出 +1」 | 卡牌施加后调用 `power.IncrementXxx()`，内部 `DynamicVars["Xxx"].BaseValue++` |
| 「N 倍层数」 | 派生变量在渲染时计算（`MultipliedAmountVar`） |

#### 三条硬约束

1. 静态 `description`（图鉴用）**不要出现数字**——那里不注入 DynamicVar，会原样显示。
2. 只有 `smartDescription` 能拿到 `DynamicVar`；`{Amount}` 两边都能用。
3. 变量是**每个能力实例一份**，可以随时 `BaseValue++`，所以「打出次数」这类计数天然可表达。

这样图标只有一个「分神」，基础版和升级版叠加在同一层数里，和狱火一致。

## 10. 卡牌专属机制 / 关键字写在 `design/mechanics.md`

```mechanic
名称: 回响
范围: 关键字
一句话: 每当你打出一张费用为 0 的牌，抽 1 张牌。
时机: 打出卡牌后
细节: 每回合最多触发 2 次。
涉及: 探针火花, 探针过载
备注: 示例机制，可以删
```

| 字段 | 必填 | 写法 |
|---|---|---|
| `名称` | ✅ | 卡牌文本里提到它就算引用 |
| `范围` | ✅ | `关键字`（多张卡共用）/ `单卡`（只有一张卡用的独特机制） |
| `一句话` | ✅ | 机制的规则总结 |
| `时机` | | 触发时机 |
| `细节` | | 边界条件、上限、是否叠加、多张同名卡怎么处理 |
| `涉及` | | 用到的卡名，逗号分隔；留空我按卡牌文本自动找 |
| `备注` | | |

### 该写在哪张表？

| 你想要的效果 | 写在哪 |
|---|---|
| 怪物/玩家头顶显示层数（像易伤、力量） | `powers.md` |
| 一条行为规则，不显示层数（像「每回合第一张攻击牌 +2 伤害」） | `mechanics.md` |
| 既是层数又有复杂规则 | 两边都写，在 `mechanics.md` 的 `细节` 里注明「由 powers.md 的 X 实现」 |

## 11. 卡牌里怎么引用

直接在效果文本里写名字，不用加特殊语法：

```card
名称: 探针过载
稀有度: 稀有
基础: 费用 1 | 施加 2 层蓄能
升级: 费用 1 | 施加 3 层蓄能
```

```card
名称: 探针火花
稀有度: 普通
基础: 费用 0 | 造成 3 点伤害
升级: 费用 0 | 造成 5 点伤害
备注: 吃「回响」机制
```

`备注` 里点一下机制名也有用——我会把你提过的机制和卡牌对上，方便检查覆盖度。

## 12. 三表全查

填完任意一份都可以跑（它会自动读同目录下的另外两份）：

```powershell
$env:PYTHONUTF8='1'
D:\STS2\.python312\python.exe D:\STS2\mods-src\probe\tools\cardspec.py
```

新增的校验：

- 卡牌里「施加 N 层X」的 `X` 必须能在 `powers.md` 找到，或者是原版状态（易伤/虚弱/脆弱/中毒/荆棘/力量/敏捷）
- `powers.md` / `mechanics.md` 内的重名
- 定义了但没有任何卡牌引用的状态/机制 → 只提示，不报错（可能留给遗物用）

---

# 第三部分：遗物

遗物有单独的格式文档：[`relic-spec-format.md`](relic-spec-format.md)，工作表是 `design/relics.md`。
上面的校验命令会**同时检查四份表**（cards / powers / mechanics / relics）。
