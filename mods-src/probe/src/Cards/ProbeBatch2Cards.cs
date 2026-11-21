using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Saria.Pools;
using Saria.Powers;

namespace Saria.Cards;

/// <summary>稳定 — block, then armour.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeSteady : SariaCardModel
{
    public ProbeSteady() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(8m, ValueProp.Move),
        new PowerVar<CalciumArmorPower>(1m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {

        // block first: armour gained afterwards is not eaten by its own decay
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        await PowerCmd.Apply<CalciumArmorPower>(ctx, Owner.Creature, DynamicVars["CalciumArmorPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
        DynamicVars["CalciumArmorPower"].UpgradeValueBy(1m);
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
        HoverTipFactory.Static(StaticHoverTip.Block),
    };
    public override List<(string, string)>? Localization => new()
    {
        ("title", "稳定"),
        ("description", "获得{Block:diff()}点[gold]格挡[/gold]。\n获得{CalciumArmorPower:diff()}层[gold]钙铠[/gold]。"),
    };
}

/// <summary>爆裂 — cash every armour stack into one hit per stack, against everything.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeDetonate : SariaCardModel
{
    public ProbeDetonate() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(2m, ValueProp.Move),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {

        int armour = Owner.Creature.GetPowerAmount<CalciumArmorPower>();
        var power = Owner.Creature.GetPower<CalciumArmorPower>();
        if (armour <= 0 || power == null)
            return;

        await PowerCmd.ModifyAmount(ctx, power, -armour, Owner.Creature, this);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, play)
            .TargetingAllOpponents(CombatState)
            .WithHitCount(armour)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(ctx);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
    };
    public override List<(string, string)>? Localization => new()
    {
        ("title", "爆裂"),
        ("description", "失去所有[gold]钙铠[/gold]，每失去1层[gold]钙铠[/gold]，就对所有敌人造成{Damage:diff()}点伤害。"),
    };
}

/// <summary>全弹发射 — X-cost: X Vulnerable, then X hits.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeFullSalvo : SariaCardModel
{
    public ProbeFullSalvo() : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override bool HasEnergyCostX => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(6m, ValueProp.Move),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {

        int x = ResolveEnergyXValue();
        if (x <= 0)
            return;

        ArgumentNullException.ThrowIfNull(play.Target, "cardPlay.Target");

        // 易伤层数 = X；命中次数 = X（升级 +1）
        await PowerCmd.Apply<VulnerablePower>(ctx, play.Target, IsUpgraded ? 2m : 1m, Owner.Creature, this);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, play)
            .Targeting(play.Target)
            .WithHitCount(IsUpgraded ? x + 1 : x)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(ctx);
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<VulnerablePower>(),
    };
    public override List<(string, string)>? Localization => new()
    {
        ("title", "全弹发射"),
        ("description", "给予{IfUpgraded:show:2|1}层[gold]易伤[/gold]。\n造成{Damage:diff()}点伤害{IfUpgraded:show:X+1|X}次。"),
    };
}

/// <summary>充能 — armour and Vigor.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeChargeUp : SariaCardModel
{
    public ProbeChargeUp() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    /// <summary>失去的钙铠层数：基础 4，升级 2。</summary>
    private int ArmorCost => IsUpgraded ? 2 : 4;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<StrengthPower>(3m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var armour = Owner.Creature.GetPower<CalciumArmorPower>();
        if (armour != null && armour.Amount > 0)
        {
            int loss = Math.Min(ArmorCost, armour.Amount);
            await PowerCmd.ModifyAmount(ctx, armour, -loss, Owner.Creature, this);
        }

        await PowerCmd.Apply<StrengthPower>(ctx, Owner.Creature, DynamicVars["StrengthPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() { }

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<CalciumArmorPower>(),
    };
    public override List<(string, string)>? Localization => new()
    {
        ("title", "充能"),
        ("description", "失去{IfUpgraded:show:2|4}层[gold]钙铠[/gold]。\n获得{StrengthPower:diff()}点[gold]力量[/gold]。"),
    };
}

/// <summary>分神 — power: turn start loses block, gains armour.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeDistract : SariaCardModel
{
    public ProbeDistract() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        // 施加的层数 = 回合开始时要失去的格挡量（分神 3，分神+ 2）
        new PowerVar<ProbeDistractPower>(3m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {

        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        var power = await PowerCmd.Apply<ProbeDistractPower>(
            ctx, Owner.Creature, DynamicVars["ProbeDistractPower"].BaseValue, Owner.Creature, this);
        power?.IncrementArmorGain();   // 每打出一次 +1 层钙铠（内部计数器，不显示在图标上）
    }

    protected override void OnUpgrade() => DynamicVars["ProbeDistractPower"].UpgradeValueBy(-1m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
        HoverTipFactory.Static(StaticHoverTip.Block),
    };
    public override List<(string, string)>? Localization => new()
    {
        ("title", "分神"),
        ("description", "在你的回合开始时，失去{IfUpgraded:show:2|3}点[gold]格挡[/gold]并获得1层[gold]钙铠[/gold]。"),
    };
}

/// <summary>凝神 — power: turn start spends armour, grants Vigor.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeFocusMind : SariaCardModel
{
    public ProbeFocusMind() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        // 每张「凝神」让每次攻击牌多给 2 点格挡；叠层时 Amount 相加
        new PowerVar<ProbeFocusMindPower>(2m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<ProbeFocusMindPower>(
            ctx, Owner.Creature, DynamicVars["ProbeFocusMindPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<ProbeFocusMindPower>(),
        HoverTipFactory.Static(StaticHoverTip.Block),
    };
    public override List<(string, string)>? Localization => new()
    {
        ("title", "凝神"),
        ("description", "你每打出一张攻击牌，获得2点[gold]格挡[/gold]。"),
    };
}
