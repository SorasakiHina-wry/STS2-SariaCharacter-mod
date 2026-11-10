using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace Probe;

/// <summary>
/// 角色选择界面的背景：原版是按 `Id.Entry` 拼路径（`scenes/char_select/char_select_bg_&lt;id&gt;`），
/// 我们的 id 是 `PROBE-SARIA`，跟任何原版资源都对不上，用占位基类时会借铁甲战士的背景。
/// 这里直接覆盖 getter，指向我们 PCK 里的场景。
/// </summary>
[HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.CharacterSelectBg), MethodType.Getter)]
public static class ProbeCharacterBgPatch
{
    private const string BgScene = "res://probe/scenes/char_select_bg_saria.tscn";

    [HarmonyPrefix]
    private static bool Prefix(CharacterModel __instance, ref string __result)
    {
        if (!__instance.Id.Entry.EndsWith("SARIA", StringComparison.Ordinal))
            return true;

        if (!ResourceLoader.Exists(BgScene))
        {
            Log.Warn($"[probe] character select bg missing: {BgScene}");
            return true;
        }

        __result = BgScene;
        return false;
    }
}
