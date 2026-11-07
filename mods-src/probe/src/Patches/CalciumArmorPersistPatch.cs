using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using Probe.Powers;

namespace Probe.Patches;

/// <summary>
/// 钙铠 is the character's signature mechanic and stays on screen for the whole combat:
/// it is applied at 0 stacks when combat starts and must never be cleaned up or go negative.
///
/// PowerModel.ShouldRemoveDueToAmount() is not virtual (it only consults AllowNegative), so both
/// rules are enforced with Harmony here instead of a subclass override.
/// </summary>
[HarmonyPatch(typeof(PowerModel), nameof(PowerModel.ShouldRemoveDueToAmount))]
public static class CalciumArmorPersistPatch
{
    [HarmonyPostfix]
    private static bool Postfix(bool __result, PowerModel __instance)
        => __instance is CalciumArmorPower ? false : __result;
}

/// <summary>Clamp the stack count at 0 so the permanent bar never shows a negative number.</summary>
[HarmonyPatch(typeof(PowerModel), nameof(PowerModel.SetAmount))]
public static class CalciumArmorClampPatch
{
    [HarmonyPrefix]
    private static void Prefix(PowerModel __instance, ref int amount)
    {
        if (__instance is CalciumArmorPower && amount < 0)
            amount = 0;
    }
}
