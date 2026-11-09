using System;
using System.Collections.Generic;
using System.Linq;
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

// 2026-11-08 batch 2 (9 cards).

/// <summary>格斗 — two hits, upgraded to three.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeBrawl : CustomCardModel
{
    public ProbeBrawl() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(10m, ValueProp.Move),
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
        ("title", "格斗"),
        ("description", "造成{Damage:diff()}点伤害{Repeat:diff()}次。"),
    };
}

/// <summary>持械 — spend armour for Vigor that lapses at end of turn.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeArm : CustomCardModel
{
    public ProbeArm() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<ProbeArmPower>(5m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var armour = Owner.Creature.GetPower<CalciumArmorPower>();
        if (armour != null && armour.Amount > 0)
            await PowerCmd.Decrement(armour);

        decimal vigor = DynamicVars["ProbeArmPower"].BaseValue;
        await PowerCmd.Apply<ProbeArmPower>(ctx, Owner.Creature, vigor, Owner.Creature, this);
        await PowerCmd.Apply<VigorPower>(ctx, Owner.Creature, vigor, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["ProbeArmPower"].UpgradeValueBy(1m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<VigorPower>(),
        HoverTipFactory.FromPower<CalciumArmorPower>(),
    };

    public override string? CustomPortraitPath => ProbeCardKit.InflamePortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "持械"),
        ("description", "失去1层[gold]钙铠[/gold]。\n在本回合获得{ProbeArmPower:diff()}点[gold]活力[/gold]。"),
    };
}

/// <summary>谨慎 — damage, block, then armour.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeCautious : CustomCardModel
{
    public ProbeCautious() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(5m, ValueProp.Move),
        new BlockVar(4m, ValueProp.Move),
        new PowerVar<CalciumArmorPower>(2m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await ProbeCardKit.Hit(ctx, play, this, "vfx/vfx_attack_slash");
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        await PowerCmd.Apply<CalciumArmorPower>(ctx, Owner.Creature, DynamicVars["CalciumArmorPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars.Block.UpgradeValueBy(1m);
        DynamicVars["CalciumArmorPower"].UpgradeValueBy(1m);
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
        HoverTipFactory.Static(StaticHoverTip.Block),
    };

    public override string? CustomPortraitPath => ProbeCardKit.StrikePortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "谨慎"),
        ("description", "造成{Damage:diff()}点伤害。\n获得{Block:diff()}点[gold]格挡[/gold]。\n获得{CalciumArmorPower:diff()}层[gold]钙铠[/gold]。"),
    };
}

/// <summary>腿甲 — armour plus card draw on the following turns.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeLegArmor : CustomCardModel
{
    public ProbeLegArmor() : base(2, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<CalciumArmorPower>(10m),
        new PowerVar<ProbeLegArmorPower>(1m),
        new BlockVar(8m, ValueProp.Move),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PowerCmd.Apply<CalciumArmorPower>(ctx, Owner.Creature, DynamicVars["CalciumArmorPower"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<ProbeLegArmorPower>(ctx, Owner.Creature, DynamicVars["ProbeLegArmorPower"].BaseValue, Owner.Creature, this);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["CalciumArmorPower"].UpgradeValueBy(2m);
        DynamicVars["ProbeLegArmorPower"].UpgradeValueBy(1m);
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
        HoverTipFactory.Static(StaticHoverTip.Block),
    };

    public override string? CustomPortraitPath => ProbeCardKit.DefendPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "腿甲"),
        ("description", "获得{CalciumArmorPower:diff()}层[gold]钙铠[/gold]。\n获得{Block:diff()}点[gold]格挡[/gold]。\n{IfUpgraded:show:在下2个回合开始时抽1张牌。|在下个回合开始时抽1张牌。}"),
    };
}

/// <summary>骨矿化 — armour whenever a card grants block.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeBoneMineralize : CustomCardModel
{
    public ProbeBoneMineralize() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<ProbeBoneMineralizePower>(1m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PowerCmd.Apply<ProbeBoneMineralizePower>(
            ctx, Owner.Creature, DynamicVars["ProbeBoneMineralizePower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
        HoverTipFactory.Static(StaticHoverTip.Block),
    };

    public override string? CustomPortraitPath => ProbeCardKit.InflamePortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "骨矿化"),
        ("description", "当你从卡牌中获得[gold]格挡[/gold]时，获得1层[gold]钙铠[/gold]。"),
    };
}

/// <summary>技艺超载 — refill the hand now, pay armour next turn.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeTechniqueOverload : CustomCardModel
{
    private const int ARMOR_LOSS = 6;

    public ProbeTechniqueOverload() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var hand = PileType.Hand.GetPile(Owner);
        int missing = Math.Max(0, CardPile.MaxCardsInHand - hand.Cards.Count);
        if (missing > 0)
            await CardPileCmd.Draw(ctx, missing, Owner);

        await PowerCmd.Apply<ProbeTechniqueOverloadPower>(ctx, Owner.Creature, ARMOR_LOSS, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
    };

    public override string? CustomPortraitPath => ProbeCardKit.BashPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "技艺超载"),
        ("description", "抽牌直到抽满[gold]手牌[/gold]。\n在下个回合开始时，失去6层[gold]钙铠[/gold]。"),
    };
}

/// <summary>精神回复 — every 18 armour becomes one energy.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeSpiritRecovery : CustomCardModel
{
    public ProbeSpiritRecovery() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new EnergyVar(1),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PowerCmd.Apply<ProbeSpiritRecoveryPower>(
            ctx, Owner.Creature, 1m, Owner.Creature, this);   // 1 层 = 每次触发 1 点能量
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
    };

    public override string? CustomPortraitPath => ProbeCardKit.InflamePortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "精神回复"),
        ("description", "你每获得18层[gold]钙铠[/gold]，获得{Energy:energyIcons()}。"),
    };
}

/// <summary>拳击手套 — trade one Dexterity for Strength.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeBoxingGloves : CustomCardModel
{
    public ProbeBoxingGloves() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<StrengthPower>(4m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PowerCmd.Apply<DexterityPower>(ctx, Owner.Creature, -1m, Owner.Creature, this);
        await PowerCmd.Apply<StrengthPower>(ctx, Owner.Creature, DynamicVars["StrengthPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["StrengthPower"].UpgradeValueBy(1m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>(),
    };

    public override string? CustomPortraitPath => ProbeCardKit.InflamePortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "拳击手套"),
        ("description", "失去1点[gold]敏捷[/gold]。\n获得{StrengthPower:diff()}点[gold]力量[/gold]。"),
    };
}

/// <summary>“科技新星” — extra Strength whenever Strength comes from elsewhere.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeTechNova : CustomCardModel
{
    public ProbeTechNova() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<ProbeTechNovaPower>(1m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PowerCmd.Apply<ProbeTechNovaPower>(
            ctx, Owner.Creature, DynamicVars["ProbeTechNovaPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<StrengthPower>(),
    };

    public override string? CustomPortraitPath => ProbeCardKit.InflamePortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "“科技新星”"),
        ("description", "每当你以此卡以外的方式获得[gold]力量[/gold]时，额外获得1点[gold]力量[/gold]。"),
    };
}
