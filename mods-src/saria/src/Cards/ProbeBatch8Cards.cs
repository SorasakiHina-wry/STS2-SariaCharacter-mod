using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using Saria.Pools;
using Saria.Powers;

namespace Saria.Cards;

/// <summary>弱化 — 伤害 + 两层虚弱。</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeWeaken : SariaCardModel
{
    public ProbeWeaken() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(7m, ValueProp.Move),
        new PowerVar<WeakPower>(2m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target, "cardPlay.Target");
        await ProbeCardKit.SpendArmor(ctx, this, 1);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, play)
            .Targeting(play.Target)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(ctx);

        await PowerCmd.Apply<WeakPower>(ctx, play.Target, DynamicVars.Weak.BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars.Weak.UpgradeValueBy(1m);
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<WeakPower>(),
    };
    public override List<(string, string)>? Localization => new()
    {
        ("title", "弱化"),
        ("description", "失去1层[gold]钙铠[/gold]。\n造成{Damage:diff()}点伤害。\n给予{WeakPower:diff()}层[gold]虚弱[/gold]。"),
    };
}

/// <summary>限位 — 全体伤害，并给每个敌人 1 层限位（下一次获得格挡被阻止）。</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeLimit : SariaCardModel
{
    public ProbeLimit() : base(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(7m, ValueProp.Move),
        new PowerVar<ProbeLimitPower>(1m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, play)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(ctx);

        foreach (var enemy in CombatState.HittableEnemies)
        {
            await PowerCmd.Apply<ProbeLimitPower>(ctx, enemy, DynamicVars["ProbeLimitPower"].BaseValue, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<ProbeLimitPower>(),
    };
    public override List<(string, string)>? Localization => new()
    {
        ("title", "限位"),
        ("description", "对所有敌人造成{Damage:diff()}点伤害。\n阻止所有敌人下一次获得[gold]格挡[/gold]。"),
    };
}

/// <summary>策略归纳 — 命中次数随斩杀永久成长（整局跨战斗保留）。</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeStrategySummary : SariaCardModel
{
    private int _extraHits;

    /// <summary>斩杀累计的额外命中次数；[SavedProperty] 让它跟着本局存档一起保留。</summary>
    [SavedProperty]
    public int ExtraHits
    {
        get => _extraHits;
        set
        {
            AssertMutable();
            _extraHits = value;

            // 卡面「造成N点伤害X次」要跟着变；照原版「巨镰」的做法直接同步 DynamicVar，
            // 不要用 CalculatedVar —— 它在牌库预览里没有目标，会显示成 0。
            if (DynamicVars != null && DynamicVars.ContainsKey("Repeat"))
            {
                DynamicVars.Repeat.BaseValue = 1 + value;
                DynamicVars.Repeat.PreviewValue = 1 + value;
            }
        }
    }

    public ProbeStrategySummary() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(8m, ValueProp.Move),
        new RepeatVar(1),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target, "cardPlay.Target");

        // 斩杀判定照原版「狂宴」：目标没有禁止 fatal 的 power，且这一击把它打死
        bool shouldTriggerFatal = play.Target.Powers.All(p => p.ShouldOwnerDeathTriggerFatal());
        int hits = Math.Max(1, (int)DynamicVars.Repeat.BaseValue);

        var attack = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, play)
            .Targeting(play.Target)
            .WithHitCount(hits)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(ctx);

        if (shouldTriggerFatal && attack.Results.SelectMany(r => r).Any(r => r.WasTargetKilled))
        {
            // 战斗内的临时副本 + 牌组里的持久副本都要成长（照原版「巨镰」的 DeckVersion 写法）
            Grow();
            (DeckVersion as ProbeStrategySummary)?.Grow();
        }
    }

    private void Grow() => ExtraHits += 1;

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.Static(StaticHoverTip.Fatal),
    };
    public override List<(string, string)>? Localization => new()
    {
        ("title", "策略归纳"),
        ("description", "造成{Damage:diff()}点伤害。\n[gold]斩杀[/gold]时，这张牌在本局游戏中永久性额外造成一次伤害。\n（命中{Repeat:diff()}次）。"),
    };
}

/// <summary>便携刻印 — 一次性的大笔钙铠。</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbePortableSeal : SariaCardModel
{
    public ProbePortableSeal() : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<CalciumArmorPower>(18m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<CalciumArmorPower>(ctx, Owner.Creature, DynamicVars["CalciumArmorPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["CalciumArmorPower"].UpgradeValueBy(6m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
    };
    public override List<(string, string)>? Localization => new()
    {
        ("title", "便携刻印"),
        ("description", "获得{CalciumArmorPower:diff()}层[gold]钙铠[/gold]。"),
    };
}

/// <summary>迅捷 — 常驻：回合开始扣钙铠，从抽牌堆随机几张里选牌。</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeSwift : SariaCardModel
{
    public ProbeSwift() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<ProbeSwiftPower>(1m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<ProbeSwiftPower>(ctx, Owner.Creature, DynamicVars["ProbeSwiftPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
    };
    public override List<(string, string)>? Localization => new()
    {
        ("title", "迅捷"),
        ("description", "在你的回合开始时，失去1层[gold]钙铠[/gold]并从[gold]抽牌堆[/gold]的随机3张牌中选择1张加入你的[gold]手牌[/gold]。"),
    };
}

/// <summary>覆还 — 本回合失去的钙铠，在下回合开始时还回来。</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeReturn : SariaCardModel
{
    public ProbeReturn() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override bool HasEnergyCostX => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        int x = ResolveEnergyXValue();
        if (x <= 0)
            return;

        // 本回合**已经**失去过的钙铠层数 × X，直接转成「下回合钙铠」
        var armour = Owner.Creature.GetPower<CalciumArmorPower>();
        int lost = armour?.LostThisTurn ?? 0;
        int carry = lost * x;
        if (carry <= 0)
            return;

        await PowerCmd.Apply<ProbeNextTurnArmorPower>(ctx, Owner.Creature, carry, Owner.Creature, this);
    }

    protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Exhaust);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
        HoverTipFactory.FromPower<ProbeNextTurnArmorPower>(),
    };

    /// <summary>
    /// 描述末尾的「（下回合获得 n 层钙铠）」：n = 本回合已失去层数 × 当前能量。
    /// 只在战斗中追加，非战斗时给空字符串（描述里那行就变成空白）。
    /// </summary>
    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);

        string note = string.Empty;
        if (CombatManager.Instance.IsInProgress && Owner != null)
        {
            var armour = Owner.Creature.GetPower<CalciumArmorPower>();
            int lost = armour?.LostThisTurn ?? 0;

            // 预览用的 X：当前能量再走一次游戏的 ModifyXValue 钩子，
            // 这样「化合物X」之类修改 X 的效果也会体现在提示里（和真实结算同一套算法）。
            int baseX = Owner.PlayerCombatState?.Energy ?? 0;
            int x = CombatState != null ? Hook.ModifyXValue(CombatState, this, baseX) : baseX;

            if (lost > 0 && x > 0)
                note = $"\n（下回合获得[blue]{lost * x}[/blue]层钙铠）";
        }

        description.Add("ReturnNote", note);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "覆还"),
        ("description", "在本回合，你每失去过1层[gold]钙铠[/gold]，就在下回合开始时获得X层[gold]钙铠[/gold]。{ReturnNote}"),
    };
}
