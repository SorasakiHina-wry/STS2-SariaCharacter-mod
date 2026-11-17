using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
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

/// <summary>苛化 — 全体多段伤害，每一段未被格挡的伤害抽一张牌。</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeCausticize : CustomCardModel
{
    public ProbeCausticize() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(3m, ValueProp.Move),
        new RepeatVar(2),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var attack = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, play)
            .TargetingAllOpponents(CombatState)
            .WithHitCount((int)DynamicVars.Repeat.BaseValue)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(ctx);

        int unblocked = attack.Results.SelectMany(r => r).Count(r => r.UnblockedDamage > 0);
        for (int i = 0; i < unblocked; i++)
            await CardPileCmd.Draw(ctx, 1m, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Repeat.UpgradeValueBy(1m);

    public override string? CustomPortraitPath => ProbeCardKit.BashPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "苛化"),
        ("description", "对所有敌人造成{Damage:diff()}点伤害{Repeat:diff()}次。\n每造成一次未被格挡的伤害，就抽1张牌。"),
    };
}

/// <summary>脉化结构 — 花钙铠抽牌，并回到手牌。</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeVascularize : CustomCardModel
{
    public ProbeVascularize() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    private int ArmorCost => IsUpgraded ? 1 : 3;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new CardsVar(5),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var armour = Owner.Creature.GetPower<CalciumArmorPower>();
        if (armour == null || armour.Amount < ArmorCost)
            return;

        await PowerCmd.ModifyAmount(ctx, armour, -ArmorCost, Owner.Creature, this);
        await CardPileCmd.Draw(ctx, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade() { }

    /// <summary>打出后回到手牌。</summary>
    protected override CardLocation GetResultLocationForCardPlay()
    {
        var location = base.GetResultLocationForCardPlay();
        if (location.pileType == PileType.Discard)
        {
            location.pileType = PileType.Hand;
            location.position = CardPilePosition.Top;
        }
        return location;
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
    };

    public override string? CustomPortraitPath => ProbeCardKit.DefendPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "脉化结构"),
        ("description", "若拥有大于等于{IfUpgraded:show:1|3}层[gold]钙铠[/gold]，则失去{IfUpgraded:show:1|3}层[gold]钙铠[/gold]，抽{Cards:diff()}张牌，并将此牌返回你的[gold]手牌[/gold]。"),
    };
}

/// <summary>低姿搏斗 — 保留越久越便宜、段数越少。</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeLowStance : CustomCardModel
{
    public ProbeLowStance() : base(3, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Retain };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(6m, ValueProp.Move),
        new RepeatVar(6),
        new PowerVar<VulnerablePower>(1m),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target, "cardPlay.Target");

        await PowerCmd.Apply<VulnerablePower>(ctx, play.Target, DynamicVars.Vulnerable.BaseValue, Owner.Creature, this);

        int hits = Math.Max(0, (int)DynamicVars.Repeat.BaseValue);
        if (hits > 0)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, play)
                .Targeting(play.Target)
                .WithHitCount(hits)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(ctx);
        }
    }

    /// <summary>每次被保留：耗能 -1（不低于 0），伤害段数 -2。</summary>
    public override Task AfterFlush(
        PlayerChoiceContext choiceContext, Player player,
        IReadOnlyCollection<CardModel> flushedCards, IReadOnlyCollection<CardModel> retainedCards)
    {
        if (player == Owner && retainedCards.Contains(this))
        {
            if (EnergyCost.GetResolved() > 0)
                EnergyCost.AddUntilPlayed(-1, reduceOnly: true);

            int hits = Math.Max(0, (int)DynamicVars.Repeat.BaseValue - 2);
            DynamicVars.Repeat.BaseValue = hits;
            DynamicVars.Repeat.PreviewValue = hits;
        }

        return Task.CompletedTask;
    }

    protected override void OnUpgrade() => DynamicVars.Repeat.UpgradeValueBy(2m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<VulnerablePower>(),
    };

    public override string? CustomPortraitPath => ProbeCardKit.BashPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "低姿搏斗"),
        ("description", "给予{VulnerablePower:diff()}层[gold]易伤[/gold]。\n造成{Damage:diff()}点伤害{Repeat:diff()}次。\n每当这张牌被[gold]保留[/gold]时，其在被打出之前的耗能减少1且造成的伤害减少2次。"),
    };
}

