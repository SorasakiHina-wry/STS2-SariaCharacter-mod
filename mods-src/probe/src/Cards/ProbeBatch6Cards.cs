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
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Saria.Pools;
using Saria.Powers;

namespace Saria.Cards;

// 2026-11-09 batch (8 cards).

/// <summary>战术 — block now, armour + block next turn.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeTactics : CustomCardModel
{
    public ProbeTactics() : base(2, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
        HoverTipFactory.Static(StaticHoverTip.Block),
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(6m, ValueProp.Move),
        new PowerVar<ProbeNextTurnArmorPower>(6m),
        new PowerVar<BlockNextTurnPower>(6m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {

        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        await PowerCmd.Apply<ProbeNextTurnArmorPower>(
            ctx, Owner.Creature, DynamicVars["ProbeNextTurnArmorPower"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<BlockNextTurnPower>(
            ctx, Owner.Creature, DynamicVars["BlockNextTurnPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
        DynamicVars["ProbeNextTurnArmorPower"].UpgradeValueBy(2m);
        DynamicVars["BlockNextTurnPower"].UpgradeValueBy(3m);
    }

    public override string? CustomPortraitPath => ProbeCardKit.DefendPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "战术"),
        ("description", "获得{Block:diff()}点[gold]格挡[/gold]。\n在下个回合开始时，获得{ProbeNextTurnArmorPower:diff()}层[gold]钙铠[/gold]与{BlockNextTurnPower:diff()}点[gold]格挡[/gold]。"),
    };
}

/// <summary>强制唤醒 — armour now, more armour next turn.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeForcedWake : CustomCardModel
{
    public ProbeForcedWake() : base(2, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Ethereal };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<CalciumArmorPower>(5m),
        new PowerVar<ProbeNextTurnArmorPower>(8m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {

        await PowerCmd.Apply<CalciumArmorPower>(
            ctx, Owner.Creature, DynamicVars["CalciumArmorPower"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<ProbeNextTurnArmorPower>(
            ctx, Owner.Creature, DynamicVars["ProbeNextTurnArmorPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["CalciumArmorPower"].UpgradeValueBy(1m);
        DynamicVars["ProbeNextTurnArmorPower"].UpgradeValueBy(2m);
    }

    public override string? CustomPortraitPath => ProbeCardKit.DefendPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "强制唤醒"),
        ("description", "获得{CalciumArmorPower:diff()}层[gold]钙铠[/gold]。\n在下个回合开始时，获得{ProbeNextTurnArmorPower:diff()}层[gold]钙铠[/gold]。"),
    };
}

/// <summary>拓印 — armour, plus a copy of the next attack you play this turn.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeImprint : CustomCardModel
{
    public ProbeImprint() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
        HoverTipFactory.FromPower<ProbeImprintPower>(),
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<CalciumArmorPower>(6m),
        new PowerVar<ProbeImprintPower>(1m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {

        await PowerCmd.Apply<CalciumArmorPower>(
            ctx, Owner.Creature, DynamicVars["CalciumArmorPower"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<ProbeImprintPower>(
            ctx, Owner.Creature, DynamicVars["ProbeImprintPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["CalciumArmorPower"].UpgradeValueBy(3m);

    public override string? CustomPortraitPath => ProbeCardKit.DefendPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "拓印"),
        ("description", "获得{CalciumArmorPower:diff()}层[gold]钙铠[/gold]。\n将你本回合打出的下一张攻击牌的复制品加入你的[gold]手牌[/gold]。"),
    };
}

/// <summary>紊乱 — damage, and the enemy pays you armour if it drops low.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeDisorder : CustomCardModel
{
    public ProbeDisorder() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
        HoverTipFactory.FromPower<ProbeDisorderPower>(),
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(8m, ValueProp.Move),
        new PowerVar<ProbeDisorderPower>(10m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {

        ArgumentNullException.ThrowIfNull(play.Target, "cardPlay.Target");

        await ProbeCardKit.Hit(ctx, play, this, "vfx/vfx_attack_slash");
        await PowerCmd.Apply<ProbeDisorderPower>(
            ctx, play.Target, DynamicVars["ProbeDisorderPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["ProbeDisorderPower"].UpgradeValueBy(5m);

    public override string? CustomPortraitPath => ProbeCardKit.BashPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "紊乱"),
        ("description", "造成{Damage:diff()}点伤害。\n当该敌人生命小于等于30%时，仅一次使你获得{ProbeDisorderPower:diff()}层[gold]钙铠[/gold]。"),
    };
}

/// <summary>精密操作 — heavy hit, next armour gain this turn refunds energy.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbePrecision : CustomCardModel
{
    public ProbePrecision() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
        HoverTipFactory.FromPower<ProbePrecisionPower>(),
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(17m, ValueProp.Move),
        new EnergyVar(1),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {

        ArgumentNullException.ThrowIfNull(play.Target, "cardPlay.Target");

        await ProbeCardKit.Hit(ctx, play, this, "vfx/vfx_attack_blunt");
        await PowerCmd.Apply<ProbePrecisionPower>(
            ctx, Owner.Creature, DynamicVars.Energy.BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars.Energy.UpgradeValueBy(1m);

    public override string? CustomPortraitPath => ProbeCardKit.StrikePortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "精密操作"),
        ("description", "造成{Damage:diff()}点伤害。\n在本回合你下一次获得[gold]钙铠[/gold]时，获得{Energy:energyIcons()}。"),
    };
}

/// <summary>冲拳 — four (upgraded five) small hits.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeRushPunch : CustomCardModel
{
    public ProbeRushPunch() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(3m, ValueProp.Move),
        new RepeatVar(4),
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
        ("title", "冲拳"),
        ("description", "造成{Damage:diff()}点伤害{Repeat:diff()}次。"),
    };
}

/// <summary>止血 — spend armour; only the first two hits a turn land.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeHemostasis : CustomCardModel
{
    public ProbeHemostasis() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    private int ArmorCost => IsUpgraded ? 1 : 3;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
        HoverTipFactory.FromPower<ProbeHemostasisPower>(),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {

        var armour = Owner.Creature.GetPower<CalciumArmorPower>();
        if (armour != null && armour.Amount > 0)
        {
            int loss = Math.Min(ArmorCost, armour.Amount);
            if (loss > 0)
                await PowerCmd.ModifyAmount(ctx, armour, -loss, Owner.Creature, this);
        }

        await PowerCmd.Apply<ProbeHemostasisPower>(ctx, Owner.Creature, 1m, Owner.Creature, this);
    }

    public override string? CustomPortraitPath => ProbeCardKit.DefendPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "止血"),
        ("description", "失去{IfUpgraded:show:1|3}层[gold]钙铠[/gold]。\n你在下个回合开始前至多受到2次伤害。"),
    };
}

/// <summary>创始人 — big hits build armour.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeFounder : CustomCardModel
{
    public ProbeFounder() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
        HoverTipFactory.FromPower<ProbeFounderPower>(),
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<ProbeFounderPower>(1m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {

        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<ProbeFounderPower>(
            ctx, Owner.Creature, DynamicVars["ProbeFounderPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["ProbeFounderPower"].UpgradeValueBy(1m);

    public override string? CustomPortraitPath => ProbeCardKit.InflamePortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "创始人"),
        ("description", "每当你造成大于等于22点的伤害时，获得{ProbeFounderPower:diff()}层[gold]钙铠[/gold]。"),
    };
}
