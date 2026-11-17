using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Saria.Pools;

namespace Saria.Cards;

/// <summary>Starter skill. Mirrors Defend for the first build.</summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeDefend : CustomCardModel
{
    public ProbeDefend()
        : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
    }

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(5m, ValueProp.Move),
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {

        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.Static(StaticHoverTip.Block),
    };

    public override string? CustomPortraitPath => "res://images/packed/card_portraits/ironclad/defend_ironclad.png";

    public override List<(string, string)>? Localization => new()
    {
        ("title", "防御"),
        ("description", "获得{Block:diff()}点[gold]格挡[/gold]。"),
    };
}
