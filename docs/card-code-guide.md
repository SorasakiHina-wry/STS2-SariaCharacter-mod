# 卡牌代码速查 —— 自己改数值用

这份文档的目标：**只调数值时，你不用找我。**
读完你应该能独立完成「把打击的伤害从 6 改成 8」这类改动，并把它装进游戏。

---

## 0. 三条命令完成一次改动

```powershell
# 1) 改代码（见第 3 节的配方）
# 2) 编译 + 部署（会自动关掉游戏，不自动启动）
powershell -ExecutionPolicy Bypass -File D:\STS2\saria\mods-src\saria\tools\apply.ps1

# 3) 想直接开一局验证
powershell -ExecutionPolicy Bypass -File D:\STS2\saria\mods-src\saria\tools\apply.ps1 -Launch
```

`apply.ps1` 干这些事：`编译 → 关闭正在运行的游戏（DLL 被占用）→ 复制到游戏 mods 目录`。

> 只想确认代码能编译、不动游戏：加 `-NoKill`（此时如果游戏开着，部署会失败，但编译结果能看）。

---

## 1. 文件都在哪

```
D:\STS2\saria\mods-src\saria\
  src\Cards\                每张卡一个类（改数值主要在这里）
      ProbeStrike.cs        打击
      ProbeDefend.cs        防御
      ProbeCalciumCards.cs  硬化、激发
      ProbeNewCards.cs      稳定、爆裂、全弹发射、充能、注射、紧密防护 …
      ProbeBatch2Cards.cs   针剂弹药、部署、粉末化、拳击、持盾打击、分神、凝神 …
      ProbeFillerCards.cs   稀有牌（Probe 系列）+ 卡牌公共工具 ProbeCardKit
  src\Powers\               自定义状态（钙铠、分神、凝神…）
  src\Relics\               遗物
  design\cards.md           ★ 卡牌设计表（你写需求用的表，也应同步数值）
  design\powers.md          状态设计表
  tools\apply.ps1           一键编译+部署
  tools\cardspec.py         设计表校验（可选）
```

游戏安装后 mod 的文件在：`D:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\saria\`

---

## 2. 一张卡的结构（以「打击」为例）

```csharp
[Pool(typeof(ProbeCardPool))]                    // ① 必须有：声明它属于哪个卡池
public sealed class ProbeStrike : CustomCardModel // ② 类名 = 存档里的 ID（大写），别随便改
{
    public ProbeStrike()
        : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
        //     ↑费用  ↑类型          ↑稀有度            ↑目标
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(6m, ValueProp.Move),        // ③ 卡面数字：这里的 6 = 伤害
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)   // ④ 实际行为：用上面那个数值打伤害
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);    // ⑤ 升级加成：+3 → 升级后 9
    }

    public override string? CustomPortraitPath => "res://images/packed/card_portraits/ironclad/strike_ironclad.png";

    public override List<(string, string)>? Localization => new()
    {
        ("title", "打击"),
        ("description", "造成{Damage:diff()}点伤害。"),   // ⑥ 卡面文字：{Damage:diff()} 会自动变成 6/9
    };
}
```

四条记忆点：

| 编号 | 是什么 | 改它会怎样 |
|---|---|---|
| ② | `base(费用, 类型, 稀有度, 目标)` | 改费用/类型/稀有度/打谁 |
| ③ | `CanonicalVars` 里的数字 | 改基础数值 |
| ⑤ | `OnUpgrade` 里的加成 | 改升级后的数值 |
| ⑥ | `Localization` 的文字 | 改卡面描述（**数字用 `{变量:diff()}` 占位**） |

---

## 3. 常见改动配方

### 3.1 改伤害 / 格挡（最常见）

找到 `new DamageVar(6m, ...)`（伤害）或 `new BlockVar(5m, ...)`（格挡），改数字即可。

```csharp
// 打击：基础 6 伤害，升级 +3（=9）
new DamageVar(6m, ValueProp.Move),
DynamicVars.Damage.UpgradeValueBy(3m);