/// <summary>预演 — 自选消耗一张手牌，抽牌、保留手牌、结束回合。</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeRehearsal : CustomCardModel
{
    public ProbeRehearsal() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new CardsVar(2),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var picked = await CardSelectCmd.FromHand(
            prefs: new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1),
            context: ctx, player: Owner, filter: null, source: this);
        var card = picked.FirstOrDefault();
        if (card != null)
            await CardCmd.Exhaust(ctx, card);

        await CardPileCmd.Draw(ctx, DynamicVars.Cards.BaseValue, Owner);
        await PowerCmd.Apply<RetainHandPower>(ctx, Owner.Creature, 1m, Owner.Creature, this);
        PlayerCmd.EndTurn(Owner, canBackOut: false);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromKeyword(CardKeyword.Retain),
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
    };

    public override string? CustomPortraitPath => ProbeCardKit.DefendPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "预演"),
        ("description", "[gold]消耗[/gold]一张牌。\n抽{Cards:diff()}张牌。\n在本回合[gold]保留[/gold]你的[gold]手牌[/gold]。\n结束你的回合。"),
    };
}

/// <summary>复分解 — 最多 5 张手牌随机变化，然后回到抽牌堆顶。</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeDecomposition : CustomCardModel
{
    public ProbeDecomposition() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new CardsVar(2),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var picked = await CardSelectCmd.FromHand(
            prefs: new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 0, DynamicVars.Cards.IntValue),
            context: ctx, player: Owner, filter: null, source: this);

        foreach (var card in picked.ToList())
            await CardCmd.TransformToRandom(card, Owner.RunState.Rng.CombatCardSelection);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    /// <summary>打出后放到抽牌堆顶部。</summary>
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

    public override string? CustomPortraitPath => ProbeCardKit.DefendPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "复分解"),
        ("description", "从[gold]手牌[/gold]中选择最多{Cards:diff()}张牌变化。\n将这张牌放置于你的[gold]抽牌堆[/gold]顶部。"),
    };
}

/// <summary>连续压制 — 每失去 1 层钙铠打一段、抽一张。</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeSuppression : CustomCardModel
{
    public ProbeSuppression() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(3m, ValueProp.Move),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target, "cardPlay.Target");

        var armour = Owner.Creature.GetPower<CalciumArmorPower>();
        int stacks = armour == null ? 0 : Math.Min(3, armour.Amount);
        if (stacks <= 0)
            return;

        await PowerCmd.ModifyAmount(ctx, armour!, -stacks, Owner.Creature, this);

        for (int i = 0; i < stacks; i++)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, play)
                .Targeting(play.Target)
                .WithHitFx("vfx/vfx_attack_blunt")
                .Execute(ctx);
            await CardPileCmd.Draw(ctx, 1m, Owner);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<CalciumArmorPower>(),
    };

    public override string? CustomPortraitPath => ProbeCardKit.BashPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "连续压制"),
        ("description", "失去至多3层[gold]钙铠[/gold]。\n每失去1层[gold]钙铠[/gold]造成{Damage:diff()}点伤害，抽1张牌。"),
    };
}

/// <summary>护身打击 — 全体伤害、按总伤害获得格挡，并反弹本回合被格挡的伤害。</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeGuardStrike : CustomCardModel
{
    public ProbeGuardStrike() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies) { }

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(7m, ValueProp.Move),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var attack = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, play)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(ctx);

        decimal total = attack.Results.SelectMany(r => r).Sum(r => r.TotalDamage + r.OverkillDamage);
        if (total > 0)
            await CreatureCmd.GainBlock(Owner.Creature, total, ValueProp.Move, play);

        await PowerCmd.Apply<ReflectPower>(ctx, Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<ReflectPower>(),
        HoverTipFactory.Static(StaticHoverTip.Block),
    };

    public override string? CustomPortraitPath => ProbeCardKit.BludgeonPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "护身打击"),
        ("description", "对所有敌人造成{Damage:diff()}点伤害。\n获得等量于所造成伤害的[gold]格挡[/gold]。\n在本回合将你[gold]格挡[/gold]掉的攻击伤害反弹给攻击者。"),
    };
}

