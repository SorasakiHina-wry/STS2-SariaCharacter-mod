using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Probe.Pools;
using Probe.Powers;

namespace Probe.Cards;

/// <summary>封装 — armour, block, and a card back from the discard pile next turn.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeEncapsulate : CustomCardModel
{
    public ProbeEncapsulate() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<CalciumArmorPower>(4m),
        new BlockVar(4m, ValueProp.Move),
        new PowerVar<ProbeEncapsulatePower>(1m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PowerCmd.Apply<CalciumArmorPower>(ctx, Owner.Creature, DynamicVars["CalciumArmorPower"].BaseValue, Owner.Creature, this);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        await PowerCmd.Apply<ProbeEncapsulatePower>(ctx, Owner.Creature, DynamicVars["ProbeEncapsulatePower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["CalciumArmorPower"].UpgradeValueBy(2m);
        DynamicVars.Block.UpgradeValueBy(2m);
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
        HoverTipFactory.Static(StaticHoverTip.Block),
    };

    public override string? CustomPortraitPath => ProbeCardKit.DefendPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "封装"),
        ("description", "获得{CalciumArmorPower:diff()}层[gold]钙铠[/gold]。\n获得{Block:diff()}点[gold]格挡[/gold]。\n在下个回合开始时，将[gold]弃牌堆[/gold]中的一张牌加入你的[gold]手牌[/gold]。"),
    };
}

/// <summary>膨化 — hit everything, then cash every armour stack into block.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbePuffUp : CustomCardModel
{
    public ProbePuffUp() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(20m, ValueProp.Move),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, play)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(ctx);

        int armour = Owner.Creature.GetPowerAmount<CalciumArmorPower>();
        var power = Owner.Creature.GetPower<CalciumArmorPower>();
        if (armour > 0 && power != null)
        {
            await PowerCmd.ModifyAmount(ctx, power, -armour, Owner.Creature, this);
            await CreatureCmd.GainBlock(Owner.Creature, armour, ValueProp.Unpowered, play);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(5m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
        HoverTipFactory.Static(StaticHoverTip.Block),
    };

    public override string? CustomPortraitPath => ProbeCardKit.BashPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "膨化"),
        ("description", "对所有敌人造成{Damage:diff()}点伤害。\n失去所有[gold]钙铠[/gold]，并获得相同数值的[gold]格挡[/gold]。"),
    };
}

/// <summary>围歼 — spend up to five armour stacks as separate hits, gain Vigor, return to hand.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeEncircle : CustomCardModel
{
    public ProbeEncircle() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(7m, ValueProp.Move),
        new PowerVar<VigorPower>(5m),
        new DynamicVar("ArmorCost", 5m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        int available = Owner.Creature.GetPowerAmount<CalciumArmorPower>();
        int spend = (int)DynamicVars["ArmorCost"].BaseValue;
        if (spend > available)
            spend = available;

        var power = Owner.Creature.GetPower<CalciumArmorPower>();
        if (spend > 0 && power != null)
        {
            await PowerCmd.ModifyAmount(ctx, power, -spend, Owner.Creature, this);
            ArgumentNullException.ThrowIfNull(play.Target, "cardPlay.Target");
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, play)
                .Targeting(play.Target)
                .WithHitCount(spend)
                .WithHitFx("vfx/vfx_attack_blunt")
                .Execute(ctx);
        }

        await PowerCmd.Apply<VigorPower>(ctx, Owner.Creature, DynamicVars["VigorPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["VigorPower"].UpgradeValueBy(3m);

    /// <summary>Returns to hand instead of the discard pile.</summary>
    protected override CardLocation GetResultLocationForCardPlay()
    {
        var location = base.GetResultLocationForCardPlay();
        if (location.pileType == PileType.Discard)
            location.pileType = PileType.Hand;
        return location;
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
        HoverTipFactory.FromPower<VigorPower>(),
    };

    public override string? CustomPortraitPath => ProbeCardKit.BludgeonPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "围歼"),
        ("description", "失去至多{ArmorCost}层[gold]钙铠[/gold]，每层造成{Damage:diff()}点伤害。\n获得{VigorPower:diff()}点[gold]活力[/gold]。\n将此牌返回你的[gold]手牌[/gold]。"),
    };
}

/// <summary>战略终结 — damage grows with every armour stack gained this combat.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeStrategicEnd : CustomCardModel
{
    public ProbeStrategicEnd() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(8m, ValueProp.Move),
        new DynamicVar("PerArmor", 2m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        int total = Owner.Creature.GetPower<CalciumArmorPower>()?.TotalGainedThisCombat ?? 0;
        decimal damage = DynamicVars.Damage.BaseValue + DynamicVars["PerArmor"].BaseValue * total;

        await DamageCmd.Attack(damage)
            .FromCard(this, play)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(ctx);
    }

    protected override void OnUpgrade() => DynamicVars["PerArmor"].UpgradeValueBy(2m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
    };

    public override string? CustomPortraitPath => ProbeCardKit.BashPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "战略终结"),
        ("description", "造成{Damage:diff()}点伤害。\n在本场战斗中，你每获得过1层[gold]钙铠[/gold]，伤害增加{PerArmor:diff()}。"),
    };
}

