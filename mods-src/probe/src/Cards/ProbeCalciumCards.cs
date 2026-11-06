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
using Probe.Pools;
using Probe.Powers;

namespace Probe.Cards;

/// <summary>硬化 — basic skill: gain 钙铠 stacks.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeHardening : CustomCardModel
{
    public ProbeHardening()
        : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<CalciumArmorPower>(6m),
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<CalciumArmorPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["CalciumArmorPower"].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() => DynamicVars["CalciumArmorPower"].UpgradeValueBy(2m);

    public override string? CustomPortraitPath => "res://images/packed/card_portraits/ironclad/defend_ironclad.png";

    public override List<(string, string)>? Localization => new()
    {
        ("title", "硬化"),
        ("description", "获得 {CalciumArmorPower:diff()} 层钙铠"),
    };
}

/// <summary>
/// 激发 — basic skill: if you have at least 1 钙铠, spend 1 stack to apply Vulnerable.
/// Upgrading only lowers the cost (1 -> 0).
/// </summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeIgnite : CustomCardModel
{
    public ProbeIgnite()
        : base(1, CardType.Skill, CardRarity.Basic, TargetType.AnyEnemy)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<VulnerablePower>(2m),
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var armor = Owner.Creature.GetPower<CalciumArmorPower>();
        if (armor == null || armor.Amount < 1)
            return; // 条件不满足：整张牌不产生效果

        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        await PowerCmd.Decrement(armor);
        await PowerCmd.Apply<VulnerablePower>(
            choiceContext,
            cardPlay.Target,
            DynamicVars.Vulnerable.BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() => DynamicVars.Vulnerable.UpgradeValueBy(1m);

    public override string? CustomPortraitPath => "res://images/packed/card_portraits/ironclad/bash.png";

    public override List<(string, string)>? Localization => new()
    {
        ("title", "激发"),
        ("description", "若拥有大于等于 1 层钙铠，则失去 1 层钙铠，并施加 {VulnerablePower:diff()} 层易伤"),
    };
}