/// <summary>锋刃 — 每次打出自身伤害降低，被消耗时补一张。</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeEdge : CustomCardModel
{
    public ProbeEdge() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Retain };

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
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(ctx);

        // 本场战斗内自身伤害 -2（不低于 0）
        decimal next = Math.Max(0m, DynamicVars.Damage.BaseValue - 2m);
        DynamicVars.Damage.BaseValue = next;
        DynamicVars.Damage.PreviewValue = next;
    }

    /// <summary>被消耗时把一张自己（升级则升级版）加入手牌。</summary>
    public override async Task AfterCardExhausted(PlayerChoiceContext ctx, CardModel card, bool causedByEthereal)
    {
        if (card != this || CombatState == null)
            return;

        var copy = CombatState.CreateCard<ProbeEdge>(Owner);
        if (IsUpgraded)
            CardCmd.Upgrade(copy);
        await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);

    public override string? CustomPortraitPath => ProbeCardKit.StrikePortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "锋刃"),
        ("description", "造成{Damage:diff()}点伤害{Repeat:diff()}次。\n将这张牌在本场战斗中的伤害降低2。\n这张牌被[gold]消耗[/gold]时，将一张{IfUpgraded:show:锋刃+|锋刃}加入你的[gold]手牌[/gold]。"),
    };
}

/// <summary>歼灭战术 — 手牌越多力量越高，然后全体多段伤害。</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeAnnihilation : CustomCardModel
{
    public ProbeAnnihilation() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(6m, ValueProp.Move),
        new RepeatVar(2),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        int hand = PileType.Hand.GetPile(Owner).Cards.Count;
        if (hand > 0)
            await PowerCmd.Apply<StrengthPower>(ctx, Owner.Creature, hand, Owner.Creature, this);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, play)
            .TargetingAllOpponents(CombatState)
            .WithHitCount((int)DynamicVars.Repeat.BaseValue)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(ctx);
    }

    protected override void OnUpgrade() => DynamicVars.Repeat.UpgradeValueBy(1m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<StrengthPower>(),
    };

    public override string? CustomPortraitPath => ProbeCardKit.BludgeonPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "歼灭战术"),
        ("description", "每有1张[gold]手牌[/gold]，就获得1点[gold]力量[/gold]一次。\n造成{Damage:diff()}点伤害{Repeat:diff()}次。"),
    };
}

/// <summary>超重拳 — 高伤 + 击晕。</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeHeavyPunch : CustomCardModel
{
    public ProbeHeavyPunch() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Retain, CardKeyword.Exhaust };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(28m, ValueProp.Move),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target, "cardPlay.Target");

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, play)
            .Targeting(play.Target)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(ctx);

        await CreatureCmd.Stun(play.Target);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(7m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        StunIntentHelpTip,
    };

    private static IHoverTip StunIntentHelpTip => MegaCrit.Sts2.Core.MonsterMoves.Intents.StunIntent.GetStaticHoverTip();

    public override string? CustomPortraitPath => ProbeCardKit.BludgeonPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "超重拳"),
        ("description", "造成{Damage:diff()}点伤害。\n[gold]击晕[/gold]该敌人。"),
    };
}

/// <summary>刺穿 — 全体多段小额伤害。</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeImpale : CustomCardModel
{
    public ProbeImpale() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(2m, ValueProp.Move),
        new RepeatVar(6),
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

    protected override void OnUpgrade() => DynamicVars.Repeat.UpgradeValueBy(2m);

    public override string? CustomPortraitPath => ProbeCardKit.StrikePortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "刺穿"),
        ("description", "造成{Damage:diff()}点伤害{Repeat:diff()}次。"),
    };
}
