using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace Saria;

/// <summary>
/// 角色头顶的「遗物触发」闪光（`NRelicFlashVfx`）。
///
/// `StartVfx` 里把三个 sprite 的贴图都设成 `_relic.Icon`（图集里的导入贴图），
/// 所以在它之后换成我们自制的小图。`StartVfx` 虽然是 async，但贴图赋值在第一个 await 之前，
/// 后置补丁（在状态机第一段 MoveNext 之后返回 Task 时执行）能拿到已经赋过值的 sprite。
/// </summary>
[HarmonyPatch(typeof(NRelicFlashVfx), "StartVfx")]
public static class ProbeRelicFlashVfxPatch
{
    private static readonly System.Reflection.FieldInfo? RelicField =
        AccessTools.Field(typeof(NRelicFlashVfx), "_relic");

    [HarmonyPostfix]
    private static void Postfix(NRelicFlashVfx __instance)
    {
        try
        {
            if (RelicField?.GetValue(__instance) is not RelicModel model)
                return;

            var texture = ProbeRelicArt.Small(model);
            if (texture == null)
                return;

            foreach (var name in new[] { "_sprite", "_sprite2", "_sprite3" })
            {
                if (AccessTools.Field(typeof(NRelicFlashVfx), name)?.GetValue(__instance) is TextureRect rect)
                    rect.Texture = texture;
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"[saria][relic] flash vfx hook failed: {ex.Message}");
        }
    }
}