/// <summary>缠斗 — damage equal to current armour, then back on top of the draw pile.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeTangle : CustomCardModel
{
    public ProbeTangle() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        int armour = Owner.Creature.GetPowerAmount<CalciumArmorPower>();
        if (armour <= 0)
            return;

        ArgumentNullException.ThrowIfNull(play.Target, "cardPlay.Target");
        await DamageCmd.Attack(armour)
            .FromCard(this, play)
            .Targeting(play.Target)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(ctx);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    /// <summary>Goes on top of the draw pile instead of the discard pile.</summary>
    protected override CardLocation GetResultLocationForCardPlay()
    {
        var location = base.GetResultLocationForCardPlay();
        if (location.pileType == PileType.Discard)
        {
            location.pileType = PileType.Draw;
            location.position = CardPilePosition.Top;
        }
        return location;
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
    };

    public override string? CustomPortraitPath => ProbeCardKit.BludgeonPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "缠斗"),
        ("description", "造成你当前[gold]钙铠[/gold]层数的伤害。\n将这张牌放置于你的[gold]抽牌堆[/gold]顶部。"),
    };
}

/// <summary>臂甲 — cheap Strength that copies itself into the discard pile.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeArmguard : CustomCardModel
{
    public ProbeArmguard() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<StrengthPower>(2m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PowerCmd.Apply<StrengthPower>(ctx, Owner.Creature, DynamicVars["StrengthPower"].BaseValue, Owner.Creature, this);

        var copy = CreateClone();
        await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Discard, Owner);
    }

    protected override void OnUpgrade() => DynamicVars["StrengthPower"].UpgradeValueBy(1m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<StrengthPower>(),
    };

    public override string? CustomPortraitPath => ProbeCardKit.DefendPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "臂甲"),
        ("description", "获得{StrengthPower:diff()}点[gold]力量[/gold]。\n将一张此牌的复制品加入你的[gold]弃牌堆[/gold]。"),
    };
}

/// <summary>渗透 — spend a stack, then apply Vulnerable + Weak several times to everything.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeOsmosis : CustomCardModel
{
    public ProbeOsmosis() : base(1, CardType.Skill, CardRarity.Ancient, TargetType.AllEnemies) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<VulnerablePower>(2m),
        new PowerVar<WeakPower>(1m),
        new RepeatVar(2),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var power = Owner.Creature.GetPower<CalciumArmorPower>();
        if (power != null && power.Amount > 0)
            await PowerCmd.ModifyAmount(ctx, power, -1m, Owner.Creature, this);

        for (int i = 0; i < (int)DynamicVars.Repeat.BaseValue; i++)
        {
            foreach (var enemy in CombatState.HittableEnemies)
            {
                await PowerCmd.Apply<VulnerablePower>(ctx, enemy, DynamicVars.Vulnerable.BaseValue, Owner.Creature, this);
                await PowerCmd.Apply<WeakPower>(ctx, enemy, DynamicVars.Weak.BaseValue, Owner.Creature, this);
            }
        }
    }

    protected override void OnUpgrade() => DynamicVars.Repeat.UpgradeValueBy(1m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
        HoverTipFactory.FromPower<VulnerablePower>(),
        HoverTipFactory.FromPower<WeakPower>(),
    };

    public override string? CustomPortraitPath => ProbeCardKit.BashPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "渗透"),
        ("description", "失去1层[gold]钙铠[/gold]。\n给予所有敌人{VulnerablePower:diff()}层[gold]易伤[/gold]与{WeakPower:diff()}层[gold]虚弱[/gold]{Repeat:diff()}次。"),
    };
}

/// <summary>极限功率 — the permanent power behind the card of the same name.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeOverclock : CustomCardModel
{
    public ProbeOverclock() : base(2, CardType.Power, CardRarity.Ancient, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<ProbeOverclockPower>(1m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PowerCmd.Apply<ProbeOverclockPower>(ctx, Owner.Creature, DynamicVars["ProbeOverclockPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
    };

    public override string? CustomPortraitPath => ProbeCardKit.BashPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "极限功率"),
        ("description", "在你的回合开始时，失去所有[gold]格挡[/gold]，并获得相同数值的[gold]钙铠[/gold]。"),
    };
}

/// <summary>共振 — every armour stack gained this turn hits the chosen enemy once.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeResonance : CustomCardModel
{
    public ProbeResonance() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<ProbeResonancePower>(1m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PowerCmd.Apply<ProbeResonancePower>(ctx, Owner.Creature, DynamicVars["ProbeResonancePower"].BaseValue, Owner.Creature, this);
        Owner.Creature.GetPower<ProbeResonancePower>()!.Target = play.Target;
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
    };

    public override string? CustomPortraitPath => ProbeCardKit.BashPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "共振"),
        ("description", "在本回合每获得1层[gold]钙铠[/gold]，就造成4点伤害一次。"),
    };
}

/// <summary>防卫经验 — the first eight armour stacks lost each turn draw cards.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeDefenseExperience : CustomCardModel
{
    public ProbeDefenseExperience() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<ProbeDefenseExperiencePower>(1m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var existing = Owner.Creature.GetPower<ProbeDefenseExperiencePower>();
        if (existing != null)
        {
            // 再打一张：抽牌数 +2，层数补回阈值
            existing.ExtraCards += 2;
            await PowerCmd.ModifyAmount(ctx, existing, ProbeDefenseExperiencePower.Threshold - existing.Amount, Owner.Creature, this);
            return;
        }

        await PowerCmd.Apply<ProbeDefenseExperiencePower>(ctx, Owner.Creature, DynamicVars["ProbeDefenseExperiencePower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
    };

    public override string? CustomPortraitPath => ProbeCardKit.BashPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "防卫经验"),
        ("description", "你在每回合第一次累计失去8层[gold]钙铠[/gold]时，抽2张牌。"),
    };
}
