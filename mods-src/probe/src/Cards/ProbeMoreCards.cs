using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Probe.Pools;

namespace Probe.Cards;

[Pool(typeof(ProbeCardPool))]
public sealed class ProbeOverload : CustomCardModel
{
    public ProbeOverload()
        : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(14m, ValueProp.Move),
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_heavy_blunt")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4m);

    public override string? CustomPortraitPath => "res://images/packed/card_portraits/ironclad/bludgeon.png";

    public override List<(string, string)>? Localization => new()
    {
        ("title", "Probe Overload"),
        ("description", "造成{Damage:diff()}点伤害"),
    };
}

[Pool(typeof(ProbeCardPool))]
public sealed class ProbeBastion : CustomCardModel
{
    public ProbeBastion()
        : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(18m, ValueProp.Move),
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(6m);

    public override string? CustomPortraitPath => "res://images/packed/card_portraits/ironclad/defend_ironclad.png";

    public override List<(string, string)>? Localization => new()
    {
        ("title", "Probe Bastion"),
        ("description", "获得{Block:diff()}点格挡"),
    };
}
