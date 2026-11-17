using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Saria;

/// <summary>
/// 临时诊断：记录自定义角色收到的动画触发名，以及触发后当前剪辑的长度/剩余时间。
/// 剪辑长度能反推播放的是哪一个 clips（Idle 2.0s / Attack 1.17s / Skill_Begin 0.70s ...）。
/// </summary>
[HarmonyPatch(typeof(NCreature), nameof(NCreature.SetAnimationTrigger))]
public static class SariaAnimProbePatch
{
    [HarmonyPrefix]
    private static void Prefix(NCreature __instance, string trigger)
    {
        LogOne($"{trigger} before", __instance);
    }

    [HarmonyPostfix]
    private static void Postfix(NCreature __instance, string trigger)
    {
        LogOne($"{trigger} after", __instance);
    }

    private static void LogOne(string label, NCreature instance)
    {
        try
        {
            var creature = instance.Entity;
            var character = creature?.Player?.Character;
            if (character == null || !character.Id.Entry.EndsWith("SARIA", StringComparison.Ordinal))
                return;

            bool spine = instance.HasSpineAnimation;
            float len = spine ? instance.GetCurrentAnimationLength() : -1f;
            float remain = spine ? instance.GetCurrentAnimationTimeRemaining() : -1f;
            Log.Info($"[saria][anim] {label} spine={spine} length={len:F2} remain={remain:F2}");
        }
        catch (Exception ex)
        {
            Log.Warn($"[saria][anim] probe failed: {ex.Message}");
        }
    }
}
