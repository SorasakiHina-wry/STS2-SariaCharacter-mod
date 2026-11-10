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

// 2026-11-08 batch. Card text mirrors design/cards.md verbatim; only numbers become dynamic vars.

/// <summary>软化 — pay 2 钙铠 for Weak.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeSoften : CustomCardModel
{
    public ProbeSoften() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<WeakPower>(3m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {

        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        ArgumentNullException.ThrowIfNull(play.Target, "cardPlay.Target");

        var armour = Owner.Creature.GetPower<CalciumArmorPower>();
        if (armour == null || armour.Amount < 2)
            return; // 条件不足：可以打出，但没有效果

        await PowerCmd.ModifyAmount(ctx, armour, -2m, Owner.Creature, this);
        await PowerCmd.Apply<WeakPower>(ctx, play.Target, DynamicVars.Weak.BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars.Weak.UpgradeValueBy(1m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<WeakPower>(),
        HoverTipFactory.FromPower<CalciumArmorPower>(),
    };

    public override string? CustomPortraitPath => ProbeCardKit.BashPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "软化"),
        ("description", "若拥有大于等于2层[gold]钙铠[/gold]，则失去2层[gold]钙铠[/gold]，并给予{WeakPower:diff()}层[gold]虚弱[/gold]。"),
    };
}

/// <summary>杀伤弹药 — three hits, exhausts.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeLiveAmmo : CustomCardModel
{
    public ProbeLiveAmmo() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(6m, ValueProp.Move),
        new RepeatVar(3),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {

        ArgumentNullException.ThrowIfNull(play.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, play)
            .Targeting(play.Target)
            .WithHitCount((int)DynamicVars.Repeat.BaseValue)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(ctx);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    public override string? CustomPortraitPath => ProbeCardKit.StrikePortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "杀伤弹药"),
        ("description", "造成{Damage:diff()}点伤害{Repeat:diff()}次。"),
    };
}

/// <summary>掩体生成 — armour, block, and Blur (block survives next turn).</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeCoverGen : CustomCardModel
{
    public ProbeCoverGen() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<CalciumArmorPower>(3m),
        new BlockVar(4m, ValueProp.Move),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {

        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<CalciumArmorPower>(ctx, Owner.Creature, DynamicVars["CalciumArmorPower"].BaseValue, Owner.Creature, this);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        await PowerCmd.Apply<BlurPower>(ctx, Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["CalciumArmorPower"].UpgradeValueBy(1m);
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
        ("title", "掩体生成"),
        ("description", "获得{CalciumArmorPower:diff()}层[gold]钙铠[/gold]。\n获得{Block:diff()}点[gold]格挡[/gold]。\n你的下一回合开始时[gold]格挡[/gold]不会消失。"),
    };
}

/// <summary>凝结 — cash all armour into energy and cards, exhausts.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeCondense : CustomCardModel
{
    public ProbeCondense() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new EnergyVar(1),
        new CardsVar(2),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {

        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        var armour = Owner.Creature.GetPower<CalciumArmorPower>();
        if (armour != null && armour.Amount > 0)
            await PowerCmd.ModifyAmount(ctx, armour, -armour.Amount, Owner.Creature, this);

        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        await CardPileCmd.Draw(ctx, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
    };

    public override string? CustomPortraitPath => ProbeCardKit.DefendPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "凝结"),
        ("description", "失去所有[gold]钙铠[/gold]。\n获得{Energy:energyIcons()}。\n抽{Cards:diff()}张牌。"),
    };
}

