using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Saria.Pools;
using Saria.Powers;

namespace Saria.Relics;

/// <summary>
/// 钙质技艺 — 初始遗物：每场战斗开始获得 5 层钙铠。
///
/// Applying the stacks also creates the power instance, so the signature bar shows up from turn one.
/// </summary>
[Pool(typeof(ProbeRelicPool))]
public sealed class CalciumCraft : CustomRelicModel
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
        ("title", "钙质技艺"),
        ("description", "在每场战斗开始时，获得[blue]5[/blue]层[gold]钙铠[/gold]。"),
    };
}
