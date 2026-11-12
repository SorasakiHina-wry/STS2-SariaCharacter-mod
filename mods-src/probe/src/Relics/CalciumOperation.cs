using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Probe.Pools;
using Probe.Powers;

namespace Probe.Relics;

/// <summary>
/// 钙质操作 — starter relic: 5 stacks of 钙铠 at the start of every combat.
///
/// Applying the stacks also creates the power instance, so the signature bar shows up from turn one.
/// </summary>
[Pool(typeof(ProbeRelicPool))]
public sealed class CalciumOperation : CustomRelicModel
{
    private const int StartingArmour = 5;

    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override string IconBaseName => "anchor";

    public override async Task BeforeCombatStart()
    {
        await PowerCmd.Apply<CalciumArmorPower>(
            new ThrowingPlayerChoiceContext(), Owner.Creature, StartingArmour, Owner.Creature, null);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "钙质操作"),
        // 描述原样取自 design/relics.md
        ("description", "在每场战斗开始时，获得[blue]5[/blue]层钙铠。"),
    };
}