/// <summary>钙热还原 — exhausts the hand, armour per card, exhausts itself.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeThermalRecovery : CustomCardModel
{
    public ProbeThermalRecovery() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<CalciumArmorPower>(3m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {

        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        var hand = PileType.Hand.GetPile(Owner).Cards.ToList();
        foreach (var card in hand)
        {
            await CardCmd.Exhaust(ctx, card);
            await PowerCmd.Apply<CalciumArmorPower>(ctx, Owner.Creature, DynamicVars["CalciumArmorPower"].BaseValue, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade() => DynamicVars["CalciumArmorPower"].UpgradeValueBy(1m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
    };

    public override string? CustomPortraitPath => ProbeCardKit.BashPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "钙热还原"),
        ("description", "消耗所有[gold]手牌[/gold]，每张获得{CalciumArmorPower:diff()}层[gold]钙铠[/gold]。"),
    };
}

/// <summary>神经干扰 — armour for three rounds of Weak on everything.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeNerveJamming : CustomCardModel
{
    private const int WEAK_ROUNDS = 3;
    private const int ARMOR_COST = 4;

    public ProbeNerveJamming() : base(2, CardType.Skill, CardRarity.Rare, TargetType.AllEnemies) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<WeakPower>(1m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {

        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        var armour = Owner.Creature.GetPower<CalciumArmorPower>();
        if (armour == null || armour.Amount < ARMOR_COST)
            return; // 钙铠不足：可以打出，但没有效果

        await PowerCmd.ModifyAmount(ctx, armour, -ARMOR_COST, Owner.Creature, this);

        for (int round = 0; round < WEAK_ROUNDS; round++)
        {
            foreach (var enemy in CombatState.HittableEnemies)
                await PowerCmd.Apply<WeakPower>(ctx, enemy, DynamicVars.Weak.BaseValue, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Weak.UpgradeValueBy(1m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
        HoverTipFactory.FromPower<WeakPower>(),
    };

    public override string? CustomPortraitPath => ProbeCardKit.BashPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "神经干扰"),
        ("description", "若拥有大于等于4层[gold]钙铠[/gold]，则失去4层[gold]钙铠[/gold]，并对所有敌人给予{WeakPower:diff()}层[gold]虚弱[/gold]3次。"),
    };
}
/// <summary>煅烧 — power: damage each turn at the cost of armour.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeBurn : CustomCardModel
{
    public ProbeBurn() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<ProbeBurnPower>(5m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {

        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        var power = await PowerCmd.Apply<ProbeBurnPower>(
            ctx, Owner.Creature, DynamicVars["ProbeBurnPower"].BaseValue, Owner.Creature, this);
        power?.IncrementArmorLose();
    }

    protected override void OnUpgrade() => DynamicVars["ProbeBurnPower"].UpgradeValueBy(3m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
    };

    public override string? CustomPortraitPath => ProbeCardKit.InflamePortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "煅烧"),
        ("description", "在你的回合开始时，失去1层[gold]钙铠[/gold]并对所有敌人造成{ProbeBurnPower:diff()}点伤害。"),
    };
}

/// <summary>急救 — power: one emergency heal charge.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeFirstAid : CustomCardModel
{
    public ProbeFirstAid() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {

        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<ProbeFirstAidPower>(ctx, Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
    };

    public override string? CustomPortraitPath => ProbeCardKit.DefendPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "急救"),
        ("description", "回合开始时，若生命小于等于25，则仅一次失去至多15层[gold]钙铠[/gold]，并回复相同数值的生命。"),
    };
}

/// <summary>钙质化 — power: bonus damage per Vulnerable stack.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeCalcify : CustomCardModel
{
    public ProbeCalcify() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<ProbeVulnerableBonusPower>(1m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {

        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<ProbeVulnerableBonusPower>(
            ctx, Owner.Creature, DynamicVars["ProbeVulnerableBonusPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<VulnerablePower>(),
    };

    public override string? CustomPortraitPath => ProbeCardKit.InflamePortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "钙质化"),
        ("description", "目标每有1层[gold]易伤[/gold]，你的攻击就额外造成1点伤害。"),
    };
}

/// <summary>牵引 — power: block whenever armour is spent.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeTraction : CustomCardModel
{
    public ProbeTraction() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<ProbeTractionPower>(3m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {

        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<ProbeTractionPower>(
            ctx, Owner.Creature, DynamicVars["ProbeTractionPower"].BaseValue, Owner.Creature, this);
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
        ("title", "牵引"),
        ("description", "每当你失去[gold]钙铠[/gold]时，获得3点[gold]格挡[/gold]。"),
    };
}

/// <summary>充能护服 — power: N turns of Strength + Dexterity.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeChargeSuit : CustomCardModel
{
    public ProbeChargeSuit() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<ProbeChargeSuitPower>(5m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {

        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<ProbeChargeSuitPower>(
            ctx, Owner.Creature, DynamicVars["ProbeChargeSuitPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>(),
    };

    public override string? CustomPortraitPath => ProbeCardKit.InflamePortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "充能护服"),
        ("description", "在你的下5个回合开始时，获得1点[gold]力量[/gold]与1点[gold]敏捷[/gold]。"),
    };
}
