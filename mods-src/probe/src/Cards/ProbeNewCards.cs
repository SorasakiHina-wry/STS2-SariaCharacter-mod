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
using Probe.Pools;
using Probe.Powers;

namespace Probe.Cards;

// Cards added in the 2026-11-07 design pass. Descriptions are the sheet's 描述/效果 text verbatim,
// with only the numbers replaced by dynamic vars so upgrades show the right values.

/// <summary>针剂弹药 — attack + armour.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeAmmoDose : CustomCardModel
{
    public ProbeAmmoDose() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(8m, ValueProp.Move),
        new PowerVar<CalciumArmorPower>(3m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {

        await ProbeCardKit.Hit(ctx, play, this, "vfx/vfx_attack_slash");
        await PowerCmd.Apply<CalciumArmorPower>(ctx, Owner.Creature, DynamicVars["CalciumArmorPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars["CalciumArmorPower"].UpgradeValueBy(1m);
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
    };

    public override string? CustomPortraitPath => ProbeCardKit.StrikePortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "针剂弹药"),
        ("description", "造成{Damage:diff()}点伤害。\n获得{CalciumArmorPower:diff()}层[gold]钙铠[/gold]。"),
    };
}

/// <summary>部署 — armour + draw.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeDeploy : CustomCardModel
{
    public ProbeDeploy() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<CalciumArmorPower>(9m),
        new CardsVar(1),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {

        await PowerCmd.Apply<CalciumArmorPower>(ctx, Owner.Creature, DynamicVars["CalciumArmorPower"].BaseValue, Owner.Creature, this);
        await CardPileCmd.Draw(ctx, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);   // 钙铠固定 9，升级只加抽牌

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
    };

    public override string? CustomPortraitPath => ProbeCardKit.DefendPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "部署"),
        ("description", "获得{CalciumArmorPower:diff()}层[gold]钙铠[/gold]。\n抽{Cards:diff()}张牌。"),
    };
}

/// <summary>粉末化 — cash the armour in as block, then weaken everything.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbePowderize : CustomCardModel
{
    public ProbePowderize() : base(1, CardType.Skill, CardRarity.Common, TargetType.AllEnemies) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<VulnerablePower>(2m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {

        int armour = Owner.Creature.GetPowerAmount<CalciumArmorPower>();
        var power = Owner.Creature.GetPower<CalciumArmorPower>();
        if (armour > 0 && power != null)
        {
            // 只消耗 1 层，不换格挡
            await PowerCmd.ModifyAmount(ctx, power, -1m, Owner.Creature, this);
        }

        foreach (var enemy in CombatState.HittableEnemies)
        {
            await PowerCmd.Apply<VulnerablePower>(ctx, enemy, DynamicVars.Vulnerable.BaseValue, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Vulnerable.UpgradeValueBy(1m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<VulnerablePower>(),
        HoverTipFactory.FromPower<CalciumArmorPower>(),
        HoverTipFactory.Static(StaticHoverTip.Block),
    };

    public override string? CustomPortraitPath => ProbeCardKit.BashPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "粉末化"),
        ("description", "失去1层[gold]钙铠[/gold]。\n对所有敌人给予{VulnerablePower:diff()}层[gold]易伤[/gold]。"),
    };
}

/// <summary>拳击 — multi-hit single target.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbePunch : CustomCardModel
{
    public ProbePunch() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(4m, ValueProp.Move),
        new RepeatVar(2),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {

        ArgumentNullException.ThrowIfNull(play.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, play)
            .Targeting(play.Target)
            .WithHitCount((int)DynamicVars.Repeat.BaseValue)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(ctx);
    }

    protected override void OnUpgrade() => DynamicVars.Repeat.UpgradeValueBy(1m);

    public override string? CustomPortraitPath => ProbeCardKit.BashPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "拳击"),
        ("description", "造成{Damage:diff()}点伤害{Repeat:diff()}次。"),
    };
}

/// <summary>持盾打击 — multi-hit against everything.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeShieldBash : CustomCardModel
{
    public ProbeShieldBash() : base(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(3m, ValueProp.Move),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, play)
            .TargetingAllOpponents(CombatState)
            .WithHitCount(2)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(ctx);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);

    public override string? CustomPortraitPath => ProbeCardKit.DefendPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "持盾打击"),
        ("description", "对所有敌人造成{Damage:diff()}点伤害2次。"),
    };
}

/// <summary>注射 — cheap block plus armour.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeInject : CustomCardModel
{
    public ProbeInject() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(6m, ValueProp.Move),
        new PowerVar<CalciumArmorPower>(4m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {

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

    public override string? CustomPortraitPath => ProbeCardKit.DefendPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "注射"),
        ("description", "获得{Block:diff()}点[gold]格挡[/gold]。\n获得{CalciumArmorPower:diff()}层[gold]钙铠[/gold]。"),
    };
}

/// <summary>紧密防护 — armour stops decaying until next turn, plus block.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeTightGuard : CustomCardModel
{
    public ProbeTightGuard() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(10m, ValueProp.Move),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {

        await PowerCmd.Apply<ProbeTightGuardPower>(ctx, Owner.Creature, 1m, Owner.Creature, this);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(5m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
        HoverTipFactory.Static(StaticHoverTip.Block),
    };

    public override string? CustomPortraitPath => ProbeCardKit.DefendPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "紧密防护"),
        ("description", "你的[gold]钙铠[/gold]在下个回合开始前不会因获得[gold]格挡[/gold]而损失。\n获得{Block:diff()}点[gold]格挡[/gold]。"),
    };
}
