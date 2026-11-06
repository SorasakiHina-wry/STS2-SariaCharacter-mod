using System.Collections.Generic;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;
using Probe.Pools;

namespace Probe.Relics;

/// <summary>
/// 脆化技艺 — starter relic.
///
///   Every attack you deal gains +1 damage per stack of Vulnerable on the target, applied after the
///   Vulnerable multiplier: 6 damage vs 1 Vulnerable becomes 6 * 1.5 + 1 = 10.
///
/// The game has no post-multiplicative hook (ModifyDamageHookType is only Additive /
/// Multiplicative / Cap), so the bonus is applied by <see cref="Probe.Patches.BrittleCraftDamagePatch"/>,
/// a postfix on Hook.ModifyDamage. This model is therefore data + localization only.
/// </summary>
[Pool(typeof(ProbeRelicPool))]
public sealed class BrittleCraft : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override string IconBaseName => "burning_blood";

    public override List<(string, string)>? Localization => new()
    {
        ("title", "脆化技艺"),
        // 描述原样取自 design/relics.md，不做任何改写
        ("description", "目标每有 1 层易伤，你的攻击就额外造成 1 点伤害"),
    };
}
