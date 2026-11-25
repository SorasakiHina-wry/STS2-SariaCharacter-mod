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
using Saria.Pools;

namespace Saria.Cards;

/// <summary>
/// Starter attack. Deliberately a near-copy of Strike so the first build only proves that
/// "new card class -> new pool -> playable" works end to end.
/// </summary>
[Pool(typeof(ProbeCardPool))]
public sealed class ProbeStrike : SariaCardModel
{
    public ProbeStrike()
        : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
    }

    /// <summary>带「打击」标签，原版的「打击」相关效果（如树叶药膏/升级打击类遗物）才能识别这张牌。</summary>
    protected override HashSet<CardTag> CanonicalTags => new HashSet<CardTag> { CardTag.Strike };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(6m, ValueProp.Move),
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "打击"),
        ("description", "造成{Damage:diff()}点伤害。"),
    };
}