// 想改成 基础 8、升级 +4（=12）：
new DamageVar(8m, ValueProp.Move),
DynamicVars.Damage.UpgradeValueBy(4m);
```

> 数字后面的 `m` 是 C# 的 decimal 后缀。写不写都能编译（`6` 会隐式转成 decimal），
> 但项目里统一写 `m`，照抄就行。

### 3.2 改费用

```csharp
public ProbeStrike() : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy) { }
//                           ↑ 这个数字就是费用
```

X 费卡（全弹发射）写 `base(0, ...)` 并另加 `protected override bool HasEnergyCostX => true;`。

### 3.3 改升级效果

| 想要的效果 | 写法 |
|---|---|
| 数值 +N | `DynamicVars.Damage.UpgradeValueBy(3m);` |
| 费用 -1 | `EnergyCost.UpgradeBy(-1);` |
| 多个数值一起变 | 在 `OnUpgrade()` 里写多行 |
| 增加攻击段数 | 不改 `OnUpgrade`，改成 `IsUpgraded ? 3 : 2` 三目（见「拳击」） |

### 3.4 改稀有度 / 类型 / 目标

```csharp
base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
//        CardType   : Attack 攻击 / Skill 技能 / Power 能力
//        CardRarity : Basic 基础 / Common 普通 / Uncommon 罕见 / Rare 稀有
//        TargetType : AnyEnemy 单体敌人 / AllEnemies 全体 / Self 自己 / RandomEnemy 随机
```

⚠️ 稀有度分布别太偏：奖励卡按稀有度抽，某一档太少会在抽干时出问题（校验脚本会提示）。

### 3.5 改词条（消耗 / 固有 / 保留 / 虚无 …）

在类里加一行：

```csharp
public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };
//                                                                                   ↑ 消耗
```

可用：`Exhaust` 消耗、`Ethereal` 虚无、`Innate` 固有、`Unplayable` 不可打出、`Retain` 保留、`Eternal` 永恒。

### 3.6 改卡面文字 / 换行

```csharp
("description", "造成{Damage:diff()}点伤害。")                       // 单行
("description", "获得{Block:diff()}点格挡。\n获得4层钙铠。")          // \n = 换行
```

规则（和游戏原版一致）：

- 中文里**不要留空格**：`造成6点伤害` 而不是 `造成 6 点伤害`；
- 结尾**要有句号**；
- 数字一律用 `{变量:diff()}`，别写死——否则升级后卡面显示的还是旧数字。

| 变量写法 | 对应数字 |
|---|---|
| `{Damage:diff()}` | `DamageVar` |
| `{Block:diff()}` | `BlockVar` |
| `{VulnerablePower:diff()}` | `PowerVar<VulnerablePower>` |
| `{CalciumArmorPower:diff()}` | `PowerVar<CalciumArmorPower>` |
| `{Cards:diff()}` | `CardsVar` |
| `{Repeat:diff()}` | `RepeatVar` |

### 3.7 加一张全新的卡（最小模板）

1. 复制 `ProbeStrike.cs` → 改名成新文件（例如 `ProbeMyCard.cs`）；
2. 改类名 `ProbeMyCard`（**类名就是存档 ID，全大写** `PROBE_MY_CARD`，不要和别的卡重复）；
3. 改 `base(...)`、`CanonicalVars`、`OnUpgrade`、`Localization`；
4. `apply.ps1` 重新部署。

卡池是自动收录的（`[Pool(typeof(ProbeCardPool))]` 就够），不需要手写注册表。

---

## 4. 为什么卡面写的是 `{Damage:diff()}`

卡面文字和数值是**两套东西**：

- 数值放在 `CanonicalVars`（游戏逻辑用）；
- 卡面文字里用 `{变量:diff()}` 占位，游戏渲染时替换成当前值；
- `:diff()` 的作用是升级后**高亮显示变动的数字**（原版就是这样）。

所以你只要改 `CanonicalVars` / `OnUpgrade`，**卡面会自动跟着变**，不用手改描述里的数字。

---

## 5. 改完之后怎么生效

```powershell
# 编译 + 部署（自动关游戏）
powershell -ExecutionPolicy Bypass -File D:\STS2\saria\mods-src\saria\tools\apply.ps1

