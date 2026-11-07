using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Probe.Powers;
using Probe.Relics;

namespace Probe.Patches;

/// <summary>
/// Post-multiplier damage bonuses for the Probe character:
///
///   * 脆化技艺 (starter relic): +2 flat when the target has any Vulnerable
///   * 钙质化  (power):        +Amount damage per Vulnerable stack
///
/// Both are documented as applying AFTER the Vulnerable multiplier (1 stack: 6 × 1.5 + 2 + 1).
/// The damage pipeline only offers Additive / Multiplicative / Cap phases
/// (<see cref="ModifyDamageHookType"/>) and Multiplicative order between models is not controllable,
/// so a hook cannot express "after the ×1.5". Hook.ModifyDamage is the single funnel every damage
/// calculation goes through, is always called with ModifyDamageHookType.All, and returns the
/// finished number — a postfix there is exactly the missing phase.
/// </summary>
[HarmonyPatch(typeof(Hook), "ModifyDamage")]
public static class BrittleCraftDamagePatch
{
    /// <summary>Flat bonus applied when the target has any Vulnerable (see design/relics.md).</summary>
    private const int BonusDamage = 2;

    [HarmonyPostfix]
    private static void Postfix(Creature? target, Creature? dealer, ValueProp props, ref decimal __result)
    {
        if (__result <= 0 || target == null || dealer == null)
            return;

        if (!props.IsPoweredAttack())
            return; // only attacks - statuses, poison and the like are untouched

        var player = dealer.Player;
        if (player == null)
            return;

        int stacks = target.GetPowerAmount<VulnerablePower>();
        if (stacks <= 0)
            return;

        if (OwnsRelic(player))
            __result += BonusDamage;

        var calcify = dealer.GetPower<ProbeVulnerableBonusPower>();
        if (calcify != null && calcify.Amount > 0)
            __result += calcify.Amount * stacks;
    }

    private static bool OwnsRelic(MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        IReadOnlyList<RelicModel> relics = player.Relics;
        for (int i = 0; i < relics.Count; i++)
        {
            if (relics[i] is BrittleCraft)
                return true;
        }
        return false;
    }
}
