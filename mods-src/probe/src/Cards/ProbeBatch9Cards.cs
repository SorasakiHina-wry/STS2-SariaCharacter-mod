using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Saria.Pools;
using Saria.Powers;

namespace Saria.Cards;

/// <summary>蒸氨回收 — 花掉一大笔钙铠换一层缓冲。</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeAmmoniaRecovery : CustomCardModel
{
    public ProbeAmmoniaRecovery() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

    private int ArmorCost => IsUpgraded ? 5 : 7;

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var armour = Owner.Creature.GetPower<CalciumArmorPower>();
        if (armour == null || armour.Amount < ArmorCost)
            return;

        await PowerCmd.ModifyAmount(ctx, armour, -ArmorCost, Owner.Creature, this);
        await PowerCmd.Apply<BufferPower>(ctx, Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade() { }

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
        HoverTipFactory.FromPower<BufferPower>(),
    };

    public override string? CustomPortraitPath => ProbeCardKit.DefendPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "蒸氨回收"),
        ("description", "若拥有大于等于{IfUpgraded:show:5|7}层[gold]钙铠[/gold]，则失去{IfUpgraded:show:5|7}层[gold]钙铠[/gold]，阻止下1次你受到的生命值损伤。"),
    };
}

/// <summary>澄清 — 加钙铠、从抽牌堆消耗一张，然后自己回到抽牌堆顶。</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeClarify : CustomCardModel
{
    public ProbeClarify() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<CalciumArmorPower>(4m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PowerCmd.Apply<CalciumArmorPower>(ctx, Owner.Creature, DynamicVars["CalciumArmorPower"].BaseValue, Owner.Creature, this);

        var drawPile = PileType.Draw.GetPile(Owner);
        if (drawPile.Cards.Count > 0)
        {
            var picked = await CardSelectCmd.FromCombatPile(
                ctx, drawPile, Owner, new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1));
            var card = picked.FirstOrDefault();
            if (card != null)
                await CardCmd.Exhaust(ctx, card);
        }
    }

    protected override void OnUpgrade() => DynamicVars["CalciumArmorPower"].UpgradeValueBy(2m);

    /// <summary>打出后回到抽牌堆顶部（和「缠斗」同样的处理）。</summary>
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
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
    };

    public override string? CustomPortraitPath => ProbeCardKit.DefendPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "澄清"),
        ("description", "获得{CalciumArmorPower:diff()}层[gold]钙铠[/gold]。\n从你的[gold]抽牌堆[/gold]中选择一张牌将其[gold]消耗[/gold]。\n将这张牌放置于你的[gold]抽牌堆[/gold]顶部。"),
    };
}

/// <summary>抗酸化 — 格挡 + 一层人工制品。</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeAcidResist : CustomCardModel
{
    public ProbeAcidResist() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(4m, ValueProp.Move),
        new PowerVar<ArtifactPower>(1m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        await PowerCmd.Apply<ArtifactPower>(ctx, Owner.Creature, DynamicVars["ArtifactPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<ArtifactPower>(),
        HoverTipFactory.Static(StaticHoverTip.Block),
    };

    public override string? CustomPortraitPath => ProbeCardKit.DefendPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "抗酸化"),
        ("description", "获得{Block:diff()}点[gold]格挡[/gold]。\n获得{ArtifactPower:diff()}层[gold]人工制品[/gold]。"),
    };
}

/// <summary>全身武装 — 常驻：每回合开始往手里塞破阵。</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeFullArmament : CustomCardModel
{
    public ProbeFullArmament() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<ProbeFullArmamentPower>(1m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<ProbeFullArmamentPower>(ctx, Owner.Creature, DynamicVars["ProbeFullArmamentPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromCard<ProbeBreach>(),
    };

    public override string? CustomPortraitPath => ProbeCardKit.InflamePortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "全身武装"),
        ("description", "在你的回合开始时，在你的[gold]手牌[/gold]中加入1张[gold]破阵[/gold]。"),
    };
}

/// <summary>紧急指令 — 钙铠 + 能量 + 抽牌，消耗。</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeEmergencyOrder : CustomCardModel
{
    public ProbeEmergencyOrder() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<CalciumArmorPower>(4m),
        new EnergyVar(1),
        new CardsVar(3),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PowerCmd.Apply<CalciumArmorPower>(ctx, Owner.Creature, DynamicVars["CalciumArmorPower"].BaseValue, Owner.Creature, this);
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        await CardPileCmd.Draw(ctx, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Energy.UpgradeValueBy(1m);
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
    };

    public override string? CustomPortraitPath => ProbeCardKit.InflamePortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "紧急指令"),
        ("description", "获得{CalciumArmorPower:diff()}层[gold]钙铠[/gold]。获得{Energy:energyIcons()}。抽{Cards:diff()}张牌。"),
    };
}

/// <summary>破阵 — 全身武装塞进手里的无色 Token 攻击牌（和 升温/捕集 一样进 Token 池，不会出现在百科）。</summary>
[Pool(typeof(TokenCardPool))]
public sealed class ProbeBreach : CustomCardModel
{
    public ProbeBreach() : base(0, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Retain, CardKeyword.Exhaust };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(2m, ValueProp.Move),
        new RepeatVar(2),
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

    protected override void OnUpgrade() => DynamicVars.Repeat.UpgradeValueBy(1m);

    public override string? CustomPortraitPath => ProbeCardKit.StrikePortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "破阵"),
        ("description", "造成{Damage:diff()}点伤害{Repeat:diff()}次。"),
    };
}

/// <summary>熟能生巧 — 常驻：每累计获得 7 层钙铠抽牌。</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbePractice : CustomCardModel
{
    public ProbePractice() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<ProbePracticePower>(1m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<ProbePracticePower>(ctx, Owner.Creature, DynamicVars["ProbePracticePower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
    };

    public override string? CustomPortraitPath => ProbeCardKit.InflamePortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "熟能生巧"),
        ("description", "你每获得7层[gold]钙铠[/gold]，就抽1张牌。"),
    };
}

/// <summary>飞龙形态 — 常驻：每回合开始获得钙铠（在钙铠结算格挡之后加，当回合只涨层数）。</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeDragonForm : CustomCardModel
{
    public ProbeDragonForm() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<ProbeDragonFormPower>(4m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<ProbeDragonFormPower>(ctx, Owner.Creature, DynamicVars["ProbeDragonFormPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["ProbeDragonFormPower"].UpgradeValueBy(1m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
    };

    public override string? CustomPortraitPath => ProbeCardKit.InflamePortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "飞龙形态"),
        ("description", "在你的回合开始时，获得{ProbeDragonFormPower:diff()}层[gold]钙铠[/gold]。"),
    };
}