# 或者一步到位：编译 + 部署 + 启动
powershell -ExecutionPolicy Bypass -File D:\STS2\saria\mods-src\saria\tools\apply.ps1 -Launch
```

- 游戏**必须重启**才能加载新的 `saria.dll`（mod 只在启动时加载）；
- `apply.ps1` 会自动处理「游戏占用 DLL」的问题。

### 出问题看哪里

| 现象 | 看哪里 |
|---|---|
| 编译报错 | `apply.ps1` 的输出（红字，带文件:行号） |
| 游戏启动后没变化 | 游戏日志 `%APPDATA%\SlayTheSpire2\logs\godot.log`，搜 `[saria]` |
| 卡面文字显示成 `{Damage:diff()}` 原文 | 描述里变量名写错，或该卡没有对应 `DynamicVar` |
| 卡面数字不随升级变化 | `OnUpgrade()` 里没写对应加成 |

---

## 6. 不要碰的部分（改了容易炸）

| 路径 | 为什么 |
|---|---|
| `src\Powers\*.cs` 的钩子方法（`AfterPlayerTurnStart` 等） | 涉及结算时机，顺序错了会出现「先扣格挡再给格挡」这类 bug |
| `src\Patches\*.cs` | Harmony 补丁，改错会导致游戏报错或行为异常 |
| `src\Pools\*.cs` | 卡池/遗物池注册 |
| 类名（`public sealed class X`） | 类名就是存档 ID，改了旧存档里的卡会丢失 |
| `[Pool(...)]` 那一行 | 删了会在加载时抛异常 |

改数值（`CanonicalVars` / `OnUpgrade` / `base` 里的数字 / 描述文字）是安全的。

---

## 7. 设计表要不要同步

`design\cards.md` 是你写需求的表，也是我做批量校验的依据。**只改代码不改表**不会立刻出问题，
但校验脚本会提示文本不一致（数字不比对，文案会比对）。

如果只想快速试数值，可以先只改代码；等数值定下来，再回头把表里的数字改一致。

校验命令（可选，需要 Python）：

```powershell
$env:PYTHONUTF8='1'
D:\STS2\.python312\python.exe D:\STS2\saria\mods-src\saria\tools\cardspec.py
```

它检查：文字是否和代码一致、换行数量、词条、卡池稀有度分布、待办标记。

---

## 8. 一页速查

```powershell
# 编译 + 部署（不启动）
powershell -ExecutionPolicy Bypass -File D:\STS2\saria\mods-src\saria\tools\apply.ps1

# 编译 + 部署 + 启动
powershell -ExecutionPolicy Bypass -File D:\STS2\saria\mods-src\saria\tools\apply.ps1 -Launch

# 只编译（不动游戏）
powershell -ExecutionPolicy Bypass -File D:\STS2\saria\mods-src\saria\tools\build.ps1
```

| 想改 | 改哪里 |
|---|---|
| 伤害 / 格挡数值 | `new DamageVar(6m, …)` / `new BlockVar(5m, …)` 的数字 |
| 升级加成 | `OnUpgrade()` 里的 `UpgradeValueBy(...)` |
| 费用 | `base(1, ...)` 的第一个参数 |
| 稀有度 / 类型 / 目标 | `base(...)` 的后三个参数 |
| 卡面文字 | `("description", "...")` |
| 换行 | 描述里写 `\n` |
| 词条 | 加 `CanonicalKeywords` |
| 新增卡 | 复制一张同类卡，改类名和数值 |

改完记得：**`apply.ps1` → 重启游戏 → 开一局看卡面数字**。
