using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Saria.Pools;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using Saria.Powers;

namespace Saria.Cards;

// 2026-11-08 batch 3 (9 cards). Token cards (升温 / 捕集) use CardRarity.Token so they never
// show up in rewards or shops, exactly like vanilla 仆从打击 (MinionStrike).

/// <summary>循环 — damage, then seed 升温 on top of the draw pile.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeCycle : SariaCardModel
{
    public ProbeCycle() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    /// <summary>详情里展示将要生成的「升温」（原版 Begone → MinionStrike 同款做法）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.FromCard<ProbeWarmUp>(IsUpgraded),
            HoverTipFactory.FromCard<ProbeCapture>(IsUpgraded),
        };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(9m, ValueProp.Move),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {

        await ProbeCardKit.Hit(ctx, play, this, "vfx/vfx_attack_slash");
        await SeedCard<ProbeWarmUp>(ctx);
    }

    /// <summary>把指定卡生成到抽牌堆顶；升级版生成的也是升级版。</summary>
    internal async Task SeedCard<T>(PlayerChoiceContext ctx) where T : CardModel
    {
        var card = CombatState.CreateCard<T>(Owner);
        if (IsUpgraded)
            CardCmd.Upgrade(card);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Draw, Owner, CardPilePosition.Top);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
    public override List<(string, string)>? Localization => new()
    {
        ("title", "循环"),
        ("description", "造成{Damage:diff()}点伤害。\n将一张[gold]{IfUpgraded:show:升温+|升温}[/gold]放到你的[gold]抽牌堆[/gold]的顶部。"),
    };
}

/// <summary>电石 — spend up to 3 armour, one hit per stack spent.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeElectrum : SariaCardModel
{
    private const int ARMOR_CAP = 4;

    public ProbeElectrum() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(4m, ValueProp.Move),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {

        ArgumentNullException.ThrowIfNull(play.Target, "cardPlay.Target");

        int spent = 0;
        var armour = Owner.Creature.GetPower<CalciumArmorPower>();
        if (armour != null && armour.Amount > 0)
        {
            spent = Math.Min(ARMOR_CAP, armour.Amount);
            await PowerCmd.ModifyAmount(ctx, armour, -spent, Owner.Creature, this);
        }

        if (spent <= 0)
            return;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, play)
            .Targeting(play.Target)
            .WithHitCount(spent)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(ctx);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
    };
    public override List<(string, string)>? Localization => new()
    {
        ("title", "电石"),
        ("description", "失去至多4层[gold]钙铠[/gold]，每层失去的[gold]钙铠[/gold]造成{Damage:diff()}点伤害。"),
    };
}

