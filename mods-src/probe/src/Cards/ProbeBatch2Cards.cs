using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Probe.Pools;
using Probe.Powers;

namespace Probe.Cards;

/// <summary>稳定 — block, then armour.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeSteady : CustomCardModel
{
    public ProbeSteady() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(8m, ValueProp.Move),
        new PowerVar<CalciumArmorPower>(3m),
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

    public override string? CustomPortraitPath => ProbeCardKit.DefendPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "稳定"),
        ("description", "获得{Block:diff()}点格挡。\n获得{CalciumArmorPower:diff()}层钙铠。"),
    };
}

/// <summary>爆裂 — cash every armour stack into one hit per stack, against everything.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeDetonate : CustomCardModel
{
    public ProbeDetonate() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(6m, ValueProp.Move),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        int armour = Owner.Creature.GetPowerAmount<CalciumArmorPower>();
        var power = Owner.Creature.GetPower<CalciumArmorPower>();
        if (armour <= 0 || power == null)
            return;

        await PowerCmd.Remove(power);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, play)
            .TargetingAllOpponents(CombatState)
            .WithHitCount(armour)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(ctx);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);

    public override string? CustomPortraitPath => ProbeCardKit.BludgeonPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "爆裂"),
        ("description", "失去所有钙铠，每失去1层钙铠，就对所有敌人造成{Damage:diff()}点伤害。"),
    };
}

/// <summary>全弹发射 — X-cost: X Vulnerable, then X hits.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeFullSalvo : CustomCardModel
{
    public ProbeFullSalvo() : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override bool HasEnergyCostX => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(5m, ValueProp.Move),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        int x = ResolveEnergyXValue();
        if (IsUpgraded)
            x += 1;
        if (x <= 0)
            return;

        ArgumentNullException.ThrowIfNull(play.Target, "cardPlay.Target");

        await PowerCmd.Apply<VulnerablePower>(ctx, play.Target, x, Owner.Creature, this);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, play)
            .Targeting(play.Target)
            .WithHitCount(x)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(ctx);
    }

    public override string? CustomPortraitPath => ProbeCardKit.StrikePortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "全弹发射"),
        ("description", "施加{IfUpgraded:show:X+1|X}层易伤。\n造成{Damage:diff()}点伤害{IfUpgraded:show:X+1|X}次。"),
    };
}

/// <summary>充能 — armour and Vigor.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeChargeUp : CustomCardModel
{
    public ProbeChargeUp() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<CalciumArmorPower>(4m),
        new PowerVar<VigorPower>(3m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PowerCmd.Apply<CalciumArmorPower>(ctx, Owner.Creature, DynamicVars["CalciumArmorPower"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<VigorPower>(ctx, Owner.Creature, DynamicVars["VigorPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["CalciumArmorPower"].UpgradeValueBy(2m);
        DynamicVars["VigorPower"].UpgradeValueBy(1m);
    }

    public override string? CustomPortraitPath => ProbeCardKit.InflamePortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "充能"),
        ("description", "获得{CalciumArmorPower:diff()}层钙铠。获得{VigorPower:diff()}点活力。"),
    };
}

/// <summary>分神 — power: turn start loses block, gains armour.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeDistract : CustomCardModel
{
    public ProbeDistract() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<ProbeDistractPower>(1m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        // 升级前后分别提供 分神 / 分神+（见 design/powers.md）
        if (IsUpgraded)
            await PowerCmd.Apply<ProbeDistractPlusPower>(ctx, Owner.Creature, 1m, Owner.Creature, this);
        else
            await PowerCmd.Apply<ProbeDistractPower>(ctx, Owner.Creature, 1m, Owner.Creature, this);
    }

    public override string? CustomPortraitPath => ProbeCardKit.InflamePortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "分神"),
        ("description", "在你的回合开始时，失去{IfUpgraded:show:2|3}点格挡并获得1层钙铠。"),
    };
}

/// <summary>凝神 — power: turn start spends one armour stack for Vigor.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeFocusMind : CustomCardModel
{
    public ProbeFocusMind() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<ProbeFocusMindPower>(1m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PowerCmd.Apply<ProbeFocusMindPower>(ctx, Owner.Creature, DynamicVars["ProbeFocusMindPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    public override string? CustomPortraitPath => ProbeCardKit.InflamePortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "凝神"),
        ("description", "在你的回合开始时，失去1层钙铠，获得4点活力。"),
    };
}
