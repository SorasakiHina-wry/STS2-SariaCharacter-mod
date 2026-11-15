using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using Probe.Pools;
using Probe.Powers;

namespace Probe.Cards;

/// <summary>弱化 — 伤害 + 两层虚弱。</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeWeaken : CustomCardModel
{
    public ProbeWeaken() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(8m, ValueProp.Move),
        new PowerVar<WeakPower>(2m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, play)
            .Targeting(play.Target)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(ctx);

        await PowerCmd.Apply<WeakPower>(ctx, play.Target, DynamicVars.Weak.BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<WeakPower>(),
    };

    public override string? CustomPortraitPath => ProbeCardKit.BashPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "弱化"),
        ("description", "造成{Damage:diff()}点伤害。\n给予{WeakPower:diff()}层[gold]虚弱[/gold]。"),
    };
}

/// <summary>限位 — 全体伤害，并给每个敌人 1 层限位（下一次获得格挡被阻止）。</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeLimit : CustomCardModel
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

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(5m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<ProbeLimitPower>(),
    };

    public override string? CustomPortraitPath => ProbeCardKit.BashPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "限位"),
        ("description", "对所有敌人造成{Damage:diff()}点伤害。\n阻止所有敌人下一次获得[gold]格挡[/gold]。"),
    };
}

/// <summary>策略归纳 — 命中次数随斩杀永久成长（整局跨战斗保留）。</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeStrategySummary : CustomCardModel
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

    public ProbeStrategySummary() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }

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
            ExtraHits += 1;
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.Static(StaticHoverTip.Fatal),
    };

    public override string? CustomPortraitPath => ProbeCardKit.BludgeonPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "策略归纳"),
        ("description", "造成{Damage:diff()}点伤害{Repeat:diff()}次。\n斩杀时，这张牌在本局游戏中永久性额外造成一次伤害。"),
    };
}

/// <summary>便携刻印 — 一次性的大笔钙铠。</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbePortableSeal : CustomCardModel
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

    public override string? CustomPortraitPath => ProbeCardKit.DefendPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "便携刻印"),
        ("description", "获得{CalciumArmorPower:diff()}层[gold]钙铠[/gold]。"),
    };
}

/// <summary>迅捷 — 常驻：回合开始扣钙铠，从抽牌堆随机几张里选牌。</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeSwift : CustomCardModel
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

    public override string? CustomPortraitPath => ProbeCardKit.InflamePortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "迅捷"),
        ("description", "在你的回合开始时，失去1层[gold]钙铠[/gold]并从[gold]抽牌堆[/gold]的随机3张牌中选择1张加入你的[gold]手牌[/gold]。"),
    };
}

/// <summary>覆还 — 本回合失去的钙铠，在下回合开始时还回来。</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeReturn : CustomCardModel
{
    public ProbeReturn() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<ProbeReturnPower>(1m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PowerCmd.Apply<ProbeReturnPower>(ctx, Owner.Creature, DynamicVars["ProbeReturnPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
        HoverTipFactory.FromPower<ProbeNextTurnArmorPower>(),
    };

    public override string? CustomPortraitPath => ProbeCardKit.DefendPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "覆还"),
        ("description", "在本回合，你每失去1层[gold]钙铠[/gold]，就在下回合开始时获得1层[gold]钙铠[/gold]。"),
    };
}