/// <summary>诱导 — trade Strength for Vigor.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeInduce : SariaCardModel
{
    public ProbeInduce() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<VigorPower>(9m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {

        await PowerCmd.Apply<StrengthPower>(ctx, Owner.Creature, -1m, Owner.Creature, this);
        await PowerCmd.Apply<VigorPower>(ctx, Owner.Creature, DynamicVars["VigorPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["VigorPower"].UpgradeValueBy(2m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<VigorPower>(),
    };
    public override List<(string, string)>? Localization => new()
    {
        ("title", "诱导"),
        ("description", "失去1点[gold]力量[/gold]。\n获得{VigorPower:diff()}点[gold]活力[/gold]。"),
    };
}

/// <summary>再塑型 — draw, then lose Dexterity for this turn (power gives it back at end).</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeReshape : SariaCardModel
{
    public ProbeReshape() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new CardsVar(3),
        new PowerVar<DexterityPower>(1m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {

        await CardPileCmd.Draw(ctx, DynamicVars.Cards.BaseValue, Owner);
        await PowerCmd.Apply<DexterityPower>(ctx, Owner.Creature, -DynamicVars["DexterityPower"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<ProbeReshapePower>(ctx, Owner.Creature, DynamicVars["DexterityPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<DexterityPower>(),
    };
    public override List<(string, string)>? Localization => new()
    {
        ("title", "再塑型"),
        ("description", "抽{Cards:diff()}张牌。\n在本回合失去{DexterityPower:diff()}点[gold]敏捷[/gold]。"),
    };
}

/// <summary>精炼 — cash armour for energy, always returns to hand.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeRefine : SariaCardModel
{
    public ProbeRefine() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    private int ArmorCost => IsUpgraded ? 4 : 6;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new EnergyVar(2),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {

        var armour = Owner.Creature.GetPower<CalciumArmorPower>();
        if (armour == null || armour.Amount < ArmorCost)
            return; // 条件不足：可以打出，但没有效果

        await PowerCmd.ModifyAmount(ctx, armour, -ArmorCost, Owner.Creature, this);
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
    }

    /// <summary>粒子墙式回收：打出后不进弃牌堆，直接回手。</summary>
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
    };
    public override List<(string, string)>? Localization => new()
    {
        ("title", "精炼"),
        ("description", "若拥有大于等于{IfUpgraded:show:4|6}层[gold]钙铠[/gold]，则失去{IfUpgraded:show:4|6}层[gold]钙铠[/gold]，获得{Energy:energyIcons()}。\n将此牌返回你的[gold]手牌[/gold]。"),
    };
}

/// <summary>珐琅质 — turn all armour into triple block; upgraded keeps the card.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeEnamel : SariaCardModel
{
    public ProbeEnamel() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    public override bool GainsBlock => true;

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {

        var armour = Owner.Creature.GetPower<CalciumArmorPower>();
        if (armour == null || armour.Amount <= 0)
            return;

        int amount = armour.Amount;
        await PowerCmd.ModifyAmount(ctx, armour, -amount, Owner.Creature, this);
        await CreatureCmd.GainBlock(Owner.Creature, amount * 2, ValueProp.Unpowered, play);
    }

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
        HoverTipFactory.Static(StaticHoverTip.Block),
    };
    public override List<(string, string)>? Localization => new()
    {
        ("title", "珐琅质"),
        ("description", "失去所有[gold]钙铠[/gold]，获得等量于失去[gold]钙铠[/gold]数值两倍的[gold]格挡[/gold]。"),
    };
}

/// <summary>洞察 — this turn, every armour gain debuffs all enemies.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeInsight : SariaCardModel
{
    public ProbeInsight() : base(1, CardType.Skill, CardRarity.Rare, TargetType.AllEnemies) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<ProbeInsightPower>(1m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {

        await PowerCmd.Apply<ProbeInsightPower>(
            ctx, Owner.Creature, DynamicVars["ProbeInsightPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
    public override List<(string, string)>? Localization => new()
    {
        ("title", "洞察"),
        ("description", "在本回合，你每次获得[gold]钙铠[/gold]时，给予所有敌人2层[gold]易伤[/gold]与1层[gold]虚弱[/gold]。"),
    };
}

/// <summary>升温 — token card: block, then seed 捕集. Registered in the vanilla Token pool so it never shows in the compendium.</summary>
[Pool(typeof(TokenCardPool))]
public sealed class ProbeWarmUp : SariaCardModel
{
    public ProbeWarmUp() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self) { }

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromCard<ProbeCapture>(IsUpgraded) };

    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Ethereal, CardKeyword.Exhaust };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(10m, ValueProp.Move),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {

        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        var card = CombatState.CreateCard<ProbeCapture>(Owner);
        if (IsUpgraded)
            CardCmd.Upgrade(card);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Draw, Owner, CardPilePosition.Top);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
    public override List<(string, string)>? Localization => new()
    {
        ("title", "升温"),
        ("description", "获得{Block:diff()}点[gold]格挡[/gold]。\n将一张[gold]{IfUpgraded:show:捕集+|捕集}[/gold]放到你的[gold]抽牌堆[/gold]的顶部。"),
    };
}

/// <summary>捕集 — token card: armour, then seed 升温. Registered in the vanilla Token pool.</summary>
[Pool(typeof(TokenCardPool))]
public sealed class ProbeCapture : SariaCardModel
{
    public ProbeCapture() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self) { }

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.FromCard<ProbeWarmUp>(IsUpgraded),
            HoverTipFactory.FromCard<ProbeCapture>(IsUpgraded),
        };

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<CalciumArmorPower>(5m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {

        await PowerCmd.Apply<CalciumArmorPower>(ctx, Owner.Creature, DynamicVars["CalciumArmorPower"].BaseValue, Owner.Creature, this);

        var card = CombatState.CreateCard<ProbeWarmUp>(Owner);
        if (IsUpgraded)
            CardCmd.Upgrade(card);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Draw, Owner, CardPilePosition.Top);
    }

    protected override void OnUpgrade() => DynamicVars["CalciumArmorPower"].UpgradeValueBy(1m);
    public override List<(string, string)>? Localization => new()
    {
        ("title", "捕集"),
        ("description", "获得{CalciumArmorPower:diff()}层[gold]钙铠[/gold]。\n将一张[gold]{IfUpgraded:show:升温+|升温}[/gold]放到你的[gold]抽牌堆[/gold]的顶部。"),
    };
}
