using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace Saria;

/// <summary>
/// 状态（power）的三种飘字/闪光特效里用的都是 `_power.BigIcon`：
///   * `NPowerAppliedVfx`  —— 状态**获得**时（`_icon` / `_iconEcho`）
///   * `NPowerRemovedVfx`  —— 状态**移除/结束**时（`_sprite`）
///   * `NPowerFlashVfx`    —— 状态**触发**时（`_sprite`）
///
/// 这三个都是 async 的 `StartVfx`，贴图赋值在第一个 await 之前，
/// 所以后置补丁（状态机首段执行完、返回 Task 时）能覆盖到。
/// 只处理 `SARIA-` 开头的状态，原版状态保持原样。
/// </summary>
internal static class ProbePowerVfx
{
    internal static void Apply(object vfx, string[] spriteFields)
    {
        try
        {
            var powerField = AccessTools.Field(vfx.GetType(), "_power");
            if (powerField?.GetValue(vfx) is not PowerModel power)
                return;

            var texture = ProbePowerArt.ForModel(power);
            if (texture == null)
                return;

            foreach (var name in spriteFields)
            {
                // 注意：NPowerFlashVfx 的 _sprite 是 Sprite2D，另外两个是 TextureRect，
                // 只处理 TextureRect 会漏掉「层数变化时的闪光」（v0.7.3 的 bug）。
                switch (AccessTools.Field(vfx.GetType(), name)?.GetValue(vfx))
                {
                    case TextureRect rect:
                        rect.Texture = texture;
                        break;
                    case Sprite2D sprite:
                        sprite.Texture = texture;
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"[saria][power-vfx] failed for {vfx.GetType().Name}: {ex.Message}");
        }
    }
}

[HarmonyPatch(typeof(NPowerAppliedVfx), "StartVfx")]
public static class ProbePowerAppliedVfxPatch
{
    [HarmonyPostfix]
    private static void Postfix(NPowerAppliedVfx __instance) =>
        ProbePowerVfx.Apply(__instance, new[] { "_icon", "_iconEcho" });
}

[HarmonyPatch(typeof(NPowerRemovedVfx), "StartVfx")]
public static class ProbePowerRemovedVfxPatch
{
    [HarmonyPostfix]
    private static void Postfix(NPowerRemovedVfx __instance) =>
        ProbePowerVfx.Apply(__instance, new[] { "_sprite" });
}

[HarmonyPatch(typeof(NPowerFlashVfx), "StartVfx")]
public static class ProbePowerFlashVfxPatch
{
    [HarmonyPostfix]
    private static void Postfix(NPowerFlashVfx __instance) =>
        ProbePowerVfx.Apply(__instance, new[] { "_sprite" });
}
