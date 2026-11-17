using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Saria;

/// <summary>临时诊断：记录自定义角色收到的动画触发名（验证 Cast/PowerUp 是否到达）。</summary>
[HarmonyPatch(typeof(NCreature), nameof(NCreature.SetAnimationTrigger))]
public static class SariaAnimProbePatch
{
    [HarmonyPrefix]
    private static void Prefix(NCreature __instance, string trigger)
    {
        try
        {
            var creature = __instance.Entity;
            if (creature?.Player?.Character == null)
                return;
            if (!creature.Player.Character.Id.Entry.EndsWith("SARIA", StringComparison.Ordinal))
                return;
            Log.Info($"[saria][anim] trigger={trigger}");
        }
        catch (Exception ex)
        {
            Log.Warn($"[saria][anim] probe failed: {ex.Message}");
        }
    }
}
