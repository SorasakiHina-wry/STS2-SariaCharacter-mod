using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;
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
///
/// Because that postfix also lands after the Cap phase, the boosted value is re-run through the Cap
/// pass once more — otherwise the bonus would punch through damage caps such as 难以杀灭
/// (HardToKillPower). The re-entrant call uses ModifyDamageHookType.Cap, which this postfix ignores,
/// so it cannot recurse.
/// </summary>
[HarmonyPatch(typeof(Hook), "ModifyDamage")]
public static class BrittleCraftDamagePatch
{
    /// <summary>Flat bonus applied when the target has any Vulnerable (see design/relics.md).</summary>
    private const int BonusDamage = 2;

    [HarmonyPostfix]
    private static void Postfix(
        IRunState runState,
        ICombatState? combatState,
        Creature? target,
        Creature? dealer,
        ValueProp props,
        CardModel? cardSource,
        CardPlay? cardPlay,
        ModifyDamageHookType modifyDamageHookType,
        CardPreviewMode previewMode,
        ref decimal __result)
    {
        // Only the full pass carries every phase; it also keeps the re-entrant Cap pass below from
        // adding the bonus a second time.
        if (modifyDamageHookType != ModifyDamageHookType.All)
            return;

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

        decimal bonus = 0m;
        if (OwnsRelic(player))
            bonus += BonusDamage;

        var calcify = dealer.GetPower<ProbeVulnerableBonusPower>();
        if (calcify != null && calcify.Amount > 0)
            bonus += calcify.Amount * stacks;

        if (bonus <= 0)
            return;

        // Re-apply the Cap phase so damage caps (难以杀灭 etc.) still hold.
        __result = Hook.ModifyDamage(
            runState, combatState, target, dealer, __result + bonus, props, cardSource, cardPlay,
            ModifyDamageHookType.Cap, previewMode, out _);
    }

    private static bool OwnsRelic(Player player)
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
